using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Wlniao;
using Wlniao.Text;

namespace Wlniao.Middleware
{
    /// <summary>
    /// 限流算法类型
    /// </summary>
    public enum RateLimitAlgorithm
    {
        /// <summary>
        /// 令牌桶算法（推荐）
        /// </summary>
        TokenBucket = 0,
        
        /// <summary>
        /// 固定窗口算法
        /// </summary>
        FixedWindow = 1,
        
        /// <summary>
        /// 滑动窗口算法
        /// </summary>
        SlidingWindow = 2
    }

    /// <summary>
    /// 限流中间件配置选项
    /// </summary>
    public class RateLimitOptions
    {
        /// <summary>
        /// 限流算法类型（默认：令牌桶）
        /// </summary>
        public RateLimitAlgorithm Algorithm { get; set; } = RateLimitAlgorithm.TokenBucket;

        /// <summary>
        /// 请求时间窗口（默认：180秒）
        /// </summary>
        public int TimeWindow { get; set; } = Convert.ToInt(Config.GetSetting("WLN_RATE_TIME_SECONDS", "180"));

        /// <summary>
        /// 最大请求次数（未设置时不启用）
        /// </summary>
        public int MaxRequests { get; set; } = Convert.ToInt(Config.GetSetting("WLN_RATE_MAX_REQUESTS", "0"));

        /// <summary>
        /// 令牌桶容量（默认与 MaxRequests 相同）
        /// </summary>
        public int BucketCapacity { get; set; } = 0;

        /// <summary>
        /// 是否隔离请求目录（不同目录分开限流）
        /// </summary>
        public bool IsolationPath { get; set; } = false;

        /// <summary>
        /// 限流的白名单目录列表，如：/api*
        /// </summary>
        public string[] WhitePath { get; set; } = Config.GetConfigs("WLN_RATE_WHITEPATH", "").SplitBy() ?? [];

        /// <summary>
        /// 限流的白名单标识列表
        /// </summary>
        public string[] WhiteKeys { get; set; } = Config.GetConfigs("WLN_RATE_WHITEKEYS", "").SplitBy() ?? [];

        /// <summary>
        /// 限流的黑名单标识列表
        /// </summary>
        public string[] BlackKeys { get; set; } = Config.GetConfigs("WLN_RATE_BLACKKEYS", "").SplitBy() ?? [];

        /// <summary>
        /// 提示消息内容，支持{ClientKey}变量
        /// </summary>
        public string MessageTpl { get; set; } = "{\"message\":\"访问频率超限，来自：{ClientKey} 的请求已被拒绝\"}";

        /// <summary>
        /// 是否启用熔断器（默认：true）
        /// </summary>
        public bool EnableCircuitBreaker { get; set; } = true;

        /// <summary>
        /// 熔断器失败阈值（默认：5次）
        /// </summary>
        public int CircuitBreakerFailureThreshold { get; set; } = 5;

        /// <summary>
        /// 熔断器恢复时间（默认：30秒）
        /// </summary>
        public int CircuitBreakerRecoveryTime { get; set; } = 30;

        /// <summary>
        /// 分布式限流存储（可选）
        /// </summary>
        public IRateLimitStore DistributedStore { get; set; } = null;

        /// <summary>
        /// 是否优先使用分布式存储
        /// </summary>
        public bool PreferDistributedStore { get; set; } = false;
    }

    /// <summary>
    /// 分布式限流存储接口
    /// </summary>
    public interface IRateLimitStore
    {
        /// <summary>
        /// 尝试获取请求许可
        /// </summary>
        /// <param name="key">限流键</param>
        /// <param name="limit">限制次数</param>
        /// <param name="window">时间窗口</param>
        /// <returns>是否允许请求</returns>
        Task<bool> TryAcquireAsync(string key, int limit, TimeSpan window);

        /// <summary>
        /// 检查存储是否可用
        /// </summary>
        bool IsAvailable { get; }
    }

    /// <summary>
    /// 令牌桶限流实现
    /// </summary>
    internal class TokenBucket
    {
        private readonly double _rate;
        private readonly double _capacity;
        private double _tokens;
        private DateTime _lastRefill;
        private readonly object _lock = new object();

        public TokenBucket(double rate, double capacity)
        {
            _rate = rate;
            _capacity = capacity;
            _tokens = capacity;
            _lastRefill = DateTime.UtcNow;
        }

        public bool TryConsume(double tokens = 1)
        {
            lock (_lock)
            {
                Refill();
                if (_tokens >= tokens)
                {
                    _tokens -= tokens;
                    return true;
                }
                return false;
            }
        }

        private void Refill()
        {
            var now = DateTime.UtcNow;
            var elapsed = (now - _lastRefill).TotalSeconds;
            if (elapsed > 0)
            {
                _tokens = Math.Min(_capacity, _tokens + elapsed * _rate);
                _lastRefill = now;
            }
        }
    }

