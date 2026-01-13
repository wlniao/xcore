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
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace Wlniao.Caching
{
    /// <summary>
    /// InMemory缓存
    /// </summary>
    public static class InMemory
    {
        private static readonly Dictionary<string, CacheData> Cache = new();


        /// <summary>
        /// 设置缓存内容
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expireSeconds"></param>
        public static bool Set(string key, string value, int expireSeconds = 86400)
        {
            if (Cache.ContainsKey(key))
            {
                Cache[key].Expire = DateTime.Now.AddSeconds(expireSeconds);
                Cache[key].Value = value ?? "";
            }
            else
            {
                var data = new CacheData
                {
                    Expire = DateTime.Now.AddSeconds(expireSeconds),
                    Value = value ?? ""
                };
                if (Cache.TryAdd(key, data)) return true;
                Cache[key].Expire = DateTime.Now.AddSeconds(expireSeconds);
                Cache[key].Value = value ?? "";
            }
            // 每100次设置操作清理一次，或者缓存大小超过8000时清理
            if (Cache.Count % 100 == 0 || Cache.Count > 8000)
            {
                CleanupExpiredAndLimitSize();
            }
            return true;
        }


        /// <summary>
        /// 设置缓存内容
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="obj"></param>
        /// <param name="expireSeconds"></param>
        /// <returns></returns>
        public static bool Set<T>(string key, T obj, int expireSeconds = 86400)
        {
            if (Cache.ContainsKey(key) || !Cache.TryAdd(key, new CacheData
                {
                    Expire = DateTime.Now.AddSeconds(expireSeconds),
                    Value = JsonSerializer.Serialize(obj, new JsonSerializerOptions
                    {
                        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) //Json序列化的时候对中文进行处理
                    })
                }))
            {
                Cache[key].Expire = DateTime.Now.AddSeconds(expireSeconds);
                Cache[key].Value = JsonSerializer.Serialize(obj, new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) //Json序列化的时候对中文进行处理
                });
            }
            // 每100次设置操作清理一次，或者缓存大小超过8000时清理
            if (Cache.Count % 100 == 0 || Cache.Count > 8000)
            {
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
            return Cache.ContainsKey(key) && Cache.Remove(key);
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
                return (from key in Cache.Keys where key.StartsWith(tmp) select Cache.Remove(key)).FirstOrDefault();
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
            if (!Cache.ContainsKey(key)) return false;
            if (Cache[key].Expire > DateTime.Now)
            {
                return true;
            }
            else
            {
                Del(key);
            }
            return false;
        }

        /// <summary>
        /// 获取一个缓存项
        /// </summary>
        /// <param name="key"></param>
        public static string Get(string key)
        {
            if (!Cache.ContainsKey(key)) return "";
            if (Cache[key].Expire > DateTime.Now)
            {
                return Cache[key].Value == null ? "" : Cache[key].Value;
            }
            else
            {
                Del(key);
            }
            return "";
        }

        /// <summary>
        /// 获取一个缓存项（允许null）
        /// </summary>
        /// <param name="key"></param>
        public static string GetAllowNull(string key)
        {
            if (!Cache.ContainsKey(key)) return null;
            if (Cache[key].Expire > DateTime.Now)
            {
                return Cache[key].Value;
            }
            else
            {
                Del(key);
            }
            return null;
        }

        /// <summary>
        /// 获取一个缓存项
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public static T Get<T>(string key)
        {
            if (!Cache.ContainsKey(key)) return default(T);
            if (Cache[key].Expire > DateTime.Now)
            {
                return JsonSerializer.Deserialize<T>(Cache[key].Value, new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) //Json序列化的时候对中文进行处理
                });
            }
            else
            {
                Del(key);
            }
            return default(T);
        }

        /// <summary>
        /// 清理过期项
        /// </summary>
        public static void CleanupExpired()
        {
            var expiredKeys = Cache.Where(x => x.Value.Expire <= DateTime.Now).Select(x => x.Key).ToList();
            foreach(var key in expiredKeys)
            {
                Del(key);
            }
        }
        
        /// <summary>
        /// 清理过期项并限制缓存大小
        /// </summary>
        /// <param name="maxCacheSize">最大缓存项数，默认10000</param>
        public static void CleanupExpiredAndLimitSize(int maxCacheSize = 10000)
        {
            // 先清理过期项
            CleanupExpired();
            
            // 如果缓存仍然过大，删除最久未使用的项
            if (Cache.Count > maxCacheSize)
            {
                var sortedKeys = Cache.OrderBy(x => x.Value.Expire) // 按过期时间排序，最早过期的在前
                                  .Take(Cache.Count - maxCacheSize) // 取出超出数量的部分
                                  .Select(x => x.Key)
                                  .ToList();
                
                foreach(var key in sortedKeys)
                {
                    Del(key);
                }
            }
        }
        
        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        /// <returns>返回缓存总数和过期项数</returns>
        public static (int totalCount, int expiredCount) GetCacheStats()
        {
            var expiredCount = Cache.Count(x => x.Value.Expire <= DateTime.Now);
            return (Cache.Count, expiredCount);
        }
    }
}