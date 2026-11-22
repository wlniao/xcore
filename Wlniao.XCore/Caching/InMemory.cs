/*==============================================================================
    文件名称：Redis.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：Redis驱动类
================================================================================
 
    Copyright 2015 XieChaoyi

    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at

               http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.

===============================================================================*/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Text.Json.Serialization.Metadata;

namespace Wlniao.Caching
{
    /// <summary>
    /// InMemory缓存
    /// </summary>
    public static class InMemory
    {
        /// <summary>
        /// 内部缓存条目：Value/Expire 只读，读路径仅用 Interlocked 更新 LRU 时间戳。
        /// 不复用公开的可变 CacheData，避免 AddOrUpdate 回调里改写共享实例造成数据竞争。
        /// </summary>
        private sealed class MemoryCacheEntry
        {
            public readonly string Value;
            public readonly DateTime Expire;
            private long _lastAccessTicks;
            public long LastAccessTicks => Interlocked.Read(ref _lastAccessTicks);

            public MemoryCacheEntry(string value, DateTime expire, DateTime lastAccess)
            {
                Value = value;
                Expire = expire;
                Interlocked.Exchange(ref _lastAccessTicks, lastAccess.Ticks);
            }

            public void Touch(DateTime now) => Interlocked.Exchange(ref _lastAccessTicks, now.Ticks);
        }

        private static readonly ConcurrentDictionary<string, MemoryCacheEntry> Cache = new();
        private static int setOps;
        private static readonly object cleanupLock = new();

        /// <summary>
        /// 仅当条目仍是读到的那个实例时才删除，避免过期清理误删已被 Set 覆盖的新值
        /// </summary>
        private static bool TryRemoveIfSame(string key, MemoryCacheEntry entry)
        {
            return ((ICollection<KeyValuePair<string, MemoryCacheEntry>>)Cache)
                .Remove(new KeyValuePair<string, MemoryCacheEntry>(key, entry));
        }


        /// <summary>
        /// 设置缓存内容
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expireSeconds"></param>
        public static bool Set(string key, string value, int expireSeconds = 86400)
        {
            var now = DateTime.UtcNow;
            var expire = now.AddSeconds(expireSeconds);
            Cache.AddOrUpdate(key, new MemoryCacheEntry(value ?? "", expire, now), (k, old) => new MemoryCacheEntry(value ?? "", expire, now));

            // 每 100 次 Set 触发一次抽样清理（ConcurrentDictionary.Count 为 O(n)，不能在热路径上判断）
            if (System.Threading.Interlocked.Increment(ref setOps) >= 100)
            {
                System.Threading.Interlocked.Exchange(ref setOps, 0);
                CleanupExpiredAndLimitSize();
            }
            return true;
        }


        /// <summary>
        /// 设置缓存内容（AOT 安全：通过源生成的 JsonTypeInfo&lt;T&gt; 序列化）
        /// </summary>
        /// <typeparam name="T">缓存值类型</typeparam>
        /// <param name="key"></param>
        /// <param name="obj">要缓存的对象</param>
        /// <param name="typeInfo">源生成的 JSON 元数据</param>
        /// <param name="expireSeconds"></param>
        /// <returns></returns>
        public static bool Set<T>(string key, T obj, JsonTypeInfo<T> typeInfo, int expireSeconds = 86400)
        {
            var now = DateTime.UtcNow;
            var expire = now.AddSeconds(expireSeconds);
            var value = JsonSerializer.Serialize(obj, typeInfo);
            Cache.AddOrUpdate(key, new MemoryCacheEntry(value, expire, now), (k, old) => new MemoryCacheEntry(value, expire, now));

            // 每 100 次 Set 触发一次抽样清理（ConcurrentDictionary.Count 为 O(n)，不能在热路径上判断）
            if (System.Threading.Interlocked.Increment(ref setOps) >= 100)
            {
                System.Threading.Interlocked.Exchange(ref setOps, 0);
                CleanupExpiredAndLimitSize();
            }
            return true;
        }



        /// <summary>
        /// 删除缓存内容
        /// </summary>
        /// <param name="key"></param>
        public static bool Del(string key)
        {
            return Cache.TryRemove(key, out _);
        }

