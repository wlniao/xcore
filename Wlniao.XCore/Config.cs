/*==============================================================================
    文件名称：Config.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：读取、修改配置文件的辅助类

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

==============================================================================*/
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Wlniao.IO;
using Wlniao.Log;

namespace Wlniao
{
    /// <summary>
    /// 读取、修改配置文件的辅助类
    /// </summary>
    /// <remarks>
    /// 每行包括键和值，中间的分隔符默认是英文冒号。
    /// 如果某行以双斜杠 // 或井号 # 开头，就表示此行内容是注释。
    /// 读路径基于 _config 引用快照（copy-on-write），不持锁；
    /// 写路径在锁内克隆并整体替换引用，磁盘 IO 在锁外执行。
    /// </remarks>
    public class Config
    {
        /// <summary>
        /// 环境变量内容
        /// </summary>
        private static Dictionary<string, string> _env = null;
        /// <summary>
        /// 配置表（只读快照可无锁并发读；写入方必须走 MutateConfig 做 copy-on-write）
        /// </summary>
        private static volatile Dictionary<string, string> _config = null;
        /// <summary>
        /// 存在尚未落盘的内存变更（GetConfigsAutoWrite 等延迟回写）
        /// </summary>
        private static volatile bool _configDirty = false;
        /// <summary>
        /// 配置版本号；每次内存变更递增，用于避免旧快照覆盖新快照
        /// </summary>
        private static long _configVersion = 0;
        /// <summary>
        /// 配置回写串行锁；磁盘 IO 不占用 XCore.Lock
        /// </summary>
        private static readonly object _configFlushLock = new object();
        /// <summary>
        /// 上次延迟回写时间（UTC ticks）
        /// </summary>
        private static long _lastConfigFlushTicks = 0;
        /// <summary>
        /// 延迟回写最小间隔：1 秒（避免启动期批量 GetSetting 触发 N 次整文件写）
        /// </summary>
        private const long ConfigFlushIntervalTicks = TimeSpan.TicksPerSecond;
        /// <summary>
        ///
        /// </summary>
        private static string _file = null;
        /// <summary>
        /// 配置文件密钥
        /// </summary>
		public static string Secret { get; set; }
		/// <summary>
		/// 配置文件路径
		/// </summary>
		internal static string FileName
        {
            get
            {
                if (_file != null)
                {
                    return _file;
                }
                if (FileEx.Exists(IO.PathTool.Map(XCore.FrameworkRoot, "xcore.dev.config")))
                {
                    _file = IO.PathTool.Map(XCore.FrameworkRoot, "xcore.dev.config");
                }
                else
                {
                    _file = IO.PathTool.Map(XCore.FrameworkRoot, "xcore.config");
                }
                return _file;
            }
        }

        /// <summary>
        /// 重置配置文件内容
        /// </summary>
        internal static void Clear()
        {
            _env = new Dictionary<string, string>();
            _config = null;
            _configDirty = false;
        }
        /// <summary>
        /// 获取设置信息（优先查找环境变量，再查找xcore.config中配置，默认值非null时向xcore.config中写入默认值）
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static string GetSetting(string key, string defaultValue = null)
        {
            var val = GetEnvironment(key.ToUpper());
            if (string.IsNullOrEmpty(val))
            {
                val = GetConfigsAutoWrite(key, defaultValue);
            }
            return val;
        }

        /// <summary>
        /// 获取 ConfigFile 中某项的值
        /// </summary>
        /// <param name="key">项的名称</param>
        /// <param name="secret">SM4密钥</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>返回一个字符串值</returns>
        public static string GetEncrypt(string key, string secret, string defaultValue = null)
        {
            var val = GetEnvironment(key.ToUpper());
            if (string.IsNullOrEmpty(secret))
            {
                return string.IsNullOrEmpty(val) ? GetConfigs(key, defaultValue) : val;
            }
            else if (string.IsNullOrEmpty(val))
            {
                val = GetConfigs(key);
                if (string.IsNullOrEmpty(val))
                {
                    return defaultValue;
                }

                if (string.IsNullOrEmpty(secret))
                {
                    return val;
                }
                else
                {
                    var tmp = Encryptor.SM4DecryptECBFromHex(val, secret);
                    if (!string.IsNullOrEmpty(tmp))
                    {
                        val = tmp;
                    }
                    else
                    {
                        SetEncrypt(key, val, secret);
                    }
                }
            }
            else
            {
                var tmp = Encryptor.SM4DecryptECBFromHex(val, secret);
                if (!string.IsNullOrEmpty(tmp))
                {
                    val = tmp;
                }
            }

            return val;
        }