    /// <summary>
    /// 熔断器实现
    /// </summary>
    internal class CircuitBreaker
    {
        private int _failureCount;
        private DateTime _lastFailureTime;
        private CircuitBreakerState _state = CircuitBreakerState.Closed;
        private readonly object _lock = new object();
        private readonly int _failureThreshold;
        private readonly TimeSpan _recoveryTime;

        public CircuitBreaker(int failureThreshold, TimeSpan recoveryTime)
        {
            _failureThreshold = failureThreshold;
            _recoveryTime = recoveryTime;
        }

        public bool IsOpen
        {
            get
            {
                lock (_lock)
                {
                    if (_state == CircuitBreakerState.Open)
                    {
                        if (DateTime.UtcNow - _lastFailureTime > _recoveryTime)
                        {
                            _state = CircuitBreakerState.HalfOpen;
                            _failureCount = 0;
                        }
                    }
                    return _state == CircuitBreakerState.Open;
                }
            }
        }

        public void OnFailure()
        {
            lock (_lock)
            {
                _lastFailureTime = DateTime.UtcNow;
                _failureCount++;

                if (_failureCount >= _failureThreshold)
                {
                    _state = CircuitBreakerState.Open;
                }
            }
        }

        public void OnSuccess()
        {
            lock (_lock)
            {
                if (_state == CircuitBreakerState.HalfOpen)
                {
                    _state = CircuitBreakerState.Closed;
                    _failureCount = 0;
                }
            }
        }

        private enum CircuitBreakerState
        {
            Closed,
            Open,
            HalfOpen
        }
    }

    /// <summary>
    /// 按标识符（IP、用户ID等）限流中间件
    /// </summary>
    public class RateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly RateLimitOptions _options;
        private readonly ConcurrentDictionary<string, TokenBucket> _tokenBuckets;
        private readonly ConcurrentDictionary<string, long> _fixedWindowCounters;
        private readonly ConcurrentDictionary<string, Queue<DateTime>> _slidingWindowQueues;
        private readonly HashSet<string> _whiteKeysSet;
        private readonly HashSet<string> _blackKeysSet;
        private readonly CircuitBreaker _circuitBreaker;
        private static readonly MemoryCache _memoryCache = new MemoryCache(Options.Create(new MemoryCacheOptions
        {
            SizeLimit = 10000
        }));

        /// <summary>
        /// 初始化限流中间件
        /// </summary>
        /// <param name="next">下一个中间件</param>
        /// <param name="options">限流配置选项</param>
        public RateLimitMiddleware(RequestDelegate next, RateLimitOptions options)
        {
            _next = next;
            _options = options ?? new RateLimitOptions();
            
            _tokenBuckets = new ConcurrentDictionary<string, TokenBucket>();
            _fixedWindowCounters = new ConcurrentDictionary<string, long>();
            _slidingWindowQueues = new ConcurrentDictionary<string, Queue<DateTime>>();
            
            _whiteKeysSet = new HashSet<string>(options.WhiteKeys ?? [], StringComparer.OrdinalIgnoreCase);
            _blackKeysSet = new HashSet<string>(options.BlackKeys ?? [], StringComparer.OrdinalIgnoreCase);
            
            if (_options.EnableCircuitBreaker)
            {
                _circuitBreaker = new CircuitBreaker(
                    _options.CircuitBreakerFailureThreshold,
                    TimeSpan.FromSeconds(_options.CircuitBreakerRecoveryTime)
                );
            }
        }

        /// <summary>
        /// 执行限流中间件
        /// </summary>
        /// <param name="context">HTTP 上下文</param>
        /// <returns>任务</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                if (_circuitBreaker?.IsOpen == true)
                {
                    await _next(context);
                    return;
                }

                var clientKey = GetClientKey(context);
                if (string.IsNullOrEmpty(clientKey) || _whiteKeysSet.Contains(clientKey))
                {
                    await _next(context);
                    return;
                }

                if (_blackKeysSet.Contains(clientKey))
                {
                    await WriteBlockedResponse(context, clientKey, StatusCodes.Status428PreconditionRequired);
                    return;
                }

                if (_options.MaxRequests <= 0)
                {
                    await _next(context);
                    return;
                }

                var path = context.Request.Path.Value;
                if (IsWhitePath(path))
                {
                    await _next(context);
                    return;
                }

                var key = GetRateLimitKey(clientKey, path);
                var isAllowed = await CheckRateLimitAsync(key);

                if (!isAllowed)
                {
                    await WriteBlockedResponse(context, clientKey, StatusCodes.Status429TooManyRequests);
                    return;
                }

