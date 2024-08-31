/*==============================================================================
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
using System.Runtime.CompilerServices;
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
		/// <param name="value"></param>
		public static void PutValue(this Dictionary<string, string> dic, string key, string value)
		{
			if (dic != null)
			{
				if (dic.ContainsKey(key))
				{
					dic[key] = value;
				}
				else
				{
					dic.Add(key, value);
				}
			}
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="dic"></param>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public static void PutValue(this Dictionary<string, object> dic, string key, object value)
		{
			if (dic != null)
			{
                if(dic.ContainsKey(key))
                {
                    dic[key] = value;
                }
                else
				{
					dic.Add(key, value);
				}
			}
        }

        /// <summary>
        /// 仅当值为空时才赋值
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void PutOnlyEmpty(this Dictionary<string, string> dic, string key, string value)
        {
            if (dic == null)
            {
                dic = new Dictionary<string, string> { { key, value } };
            }
            else if (!dic.ContainsKey(key))
            {
                dic.TryAdd(key, value);
            }
            else if (string.IsNullOrEmpty(dic[key]))
            {
                dic[key] = value;
            }
        }

        /// <summary>
        /// 仅当值为空时才赋值
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void PutOnlyEmpty(this Dictionary<string, object> dic, string key, object value)
        {
            if (dic == null)
            {
                dic = new Dictionary<string, object> { { key, value } };
            }
            else if (!dic.ContainsKey(key))
            {
                dic.TryAdd(key, value);
            }
            else if (dic[key] == null || (dic[key] is string && string.IsNullOrEmpty(dic[key] as string)))
            {
                dic[key] = value;
            }
        }

        /// <summary>
        /// 获取字典表中的值
        /// </summary>
        /// <param name="dic">字典</param>
        /// <param name="key">字段标识</param>
        /// <param name="defaultVal">无字段或为空时的默认值</param>
        /// <param name="allowEmpty">是否允许获取空值</param>
        /// <returns></returns>
        public static string GetString(this Dictionary<string, string> dic, string key, string defaultVal = "", bool allowEmpty = false)
        {
            if (dic != null && dic.ContainsKey(key) && (allowEmpty || !string.IsNullOrEmpty(dic[key])))
            {
                return dic[key];
            }
            else
            {
                return defaultVal;
            }
        }
        /// <summary>
        /// 获取字典表中的值
        /// </summary>
        /// <param name="dic">字典</param>
        /// <param name="key">字段标识</param>
        /// <param name="defaultVal">无字段或为空时的默认值</param>
        /// <param name="allowEmpty">是否允许获取空值</param>
        /// <returns></returns>
        public static string GetString(this Dictionary<string, object> dic, string key, string defaultVal = "", bool allowEmpty = false)
        {
            if (dic != null && dic.ContainsKey(key))
            {
                var svalue = "";
                try
                {
                    svalue = dic[key] == null ? "" : dic[key].ToString();
                }
                catch { svalue = ""; }
                if (allowEmpty || !string.IsNullOrEmpty(svalue))
                {
                    return svalue;
                }
            }
            return defaultVal;
        }

        /// <summary>
        /// 获取字典表中的值
        /// </summary>
        /// <param name="dic">字典</param>
        /// <param name="key">字段标识</param>
        /// <param name="defaultVal">无字段或为空时的默认值</param>
        /// <param name="allowEmpty">是否允许获取空值</param>
        /// <returns></returns>
        public static object GetValue(this Dictionary<string, object> dic, string key, string defaultVal = "", bool allowEmpty = false)
        {
            if (dic != null && dic.ContainsKey(key) && (allowEmpty || dic[key] != null))
            {
                return dic[key];
            }
            else
            {
                return defaultVal;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dic"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static T GetValue<T>(this Dictionary<string, object> dic, string key)
        {
            return dic.GetValue<T>(key, default(T));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="key"></param>
        /// <param name="Default"></param>
        /// <returns></returns>
        public static T GetValue<T>(this Dictionary<string, object> dic, string key, T Default)
        {
            if (dic != null && dic.ContainsKey(key))
            {
                try
                {
                    var json = Json.ToString(dic[key]);
                    return Json.ToObject<T>(json);
                }
                catch { }
            }
            return Default;
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
        public static long GetInt64(this Dictionary<string, object> dic, string key, long defaultValue = 0)
        {
            if (dic != null && dic.ContainsKey(key))
            {
                return Convert.ToLong(dic[key]);
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
        public static long GetInt64(this Dictionary<string, string> dic, string key, long defaultValue = 0)
        {
            if (dic != null && dic.ContainsKey(key))
            {
                return Convert.ToLong(dic[key] ?? "" + defaultValue);
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