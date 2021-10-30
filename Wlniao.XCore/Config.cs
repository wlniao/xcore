/*==============================================================================
    文件名称：Config.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：读取、修改配置文件的辅助类
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
using System.Linq;
using System.Collections.Generic;
using System.Text;
namespace Wlniao
{
    /// <summary>
    /// 读取、修改配置文件的辅助类
    /// </summary>
    /// <remarks>
    /// 每行包括键和值，中间的分隔符默认是英文冒号。
    /// 如果某行以双斜杠 // 或井号 # 开头，就表示此行内容是注释
    /// </remarks>
    public class Config
    {
        /// <summary>
        /// 环境变量内容
        /// </summary>
        private static Dictionary<String, String> _env = null;
        /// <summary>
        /// 环境变量内容
        /// </summary>
        private static Dictionary<String, String> _config = null;
        private static String _file = null;
        /// <summary>
        /// 配置文件路径
        /// </summary>
        internal static String FileName
        {
            get
            {
                if (_file == null)
                {
                    if (file.Exists(IO.PathTool.Map(XCore.FrameworkRoot, "xcore.dev.config")))
                    {
                        _file = IO.PathTool.Map(XCore.FrameworkRoot, "xcore.dev.config");
                    }
                    else
                    {
                        _file = IO.PathTool.Map(XCore.FrameworkRoot, "xcore.config");
                    }
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
        }
        /// <summary>
        /// 获取设置信息（优先查找环境变量，再查找xcore.config中配置，默认值非null时向xcore.config中写入默认值）
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static String GetSetting(String key, String defaultValue = null)
        {
            var val = GetEnvironment(key);
            if (string.IsNullOrEmpty(val))
            {
                if (defaultValue == null)
                {
                    val = GetConfigs(key, defaultValue);
                }
                else
                {
                    val = GetConfigsAutoWrite(key, defaultValue);
                }
            }
            return val;
        }
        /// <summary>
        /// 获取 ConfigFile 中某项的值
        /// </summary>
        /// <param name="key">项的名称</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>返回一个字符串值</returns>
        public static String GetConfigs(String key, String defaultValue = null)
        {
            lock (XCore.Lock)
            {
                if (_config == null)
                {
                    Read(FileName);
                }
            }
            var tmpKey = _config.Keys.Where(o => o.ToUpper() == key.ToUpper()).FirstOrDefault();
            if (string.IsNullOrEmpty(tmpKey))
            {
                tmpKey = key;
            }
            if (_config.ContainsKey(tmpKey))
            {
                return _config[tmpKey];
            }
            return string.IsNullOrEmpty(defaultValue) ? "" : defaultValue;
        }
        /// <summary>
        /// 获取 ConfigFile 中某项的值
        /// </summary>
        /// <param name="key">项的名称</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>返回一个字符串值</returns>
        public static String GetConfigsNoCache(String key, String defaultValue = "")
        {
            var tmpKey = _config.Keys.Where(o => o.ToUpper() == key.ToUpper()).FirstOrDefault();
            if (string.IsNullOrEmpty(tmpKey))
            {
                tmpKey = key;
            }
            Read(FileName, false);
            if (_config.ContainsKey(tmpKey))
            {
                return _config[tmpKey];
            }
            return string.IsNullOrEmpty(defaultValue) ? "" : defaultValue;
        }
        /// <summary>
        /// 获取 ConfigFile 中某项的值
        /// </summary>
        /// <param name="key">项的名称</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>返回一个字符串值</returns>
        public static String GetConfigsAutoWrite(String key, String defaultValue = null)
        {
            lock (XCore.Lock)
            {
                if (_config == null)
                {
                    Read(FileName);
                }
            }
            var tmpKey = _config.Keys.Where(o => o.ToUpper() == key.ToUpper()).FirstOrDefault();
            if (string.IsNullOrEmpty(tmpKey))
            {
                tmpKey = key;
            }
            if (_config.ContainsKey(tmpKey))
            {
                return _config[tmpKey];
            }
            else if (string.IsNullOrEmpty(defaultValue))
            {
                _config.Add(tmpKey, "");
            }
            else
            {
                _config.Add(tmpKey, defaultValue);
            }
            try
            {
                Write(_config);
            }
            catch { }
            return string.IsNullOrEmpty(defaultValue) ? "" : defaultValue;
        }
        /// <summary>
        /// 获取环境变量值
        /// </summary>
        /// <param name="key">项的名称</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>返回一个字符串值</returns>
        public static String GetEnvironment(String key, String defaultValue = "")
        {
            key = key.ToUpper();
            lock (XCore.Lock)
            {
                if (_env == null || _env.Count == 0)
                {
                    _env = new Dictionary<string, string>();
                    var en = System.Environment.GetEnvironmentVariables().GetEnumerator();
                    while (en.MoveNext())
                    {
                        _env.TryAdd(en.Key.ToString().ToUpper(), en.Value.ToString());
                    }
                }
            }
            if (_env.ContainsKey(key))
            {
                return _env[key];
            }
            else
            {
                return defaultValue;
            }
        }
        /// <summary>
        /// 添加一个临时的环境变量
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void AddEnvironment(String key, String value = null)
        {
            var tmpKey = _env.Keys.Where(o => o.ToUpper() == key.ToUpper()).FirstOrDefault();
            if (string.IsNullOrEmpty(tmpKey))
            {
                tmpKey = key;
            }
            if (_env == null)
            {
                _env = new Dictionary<string, string>();
            }
            if (_env.ContainsKey(tmpKey))
            {
                _env[tmpKey] = value;
            }
            else
            {
                _env.Add(tmpKey, value);
            }
        }
        /// <summary>
        /// 设置 xcore.config 中某项的值
        /// </summary>
        /// <param name="key">项的名称</param>
        /// <param name="value">项的值</param>
        /// <returns>返回结果</returns>
        public static Boolean SetConfigs(String key, String value)
        {
            var tmpKey = _config.Keys.Where(o => o.ToUpper() == key.ToUpper()).FirstOrDefault();
            if (string.IsNullOrEmpty(tmpKey))
            {
                tmpKey = key;
            }
            if (_config == null)
            {
                GetConfigs(tmpKey);
            }
            if (_config.ContainsKey(tmpKey))
            {
                _config[tmpKey] = value;
            }
            else if (_config.ContainsKey(tmpKey.ToUpper()))
            {
                _config[tmpKey.ToUpper()] = value;
            }
            else
            {
                _config.Add(tmpKey, value);
            }
            Write(_config, FileName);
            return true;
        }
        /// <summary>
        /// 设置 xcore.config 中某项的值
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static Boolean SetConfigs(String key)
        {
            return SetConfigs(key, "");
        }
        /// <summary>
        /// 使用yaml格式
        /// </summary>
        /// <returns></returns>
        public static Boolean UseYaml()
        {
            return SetConfigs("yaml", "");
        }

        /// <summary>
        /// 设置 ConfigFile 中某项的值
        /// </summary>
        /// <param name="key">项的名称</param>
        /// <returns>返回结果</returns>
        public static Boolean Remove(String key)
        {
            if (_config != null)
            {
                var tmpKey = _config.Keys.Where(o => o.ToUpper() == key.ToUpper()).FirstOrDefault();
                if (!string.IsNullOrEmpty(tmpKey))
                {
                    _config.Remove(tmpKey);
                    Write(_config, FileName);
                }
            }
            return true;
        }
        /// <summary>
        /// 读取配置文件，返回一个 Dictionary，键值都是字符串
        /// </summary>
        /// <param name="path">配置文件的路径(相对路径，相对于项目的根目录)</param>
        /// <param name="init">是否为初始化调用</param>
        /// <returns>返回一个 Dictionary</returns>
        public static void Read(String path, Boolean init = true)
        {
            if (file.Exists(path))
            {
                _config = cvt.ToDictionary(file.Read(path));
            }
            else if (init)
            {
                _config = new Dictionary<string, string>();
                if (XCore._console)
                {
                    log.Console("missing config:" + path, ConsoleColor.Red);
                }
            }
        }

        /// <summary>
        /// 将 Dictionary 对象持久化到磁盘
        /// </summary>
        /// <param name="dic">一个 Dictionary</param>
        /// <param name="path">配置文件的路径(相对路径，相对于项目的根目录)</param>
        public static void Write(Dictionary<String, String> dic, String path = "/xcore/xcore.config")
        {
            if (_config != null)
            {
                var sb = new StringBuilder();
                foreach (KeyValuePair<String, String> pair in _config)
                {
                    if (_config.ContainsKey("yaml"))
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
                file.Write(IO.PathTool.Map(path), sb.ToString(), true);
            }
        }
    }
}