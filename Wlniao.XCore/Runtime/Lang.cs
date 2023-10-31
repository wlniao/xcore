/*==============================================================================
    文件名称：Lang.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：语言包工具，用于加载多国语言
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
using Wlniao.IO;
namespace Wlniao.Runtime
{
    /// <summary>
    /// 语言包工具，用于加载多国语言
    /// </summary>
    /// <remarks>
    /// 默认语言包文件存放在 /xcore/lang/ 中，比如 /xcore/lang/zh-cn.ini 。只要在 /xcore/lang/ 中新增一个语言包文件，则系统将其作为语言包列表自动加载。可添加的语言包名称包括：en-us,en-gb,zh-cn,zh-tw,ja,ko,fr,de,it
    /// </remarks>
    public class Lang
    {
        private const string defaultLang = "zh-cn";
        // 这个一定要放在第一行，以保证第一个加载
        private static Dictionary<String, Dictionary<String, String>> langSetting = new Dictionary<String, Dictionary<String, String>>(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// 获取某 key 的语言值
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static String Get(String key)
        {
            return Get(GetLang(), key, null);
        }
        /// <summary>
        /// 获取某 key 的语言值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultStr"></param>
        /// <returns></returns>
        public static String Get(String key, String defaultStr)
        {
            return Get(GetLang(), key, defaultStr);
        }
        /// <summary>
        /// 获取指定语言某 key 的值
        /// </summary>
        /// <param name="langStr"></param>
        /// <param name="key"></param>
        /// <param name="defaultStr"></param>
        /// <returns></returns>
        public static String Get(String langStr, String key, String defaultStr)
        {
            if (string.IsNullOrEmpty(langStr))
            {
                langStr = defaultLang;
            }
            if (langSetting.Count == 0)
            {
                lock (XCore.Lock)
                {
                    if (langSetting.Count == 0)
                    {
                        try
                        {
                            var langPath = PathTool.Map(XCore.FrameworkRoot, "lang");
                            foreach (String file in Directory.GetFiles(langPath))
                            {
                                if (file.EndsWith(".ini"))
                                {
                                    var str = Wlniao.File.Read(file);
                                    var langKey = Path.GetFileNameWithoutExtension(file);
                                    langSetting.TryAdd(Path.GetFileNameWithoutExtension(file), cvt.ToDictionary(str));
                                }
                            }
                        }
                        catch { }
                        if (!langSetting.ContainsKey(langStr))
                        {
                            langSetting.TryAdd(langStr, new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase));
                            if (langStr == defaultLang)
                            {
                                langSetting[defaultLang].TryAdd("today", "今天");
                                langSetting[defaultLang].TryAdd("yesterday", "昨天");
                                langSetting[defaultLang].TryAdd("thedaybeforeyesterday", "前天");
                                langSetting[defaultLang].TryAdd("houresAgo", "小时前");
                                langSetting[defaultLang].TryAdd("minuteAgo", "分钟前");
                                langSetting[defaultLang].TryAdd("secondAgo", "秒前");
                                langSetting[defaultLang].TryAdd("justNow", "刚刚");
                                langSetting[defaultLang].TryAdd("findtotal", "共找到{0}条记录");
                                langSetting[defaultLang].TryAdd("empty", "加载完成 暂无数据");
                            }
                        }
                    }
                }
            }

            if (langSetting[langStr].ContainsKey(key) && !string.IsNullOrEmpty(langSetting[langStr][key]))
            {
                return langSetting[langStr][key];
            }
            else if (defaultStr == null)
            {
                try
                {
                    // 写入空行到语言包
                    langSetting[langStr].TryAdd(key, defaultStr);
                    Config.Write(langSetting[langStr], PathTool.Map(XCore.FrameworkRoot, "lang", langStr + ".ini"));
                }
                catch { }
                return "{" + key + "}"; // 返回约定内容
            }
            else
            {
                return defaultStr; // 返回用户默认内容
            }
        }
        /// <summary>
        /// 获取所有支持的语言包
        /// </summary>
        /// <returns></returns>
        public static List<Dictionary<String, String>> GetSupportedLang()
        {
            var list = new List<Dictionary<String, String>>();
            foreach (String key in langSetting.Keys)
            {
                Dictionary<String, String> pair = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
                pair.TryAdd("Name", GetLangName(key));
                pair.TryAdd("Value", key);
                list.Add(pair);
            }
            return list;
        }
        /// <summary>
        /// 获取当前语言字符(比如 zh-cn，或 en-us)
        /// </summary>
        /// <returns></returns>
        public static String GetLang()
        {
            var nativeLang = System.Globalization.CultureInfo.CurrentCulture.Name.ToLower();
            if (langSetting.ContainsKey(nativeLang))
            {
                return nativeLang;
            }
            return defaultLang;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="langStr"></param>
        /// <returns></returns>
        public static String GetLangName(String langStr)
        {
            switch (langStr.ToLower())
            {
                case "en-us": return "English (US)";
                case "en-gb": return "English (British)";
                case "zh-cn": return "中文(简体)";
                case "zh-tw": return "正體中文(繁體)";
                case "de": return "Deutsch";
                case "fr": return "Français";
                case "it": return "Italiano";
                case "ko": return "한국어";
                case "ja": return "日本語";
                default: return langStr;
            }
        }
    }
}