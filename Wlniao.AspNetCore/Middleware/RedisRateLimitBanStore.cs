using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Wlniao.Caching;
using Wlniao.Log;
using Wlniao.Net;

namespace Wlniao.Middleware
{
    /// <summary>
    /// 基于现有 WLN_REDIS_* / WLN_REDIS 配置的分布式封禁存储。
    /// 复用 Wlniao.Caching.Redis 的连接池与 SelectDb，不引入新的配置项或第三方依赖。
    /// 每个封禁条目写入一个带 TTL 的 Redis String：key = prefix + Escape(key)，
    /// value = createdMs|unbanAtMs|escapedReason，TTL 取整秒（最多晚 1 秒解封）。
    /// </summary>
    public sealed class RedisRateLimitBanStore : IRateLimitBanStore
    {
        /// <summary>默认 Redis 键前缀</summary>
        public const string DefaultKeyPrefix = "wlniao:ratelimit:ban:";

        private readonly string _keyPrefix;
        private readonly ConcurrentDictionary<string, byte> _knownKeys = new(StringComparer.OrdinalIgnoreCase);
        private const int MaxSnapshotKeys = 1000;
        private static int _warned;

        /// <summary>创建分布式封禁存储</summary>
        /// <param name="keyPrefix">Redis 键前缀，传空使用默认值</param>
        public RedisRateLimitBanStore(string keyPrefix = DefaultKeyPrefix)
        {
            _keyPrefix = string.IsNullOrEmpty(keyPrefix) ? DefaultKeyPrefix : keyPrefix;
        }

        /// <inheritdoc />
        public bool IsAvailable => !string.IsNullOrEmpty(Redis.ConnStr);

        /// <inheritdoc />
        public async Task<BanEntry?> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }
            cancellationToken.ThrowIfCancellationRequested();
            var client = GetClient();
            if (client == null)
            {
                return null;
            }
            var raw = client.Get(FullKey(key));
            if (string.IsNullOrEmpty(raw))
            {
                _knownKeys.TryRemove(key, out _);
                return null;
            }
            var parts = raw.Split('|', 3);
            if (parts.Length != 3 || !long.TryParse(parts[0], out var createdMs) || !long.TryParse(parts[1], out var unbanMs))
            {
                // 非法值按未封禁处理，并清理避免反复解析
                await UnbanAsync(key, cancellationToken).ConfigureAwait(false);
                return null;
            }
            var unbanAt = DateTimeOffset.FromUnixTimeMilliseconds(unbanMs);
            if (unbanAt <= DateTimeOffset.UtcNow)
            {
                await UnbanAsync(key, cancellationToken).ConfigureAwait(false);
                return null;
            }
            return new BanEntry
            {
                Key = key,
                CreatedAt = DateTimeOffset.FromUnixTimeMilliseconds(createdMs),
                UnbanAt = unbanAt,
                Reason = parts[2].Length == 0 ? null : Uri.UnescapeDataString(parts[2])
            };
        }

        /// <inheritdoc />
        public Task<bool> BanAsync(string key, DateTimeOffset unbanAt, string? reason, CancellationToken cancellationToken = default)
        {
            return WriteAsync(key, unbanAt, reason, DateTimeOffset.UtcNow, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<bool> SetUnbanAtAsync(string key, DateTimeOffset unbanAt, CancellationToken cancellationToken = default)
        {
            var existing = await GetAsync(key, cancellationToken).ConfigureAwait(false);
            return await WriteAsync(key, unbanAt, existing?.Reason, existing?.CreatedAt ?? DateTimeOffset.UtcNow, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task<bool> UnbanAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key))
            {
                return Task.FromResult(false);
            }
            cancellationToken.ThrowIfCancellationRequested();
            var client = GetClient();
            if (client == null)
            {
                return Task.FromResult(false);
            }
            var ok = client.KeyDelete(FullKey(key));
            if (ok)
            {
                _knownKeys.TryRemove(key, out _);
            }
            return Task.FromResult(ok);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<BanEntry>> GetBansAsync(CancellationToken cancellationToken = default)
        {
            var result = new List<BanEntry>();
            foreach (var key in _knownKeys.Keys.Take(MaxSnapshotKeys))
            {
                var entry = await GetAsync(key, cancellationToken).ConfigureAwait(false);
                if (entry != null)
                {
                    result.Add(entry);
                }
            }
            return result;
        }

        private async Task<bool> WriteAsync(string key, DateTimeOffset unbanAt, string? reason, DateTimeOffset createdAt, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }
            cancellationToken.ThrowIfCancellationRequested();
            if (unbanAt <= DateTimeOffset.UtcNow)
            {
                return await UnbanAsync(key, cancellationToken).ConfigureAwait(false);
            }
            var client = GetClient();
            if (client == null)
            {
                return false;
            }
            var deltaSeconds = (unbanAt - DateTimeOffset.UtcNow).TotalSeconds;
            if (deltaSeconds <= 0)
            {
                return await UnbanAsync(key, cancellationToken).ConfigureAwait(false);
            }
            var ttl = deltaSeconds >= int.MaxValue ? int.MaxValue : (int)Math.Ceiling(deltaSeconds);
            var value = string.Concat(
                createdAt.ToUnixTimeMilliseconds().ToString(),
                "|",
                unbanAt.ToUnixTimeMilliseconds().ToString(),
                "|",
                Uri.EscapeDataString(reason ?? ""));
            var ok = client.Set(FullKey(key), Encoding.UTF8.GetBytes(value), Math.Min(ttl, int.MaxValue));
            if (ok)
            {
                _knownKeys.TryAdd(key, 0);
            }
            return ok;
        }

        private string FullKey(string key) => _keyPrefix + Uri.EscapeDataString(key);

        private static RedisClient? GetClient()
        {
            try
            {
                if (string.IsNullOrEmpty(Redis.ConnStr))
                {
                    return null;
                }
                return Redis.Instance;
            }
            catch (Exception ex)
            {
                if (Interlocked.Exchange(ref _warned, 1) == 0)
                {
                    Loger.Warn("RedisRateLimitBanStore unavailable: " + ex.Message);
                }
                return null;
            }
        }
    }
}
