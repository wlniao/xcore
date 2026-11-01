using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
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
        /// 可信代理白名单（可选，默认空数组 = 保持历史行为）。
        /// 支持单个 IP、CIDR 网段与通配符，如 "10.0.0.5"、"10.0.0.0/8"、"::1"、"*"，多项用逗号分隔。
        /// 配置项：WLN_RATE_TRUSTED_PROXIES。
        /// <para>
        /// 为空（或含 "*"）时：**无条件采信** X-Forwarded-For 的第一个有效 IP，与历史行为完全一致。
        /// </para>
        /// <para>
        /// 非空时：仅当直连对端（<c>Connection.RemoteIpAddress</c>）命中白名单才采信该头，
        /// 否则一律使用直连地址；采信时自右向左回溯，取第一个不可信的跳作为客户端地址，
        /// 避免可信代理「追加而非覆盖」时攻击者预置伪造的最左项。
        /// </para>
        /// </summary>
        public string[] TrustedProxies { get; set; } = Config.GetConfigs("WLN_RATE_TRUSTED_PROXIES", "").SplitBy() ?? [];

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
    /// 固定窗口计数器
    /// </summary>
    internal class WindowCounter
    {
        /// <summary>
        /// 当前窗口内的请求计数
        /// </summary>
        public long Value;
    }

    /// <summary>
    /// 令牌桶限流实现
    /// </summary>
    internal class TokenBucket(double rate, double capacity)
    {
        private readonly double _rate = rate;
        private readonly double _capacity = capacity;
        private double _tokens = capacity;
        private DateTime _lastRefill = DateTime.UtcNow;
        private readonly object _lock = new();

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
    internal class CircuitBreaker(int failureThreshold, TimeSpan recoveryTime)
    {
        private int _failureCount;
        private DateTime _lastFailureTime;
        private CircuitBreakerState _state = CircuitBreakerState.Closed;
        private readonly object _lock = new();
        private readonly int _failureThreshold = failureThreshold;
        private readonly TimeSpan _recoveryTime = recoveryTime;

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
    /// 可信代理匹配器：判断直连对端是否为可信代理，用于决定是否采信 X-Forwarded-For。
    /// 支持单个 IP、CIDR 网段（IPv4 / IPv6）与通配符 "*"。
    /// </summary>
    internal sealed class TrustedProxyMatcher
    {
        private readonly List<(byte[] Network, int Prefix)> _ranges = [];

        /// <summary>
        /// 是否显式配置了通配符 "*"（表示全部地址可信）
        /// </summary>
        public bool MatchAll { get; private set; }

        /// <summary>
        /// 是否配置了任意非空白项（无论是否解析成功）
        /// </summary>
        public bool Configured { get; private set; }

        /// <summary>
        /// 是否成功解析出可用的可信代理项
        /// </summary>
        public bool Any => MatchAll || _ranges.Count > 0;

        /// <summary>
        /// 初始化匹配器
        /// </summary>
        /// <param name="entries">IP / CIDR / 通配符列表，非法项忽略</param>
        public TrustedProxyMatcher(IEnumerable<string>? entries)
        {
            foreach (var entry in entries ?? [])
            {
                var item = entry?.Trim();
                if (string.IsNullOrEmpty(item))
                {
                    continue;
                }

                Configured = true;

                if (item == "*")
                {
                    MatchAll = true;
                    continue;
                }

                var slash = item.IndexOf('/');
                if (!IPAddress.TryParse(slash < 0 ? item : item[..slash], out var network))
                {
                    continue; // 非法项忽略：调用方按「已配置但无有效项」处理，不可退化为信任全部
                }

                network = ToComparable(network);
                var maxPrefix = network.AddressFamily == AddressFamily.InterNetworkV6 ? 128 : 32;
                var prefix = maxPrefix;
                if (slash >= 0 && (!int.TryParse(item[(slash + 1)..], out prefix) || prefix < 0 || prefix > maxPrefix))
                {
                    prefix = maxPrefix; // 掩码非法时退化为精确匹配，宁可收紧也不放宽
                }

                _ranges.Add((network.GetAddressBytes(), prefix));
            }
        }

        /// <summary>
        /// 判断地址是否命中可信代理白名单
        /// </summary>
        /// <param name="address">待判断地址</param>
        /// <returns></returns>
        public bool IsTrusted(IPAddress? address)
        {
            if (address == null)
            {
                return false;
            }

            if (MatchAll)
            {
                return true;
            }

            if (_ranges.Count == 0)
            {
                return false;
            }

            var ip = ToComparable(address);
            var bytes = ip.GetAddressBytes();
            foreach (var (network, prefix) in _ranges)
            {
                if (network.Length != bytes.Length)
                {
                    continue;
                }

                var fullBytes = prefix / 8;
                var matched = true;
                for (var i = 0; i < fullBytes; i++)
                {
                    if (bytes[i] != network[i])
                    {
                        matched = false;
                        break;
                    }
                }

                if (!matched)
                {
                    continue;
                }

                var restBits = prefix % 8;
                if (restBits > 0)
                {
                    var mask = (byte)(0xFF << (8 - restBits));
                    if ((bytes[fullBytes] & mask) != (network[fullBytes] & mask))
                    {
                        continue;
                    }
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// IPv4 映射的 IPv6 地址统一转为 IPv4，避免同一地址因表示形式不同而漏判
        /// </summary>
        private static IPAddress ToComparable(IPAddress address)
            => address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
    }

    /// <summary>
    /// 按标识符（IP、用户ID等）限流中间件
    /// </summary>
    public class RateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly RateLimitOptions _options;
        private readonly HashSet<string> _whiteKeysSet;
        private readonly HashSet<string> _blackKeysSet;
        private readonly TrustedProxyMatcher _trustedProxies;
        /// <summary>
        /// 未配置可信代理（或配置为 "*"）时保持历史行为：无条件采信 X-Forwarded-For
        /// </summary>
        private readonly bool _legacyTrustAll;
        private readonly CircuitBreaker? _circuitBreaker;
        /// <summary>
        /// 限流状态统一放在带容量与过期策略的缓存中，避免字典只增不删造成内存泄漏。
        /// 注意：MemoryCache 超过 SizeLimit 时不会抛异常，而是触发压缩并淘汰条目
        /// （实测 SizeLimit=100 时连续写入 5000 条，最终只存活约 95 条）。
        /// 若被淘汰的正是当前计数条目，计数会从 0 重来，表现为限流静默失效，
        /// 因此由 <see cref="CacheUnderPressure"/> 在容量压力下提前切换到粗粒度桶。
        /// </summary>
        private readonly MemoryCache _memoryCache = new(Options.Create(new MemoryCacheOptions
        {
            SizeLimit = CacheSizeLimit
        }));

        private const int CacheSizeLimit = 10000;
        /// <summary>
        /// 条目数达到该比例即认定处于容量压力状态（可能正在被大量伪造标识冲击），
        /// 此时不再依赖会被淘汰的细粒度计数，直接使用粗粒度桶。
        /// 若站点存在远超此规模的正常活跃客户端，应调大 <see cref="CacheSizeLimit"/>。
        /// </summary>
        private const int CoarseTriggerCount = CacheSizeLimit * 9 / 10;

        private const int CoarseBucketCount = 128;
        private readonly long[] _coarseWindows = new long[CoarseBucketCount];
        private readonly long[] _coarseCounts = new long[CoarseBucketCount];
        private readonly object _coarseLock = new();

        private long _pressureProbe;
        private volatile bool _cachePressure;

        /// <summary>
        /// 初始化限流中间件
        /// </summary>
        /// <param name="next">下一个中间件</param>
        /// <param name="options">限流配置选项</param>
        public RateLimitMiddleware(RequestDelegate next, RateLimitOptions options)
        {
            _next = next;
            _options = options ?? new RateLimitOptions();
            
            _whiteKeysSet = new HashSet<string>(_options.WhiteKeys ?? [], StringComparer.OrdinalIgnoreCase);
            _blackKeysSet = new HashSet<string>(_options.BlackKeys ?? [], StringComparer.OrdinalIgnoreCase);
            _trustedProxies = new TrustedProxyMatcher(_options.TrustedProxies);
            // 未配置或配置为 "*" 时保持历史行为；配置了却没有任何有效项时不退回信任全部，
            // 而是 fail-closed（忽略 X-Forwarded-For），避免因配置笔误把伪造能力打开
            _legacyTrustAll = _trustedProxies.MatchAll || !_trustedProxies.Configured;
            if (!_legacyTrustAll && !_trustedProxies.Any)
            {
                Log.Loger.Warn("RateLimit: TrustedProxies 未包含任何有效的 IP / CIDR，X-Forwarded-For 将不被采信");
            }
            
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
        /// <remarks>
        /// 判定优先级（自上而下，命中即返回）：客户端白名单 → <b>黑名单（封禁，428）</b> → 熔断打开（放行）
        /// → 未启用限流 / 路径白名单（放行）→ 限流（429）。
        /// 黑名单代表「封禁该标识」，必须先于限流开关与路径白名单判定，否则会出现「拉黑了却因限流未启用而被放行」。
        /// <para>
        /// 熔断器语义（与历史行为一致，本次仅去掉重复执行）：同时观察「限流检查自身」与「下游处理」。
        /// 限流检查抛异常、或下游处理抛异常都计为失败（OnFailure）；两者都成功计为成功（OnSuccess）。
        /// 熔断打开期间跳过限流检查直接放行，且不更新熔断状态，等待恢复时间后半开自愈。
        /// </para>
        /// </remarks>
        /// <param name="context">HTTP 上下文</param>
        /// <returns>任务</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            // 唯一不变量：_next 在整个方法中只能被调用一次。
            // 限流判断阶段只产出「放行 / 拒绝」结论，不推进管道；
            // 否则下游抛出的异常会回流到判断阶段的 catch，导致整条管道被执行两次。
            var allow = true;
            var blockedStatus = 0;
            var clientKey = string.Empty;
            // 熔断打开期间仍需放行请求，但不再更新熔断状态，等待恢复时间后半开自愈
            var observeCircuit = _circuitBreaker != null;

            try
            {
                clientKey = GetClientKey(context);
                var path = context.Request.Path.Value;

                if (string.IsNullOrEmpty(clientKey) || _whiteKeysSet.Contains(clientKey))
                {
                    // 客户端白名单放行。与历史行为一致：同一标识同时出现在黑白名单时以白名单为准
                }
                else if (_blackKeysSet.Contains(clientKey))
                {
                    // 黑名单语义是「封禁该标识」，优先级必须高于熔断状态、限流开关与路径白名单：
                    // 被拉黑的客户端即使在「未启用限流」「路径白名单」「熔断打开」时也必须继续被拦截。
                    // 曾因合并条件短路（MaxRequests<=0 || IsWhitePath 前置）导致封禁失效，勿再调整顺序。
                    allow = false;
                    blockedStatus = StatusCodes.Status428PreconditionRequired;
                }
                else if (_circuitBreaker?.IsOpen == true)
                {
                    // 熔断打开：跳过限流检查，且不更新熔断状态，等待恢复时间后半开自愈
                    observeCircuit = false;
                }
                else if (_options.MaxRequests <= 0 || IsWhitePath(path))
                {
                    // 未启用限流 / 路径白名单：放行
                }
                else if (!await CheckRateLimitAsync(GetRateLimitKey(clientKey, path)))
                {
                    allow = false;
                    blockedStatus = StatusCodes.Status429TooManyRequests;
                }
            }
            catch (Exception ex)
            {
                // 限流是旁路能力，自身故障时应放行而不是把请求打成 5xx
                Log.Loger.Error($"RateLimit error: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
                observeCircuit = false;
                _circuitBreaker?.OnFailure();
                allow = true;
            }

            if (!allow)
            {
                await WriteBlockedResponse(context, clientKey, blockedStatus);
                return;
            }

            // 下游处理独立 try：异常只用于熔断计数，然后原样向上抛出（响应由 ErrorHandling 兜底），
            // 这里不再重试或二次调用 _next
            try
            {
                await _next(context);
            }
            catch
            {
                if (observeCircuit)
                {
                    _circuitBreaker?.OnFailure();
                }
                throw;
            }

            if (observeCircuit)
            {
                _circuitBreaker?.OnSuccess();
            }
        }

        private string GetClientKey(HttpContext context)
        {
            var remoteIp = context.Connection.RemoteIpAddress;

            // 未配置可信代理（或配置 "*"）：保持历史行为，无条件采信 X-Forwarded-For 的第一个有效 IP
            if (_legacyTrustAll)
            {
                var clientKey = NormalizeIp(remoteIp);
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

                return clientKey;
            }

            // 已配置可信代理：直连对端不在白名单内时不采信该头。
            // 否则客户端只需每次伪造一个不同的 X-Forwarded-For，就能获得无限限流键、直接绕过限流
            if (!_trustedProxies.IsTrusted(remoteIp))
            {
                return NormalizeIp(remoteIp);
            }

            if (!context.Request.Headers.TryGetValue("x-forwarded-for", out var forwarded))
            {
                return NormalizeIp(remoteIp);
            }

            // 自右向左回溯，跳过可信代理，取第一个不可信的跳。
            // 不能直接取最左值：可信代理通常在校验后「追加」本跳地址而不覆盖，
            // 攻击者可预置伪造的最左项，只有最右侧不可信跳才是无法伪造的
            var hops = forwarded.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            for (var i = hops.Length - 1; i >= 0; i--)
            {
                if (!IPAddress.TryParse(hops[i], out var hop) || _trustedProxies.IsTrusted(hop))
                {
                    continue;
                }

                return NormalizeIp(hop);
            }

            // 整条链都是可信代理或无法解析时，退回直连地址
            return NormalizeIp(remoteIp);
        }

        /// <summary>
        /// 统一地址表示：IPv4 映射的 IPv6 还原为 IPv4，空地址返回空字符串
        /// </summary>
        /// <param name="address">地址</param>
        /// <returns></returns>
        private static string NormalizeIp(IPAddress? address)
        {
            if (address == null)
            {
                return string.Empty;
            }

            if (address.IsIPv4MappedToIPv6)
            {
                address = address.MapToIPv4();
            }

            return address.ToString();
        }

        private bool IsWhitePath(string? path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            foreach (var item in _options.WhitePath ?? [])
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
            // 缓存处于容量压力状态时，细粒度计数随时可能被压缩淘汰而失真，
            // 直接退化为粗粒度桶，避免「被攻击时限流静默失效」
            if (CacheUnderPressure())
            {
                return CheckCoarseBucket(key);
            }

            try
            {
                return _options.Algorithm switch
                {
                    RateLimitAlgorithm.TokenBucket => CheckTokenBucket(key),
                    RateLimitAlgorithm.FixedWindow => CheckFixedWindow(key),
                    RateLimitAlgorithm.SlidingWindow => CheckSlidingWindow(key),
                    _ => CheckTokenBucket(key),
                };
            }
            catch (Exception ex)
            {
                // 缓存实现变更导致异常时同样退化为粗粒度桶，而不是直接 fail-open：
                // 客户端标识（IP / X-Forwarded-For）攻击者可控，一旦放行就等于关掉限流
                Log.Loger.Warn($"Rate limit storage: {ex.Message}, fallback to coarse bucket");
                return CheckCoarseBucket(key);
            }
        }

        /// <summary>
        /// 判断缓存是否处于容量压力状态。<see cref="MemoryCache.Count"/> 需要遍历分桶，
        /// 故按 64 次采样一次；状态切换最多滞后 64 个请求，对限流判断无实质影响。
        /// </summary>
        /// <returns></returns>
        private bool CacheUnderPressure()
        {
            if ((Interlocked.Increment(ref _pressureProbe) & 63) != 0)
            {
                return _cachePressure;
            }

            _cachePressure = _memoryCache.Count >= CoarseTriggerCount;
            return _cachePressure;
        }

        /// <summary>
        /// 粗粒度兜底限流：缓存处于容量压力或存储异常时使用。分桶数固定为
        /// <see cref="CoarseBucketCount"/>，内存占用恒定且永不增长；同一桶内的客户端
        /// 共享一份配额（每桶每窗口 <see cref="RateLimitOptions.MaxRequests"/> 次），
        /// 属于攻击场景下「宁可误伤也不放弃限流」的降级行为。
        /// </summary>
        /// <param name="key">限流键</param>
        /// <returns>是否放行</returns>
        private bool CheckCoarseBucket(string key)
        {
            var bucket = (int)((uint)key.GetHashCode() % CoarseBucketCount);
            var windowSeconds = Math.Max(1, _options.TimeWindow);
            var windowTicks = TimeSpan.TicksPerSecond * windowSeconds;
            var currentWindow = DateTime.UtcNow.Ticks / windowTicks;
            var limit = Math.Max(1, _options.MaxRequests);

            lock (_coarseLock)
            {
                if (_coarseWindows[bucket] != currentWindow)
                {
                    _coarseWindows[bucket] = currentWindow;
                    _coarseCounts[bucket] = 0;
                }

                return ++_coarseCounts[bucket] <= limit;
            }
        }

        /// <summary>
        /// 取得缓存中的唯一状态实例。GetOrCreate 并非原子操作，并发下多个线程可能各自
        /// 创建一份状态，必须以真正落入缓存的实例为准，否则计数会打在落选实例上导致限流偏松。
        /// 注意：本方法只是把计数收敛到「缓存赢家」，仍不是严格原子——若线程 A 的 TryGetValue
        /// 早于线程 B 的 Set 提交，A 仍可能拿到落选实例。窗口极窄，最坏结果是限流略松，
        /// 对限流场景可接受；如需严格精确应改用原子计数器或分布式存储。
        /// </summary>
        /// <typeparam name="T">状态类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <param name="factory">状态工厂</param>
        /// <param name="absoluteSeconds">绝对过期秒数（与滑动过期二选一）</param>
        /// <param name="slidingSeconds">滑动过期秒数（大于 0 时优先生效）</param>
        /// <returns></returns>
        private T? GetState<T>(string key, Func<T> factory, int absoluteSeconds, int slidingSeconds = 0) where T : class
        {
            _memoryCache.GetOrCreate(key, entry =>
            {
                entry.Size = 1;
                if (slidingSeconds > 0)
                {
                    entry.SlidingExpiration = TimeSpan.FromSeconds(slidingSeconds);
                }
                else
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(absoluteSeconds);
                }
                return factory();
            });

            return _memoryCache.TryGetValue(key, out T? cached) ? cached : null;
        }

        private bool CheckTokenBucket(string key)
        {
            var windowSeconds = Math.Max(1, _options.TimeWindow);
            var bucket = GetState(key,
                () =>
                {
                    var capacity = _options.BucketCapacity > 0 ? _options.BucketCapacity : _options.MaxRequests;
                    return new TokenBucket((double)_options.MaxRequests / windowSeconds, capacity);
                },
                windowSeconds * 2,
                slidingSeconds: windowSeconds * 2);

            // 取不到实例说明刚写入即被缓存压缩淘汰，此时不能当作「放行」，
            // 否则淘汰动作本身就成了绕过限流的手段
            return bucket == null ? CheckCoarseBucket(key) : bucket.TryConsume();
        }

        private bool CheckFixedWindow(string key)
        {
            var windowSeconds = Math.Max(1, _options.TimeWindow);
            var windowTicks = TimeSpan.TicksPerSecond * windowSeconds;
            var windowKey = $"{key}_{DateTime.UtcNow.Ticks / windowTicks}";

            var counter = GetState(windowKey, () => new WindowCounter(), windowSeconds * 2);
            if (counter == null)
            {
                return CheckCoarseBucket(key);
            }

            return Interlocked.Increment(ref counter.Value) <= _options.MaxRequests;
        }

        private bool CheckSlidingWindow(string key)
        {
            var windowSeconds = Math.Max(1, _options.TimeWindow);
            var window = TimeSpan.FromSeconds(windowSeconds);

            var queue = GetState(key, () => new Queue<DateTime>(), windowSeconds * 2, slidingSeconds: windowSeconds * 2);
            if (queue == null)
            {
                return CheckCoarseBucket(key);
            }

            var now = DateTime.UtcNow;
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

            // 仅当请求本身是 JSON 时按 JSON 返回，两者都是标准 MIME 类型
            var isJson = context.Request.ContentType?.Contains("json", StringComparison.OrdinalIgnoreCase) ?? false;
            context.Response.ContentType = isJson ? "application/json; charset=utf-8" : "text/plain; charset=utf-8";

            var message = (_options.MessageTpl ?? string.Empty).Replace("{ClientKey}", clientKey);
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
