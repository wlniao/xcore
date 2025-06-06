using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Wlniao;
using Wlniao.Text;

namespace Wlniao.Middleware
{
    /// <summary>
    /// 按标识符（IP、用户ID等）限流
    /// Usage: app.UseMiddleware<RateLimitMiddleware>(new RateLimitMiddleware.RateLimitOptions { TimeWindow = 30, MaxRequests = 60 });
    /// </summary>
    public class RateLimitMiddleware
    {
        public class RateLimitOptions
        {
            /// <summary>
            /// 时间窗口分片数量（默认：60个）
            /// </summary>
            public int TZoneCount { get; set; } = 0;
            /// <summary>
            /// 请求时间窗口（默认：30秒）
            /// </summary>
            public int TimeWindow { get; set; } = Wlniao.Convert.ToInt(Wlniao.Config.GetSetting("WLN_RATE_TIME_SECONDS", "30"));
            /// <summary>
            /// 最大请求次数
            /// </summary>
            public int MaxRequests { get; set; } = Wlniao.Convert.ToInt(Wlniao.Config.GetSetting("WLN_RATE_MAX_REQUESTS", "0"));
            /// <summary>
            /// 是否隔离请求目录（不同目录分开限流）
            /// </summary>
            public bool IsolationPath { get; set; } = false;
            /// <summary>
            /// 限流的白名单目录列表
            /// </summary>
            public string[] WhitePath { get; set; } = Wlniao.Config.GetSetting("WLN_RATE_WHITEPATH", "/*").SplitBy();
            /// <summary>
            /// 限流的白名单标识列表
            /// </summary>
            public string[] WhiteKeys { get; set; } = Wlniao.Config.GetSetting("WLN_RATE_WHITEKEYS").SplitBy();
            /// <summary>
            /// 限流的黑名单标识列表
            /// </summary>
            public string[] BlackKeys { get; set; } = Wlniao.Config.GetSetting("WLN_RATE_BLACKKEYS").SplitBy();

            /// <summary>
            /// 提示消息内容，支持{ClientKey}变量
            /// </summary>
            public string MessageTpl { get; set; } = "{\"message\":\"访问频率超限，来自：{ClientKey} 的请求已被拒绝\"}";
        }
        private readonly RequestDelegate _next;
        private readonly RateLimitOptions _options;
        private static MemoryCache memoryCache = new MemoryCache(Options.Create(new MemoryCacheOptions()));
        public RateLimitMiddleware(RequestDelegate next, RateLimitOptions options)
        {
            _next = next;
            _options = options;
            if (_options.TZoneCount > 0)
            {
                _options.TZoneCount = _options.TZoneCount < 360 ? _options.TZoneCount : 360;
            }
            else if (_options.TimeWindow >= 86400)
            {
                _options.TZoneCount = 120;
            }
            else if (_options.TimeWindow >= 3600)
            {
                _options.TZoneCount = _options.TimeWindow / 60;
            }
            else if (_options.TimeWindow >= 600)
            {
                _options.TZoneCount = _options.TimeWindow / 10;
            }
            else if (_options.TimeWindow >= 120)
            {
                _options.TZoneCount = _options.TimeWindow / 2;
            }
            else if (_options.TimeWindow < 60)
            {
                _options.TZoneCount = _options.TimeWindow;
            }
            else
            {
                _options.TZoneCount = 60;
            }
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var clientKey = (context.Connection.RemoteIpAddress != null && !context.Connection.RemoteIpAddress.IsIPv4MappedToIPv6) ? context.Connection.RemoteIpAddress?.ToString() : context.Connection.RemoteIpAddress?.MapToIPv4().ToString();
            if (context.Request.Headers.TryGetValue("x-forwarded-for", out StringValues value))
            {
                foreach (string ip in value.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (ip != "::1" && ip != "127.0.0.1" && StringUtil.IsIP(ip))
                    {
                        clientKey = ip;
                        break;
                    }
                }
            }
            if (string.IsNullOrEmpty(clientKey) || _options.WhiteKeys.Contains(clientKey))
            {
                // 如果符合白名单或无法获取客户端标识,则直接放行
                await _next(context);
                return;
            }
            else if (_options.BlackKeys.Contains(clientKey))
            {
                // 如果符合黑名单或无法获取客户端标识,则直接限行
                context.Response.StatusCode = StatusCodes.Status428PreconditionRequired;
                context.Response.ContentType = "text/json";
                await context.Response.WriteAsync(_options.MessageTpl.Replace("{ClientKey}", clientKey));
                return;
            }


            if (_options.MaxRequests <= 0)
            {
                // 如果未配置最大请求次数
                await _next(context);
                return;
            }
            var path = context.Request.Path.Value;
            foreach (var item in _options.WhitePath)
            {
                // 判断当前请求是否非限流目录
                if (item.EndsWith('*'))
                {
                    if (path.StartsWith(item.TrimEnd('*')))
                    {
                        await _next(context);
                        return;
                    }
                }
                else if (path == item)
                {
                    await _next(context);
                    return;
                }
            }

            var key = $"wln_rate_{clientKey}";
            if (_options.IsolationPath)
            {
                key += path;
            }
            var granularity = _options.TimeWindow * 10000000L / _options.TZoneCount; // 时间粒度
            var tickNowMin = (DateTime.UtcNow.Ticks / granularity) * granularity; // 当前统计区间起点时间（取整）
            var tickExpire = tickNowMin - granularity * _options.TZoneCount; // 请求记录过期时间
            var requestCount = 1L; //累计次数默认加上本次访问
            var requestRecords = new List<long>();
            foreach (var record in memoryCache.GetOrCreate<List<long>>(key, entry => { return new List<long>(); }))
            {
                if (record > tickNowMin)
                {
                    requestCount += record % granularity;
                    tickNowMin = record; // 取出当前统计区间
                }
                else if (record > tickExpire)
                {
                    requestCount += record % granularity;
                    requestRecords.Add(record);
                }
            }

            // 记录当前统计区间
            requestRecords.Add(tickNowMin + 1);

            // 缓存新的请求记录（并根据请求时间窗口设置缓存滑动过期时间）
            memoryCache.Set(key, requestRecords, new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromSeconds(_options.TimeWindow) });

            // 检查是否超过限制
            if (requestCount > _options.MaxRequests)
            {
                // 如果超过限制,则根据策略进行处理,比如返回429 Too Many Requests状态码
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.ContentType = "text/json";
                await context.Response.WriteAsync(_options.MessageTpl.Replace("{ClientKey}", clientKey));
                return;
            }

            // 如果没有超过限制,则继续处理请求
            await _next(context);
        }

    }
}