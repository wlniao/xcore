/*==============================================================================
    文件名称：Cache.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：缓存管理工具
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
using System.Threading.Tasks;
using Wlniao.IO;
using Wlniao.Log;

namespace Wlniao.Caching
{
    /// <summary>
    /// 缓存管理
    /// </summary>
    public class Cache
    {
        private static object olock = new object();
        private static CacheType ctype = CacheType.None;
        internal static CacheType cType
        {
            get
            {
                lock (olock)
                {
                    if (ctype == CacheType.None)
                    {
                        var cacheType = Config.GetConfigs("WLN_CACHE", "auto").ToLower();
                        if (cacheType == "redis" || (cacheType == "auto" && Redis.CanUse))
                        {
                            ctype = CacheType.Redis;
                        }
                        else if (cacheType.Contains("file"))
                        {
                            ctype = CacheType.InFile;
                        }
                        else if (cacheType.Contains("memory"))
                        {
                            ctype = CacheType.InMemory;
                        }
                        else
                        {
                            ctype = CacheType.InMemory;
                        }
                    }
                }
                return ctype;
            }
        }
        /// <summary>
        /// 启用Redis作为缓存机制
        /// </summary>
        /// <param name="connstr"></param>
        public static void UseRedis(string connstr = null)
        {
            ctype = CacheType.Redis;
            if (!string.IsNullOrEmpty(connstr))
            {
                Redis.UseConnStr(connstr);
            }
        }
        /// <summary>
        /// 启用Redis作为缓存机制
        /// </summary>
        /// <param name="server"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        public static void UseRedis(string server, string username, string password)
        {
            ctype = CacheType.Redis;
            if (string.IsNullOrEmpty(server))
            {
                Redis.Instance = null;
            }
            else
            {
                var connstr = server;
                if (!string.IsNullOrEmpty(username))
                {
                    connstr += ",username=" + username;
                }
                if (!string.IsNullOrEmpty(password))
                {
                    connstr += ",password=" + password;
                }
                Redis.UseConnStr(connstr);
            }
        }

        /// <summary>
        /// 缓存一个字符串
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expireSeconds">缓存过期时间（秒）</param>
        /// <returns></returns>
        public static bool Set(string key, string value, int expireSeconds = 86400)
        {
            return cType switch
            {
                CacheType.Redis => Redis.Set(key, value, expireSeconds),
                CacheType.InFile => FileCache.Set(key, value, expireSeconds),
                _ => InMemory.Set(key, value, expireSeconds)
            };
        }

        /// <summary>
        /// 缓存一个对象实例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">缓存的Key值</param>
        /// <param name="obj">要缓存的对象</param>
        /// <param name="expireSeconds">缓存过期时间（秒）</param>
        /// <returns></returns>
        public static bool Set<T>(string key, T obj, int expireSeconds = 86400)
        {
            return cType switch
            {
                CacheType.Redis => Redis.Set<T>(key, obj, expireSeconds),
                CacheType.InFile => FileCache.Set<T>(key, obj, expireSeconds),
                _ => InMemory.Set<T>(key, obj, expireSeconds)
            };
        }

        /// <summary>
        /// 删除缓存内容
        /// </summary>
        /// <param name="key"></param>
        public static bool Del(string key)
        {
            return cType switch
            {
                CacheType.Redis => Redis.KeyDelete(key),
                CacheType.InFile => FileCache.Del(key),
                _ => InMemory.Del(key)
            };
        }

        /// <summary>
        /// 判断是否存在缓存项
        /// </summary>
        /// <param name="key"></param>
        public static bool Exists(string key)
        {
            return cType switch
            {
                CacheType.Redis => Redis.KeyExists(key),
                CacheType.InFile => FileCache.Exists(key),
                _ => InMemory.Exists(key)
            };
        }

        /// <summary>
        /// 获取一个缓存项
        /// </summary>
        /// <param name="key"></param>
        public static string Get(string key)
        {
            return cType switch
            {
                CacheType.Redis => Redis.Get(key),
                CacheType.InFile => FileCache.Get(key),
                _ => InMemory.Get(key)
            };
        }
        /// <summary>
        /// 获取一个缓存项（允许null）
        /// </summary>
        /// <param name="key"></param>
        public static string GetAllowNull(string key)
        {
            return cType switch
            {
                CacheType.Redis => Redis.Get(key),
                CacheType.InFile => FileCache.Get(key),
                _ => InMemory.GetAllowNull(key)
            };
        }
        /// <summary>
        /// 获取一个缓存项
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public static T Get<T>(string key)
        {
            return cType switch
            {
                CacheType.Redis => Redis.Get<T>(key),
                CacheType.InFile => FileCache.Get<T>(key),
                _ => InMemory.Get<T>(key)
            };
        }


        private static string _cachePath = null;
        /// <summary>
        /// 
        /// </summary>
        public static string CachePath
        {
            get => _cachePath;
            set
            {
                _cachePath = IO.PathTool.Map(value.TrimEnd('/').TrimEnd('\\') + "/");
                if (System.IO.Directory.Exists(_cachePath))
                {
                    var t = DateTime.Now.AddDays(-1);
                    foreach (var path in System.IO.Directory.GetFiles(_cachePath))
                    {
                        try
                        {
                            var fi = new System.IO.FileInfo(path);
                            if (fi.LastWriteTime < t)
                            {
                                fi.Delete();
                            }
                        }
                        catch (Exception ex)
                        {
                            Loger.Error($"{ex.Message}{Environment.NewLine}{ex.StackTrace}");
                        }
                    }
                }
                else
                {
                    System.IO.Directory.CreateDirectory(_cachePath);
                }
            }
        }

        /// <summary>
        /// 缓存一个字符串
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool LocalSet(string key, string value)
        {
            if (_cachePath == null)
            {
                return false;
            }
            try
            {
                FileEx.Write(CachePath + key, value);
                return true;
            }
            catch { return false; }
        }
        /// <summary>
        /// 获取一个缓存项
        /// </summary>
        /// <param name="key"></param>
        /// <param name="expireSeconds">缓存过期时间（秒）</param>
        public static string LocalGet(string key, int expireSeconds = 86400)
        {
            if (_cachePath == null)
            {
                throw new Exception("");
            }
            var fileInfo = new System.IO.FileInfo(CachePath + key);
            if (fileInfo.Exists && fileInfo.LastWriteTime.AddSeconds(expireSeconds) > DateTime.Now)
            {
                return FileEx.Read(fileInfo.FullName);
            }
            return "";
        }
    }
}
