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

namespace Wlniao.Caching
{
    /// <summary>
    /// 缓存管理
    /// </summary>
    public class Cache
    {
        private static CacheType ctype = CacheType.None;
        internal static CacheType cType
        {
            get
            {
                if (ctype == CacheType.None)
                {
                    try
                    {
                        ctype = CacheType.InMemory;
                        if (Redis.CanUse())
                        {
                            ctype = CacheType.Redis;
                        }
                    }
                    catch
                    { }
                }
                return ctype;
            }
        }
        /// <summary>
        /// 启用Redis作为缓存机制
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="pass"></param>
        public static void UseRedis(string host, int port = 6379, string pass = "")
        {
            ctype = CacheType.Redis;
            if (!string.IsNullOrEmpty(host))
            {
                if (string.IsNullOrEmpty(Config.GetConfigs("WLN_REDIS_HOST")))
                {
                    Config.SetConfigs("WLN_REDIS_HOST", host);
                }
                if (Config.GetConfigs("WLN_REDIS_PASS") != pass)
                {
                    Config.SetConfigs("WLN_REDIS_PASS", pass);
                }
                Redis._host = host;
                Redis._port = port;
                Redis._pass = pass;
            }
        }

        /// <summary>
        /// 启用InMemory作为缓存机制
        /// </summary>
        public static void UseInMemory()
        {
            ctype = CacheType.InMemory;
        }
        /// <summary>
        /// 缓存一个字符串
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expireSeconds">缓存过期时间（秒）</param>
        /// <returns></returns>
        public static Boolean Set(String key, String value, int expireSeconds = 86400)
        {
            if (cType == CacheType.Redis)
            {
                return Redis.Set(key, value, expireSeconds);
            }
            else
            {
                return InMemory.Set(key, value, expireSeconds);
            }
        }

        /// <summary>
        /// 缓存一个对象实例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">缓存的Key值</param>
        /// <param name="obj">要缓存的对象</param>
        /// <param name="expireSeconds">缓存过期时间（秒）</param>
        /// <returns></returns>
        public static Boolean Set<T>(String key, T obj, int expireSeconds = 86400)
        {
            if (cType == CacheType.Redis)
            {
                return Redis.Set<T>(key, obj, expireSeconds);
            }
            else
            {
                return InMemory.Set<T>(key, obj, expireSeconds);
            }
        }

        /// <summary>
        /// 删除缓存内容
        /// </summary>
        /// <param name="key"></param>
        public static Boolean Del(String key)
        {
            if (cType == CacheType.Redis)
            {
                return Redis.Del(key);
            }
            else
            {
                return InMemory.Del(key);
            }
        }

        /// <summary>
        /// 判断是否存在缓存项
        /// </summary>
        /// <param name="key"></param>
        public static Boolean Exists(String key)
        {
            if (cType == CacheType.Redis)
            {
                return Redis.Exists(key);
            }
            else
            {
                return InMemory.Exists(key);
            }
        }

        /// <summary>
        /// 获取一个缓存项
        /// </summary>
        /// <param name="key"></param>
        public static String Get(String key)
        {
            if (cType == CacheType.Redis)
            {
                return Redis.Get(key);
            }
            else
            {
                return InMemory.Get(key);
            }
        }
        /// <summary>
        /// 获取一个缓存项（允许null）
        /// </summary>
        /// <param name="key"></param>
        public static String GetAllowNull(String key)
        {
            if (cType == CacheType.Redis)
            {
                return Redis.GetAllowNull(key);
            }
            else
            {
                return InMemory.GetAllowNull(key);
            }
        }
        /// <summary>
        /// 获取一个缓存项
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public static T Get<T>(String key)
        {
            if (cType == CacheType.Redis)
            {
                return Redis.Get<T>(key);
            }
            else
            {
                return InMemory.Get<T>(key);
            }
        }


        private static string cachePath = null;
        /// <summary>
        /// 
        /// </summary>
        public static string CachePath
        {
            get
            {
                return cachePath;
            }
            set
            {
                cachePath = IO.PathTool.Map(value.TrimEnd('/').TrimEnd('\\') + "/");
                if (System.IO.Directory.Exists(cachePath))
                {
                    var t = DateTime.Now.AddDays(-1);
                    foreach (var path in System.IO.Directory.GetFiles(cachePath))
                    {
                        try
                        {
                            var fi = new System.IO.FileInfo(path);
                            if (fi.LastWriteTime < t)
                            {
                                fi.Delete();
                            }
                        }
                        catch { }
                    }
                }
                else
                {
                    System.IO.Directory.CreateDirectory(cachePath);
                }
            }
        }

        /// <summary>
        /// 缓存一个字符串
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Boolean LocalSet(String key, String value)
        {
            if (cachePath == null)
            {
                return false;
            }
            try
            {
                file.Write(CachePath + key, value);
                return true;
            }
            catch { return false; }
        }
        /// <summary>
        /// 获取一个缓存项
        /// </summary>
        /// <param name="key"></param>
        /// <param name="expireSeconds">缓存过期时间（秒）</param>
        public static String LocalGet(String key, int expireSeconds = 86400)
        {
            if (cachePath == null)
            {
                throw new Exception("");
            }
            var fileinfo = new System.IO.FileInfo(CachePath + key);
            if (fileinfo.Exists && fileinfo.LastWriteTime.AddSeconds(expireSeconds) > DateTime.Now)
            {
                return file.Read(fileinfo.FullName);
            }
            return "";
        }
    }
}