                await _next(context);
                _circuitBreaker?.OnSuccess();
            }
            catch (Exception ex)
            {
                Log.Loger.Error($"RateLimit error: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
                _circuitBreaker?.OnFailure();
                await _next(context);
            }
        }

        private string GetClientKey(HttpContext context)
        {
            var clientKey = (context.Connection.RemoteIpAddress != null && !context.Connection.RemoteIpAddress.IsIPv4MappedToIPv6) 
                ? context.Connection.RemoteIpAddress?.ToString() 
                : context.Connection.RemoteIpAddress?.MapToIPv4().ToString();

            if (context.Request.Headers.TryGetValue("x-forwarded-for", out var value))
            {
                foreach (var ip in value.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (ip == "::1" || ip == "127.0.0.1" || !StringUtil.IsIP(ip))
                    {
                        continue;
                    }
                    clientKey = ip;
                    break;
                }
            }

            return clientKey ?? string.Empty;
        }

        private bool IsWhitePath(string? path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            foreach (var item in _options.WhitePath)
            {
                if (string.IsNullOrEmpty(item))
                {
                    continue;
                }

                if (item.EndsWith('*') && path.StartsWith(item.TrimEnd('*'), StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                else if (path.Equals(item, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private string GetRateLimitKey(string clientKey, string? path)
        {
            var key = $"wln_rate_{clientKey}";
            if (_options.IsolationPath && !string.IsNullOrEmpty(path))
            {
                key += path;
            }
            return key;
        }

        private async Task<bool> CheckRateLimitAsync(string key)
        {
            if (_options.PreferDistributedStore && _options.DistributedStore != null && _options.DistributedStore.IsAvailable)
            {
                try
                {
                    return await _options.DistributedStore.TryAcquireAsync(
                        key, 
                        _options.MaxRequests, 
                        TimeSpan.FromSeconds(_options.TimeWindow)
                    );
                }
                catch
                {
                }
            }

            return CheckLocalRateLimit(key);
        }

        private bool CheckLocalRateLimit(string key)
        {
            switch (_options.Algorithm)
            {
                case RateLimitAlgorithm.TokenBucket:
                    return CheckTokenBucket(key);
                case RateLimitAlgorithm.FixedWindow:
                    return CheckFixedWindow(key);
                case RateLimitAlgorithm.SlidingWindow:
                    return CheckSlidingWindow(key);
                default:
                    return CheckTokenBucket(key);
            }
        }

        private bool CheckTokenBucket(string key)
        {
            var bucket = _tokenBuckets.GetOrAdd(key, _ => 
            {
                var capacity = _options.BucketCapacity > 0 ? _options.BucketCapacity : _options.MaxRequests;
                var rate = (double)_options.MaxRequests / _options.TimeWindow;
                return new TokenBucket(rate, capacity);
            });

            return bucket.TryConsume();
        }

        private bool CheckFixedWindow(string key)
        {
            var now = DateTime.UtcNow;
            var windowStart = new DateTime(now.Ticks / TimeSpan.TicksPerSecond / _options.TimeWindow * _options.TimeWindow * TimeSpan.TicksPerSecond);
            var windowKey = $"{key}_{windowStart.Ticks}";

            var count = _fixedWindowCounters.AddOrUpdate(windowKey, 1, (_, old) => old + 1);

            var entryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_options.TimeWindow * 2),
                Size = 1
            };
            _memoryCache.Set(windowKey, count, entryOptions);

            return count <= _options.MaxRequests;
        }

        private bool CheckSlidingWindow(string key)
        {
            var now = DateTime.UtcNow;
            var window = TimeSpan.FromSeconds(_options.TimeWindow);

            var queue = _slidingWindowQueues.GetOrAdd(key, _ => new Queue<DateTime>());

            lock (queue)
            {
                while (queue.Count > 0 && now - queue.Peek() > window)
                {
                    queue.Dequeue();
                }

                if (queue.Count >= _options.MaxRequests)
                {
                    return false;
                }

                queue.Enqueue(now);
                return true;
            }
        }

        private Task WriteBlockedResponse(HttpContext context, string clientKey, int statusCode)
        {
            context.Response.StatusCode = statusCode;
            
            if (!string.IsNullOrEmpty(context.Request.ContentType) &&
                (context.Request.ContentType.Contains("json") ||
                 context.Request.ContentType.Contains("text/plain")))
            {
                context.Response.ContentType = "text/json";
            }
            else
            {
                context.Response.ContentType = "text/plain";
            }

            var message = _options.MessageTpl.Replace("{ClientKey}", clientKey);
            return context.Response.WriteAsync(message);
        }
    }

    /// <summary>
    /// 限流中间件扩展
    /// </summary>
    public static class RateLimitExtension
    {
        /// <summary>
        /// 配置使用限流中间件
        /// </summary>
        /// <param name="app">应用构建器</param>
        /// <param name="configureOptions">配置选项</param>
        /// <returns>应用构建器</returns>
        public static IApplicationBuilder UseRateLimit(this IApplicationBuilder app, Action<RateLimitOptions> configureOptions)
        {
            var options = new RateLimitOptions();
            configureOptions(options);
            return app.UseMiddleware<RateLimitMiddleware>(options);
        }
    }
}
