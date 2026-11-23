using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wlniao.Log;

namespace Wlniao.Middleware
{
    /// <summary>
    /// 动态封禁条目。UnbanAt 为 UTC 绝对解封时间，到期后由存储自动失效。
    /// </summary>
    public sealed class BanEntry
    {
        /// <summary>被封禁的限流键（通常是客户端 IP）</summary>
        public string Key { get; set; } = "";

        /// <summary>UTC 绝对解封时间</summary>
        public DateTimeOffset UnbanAt { get; set; }

        /// <summary>封禁原因（如 404-scan、manual）</summary>
        public string? Reason { get; set; }

        /// <summary>封禁创建时间（UTC）</summary>
        public DateTimeOffset CreatedAt { get; set; }
    }

    /// <summary>
    /// 动态封禁存储。默认实现为进程内字典；分布式场景可换成基于 Redis 的实现。
    /// </summary>
    public interface IRateLimitBanStore
    {
        /// <summary>存储是否可用（Redis 未配置时为 false）</summary>
        bool IsAvailable { get; }

        /// <summary>读取封禁条目；已过期条目必须返回 null</summary>
        /// <param name="key">限流键</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>有效封禁条目，未封禁返回 null</returns>
        Task<BanEntry?> GetAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>写入或改期封禁；UnbanAt &lt;= now 时等价于解封</summary>
        /// <param name="key">限流键</param>
        /// <param name="unbanAt">UTC 绝对解封时间</param>
        /// <param name="reason">封禁原因</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否写入成功</returns>
        Task<bool> BanAsync(string key, DateTimeOffset unbanAt, string? reason, CancellationToken cancellationToken = default);

        /// <summary>仅修改解封时间，保留原 Reason / CreatedAt；未封禁时新建条目</summary>
        /// <param name="key">限流键</param>
        /// <param name="unbanAt">UTC 绝对解封时间</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否修改成功</returns>
        Task<bool> SetUnbanAtAsync(string key, DateTimeOffset unbanAt, CancellationToken cancellationToken = default);

        /// <summary>立即解封</summary>
        /// <param name="key">限流键</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否确实移除了条目</returns>
        Task<bool> UnbanAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>列出仍有效的封禁条目（诊断用，Redis 实现只列本进程写入过的键）</summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>封禁条目快照</returns>
        Task<IReadOnlyList<BanEntry>> GetBansAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 进程内封禁存储：零依赖默认实现，供单机部署和测试使用。
    /// </summary>
    internal sealed class InMemoryRateLimitBanStore : IRateLimitBanStore
    {
        private readonly ConcurrentDictionary<string, BanEntry> _entries = new(StringComparer.OrdinalIgnoreCase);
        private long _lastSweepTicks;

        /// <inheritdoc />
        public bool IsAvailable => true;

