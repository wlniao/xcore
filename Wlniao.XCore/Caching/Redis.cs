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
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Wlniao.Caching
{
    /// <summary>
    /// Redis缓存
    /// </summary>
    public class Redis
    {
        /// <summary>
        /// 使用的数据库序号
        /// </summary>
        public static int Select = cvt.ToInt(Config.GetConfigs("WLN_REDIS_DB"));
        private static string connstr = "";
        /// <summary>
        /// 下次尝试链接时间
        /// </summary>
        private static DateTime nextconnect = DateTime.MinValue;
        /// <summary>
        /// 
        /// </summary>
        private static ConnectionMultiplexer redis = null;

        /// <summary>
        /// 数据库链接字符串
        /// </summary>
        public static string ConnStr
        {
            get
            {
                if (string.IsNullOrEmpty(connstr))
                {
                    lock (XCore.Lock)
                    {
                        connstr = Config.GetConfigs("WLN_REDIS");
                        if (string.IsNullOrEmpty(connstr))
                        {
                            var host = Config.GetConfigs("WLN_REDIS_HOST");
                            var pass = Config.GetConfigs("WLN_REDIS_PASS");
                            var port = cvt.ToInt(Config.GetConfigs("WLN_REDIS_PORT", "6379"));
                            if (port > 0 && port < 65535 && !string.IsNullOrEmpty(host))
                            {
                                connstr = host + ":" + port;
                                if (!string.IsNullOrEmpty(pass))
                                {
                                    connstr += ",password=" + pass;
                                }
                            }
                        }
                    }
                }
                return connstr;
            }
            set
            {
                connstr = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static ConnectionMultiplexer Instance
        {
            get
            {
                try
                {
                    if (redis == null && nextconnect < DateTime.Now)
                    {
                        nextconnect = DateTime.Now.AddSeconds(1);
                        redis = ConnectionMultiplexer.Connect(ConnStr);
                    }
                }
                catch
                {
                    log.Warn("Redis connect error");
                }
                return redis;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static IDatabase Database
        {
            get
            {
                try
                {
                    if (redis == null && nextconnect < DateTime.Now)
                    {
                        nextconnect = DateTime.Now.AddSeconds(10);
                        redis = ConnectionMultiplexer.Connect(ConnStr);
                    }
                    if (redis != null)
                    {
                        return redis.GetDatabase(Select);
                    }
                }
                catch
                {
                    redis = null;
                    log.Warn("Redis connect error");
                }
                return null;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static ISubscriber Subscriber
        {
            get
            {
                try
                {
                    if (redis == null && nextconnect < DateTime.Now)
                    {
                        nextconnect = DateTime.Now.AddSeconds(10);
                        redis = StackExchange.Redis.ConnectionMultiplexer.Connect(ConnStr);
                    }
                    if (redis != null)
                    {
                        return redis.GetSubscriber();
                    }
                }
                catch
                {
                    redis = null;
                    log.Warn("Redis connect error");
                }
                return null;
            }
        }
        /// <summary>
        /// 重新链接
        /// </summary>
        public static ConnectionMultiplexer Reconnect()
        {
            redis = null;
            nextconnect = DateTime.MinValue;
            return Instance;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string Get(string key)
        {
            try
            {
                if (redis == null && nextconnect < DateTime.Now)
                {
                    nextconnect = DateTime.Now.AddSeconds(60);
                    redis = StackExchange.Redis.ConnectionMultiplexer.Connect(ConnStr);
                }
                if (redis != null)
                {
                    var val = redis.GetDatabase(Select).StringGet(key);
                    if (val.HasValue && !val.IsNullOrEmpty)
                    {
                        return val.ToString();
                    }
                }
            }
            catch
            {
                redis = null;
                log.Warn("Redis connect error");
            }
            return "";
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public static T Get<T>(String key)
        {
            var str = Get(key);
            if (!string.IsNullOrEmpty(str))
            {
                return Json.ToObject<T>(str);
            }
            return default(T);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expire"></param>
        public static Boolean Set(String key, String value, Int32 expire)
        {
            try
            {
                if (redis == null && nextconnect < DateTime.Now)
                {
                    nextconnect = DateTime.Now.AddSeconds(60);
                    redis = StackExchange.Redis.ConnectionMultiplexer.Connect(ConnStr);
                }
                if (redis != null)
                {
                    return redis.GetDatabase(Select).StringSet(key, value, TimeSpan.FromSeconds(expire));
                }
            }
            catch
            {
                redis = null;
                log.Warn("Redis connect error");
            }
            return false;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="obj"></param>
        /// <param name="expire"></param>
        /// <returns></returns>
        public static Boolean Set<T>(String key, T obj, Int32 expire)
        {
            return Set(key, Json.ToString(obj), expire);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        public static Boolean KeyDelete(String key)
        {
            try
            {
                if (redis == null && nextconnect < DateTime.Now)
                {
                    nextconnect = DateTime.Now.AddSeconds(60);
                    redis = ConnectionMultiplexer.Connect(ConnStr);
                }
                if (redis != null)
                {
                    return redis.GetDatabase(Select).KeyDelete(key);
                }
            }
            catch
            {
                redis = null;
                log.Warn("Redis connect error");
            }
            return false;
        }

        /// <summary>
        /// 批量删除
        /// </summary>
        /// <param name="keys"></param>
        public static Boolean RangeDelete(String keys)
        {
            try
            {
                if (redis == null && nextconnect < DateTime.Now)
                {
                    nextconnect = DateTime.Now.AddSeconds(60);
                    redis = ConnectionMultiplexer.Connect(ConnStr);
                }
                if (redis != null)
                {
                    return redis.GetDatabase(Select).KeyDelete(keys + "*");
                }
            }
            catch
            {
                redis = null;
                log.Warn("Redis connect error");
            }
            return false;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        public static Boolean KeyExists(String key)
        {
            try
            {
                if (redis == null && nextconnect < DateTime.Now)
                {
                    nextconnect = DateTime.Now.AddSeconds(60);
                    redis = ConnectionMultiplexer.Connect(ConnStr);
                }
                if (redis != null)
                {
                    return redis.GetDatabase(Select).KeyExists(key);
                }
            }
            catch
            {
                redis = null;
                log.Warn("Redis connect error");
            }
            return false;
        }
    }
}