        /// <summary>
        /// 获取 ConfigFile 中某项的值（读路径不持锁，基于引用快照）
        /// </summary>
        /// <param name="key">项的名称</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>返回一个字符串值</returns>
        public static string GetConfigs(string key, string defaultValue = null)
        {
            var val = GetEnvironment(key);
            if (!string.IsNullOrEmpty(val))
            {
                return val;
            }
            var cfg = _config;
            if (cfg == null)
            {
                EnsureConfigLoaded();
                cfg = _config;
            }
            if (TryGetConfig(cfg, key, out var configs))
            {
                return configs;
            }
            // 顺带消化延迟回写（不阻塞本次读）
            TryFlushConfig(force: false);
            return defaultValue ?? "";
        }
        /// <summary>
        /// 获取 ConfigFile 中某项的值（缺项时写入默认值，回写按间隔节流）
        /// </summary>
        /// <param name="key">项的名称</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>返回一个字符串值</returns>
        private static string GetConfigsAutoWrite(string key, string defaultValue = null)
        {
            var cfg = _config;
            if (cfg == null)
            {
                EnsureConfigLoaded();
                cfg = _config;
            }
            if (cfg != null && TryGetConfig(cfg, key, out var write))
            {
                return write;
            }
            if (cfg == null)
            {
                return string.IsNullOrEmpty(defaultValue) ? "" : defaultValue;
            }
            var value = defaultValue ?? "";
            MutateConfig(c => c[key] = value, forceFlush: false);
            return value;
        }

        private static void EnsureConfigLoaded()
        {
            if (_config != null)
            {
                return;
            }
            lock (XCore.Lock)
            {
                if (_config == null)
                {
                    Read(FileName);
                }
            }
        }

        /// <summary>
        /// 锁内克隆并替换 _config（copy-on-write），锁外按需落盘。
        /// </summary>
        private static bool MutateConfig(Action<Dictionary<string, string>> mutate, bool forceFlush)
        {
            Dictionary<string, string> snapshot;
            long version;
            lock (XCore.Lock)
            {
                if (_config == null)
                {
                    Read(FileName);
                }
                if (_config == null)
                {
                    return false;
                }
                var next = new Dictionary<string, string>(_config, StringComparer.OrdinalIgnoreCase);
                mutate(next);
                _config = next;
                _configDirty = true;
                version = Interlocked.Increment(ref _configVersion);
                snapshot = next;
            }
            if (forceFlush)
            {
                return FlushConfigSnapshot(snapshot, version);
            }
            TryFlushConfig(force: false);
            return true;
        }

        private static void TryFlushConfig(bool force)
        {
            if (!_configDirty)
            {
                return;
            }
            if (!force)
            {
                var now = DateTime.UtcNow.Ticks;
                var last = Interlocked.Read(ref _lastConfigFlushTicks);
                if (now - last < ConfigFlushIntervalTicks)
                {
                    return;
                }
            }
            Dictionary<string, string> snapshot;
            long version;
            lock (XCore.Lock)
            {
                if (!_configDirty || _config == null)
                {
                    return;
                }
                snapshot = new Dictionary<string, string>(_config, StringComparer.OrdinalIgnoreCase);
                version = Volatile.Read(ref _configVersion);
                _configDirty = false;
                Interlocked.Exchange(ref _lastConfigFlushTicks, DateTime.UtcNow.Ticks);
            }

            // 磁盘 IO 不持有 XCore.Lock；串行执行并避免旧快照覆盖新快照
            lock (_configFlushLock)
            {
                if (Volatile.Read(ref _configVersion) > version)
                {
                    _configDirty = true;
                    return;
                }
                if (!Write(snapshot, FileName))
                {
                    // 落盘失败保持脏标记，下次继续尝试
                    _configDirty = true;
                }
            }
        }

        private static bool FlushConfigSnapshot(Dictionary<string, string> snapshot, long version)
        {
            lock (_configFlushLock)
            {
                if (Volatile.Read(ref _configVersion) > version)
                {
                    _configDirty = true;
                    return false;
                }
                var ok = Write(snapshot, FileName);
                if (ok)
                {
                    _configDirty = false;
                    Interlocked.Exchange(ref _lastConfigFlushTicks, DateTime.UtcNow.Ticks);
                }
                else
                {
                    _configDirty = true;
                }
                return ok;
            }
        }

