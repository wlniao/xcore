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
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using Wlniao.IO;
using Wlniao.Log;

namespace Wlniao.Caching
{
    /// <summary>
    /// FileCache缓存
    /// </summary>
    public class FileCache
    {
        private static string _path = null;
        /// <summary>
        /// L1 内存层：避免每次 Get/Exists 都读磁盘（按剩余有效期缓存）
        /// </summary>
        private static readonly ConcurrentDictionary<string, L1Entry> _l1 = new();
        private const int L1MaxCount = 10000;
        /// <summary>L1 内存层累计命中次数（诊断/测试用，跨线程安全）</summary>
        private static long _l1HitCount;
        /// <summary>L1 内存层累计未命中次数（诊断/测试用，跨线程安全）</summary>
        private static long _l1MissCount;

        private sealed class L1Entry
        {
            public long ExpireUnix;
            public string Value;
            public long FileWriteUnix;
            public long FileLength;
        }

        private static void L1Set(string key, string value, long expireUnix, string filePath = null)
        {

            if (_l1.Count >= L1MaxCount)
            {
                var now = DateTools.GetUnix();
                foreach (var kv in _l1)
                {
                    if (kv.Value.ExpireUnix <= now)
                    {
                        _l1.TryRemove(kv.Key, out _);
                    }
                }
                // 仍超限则按插入序丢弃一部分（Dictionary 枚举序近似插入序，够用）
                if (_l1.Count >= L1MaxCount)
                {
                    var drop = _l1.Count - L1MaxCount + 1;
                    foreach (var kv in _l1)
                    {
                        if (drop-- <= 0) break;
                        _l1.TryRemove(kv.Key, out _);
                    }
                }
            }
            var entry = new L1Entry { ExpireUnix = expireUnix, Value = value ?? "" };
            if (!string.IsNullOrEmpty(filePath) && System.IO.File.Exists(filePath))
            {
                var fi = new FileInfo(filePath);
                entry.FileWriteUnix = fi.LastWriteTimeUtc.Ticks;
                entry.FileLength = fi.Length;
            }
            _l1[key] = entry;
        }

        private static bool L1TryGet(string key, out string value, out bool expired, string filePath = null)
        {
            value = null;
            expired = false;
            var hit = false;
            if (_l1.TryGetValue(key, out var e))
            {
                if (!string.IsNullOrEmpty(filePath))
                {
                    var fi = new FileInfo(filePath);
                    if (!fi.Exists)
                    {
                        _l1.TryRemove(key, out _);
                    }
                    else if (fi.LastWriteTimeUtc.Ticks != e.FileWriteUnix || fi.Length != e.FileLength)
                    {
                        // 其他进程/实例已更新共享缓存文件，禁用本进程陈旧副本
                        _l1.TryRemove(key, out _);
                    }
                    else if (e.ExpireUnix > DateTools.GetUnix())
                    {
                        value = e.Value;
                        hit = true;
                    }
                    else
                    {
                        expired = true;
                        _l1.TryRemove(key, out _);
                    }
                }
                else if (e.ExpireUnix > DateTools.GetUnix())
                {
                    value = e.Value;
                    hit = true;
                }
                else
                {
                    expired = true;
                    _l1.TryRemove(key, out _);
                }
            }
            if (hit)
            {
                Interlocked.Increment(ref _l1HitCount);
            }
            else
            {
                Interlocked.Increment(ref _l1MissCount);
            }
            return hit;
        }

        /// <summary>
        /// L1 内存层累计命中次数（诊断/测试用）
        /// </summary>
        public static long L1HitCount => Interlocked.Read(ref _l1HitCount);

        /// <summary>
        /// L1 内存层累计未命中次数（诊断/测试用）
        /// </summary>
        public static long L1MissCount => Interlocked.Read(ref _l1MissCount);

