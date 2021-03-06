﻿/*==============================================================================
    文件名称：DictionaryExtend.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：Dictionary组扩展
================================================================================
 
    Copyright 2014 XieChaoyi

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
using System.Linq.Expressions;
namespace Wlniao
{
    /// <summary>
    /// Dictionary组扩展
    /// </summary>
    public static class DictionaryExtend
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void TryAdd(this Dictionary<string, string> dic, string key, string value)
        {
            if (dic != null && !dic.ContainsKey(key))
            {
                dic.Add(key, value);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void TryAdd(this Dictionary<string, object> dic, string key, object value)
        {
            if (dic != null && !dic.ContainsKey(key))
            {
                dic.Add(key, value);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="key"></param>
        /// <param name="Default"></param>
        /// <returns></returns>
        public static string GetString(this Dictionary<string, string> dic, string key, string Default = "")
        {
            if (dic != null && dic.ContainsKey(key))
            {
                return dic[key];
            }
            else
            {
                return Default;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="key"></param>
        /// <param name="Default"></param>
        /// <returns></returns>
        public static object GetValue(this Dictionary<string, object> dic, string key, string Default = "")
        {
            if (dic != null && dic.ContainsKey(key))
            {
                return dic[key];
            }
            else
            {
                return Default;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static string GetString(this Dictionary<string, object> dic, string key, string defaultValue = "")
        {
            if (dic != null && dic.ContainsKey(key))
            {
                object obj = dic[key];
                if (obj == null)
                {
                    return "";
                }
                return dic[key].ToString();
            }
            else
            {
                return defaultValue;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static decimal GetDecimal(this Dictionary<string, object> dic, string key, decimal defaultValue = 0)
        {
            if (dic != null && dic.ContainsKey(key))
            {
                return Convert.ToDecimal(dic[key].ToString());
            }
            else
            {
                return defaultValue;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static decimal GetDecimal(this Dictionary<string, string> dic, string key, decimal defaultValue = 0)
        {
            if (dic != null && dic.ContainsKey(key))
            {
                return Convert.ToDecimal(dic[key]);
            }
            else
            {
                return defaultValue;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static double GetDouble(this Dictionary<string, object> dic, string key, double defaultValue = 0)
        {
            if (dic != null && dic.ContainsKey(key))
            {
                return Convert.ToDouble(dic[key].ToString());
            }
            else
            {
                return defaultValue;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static double GetDouble(this Dictionary<string, string> dic, string key, double defaultValue = 0)
        {
            if (dic != null && dic.ContainsKey(key))
            {
                return Convert.ToDouble(dic[key]);
            }
            else
            {
                return defaultValue;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static float GetFloat(this Dictionary<string, string> dic, string key, double defaultValue = 0)
        {
            return (float)dic.GetDouble(key, defaultValue);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static float GetFloat(this Dictionary<string, object> dic, string key, double defaultValue = 0)
        {
            return (float)dic.GetDouble(key, defaultValue);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static int GetInt32(this Dictionary<string, object> dic, string key, int defaultValue = 0)
        {
            if (dic != null && dic.ContainsKey(key))
            {
                return Convert.ToInt(dic[key]);
            }
            else
            {
                return defaultValue;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static int GetInt32(this Dictionary<string, string> dic, string key, int defaultValue = 0)
        {
            if (dic != null && dic.ContainsKey(key))
            {
                return Convert.ToInt(dic[key] ?? "" + defaultValue);
            }
            else
            {
                return defaultValue;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static bool GetBoolean(this Dictionary<string, string> dic, string key, string defaultValue = "false")
        {
            string value = dic.GetString(key, defaultValue);
            return value.ToLower() == "true" || value == "1";
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static bool GetBoolean(this Dictionary<string, object> dic, string key, string defaultValue = "false")
        {
            string value = dic.GetString(key, defaultValue);
            return value.ToLower() == "true" || value == "1";
        }

    }
}