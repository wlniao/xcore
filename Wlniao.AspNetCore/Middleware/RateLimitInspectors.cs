using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Wlniao.Middleware
{
    /// <summary>
    /// 请求处理完成后可观察到的信息。ClientKey 与限流中间件使用的键完全一致，
    /// 检查器不应自行读取 X-Forwarded-For，避免与限流键/封禁键不一致。
    /// </summary>
    public sealed class RateLimitObservation
    {
        /// <summary>与限流/封禁一致的客户端键</summary>
        public string ClientKey { get; set; } = "";

        /// <summary>下游执行完成后的响应状态码</summary>
        public int StatusCode { get; set; }

        /// <summary>请求路径</summary>
        public string Path { get; set; } = "";

        /// <summary>请求方法</summary>
        public string Method { get; set; } = "";

        /// <summary>当前 HTTP 上下文</summary>
        public HttpContext HttpContext { get; set; } = null!;
    }

    /// <summary>
    /// 响应后检查器：可按状态码、路径、频率等规则决定是否把客户端加入动态封禁列表。
    /// 实现必须保证自身异常不外抛（中间件侧也会兜底）。
    /// </summary>
    public interface IRateLimitInspector
    {
        /// <summary>观察一次已完成的下游请求</summary>
        /// <param name="observation">请求观察数据</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>异步任务</returns>
        ValueTask InspectAsync(RateLimitObservation observation, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 404 扫描检测器：窗口内累计 404 次数，达到阈值后自动封禁客户端。
    /// </summary>
    public sealed class NotFoundScanInspector : IRateLimitInspector
    {
        private sealed class AbuseWindow
        {
            public long WindowStartMs;
            public int Count;
        }

        private readonly ConcurrentDictionary<string, AbuseWindow> _windows = new(StringComparer.OrdinalIgnoreCase);
        private const long SweepIntervalMs = 60_000;
        private int _maxTrackedClients = 10000;
        private long _lastSweepMs;

        /// <summary>
        /// 最多跟踪的客户端窗口数，默认 10000；超出时按窗口开始时间淘汰最旧条目，避免无界增长
        /// </summary>
        public int MaxTrackedClients
        {
            get => _maxTrackedClients;
            set => _maxTrackedClients = Math.Max(1, value);
        }

        /// <summary>当前跟踪的客户端窗口数（诊断用）</summary>
        public int TrackedClientCount => _windows.Count;

        /// <summary>触发封禁的 404 次数，默认 20</summary>
        public int Threshold { get; set; } = 20;

        /// <summary>统计窗口，默认 1 分钟</summary>
        public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>封禁时长，默认 2 小时；BanUntil 设置后本值忽略</summary>
        public TimeSpan BanDuration { get; set; } = TimeSpan.FromHours(2);

        /// <summary>绝对解封时间（UTC），例如每天 09:00 解封；设置后优先于 BanDuration</summary>
        public DateTimeOffset? BanUntil { get; set; }

        /// <summary>额外过滤条件，返回 false 时不计数（如排除 /health）</summary>
        public Func<RateLimitObservation, bool>? Filter { get; set; }

        /// <summary>被统计的状态码，默认 404</summary>
        public int StatusCode { get; set; } = 404;

        /// <inheritdoc />
        public async ValueTask InspectAsync(RateLimitObservation observation, CancellationToken cancellationToken = default)
        {
            if (observation == null || string.IsNullOrEmpty(observation.ClientKey) ||
                observation.StatusCode != StatusCode || (Filter != null && !Filter(observation)))
            {
                return;
            }

            var threshold = Math.Max(1, Threshold);
            var window = Window < TimeSpan.FromSeconds(1) ? TimeSpan.FromSeconds(1) : Window;
            var windowMs = (long)window.TotalMilliseconds;
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var shouldBan = false;
            Sweep(nowMs, windowMs, force: false);

            var state = _windows.GetOrAdd(observation.ClientKey, _ => new AbuseWindow
            {
                WindowStartMs = nowMs,
                Count = 0
            });
            lock (state)
            {
                if (nowMs - state.WindowStartMs >= windowMs)
                {
                    state.WindowStartMs = nowMs;
                    state.Count = 0;
                }
                state.Count++;
                if (state.Count >= threshold)
                {
                    shouldBan = true;
                    state.Count = 0;
                }
            }

            if (!shouldBan)
            {
                // 容量超限时强制淘汰，避免未达阈值就离开的客户端永久占用窗口对象
                Sweep(nowMs, windowMs, force: _windows.Count > MaxTrackedClients);
                return;
            }

            // 真正互斥的是锁内 Count = 0：并发达到阈值时只有一个请求进入封禁分支；
            // 这里的条件删除只负责清理窗口状态，不承担互斥职责
            RemoveIfSame(observation.ClientKey, state);
            var unbanAt = BanUntil ?? DateTimeOffset.UtcNow + BanDuration;
            await RateLimitControl.BanAsync(observation.ClientKey, unbanAt, $"http-{StatusCode}-scan", cancellationToken).ConfigureAwait(false);
        }

        private void Sweep(long nowMs, long windowMs, bool force)
        {
            // force 路径必须真正执行一次淘汰：CAS 失败时重试直到拿到 sweep 槽位，
            // 否则并发下所有超限调用都让出，窗口数可能暂时突破上限。
            long last;
            while (true)
            {
                last = Interlocked.Read(ref _lastSweepMs);
                if (!force && nowMs - last < SweepIntervalMs)
                {
                    return;
                }
                var stamp = Math.Max(nowMs, last);
                if (Interlocked.CompareExchange(ref _lastSweepMs, stamp, last) == last)
                {
                    break;
                }
            }

            foreach (var item in _windows)
            {
                bool expired;
                lock (item.Value)
                {
                    expired = nowMs - item.Value.WindowStartMs >= windowMs;
                }
                if (expired)
                {
                    RemoveIfSame(item.Key, item.Value);
                }
            }

            if (_windows.Count <= MaxTrackedClients)
            {
                return;
            }
            foreach (var item in _windows.OrderBy(x => x.Value.WindowStartMs)
                         .Take(_windows.Count - MaxTrackedClients))
            {
                RemoveIfSame(item.Key, item.Value);
            }
        }

        private void RemoveIfSame(string key, AbuseWindow window)
        {
            ((ICollection<KeyValuePair<string, AbuseWindow>>)_windows)
                .Remove(new KeyValuePair<string, AbuseWindow>(key, window));
        }
    }
}