        /// <inheritdoc />
        public Task<BanEntry?> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key))
            {
                return Task.FromResult<BanEntry?>(null);
            }
            Sweep();
            if (!_entries.TryGetValue(key, out var entry))
            {
                return Task.FromResult<BanEntry?>(null);
            }
            if (entry.UnbanAt <= DateTimeOffset.UtcNow)
            {
                _entries.TryRemove(key, out _);
                return Task.FromResult<BanEntry?>(null);
            }
            return Task.FromResult<BanEntry?>(entry);
        }

        /// <inheritdoc />
        public Task<bool> BanAsync(string key, DateTimeOffset unbanAt, string? reason, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key))
            {
                return Task.FromResult(false);
            }
            if (unbanAt <= DateTimeOffset.UtcNow)
            {
                _entries.TryRemove(key, out _);
                return Task.FromResult(true);
            }
            _entries[key] = new BanEntry
            {
                Key = key,
                UnbanAt = unbanAt,
                Reason = reason,
                CreatedAt = DateTimeOffset.UtcNow
            };
            return Task.FromResult(true);
        }

        /// <inheritdoc />
        public Task<bool> SetUnbanAtAsync(string key, DateTimeOffset unbanAt, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key))
            {
                return Task.FromResult(false);
            }
            if (unbanAt <= DateTimeOffset.UtcNow)
            {
                _entries.TryRemove(key, out _);
                return Task.FromResult(true);
            }
            var changed = false;
            _entries.AddOrUpdate(key,
                _ =>
                {
                    changed = true;
                    return new BanEntry
                    {
                        Key = key,
                        UnbanAt = unbanAt,
                        CreatedAt = DateTimeOffset.UtcNow
                    };
                },
                (_, old) =>
                {
                    changed = true;
                    return new BanEntry
                    {
                        Key = key,
                        UnbanAt = unbanAt,
                        Reason = old.Reason,
                        CreatedAt = old.CreatedAt
                    };
                });
            return Task.FromResult(changed);
        }

        /// <inheritdoc />
        public Task<bool> UnbanAsync(string key, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(!string.IsNullOrEmpty(key) && _entries.TryRemove(key, out _));
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<BanEntry>> GetBansAsync(CancellationToken cancellationToken = default)
        {
            Sweep();
            var now = DateTimeOffset.UtcNow;
            IReadOnlyList<BanEntry> result = _entries.Values
                .Where(x => x.UnbanAt > now)
                .Select(x => new BanEntry
                {
                    Key = x.Key,
                    UnbanAt = x.UnbanAt,
                    Reason = x.Reason,
                    CreatedAt = x.CreatedAt
                })
                .ToArray();
            return Task.FromResult(result);
        }

        private void Sweep()
        {
            var now = DateTimeOffset.UtcNow.Ticks;
            if (now - Interlocked.Read(ref _lastSweepTicks) < TimeSpan.TicksPerMinute)
            {
                return;
            }
            Interlocked.Exchange(ref _lastSweepTicks, now);
            foreach (var item in _entries)
            {
                if (item.Value.UnbanAt.UtcTicks <= now)
                {
                    _entries.TryRemove(item.Key, out _);
                }
            }
        }
    }

    /// <summary>
    /// 动态封禁公共入口。业务代码可直接调用；限流中间件在静态黑名单之后检查这里的条目。
    /// 同步方法是对异步方法的薄封装，Redis 存储下会短暂阻塞当前线程（现有 RedisClient 为同步实现）。
    /// </summary>
    public static class RateLimitControl
    {
        private static IRateLimitBanStore _store = new InMemoryRateLimitBanStore();

        /// <summary>替换封禁存储；传 null 恢复进程内默认实现</summary>
        /// <param name="store">新的存储实现</param>
        public static void UseStore(IRateLimitBanStore? store)
        {
            Interlocked.Exchange(ref _store, store ?? new InMemoryRateLimitBanStore());
        }

        /// <summary>当前封禁存储</summary>
        public static IRateLimitBanStore Store => Volatile.Read(ref _store);

        /// <summary>封禁指定限流键，unbanAt 为 UTC 绝对解封时间</summary>
        /// <param name="key">限流键</param>
        /// <param name="unbanAt">UTC 绝对解封时间；早于当前时间等价于解封</param>
        /// <param name="reason">封禁原因</param>
        /// <returns>是否写入成功</returns>
        public static bool Ban(string key, DateTimeOffset unbanAt, string? reason = null)
            => BanAsync(key, unbanAt, reason).GetAwaiter().GetResult();

        /// <summary>封禁指定限流键，duration 为相对时长</summary>
        /// <param name="key">限流键</param>
        /// <param name="duration">相对封禁时长</param>
        /// <param name="reason">封禁原因</param>
        /// <returns>是否写入成功</returns>
        public static bool Ban(string key, TimeSpan duration, string? reason = null)
            => Ban(key, DateTimeOffset.UtcNow + duration, reason);

        /// <summary>异步封禁（绝对解封时间）</summary>
        /// <param name="key">限流键</param>
        /// <param name="unbanAt">UTC 绝对解封时间</param>
        /// <param name="reason">封禁原因</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否写入成功</returns>
        public static Task<bool> BanAsync(string key, DateTimeOffset unbanAt, string? reason = null, CancellationToken cancellationToken = default)
            => SafeCallAsync(() => Volatile.Read(ref _store).BanAsync(key, unbanAt, reason, cancellationToken), false);

        /// <summary>修改已封禁对象的解封时间，保留原原因；未封禁时新建条目</summary>
        /// <param name="key">限流键</param>
        /// <param name="unbanAt">UTC 绝对解封时间</param>
        /// <returns>是否修改成功</returns>
        public static bool SetUnbanAt(string key, DateTimeOffset unbanAt)
            => SetUnbanAtAsync(key, unbanAt).GetAwaiter().GetResult();

        /// <summary>异步修改解封时间</summary>
        /// <param name="key">限流键</param>
        /// <param name="unbanAt">UTC 绝对解封时间</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否修改成功</returns>
        public static Task<bool> SetUnbanAtAsync(string key, DateTimeOffset unbanAt, CancellationToken cancellationToken = default)
            => SafeCallAsync(() => Volatile.Read(ref _store).SetUnbanAtAsync(key, unbanAt, cancellationToken), false);

        /// <summary>立即解封</summary>
        /// <param name="key">限流键</param>
        /// <returns>是否确实移除了条目</returns>
        public static bool Unban(string key)
            => UnbanAsync(key).GetAwaiter().GetResult();

        /// <summary>异步立即解封</summary>
        /// <param name="key">限流键</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否确实移除了条目</returns>
        public static Task<bool> UnbanAsync(string key, CancellationToken cancellationToken = default)
            => SafeCallAsync(() => Volatile.Read(ref _store).UnbanAsync(key, cancellationToken), false);

        /// <summary>是否处于封禁状态</summary>
        /// <param name="key">限流键</param>
        /// <returns>true 表示当前被封禁</returns>
        public static bool IsBanned(string key)
            => IsBannedAsync(key).GetAwaiter().GetResult();

        /// <summary>异步查询封禁状态</summary>
        /// <param name="key">限流键</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>true 表示当前被封禁</returns>
        public static async Task<bool> IsBannedAsync(string key, CancellationToken cancellationToken = default)
        {
            var entry = await SafeCallAsync(() => Volatile.Read(ref _store).GetAsync(key, cancellationToken), null).ConfigureAwait(false);
            return entry != null && entry.UnbanAt > DateTimeOffset.UtcNow;
        }

        /// <summary>查询解封时间；未封禁返回 null</summary>
        /// <param name="key">限流键</param>
        /// <returns>UTC 解封时间或 null</returns>
        public static DateTimeOffset? GetUnbanAt(string key)
            => GetUnbanAtAsync(key).GetAwaiter().GetResult();

        /// <summary>异步查询解封时间</summary>
        /// <param name="key">限流键</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>UTC 解封时间或 null</returns>
        public static async Task<DateTimeOffset?> GetUnbanAtAsync(string key, CancellationToken cancellationToken = default)
        {
            var entry = await SafeCallAsync(() => Volatile.Read(ref _store).GetAsync(key, cancellationToken), null).ConfigureAwait(false);
            return entry?.UnbanAt;
        }

        /// <summary>查询封禁条目</summary>
        /// <param name="key">限流键</param>
        /// <returns>封禁条目或 null</returns>
        public static BanEntry? GetBan(string key)
            => GetBanAsync(key).GetAwaiter().GetResult();

        /// <summary>异步查询封禁条目</summary>
        /// <param name="key">限流键</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>封禁条目或 null</returns>
        public static Task<BanEntry?> GetBanAsync(string key, CancellationToken cancellationToken = default)
            => SafeCallAsync(() => Volatile.Read(ref _store).GetAsync(key, cancellationToken), null);

        /// <summary>列出当前有效封禁（诊断用）</summary>
        /// <returns>封禁条目快照</returns>
        public static IReadOnlyList<BanEntry> GetBans()
            => GetBansAsync().GetAwaiter().GetResult();

        /// <summary>异步列出当前有效封禁</summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>封禁条目快照</returns>
        public static Task<IReadOnlyList<BanEntry>> GetBansAsync(CancellationToken cancellationToken = default)
            => SafeCallAsync(() => Volatile.Read(ref _store).GetBansAsync(cancellationToken), Array.Empty<BanEntry>());

        private static async Task<T> SafeCallAsync<T>(Func<Task<T>> call, T fallback)
        {
            try
            {
                return await call().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // 限流/封禁是旁路能力：存储异常不得影响主链路
                Loger.Warn("RateLimit ban store error: " + ex.Message);
                return fallback;
            }
        }
    }
}
