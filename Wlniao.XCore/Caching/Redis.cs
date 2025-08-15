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
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Wlniao.Log;
using Wlniao.Net;
using Wlniao.Runtime;

namespace Wlniao.Caching
{
    /// <summary>
    /// Redis缓存
    /// </summary>
    public class Redis
    {
        private static int _canuse = 0;
        private static object _lock = new { };
        private static string _connstr = null;
        private static RedisClient _instance = null;
        /// <summary>
        /// 使用的数据库序号
        /// </summary>
        public static int Select = Convert.ToInt(Config.GetConfigs("WLN_REDIS_DB"));
        /// <summary>
        /// 判断Redis缓存是否可用
        /// </summary>
        internal static bool CanUse
        {
            get
            {
                if (_canuse > 0)
                {
                    return _canuse > 0;
                }
                else if (_canuse == 0 && Instance == null)
                {
                    return _canuse > 0;
                }
                else
                {
                    return false;
                }
            }
        }
        /// <summary>
        /// 下次尝试链接时间
        /// </summary>
        private static DateTime nextconnect = DateTime.MinValue;
        /// <summary>
        /// Redis链接字符串
        /// </summary>
        public static string ConnStr
        {
            get
            {
                lock (_lock)
                {
                    if (_connstr == null)
                    {
                        var connstr = Config.GetConfigs("WLN_REDIS");
                        if (string.IsNullOrEmpty(connstr))
                        {
                            var host = Config.GetConfigs("WLN_REDIS_HOST");
                            var pass = Config.GetEncrypt("WLN_REDIS_PASS", Config.Secret);
                            var user = Config.GetEncrypt("WLN_REDIS_USER", Config.Secret);
                            var port = Convert.ToInt(Config.GetConfigs("WLN_REDIS_PORT", "6379"));
                            if (port > 0 && port < 65535 && !string.IsNullOrEmpty(host))
                            {
                                connstr = host + ":" + port;
                                if (!string.IsNullOrEmpty(pass))
                                {
                                    connstr += ",password=" + pass;
                                }
                                if (!string.IsNullOrEmpty(user))
                                {
                                    connstr += ",username=" + user;
                                }
                            }
                        }
                        _connstr = connstr ?? "";
                    }
                }
                return _connstr;
            }
        }
        /// <summary>
        /// 数据库链接字符串
        /// </summary>
        public static RedisClient Instance
        {
            get
            {
                if(_instance == null)
                {
                    try
                    {
                        _instance = UseConnStr(ConnStr);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Redis connection configuration error: " + ex.Message);
                    }
                }
                return _instance;
            }
            set
            {
                _instance = value;
                _canuse = value == null || value.EndPointList.Count == 0 ? -1 : 1;
            }
        }
        /// <summary>
        /// 应用连接字符串
        /// </summary>
        /// <param name="connstr"></param>
        internal static RedisClient UseConnStr(string connstr)
        {
            if (string.IsNullOrEmpty(connstr))
            {
                return null;
            }
            var args = connstr.Split(',', StringSplitOptions.RemoveEmptyEntries);
            var instance = new RedisClient { SelectDB = Select };
            foreach (var item in args)
            {
                var arg = item.Trim();
                if (arg.StartsWith("password="))
                {
                    instance.Password = arg.Substring(9);
                }
                else if (arg.StartsWith("username="))
                {
                    instance.Username = arg.Substring(9);
                }
                else
                {
                    _canuse = 1;
                    var ips = arg.Split(':', StringSplitOptions.RemoveEmptyEntries);
                    if (ips.Length == 2 && int.TryParse(ips[1], out var port))
                    {
                        instance.AddEndPoint(new DnsEndPoint(ips[0], port));
                    }
                    else
                    {
                        instance.AddEndPoint(new DnsEndPoint(ips[0], 6379));
                    }
                }
            }
            return instance;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="usePool"></param>
        /// <returns></returns>
        public static string Get(string key, bool usePool = false)
        {
            try
            {
                if (usePool)
                {
                    return Instance.Get(key);
                }
                else
                {
                    return UseConnStr(ConnStr).Get(key);
                }
            }
            catch (Exception ex)
            {
                Loger.Topic("xcore", "Caching.Redis.Get => " + ex.Message, Log.LogLevel.Error);
            }
            return "";
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public static T Get<T>(string key)
        {
            var str = Get(key);
            if (!string.IsNullOrEmpty(str))
            {
                return JsonSerializer.Deserialize<T>(str, new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) //Json序列化的时候对中文进行处理
                });
            }
            return default(T);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expire"></param>
        public static bool Set(string key, byte[] value, int expire)
        {
            try
            {
                return Instance.Set(key, value, expire);
            }
            catch (XCoreException ex)
            {
                Loger.Topic("xcore", ex.Message, Log.LogLevel.Error);
            }
            catch (Exception ex)
            {
                Loger.Topic("xcore", "Caching.Redis.Set => " + ex.Message, Log.LogLevel.Error);
            }
            return false;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expire"></param>
        public static bool Set(string key, string value, int expire)
        {
            return Set(key, Encoding.UTF8.GetBytes(value), expire);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="obj"></param>
        /// <param name="expire"></param>
        /// <returns></returns>
        public static bool Set<T>(string key, T obj, int expire)
        {
            return Set(key, JsonSerializer.Serialize(obj, new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) //Json序列化的时候对中文进行处理
            }), expire);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        public static bool KeyDelete(string key)
        {
            try
            {
                return Instance.KeyDelete(key);
            }
            catch (XCoreException ex)
            {
                Loger.Topic("xcore", ex.Message, Log.LogLevel.Error);
            }
            catch (Exception ex)
            {
                Loger.Topic("xcore", "Caching.Redis.KeyDelete => " + ex.Message, Log.LogLevel.Error);
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        public static bool KeyExists(string key)
        {
            try
            {
                return Instance.KeyExists(key);
            }
            catch (XCoreException ex)
            {
                Loger.Topic("xcore", ex.Message, Log.LogLevel.Error);
            }
            catch (Exception ex)
            {
                Loger.Topic("xcore", "Caching.Redis.KeyExists => " + ex.Message, Log.LogLevel.Error);
            }
            return false;


        }



    }

}