        /// <summary>
        /// 清零 L1 命中统计（诊断/测试用）
        /// </summary>
        public static void ResetL1Statistics()
        {
            Interlocked.Exchange(ref _l1HitCount, 0);
            Interlocked.Exchange(ref _l1MissCount, 0);
        }
        /// <summary>
        /// 缓存文件目录
        /// </summary>
        private static string CachePath
        {
            get
            {
                if (_path == null)
                {
                    _path = Config.GetSetting("WLN_CACHE_PATH");
                    if (string.IsNullOrEmpty(_path))
                    {
                        _path = IO.PathTool.Map(XCore.StartupRoot, XCore.FrameworkRoot, "caches");
                    }
                }
                if (!System.IO.Directory.Exists(_path))
                {
                    System.IO.Directory.CreateDirectory(_path);
                }
                return _path;
            }
        }

        /// <summary>
        /// 获取键值文件路径
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private static string GetKeyPath(string key)
        {
            return IO.PathTool.JoinPath(CachePath, key.Replace("\\", "_").Replace(":", "_").Replace("+", "_").Replace(".", "_"));
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
                var file = GetKeyPath(key);
                var data = (DateTools.GetUnix() + expireSeconds) + "#" + value;
                var parent = System.IO.Path.GetDirectoryName(file);
                if (!System.IO.Directory.Exists(parent))
                {
                    //创建父级目录
                    System.IO.Directory.CreateDirectory(parent!);
                }
                using var fs = new FileStream(file, FileMode.Create, FileAccess.ReadWrite,FileShare.ReadWrite);
                var writer = new StreamWriter(fs, Wlniao.Text.Encodings.UTF8);
                writer.Write(data);
                writer.Flush();
                L1Set(key, value, DateTools.GetUnix() + expireSeconds, file);
                return true;
            }
            catch (Exception ex)
            {
                Loger.Error($"{ex.Message}{Environment.NewLine}{ex.StackTrace}");
            }
            return false;
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
            return Set(key, JsonSerializer.Serialize(obj, typeInfo), expireSeconds);
        }



        /// <summary>
        /// 删除缓存内容
        /// </summary>
        /// <param name="key"></param>
        public static bool Del(string key)
        {
            try
            {
                _l1.TryRemove(key, out _);
                var fs = GetKeyPath(key);
                if (System.IO.File.Exists(fs))
                {
                    System.IO.File.Delete(fs);
                }
                return true;
            }
            catch (Exception ex)
            {
                Loger.Error($"{ex.Message}{Environment.NewLine}{ex.StackTrace}");
            }
            return false;
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
                if (L1TryGet(key, out _, out _, fs))
                {
                    return true;
                }
                if (System.IO.File.Exists(fs))
                {
                    var text = FileEx.ReadUTF8String(fs);
                    var data = text.Split('#', 2, StringSplitOptions.RemoveEmptyEntries);
                    if (Convert.ToLong(data[0]) > DateTools.GetUnix())
                    {
                        L1Set(key, data.Length > 1 ? data[1] : "", Convert.ToLong(data[0]), fs);
                        return true;
                    }
                    System.IO.File.Delete(fs);
                }
            }
            catch (Exception ex)
            {
                Loger.Error($"{ex.Message}{Environment.NewLine}{ex.StackTrace}");
            }
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
                if (L1TryGet(key, out var cached, out _, fs))
                {
                    return cached;
                }
                if (System.IO.File.Exists(fs))
                {
                    var text = FileEx.ReadUTF8String(fs);
                    var data = text.Split('#', 2, StringSplitOptions.RemoveEmptyEntries);
                    if (Convert.ToLong(data[0]) > DateTools.GetUnix())
                    {
                        var value = data.Length > 1 ? data[1] : "";
                        L1Set(key, value, Convert.ToLong(data[0]), fs);
                        return value;
                    }
                    System.IO.File.Delete(fs);
                }
            }
            catch (Exception ex)
            {
                Loger.Error($"{ex.Message}{Environment.NewLine}{ex.StackTrace}");
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
            var json = Get(key);
            return string.IsNullOrEmpty(json) ? default : JsonSerializer.Deserialize(json, typeInfo);
        }
    }
}