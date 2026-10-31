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

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Wlniao
{
    /// <summary>
    /// Dictionary组扩展
    /// </summary>
    public static class DictionaryExtend
    {
        /// <summary>
        /// 未显式传入 JsonTypeInfo 时的反射兜底说明（与 Json 逃生舱重载保持同一口径）
        /// </summary>
        private const string UnAotMessage = "未显式传入 JsonTypeInfo<T> 的 ToDictionary 重载，在目标类型未被源生成上下文覆盖时需要运行时反射，在 NativeAOT/裁剪环境下可能丢字段或失效。AOT 场景请改用 ToDictionary<T>(T, JsonTypeInfo<T>)，或在 JsonSerializerContext 上登记目标类型以命中零反射分支。";

        /// <summary>
        /// 对象转换为 Dictionary（AOT 安全：基于源生成元数据序列化后经 JsonNode 解析，零反射）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">需要转换的对象</param>
        /// <param name="typeInfo">由 JsonSerializerContext 生成的类型元数据</param>
        public static Dictionary<string, object> ToDictionary<T>(this T obj, JsonTypeInfo<T> typeInfo)
        {
            // AOT 安全分支
            if (obj is IDictionaryConvertible convertible)
            {
                return convertible.ToDictionary();
            }

            // 源生成序列化后经 JsonNode 解析为字典（零反射，AOT 安全）
            return FromJsonObject(Json.Serialize(obj, typeInfo));
        }

        /// <summary>
        /// 对象转换为 Dictionary（无需显式 typeInfo：命中源生成上下文走零反射，未命中时反射兜底）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">需要转换的对象</param>
        /// <param name="context">源生成上下文，默认使用 <see cref="JsonContext"/>；传入自有上下文可让已登记类型走零反射路径</param>
        /// <remarks>
        /// 解析顺序：IDictionaryConvertible 接口 → 上下文按运行时类型解析元数据（零反射、AOT 安全）→ 反射式序列化兜底。
        /// 前两分支均不使用反射；只有目标类型（取 <c>obj.GetType()</c>）未登记在上下文中时才会走最后的反射兜底，
        /// 其行为与 Json.Serialize 的逃生舱重载一致（含 DegradeOptions 降级与 <see cref="Json.StrictTrimDiagnostics"/> 诊断）。
        /// 因存在反射兜底分支，本重载标注了 <c>Requires*</c>：严格 AOT/裁剪场景请改用
        /// <see cref="ToDictionary{T}(T, JsonTypeInfo{T})"/>，或将目标类型登记进上下文以稳定命中零反射分支。
        /// </remarks>
        [RequiresUnreferencedCode(UnAotMessage)]
        [RequiresDynamicCode(UnAotMessage)]
        public static Dictionary<string, object> ToDictionary<T>(this T obj, JsonSerializerContext context = null)
        {
            // AOT 安全分支
            if (obj is IDictionaryConvertible convertible)
            {
                return convertible.ToDictionary();
            }
            // 按运行时类型解析元数据：命中即零反射（声明为基类/接口时也不会丢派生成员）
            var typeInfo = (context ?? JsonContext.Instance).GetTypeInfo(obj == null ? typeof(T) : obj.GetType());
            if (typeInfo != null)
            {
                return FromJsonObject(JsonSerializer.Serialize(obj, typeInfo));
            }
            // 未登记：反射兜底
            return FromJsonObject(Json.Serialize(obj));
        }

        /// <summary>
        /// 源生成序列化后的 JSON 经 JsonNode 解析为字典并过滤空值
        /// </summary>
        /// <param name="json">序列化结果</param>
        private static Dictionary<string, object> FromJsonObject(string json)
        {
            var result = new Dictionary<string, object>();
            foreach (var kvp in Json.DeserializeToDic(json))
            {
                if (kvp.Value != null)
                {
                    result[kvp.Key] = kvp.Value;
                }
            }
            return result;
        }


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
        /// <param name="typeInfo">由 JsonSerializerContext 生成的类型元数据</param>
        /// <returns></returns>
        public static T GetValue<T>(this Dictionary<string, object> dic, string key, JsonTypeInfo<T> typeInfo)
        {
            return dic.GetValue<T>(key, default(T), typeInfo);
        }
        /// <summary>
        /// 从字典获取指定键的值（AOT 安全，基于源生成元数据）
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="key"></param>
        /// <param name="Default"></param>
        /// <param name="typeInfo">由 JsonSerializerContext 生成的类型元数据</param>
        /// <returns></returns>
        public static T GetValue<T>(this Dictionary<string, object> dic, string key, T Default, JsonTypeInfo<T> typeInfo)
        {
            if (dic != null && dic.TryGetValue(key, out var v))
            {
                // 最常见路径：值本身已是目标类型（如 Json.DeserializeToDic 后的基元类型）
                if (v is T t)
                {
                    return t;
                }
                // 兼容以 JSON 字符串形式存储的值：基于源生成元数据还原（零反射）
                if (v is string s)
                {
                    try
                    {
                        return Json.Deserialize<T>(s, typeInfo);
                    }
                    catch { }
                }
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
            var value = dic.GetString(key, defaultValue);
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
            var value = dic.GetString(key, defaultValue);
            return value.ToLower() == "true" || value == "1";
        }

    }
}