        private static bool TryGetConfig(Dictionary<string, string> cfg, string key, out string value)
        {
            if (cfg != null)
            {
                if (cfg.TryGetValue(key, out value))
                {
                    return true;
                }
            }
            value = null;
            return false;
        }
        /// <summary>
        /// 获取环境变量值
        /// </summary>
        /// <param name="key">项的名称</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>返回一个字符串值</returns>
        public static string GetEnvironment(string key, string defaultValue = "")
        {
            if (_env == null || _env.Count == 0)
            {
                lock (XCore.Lock)
                {
                    if (_env == null || _env.Count == 0)
                    {
                        var env = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        var en = System.Environment.GetEnvironmentVariables().GetEnumerator();
                        while (en.MoveNext())
                        {
                            env.TryAdd(en.Key.ToString().ToUpper(), en.Value.ToString());
                        }
                        _env = env;
                    }
                }
            }

            return _env.GetValueOrDefault(key, defaultValue);
        }
        /// <summary>
        /// 重新加载环境变量快照；用于容器/宿主运行时新增或修改环境变量的场景
        /// </summary>
        public static void ReloadEnvironment()
        {
            lock (XCore.Lock)
            {
                _env = null;
            }
        }

        /// <summary>
        /// 设置 xcore.config 中某项的值
        /// </summary>
        /// <param name="key">项的名称</param>
        /// <param name="value">项的值</param>
        /// <returns>返回结果</returns>
        public static bool SetConfigs(string key, string value = "")
        {
            return MutateConfig(c => c.PutValue(key, value), forceFlush: true);
        }
        /// <summary>
        /// 设置 xcore.config 中某项的值
        /// </summary>
        /// <param name="key">项的名称</param>
        /// <param name="value">项的值</param>
        /// <param name="secret">SM4密钥</param>
        /// <returns>返回结果</returns>
        public static bool SetEncrypt(string key, string value, string secret)
        {
            if (!string.IsNullOrEmpty(secret))
            {
                value = Wlniao.Encryptor.SM4EncryptECBToHex(value, secret, true);
            }
            return MutateConfig(c => c.PutValue(key, value), forceFlush: true);
        }

		/// <summary>
		/// 移除 ConfigFile 中某项的值
		/// </summary>
		/// <param name="key">项的名称</param>
		/// <returns>返回结果</returns>
		public static bool Remove(string key)
        {
            var cfg = _config;
            if (cfg == null)
            {
                return true;
            }
            var tmpKey = cfg.Keys.FirstOrDefault(o => string.Equals(o, key, StringComparison.CurrentCultureIgnoreCase));
            if (string.IsNullOrEmpty(tmpKey))
            {
                return true;
            }
            return MutateConfig(c => c.Remove(tmpKey), forceFlush: true);
        }
        /// <summary>
        /// 读取配置文件，返回一个 Dictionary，键值都是字符串
        /// </summary>
        /// <param name="path">配置文件的路径(相对路径，相对于项目的根目录)</param>
        /// <returns>返回一个 Dictionary</returns>
        public static void Read(string path)
        {
            lock (XCore.Lock)
            {
                _config = FileEx.Exists(path) ? Convert.ToDictionary(FileEx.Read(path)) : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                _configDirty = false;
            }
        }

        /// <summary>
        /// 将 Dictionary 对象持久化到磁盘（使用传入的 dic 快照，不读共享字段）
        /// </summary>
        /// <param name="dic">一个 Dictionary</param>
        /// <param name="path">配置文件的路径(相对路径，相对于项目的根目录)</param>
        public static bool Write(Dictionary<string, string> dic, string path)
        {
            try
            {
                if (dic != null)
                {
                    var sb = new StringBuilder();
                    foreach (var pair in dic.OrderBy(o => o.Key))
                    {
                        if (dic.ContainsKey("yaml"))
                        {
                            if (pair.Key == "yaml")
                            {
                                sb.Insert(0, "- yaml" + Environment.NewLine);
                            }
                            else
                            {
                                sb.Append("- ");
                                sb.Append(pair.Key);
                                sb.Append("=");
                                sb.Append(pair.Value);
                                sb.Append(Environment.NewLine);
                            }
                        }
                        else
                        {
                            sb.Append(pair.Key);
                            sb.Append("=");
                            sb.Append(pair.Value);
                            sb.Append(Environment.NewLine);
                        }
                    }
                    if (string.IsNullOrEmpty(path))
                    {
                        path = IO.PathTool.Map(XCore.FrameworkRoot, "xcore.config");
                    }
                    FileEx.Write(path, sb.ToString(), true);
                    return true;
                }
            }
            catch(Exception ex)
            {
                Loger.Error($"{ex.Message}{Environment.NewLine}{ex.StackTrace}");
            }

            return false;
        }
    }
}