        /// <summary>
        /// 删除缓存内容
        /// </summary>
        /// <param name="keys"></param>
        public static bool RangeDelete(string keys)
        {
            if (string.IsNullOrEmpty(keys))
            {
                return false;
            }
            else if (keys.EndsWith('*'))
            {
                var tmp = keys.TrimEnd('*');
                var removed = 0;
                foreach (var key in Cache.Keys)
                {
                    if (key.StartsWith(tmp) && Cache.TryRemove(key, out _))
                    {
                        removed++;
                    }
                }
                return removed > 0;
            }
            else
            {
                return Del(keys);
            }
        }

        /// <summary>
        /// 判断是否存在缓存项
        /// </summary>
        /// <param name="key"></param>
        public static bool Exists(string key)
        {
            if (!Cache.TryGetValue(key, out var val))
            {
                return false;
            }
            if (val.Expire > DateTime.UtcNow)
            {
                return true;
            }
            TryRemoveIfSame(key, val);
            return false;
        }

        /// <summary>
        /// 获取一个缓存项
        /// </summary>
        /// <param name="key"></param>
        public static string Get(string key)
        {
            if(Cache.TryGetValue(key, out var val))
            {
                if (val.Expire > DateTime.UtcNow)
                {
                    val.Touch(DateTime.UtcNow);
                    return val.Value ?? "";
                }
                else
                {
                    TryRemoveIfSame(key, val);
                }
            }
            return "";
        }

        /// <summary>
        /// 获取一个缓存项（允许null）
        /// </summary>
        /// <param name="key"></param>
        public static string GetAllowNull(string key)
        {
            if(Cache.TryGetValue(key, out var val))
            {
                if (val.Expire > DateTime.UtcNow)
                {
                    val.Touch(DateTime.UtcNow);
                    return val.Value;
                }
                else
                {
                    TryRemoveIfSame(key, val);
                }
            }
            return null;
        }


        /// <summary>
        /// 获取一个缓存项（AOT 安全：通过源生成的 JsonTypeInfo&lt;T&gt; 反序列化）
        /// </summary>
        /// <typeparam name="T">缓存值类型</typeparam>
        /// <param name="key"></param>
        /// <param name="typeInfo">源生成的 JSON 元数据</param>
        /// <returns></returns>
        public static T Get<T>(string key, JsonTypeInfo<T> typeInfo)
        {
            if(Cache.TryGetValue(key, out var val))
            {
                if (val.Expire > DateTime.UtcNow)
                {
                    val.Touch(DateTime.UtcNow);
                    return JsonSerializer.Deserialize(val.Value, typeInfo);
                }
                else
                {
                    TryRemoveIfSame(key, val);
                }
            }
            return default(T);
        }

        /// <summary>
        /// 清理过期项
        /// </summary>
        public static void CleanupExpired()
        {
            var now = DateTime.UtcNow;
            foreach (var item in Cache.Where(x => x.Value.Expire <= now))
            {
                TryRemoveIfSame(item.Key, item.Value);
            }
        }
        
        /// <summary>
        /// 清理过期项并限制缓存大小
        /// </summary>
        /// <param name="maxCacheSize">最大缓存项数，默认10000</param>
        public static void CleanupExpiredAndLimitSize(int maxCacheSize = 10000)
        {
            // 避免并发清理叠加放大
            if (!System.Threading.Monitor.TryEnter(cleanupLock))
            {
                return;
            }
            try
            {
                CleanupExpired();

                // 超限时按最近访问时间做近似 LRU 淘汰；仅在清理路径执行
                var count = Cache.Count;
                if (count > maxCacheSize)
                {
                    var excess = count - maxCacheSize;
                    foreach (var item in Cache.OrderBy(x => x.Value.LastAccessTicks))
                    {
                        if (excess <= 0)
                        {
                            break;
                        }
                        if (TryRemoveIfSame(item.Key, item.Value))
                        {
                            excess--;
                        }
                    }
                }
            }
            finally
            {
                System.Threading.Monitor.Exit(cleanupLock);
            }
        }
        
        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        /// <returns>返回缓存总数和过期项数</returns>
        public static (int totalCount, int expiredCount) GetCacheStats()
        {
            var expiredCount = Cache.Count(x => x.Value.Expire <= DateTime.UtcNow);
            return (Cache.Count, expiredCount);
        }
    }
}