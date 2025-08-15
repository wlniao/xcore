/*==============================================================================
    文件名称：FileCache.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：文件缓存系统驱动类
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
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Wlniao.IO;
using Wlniao.Text;

namespace Wlniao.Caching
{
    /// <summary>
    /// FileCache缓存
    /// </summary>
    public class FileCache
    {
        private static string path = null;
        /// <summary>
        /// 缓存文件目录
        /// </summary>
        internal static string CachePath
        {
            get
            {
                if (path == null)
                {
                    path = Config.GetSetting("WLN_CACHE_PATH");
                    if (string.IsNullOrEmpty(path))
                    {
                        path = IO.PathTool.Map(XCore.StartupRoot, "caches");
                    }
                }
                if (!System.IO.Directory.Exists(path))
                {
                    System.IO.Directory.CreateDirectory(path);
                }
                return path;
            }
        }

        /// <summary>
        /// 获取键值文件路径
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        internal static string GetKeyPath(string key)
        {
            return IO.PathTool.JoinPath(CachePath, key.Replace("\\", "_").Replace("+", "_").Replace(".", "_"));
        }

        /// <summary>
        /// 设置缓存内容
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expireSeconds"></param>
        public static bool Set(string key, string value, int expireSeconds = 86400)
        {
            try
            {
                var data = (DateTools.GetUnix() + expireSeconds) + "#" + value;
                using (var fs = new FileStream(GetKeyPath(key), FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    var writer = new StreamWriter(fs, Wlniao.Text.Encoding.UTF8);
                    writer.Write(data);
                    writer.Flush();
                    return true;
                }
            }
            catch { return false; }
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
            return Set(key, JsonSerializer.Serialize(obj, new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) //Json序列化的时候对中文进行处理
            }), expireSeconds);
        }



        /// <summary>
        /// 删除缓存内容
        /// </summary>
        /// <param name="key"></param>
        public static bool Del(string key)
        {
            try
            {
                var fs = GetKeyPath(key);
                if (System.IO.File.Exists(fs))
                {
                    System.IO.File.Delete(fs);
                }
                return true;
            }
            catch { return false; }
        }


        /// <summary>
        /// 判断是否存在缓存项
        /// </summary>
        /// <param name="key"></param>
        public static bool Exists(string key)
        {
            try
            {
                var fs = GetKeyPath(key);
                if (System.IO.File.Exists(fs))
                {
                    var text = FileEx.ReadUTF8String(fs);
                    var data = text.Split('#', 2, StringSplitOptions.RemoveEmptyEntries);
                    if (Convert.ToLong(data[0]) > DateTools.GetUnix())
                    {
                        return true;
                    }
                    System.IO.File.Delete(fs);
                }
            }
            catch { }
            return false;
        }

        /// <summary>
        /// 获取一个缓存项
        /// </summary>
        /// <param name="key"></param>
        public static string Get(string key)
        {
            try
            {
                var fs = GetKeyPath(key);
                if (System.IO.File.Exists(fs))
                {
                    var text = FileEx.ReadUTF8String(fs);
                    var data = text.Split('#', 2, StringSplitOptions.RemoveEmptyEntries);
                    if (Convert.ToLong(data[0]) > DateTools.GetUnix())
                    {
                        return data[1];
                    }
                    System.IO.File.Delete(fs);
                }
            }
            catch { }
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
            var json = Get(key);
            if (!string.IsNullOrEmpty(json))
            {
                return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) //Json序列化的时候对中文进行处理
                });
            }
            return default(T);
        }
    }
}