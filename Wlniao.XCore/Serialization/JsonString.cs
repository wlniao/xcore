/*==============================================================================
    文件名称：JsonString.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：Json 序列化工具：将对象转换成 json 字符串
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
using System.Text;
using System.Collections;
using System.Reflection;
namespace Wlniao.Serialization
{

    /// <summary>
    /// Json 序列化工具：将对象转换成 json 字符串
    /// </summary>
    public partial class JsonString
    {
        private static Boolean getDefaultIsBreakline()
        {
            return false;
        }
        private static String empty()
        {
            return "\"\"";
        }
        /// <summary>
        /// 将对象转换成 json 字符串
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static String Convert(Object obj)
        {
            return Convert(obj, getDefaultIsBreakline(), "");
        }
        /// <summary>
        /// 将对象转换成 json 字符串
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="TypeList"></param>
        /// <returns></returns>
        public static String Convert(Object obj, String TypeList)
        {
            return Convert(obj, getDefaultIsBreakline(), TypeList);
        }
        /// <summary>
        /// 将对象转换成 json 字符串
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="isBreakline">是否换行(默认不换行)</param>
        /// <param name="TypeList"></param>
        /// <returns></returns>
        public static String Convert(Object obj, Boolean isBreakline, String TypeList)
        {
            if (obj == null)
            {
                return empty();
            }
            else
            {
                var t = obj.GetType();
                if (t == typeof(String))
                {
                    string _temp = "";
                    if (obj != null)
                    {
                        _temp = obj.ToString();
                        _temp = ClearNewLine(_temp);    //消除换行
                    }
                    return "\"" + _temp + "\"";         //转义双引号
                }
                if (t.IsArray)
                {
                    return ConvertArray((object[])obj, TypeList);
                }
                if (rft.IsInterface(t, typeof(IList)))
                {
                    return ConvertList((IList)obj, TypeList);
                }
                if (rft.IsInterface(t, typeof(IDictionary)))
                {
                    return ConvertDictionary((IDictionary)obj, isBreakline, TypeList);
                }
                if (t == typeof(int) ||
                    t == typeof(long) ||
                    t == typeof(short) ||
                    t == typeof(decimal) ||
                    t == typeof(double) ||
                    t == typeof(float))
                {
                    return obj.ToString();
                }
                if (t == typeof(Boolean))
                {
                    return obj.ToString().ToLower();
                }
                if (t == typeof(DateTime))
                {
                    return "\"" + obj.ToString() + "\"";
                }
                return ConvertObject(obj, isBreakline, TypeList);
            }
        }
        /// <summary>
        /// 清除json字符串中的换行符
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static String ClearNewLine(String str)
        {
            if (str == null) return null;
            return str
                    .Replace(@"\", @"\\")
                    .Replace("\"", "\\" + "\"")
                    .Replace("\r\n", "")
                    .Replace("\r", "")
                    .Replace("\n", "")
                    .Replace("\t", "");
        }
        /// <summary>
        /// 将数组转换成 json 字符串
        /// </summary>
        /// <param name="arrObj"></param>
        /// <param name="TypeList"></param>
        /// <returns></returns>
        public static String ConvertArray(object[] arrObj, String TypeList)
        {
            if (arrObj == null)
            {
                return "[]";
            }
            var sb = new StringBuilder("[");
            for (int i = 0; i < arrObj.Length; i++)
            {
                if (arrObj[i] == null)
                {
                    continue;
                }
                sb.Append(Convert(arrObj[i], getDefaultIsBreakline(), TypeList));
                if (i < arrObj.Length - 1)
                {
                    sb.Append(",");
                }
            }
            sb.Append("]");
            return sb.ToString();
        }
        /// <summary>
        /// 将List转换成 json 字符串
        /// </summary>
        /// <param name="list"></param>
        /// <param name="TypeList"></param>
        /// <returns></returns>
        public static String ConvertList(IList list, String TypeList)
        {
            return ConvertList(list, getDefaultIsBreakline(), TypeList);
        }
        /// <summary>
        /// 将List转换成 json 字符串
        /// </summary>
        /// <param name="list"></param>
        /// <param name="isBreakline">是否换行(默认不换行，阅读起来更加清晰)</param>
        /// <param name="TypeList"></param>
        /// <returns></returns>
        public static String ConvertList(IList list, Boolean isBreakline, String TypeList)
        {
            if (list == null)
            {
                return "[]";
            }
            var sb = new StringBuilder("[");
            if (isBreakline)
            {
                sb.AppendLine();
            }
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == null)
                {
                    continue;
                }
                sb.Append(Convert(list[i], isBreakline, TypeList));
                if (i < list.Count - 1)
                {
                    sb.Append(",");
                }
                if (isBreakline)
                {
                    sb.AppendLine();
                }
            }
            sb.Append("]");
            return sb.ToString();
        }
        /// <summary>
        /// 将字典 Dictionary 转换成 json 字符串
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="TypeList"></param>
        /// <returns></returns>
        public static String ConvertDictionary(IDictionary dic, String TypeList)
        {
            return ConvertDictionary(dic, getDefaultIsBreakline(), TypeList);
        }
        /// <summary>
        /// 将字典 Dictionary 转换成 json 字符串
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="isBreakline">是否换行(默认不换行)</param>
        /// <param name="TypeList"></param>
        /// <returns></returns>
        public static String ConvertDictionary(IDictionary dic, Boolean isBreakline, String TypeList)
        {
            if (dic == null)
            {
                return empty();
            }
            var builder = new StringBuilder("{");
            if (isBreakline)
            {
                builder.AppendLine();
            }
            foreach (DictionaryEntry pair in dic)
            {
                builder.Append("\"");
                builder.Append(pair.Key);
                builder.Append("\":");
                builder.Append(Convert(pair.Value, isBreakline, TypeList));
                builder.Append(",");
                if (isBreakline) builder.AppendLine();
            }
            var result = builder.ToString().Trim().TrimEnd(',');
            if (isBreakline)
            {
                result += Environment.NewLine;
            }
            return result + "}";
        }
        /// <summary>
        /// 将对象转换成 json 字符串
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="TypeList"></param>
        /// <returns></returns>
        public static String ConvertObject(Object obj, String TypeList)
        {
            return ConvertObject(obj, getDefaultIsBreakline(), TypeList);
        }
        /// <summary>
        /// 将对象转换成 json 字符串
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="isBreakline">是否换行(默认不换行)</param>
        /// <returns></returns>
        public static String ConvertObject(Object obj, Boolean isBreakline)
        {
            return ConvertObject(obj, isBreakline, true, "");
        }
        /// <summary>
        /// 将对象转换成 json 字符串
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="isBreakline">是否换行(默认不换行)</param>
        /// <param name="TypeList"></param>
        /// <returns></returns>
        public static String ConvertObject(Object obj, Boolean isBreakline, String TypeList)
        {
            return ConvertObject(obj, isBreakline, true, TypeList);
        }
        /// <summary>
        /// 将对象转换成 json 字符串
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="isBreakline">是否换行(默认不换行)</param>
        /// <param name="withQuotation">属性名是否使用引号(默认不启用)</param>
        /// <param name="TypeList"></param>
        /// <returns></returns>
        public static String ConvertObject(Object obj, Boolean isBreakline, Boolean withQuotation, String TypeList)
        {
            TypeList = TypeList + obj.GetType().FullName + ",";
            StringBuilder builder = new StringBuilder();
            builder.Append("{");
            if (isBreakline) builder.AppendLine();
            Object idValue = "";
            Object nameValue = "";
            PropertyInfo[] properties = rft.GetPropertyList(obj.GetType());
            foreach (PropertyInfo info in properties)
            {
                var methodInfo = info.GetGetMethod();
                if (methodInfo == null || methodInfo.IsStatic || !methodInfo.IsPublic)
                {
                    continue;
                }
                else if (info.GetCustomAttribute(typeof(NotSerializeAttribute)) != null)
                {
                    continue;
                }
                Object propertyValue = info.GetValue(obj, null);
                String jsonValue = string.Empty;
                try
                {
                    if (info.PropertyType == typeof(String))
                    {
                        jsonValue = "\"\"";
                        if (propertyValue != null)
                        {
                            var _temp = propertyValue.ToString();
                            _temp = ClearNewLine(_temp);    //消除换行
                            jsonValue = "\"" + _temp + "\"";    //转义双引号
                        }
                    }
                    else if (info.PropertyType == typeof(int) ||
                        info.PropertyType == typeof(long) ||
                        info.PropertyType == typeof(short) ||
                        info.PropertyType == typeof(decimal) ||
                        info.PropertyType == typeof(double) ||
                        info.PropertyType == typeof(float))
                    {
                        jsonValue = propertyValue.ToString();
                    }
                    else if (info.PropertyType == typeof(Boolean))
                    {
                        jsonValue = propertyValue.ToString().ToLower();
                    }
                    else if (info.PropertyType == typeof(DateTime))
                    {
                        jsonValue = "\"" + propertyValue.ToString() + "\"";
                    }
                    else if (info.PropertyType.IsArray)
                    {
                        jsonValue = ConvertArray((object[])propertyValue, TypeList);
                    }
                    else if (rft.IsInterface(info.PropertyType, typeof(IList)))
                    {
                        jsonValue = ConvertList((IList)propertyValue, isBreakline, "");
                    }
                    else
                    {
                        jsonValue = Convert(propertyValue, isBreakline, TypeList);
                    }
                }
                catch{ }
                if (withQuotation)
                {
                    builder.AppendFormat("\"{0}\":{1}", info.Name, jsonValue);
                }
                else
                {
                    builder.AppendFormat("{0}:{1}", info.Name, jsonValue);
                }
                builder.Append(",");
                if (isBreakline)
                {
                    builder.AppendLine();
                }
            }
            String result = builder.ToString().Trim().TrimEnd(',');
            if (isBreakline) result += Environment.NewLine;
            return result + "}";
        }
    }
}
