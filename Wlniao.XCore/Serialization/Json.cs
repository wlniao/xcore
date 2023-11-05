/*==============================================================================
    文件名称：Json.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：Json序列化、反序列化工具
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
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
namespace Wlniao.Serialization
{
    /// <summary>
    /// Json序列化、反序列化工具
    /// </summary>
    public class Json
    {
        /// <summary>
        /// 获取键值对中某一字段的值
        /// </summary>
        /// <param name="map">键值对</param>
        /// <param name="field">字段名称</param>
        /// <returns></returns>
        public static Object GetField(Dictionary<String, object> map, String field)
        {
            foreach (KeyValuePair<String, object> pair in map)
            {
                if (pair.Key == field)
                {
                    return pair.Value;
                }
            }
            return null;
        }
        /// <summary>
        /// 获取键值对中某一字段的值
        /// </summary>
        /// <param name="map">键值对</param>
        /// <param name="field">字段名称</param>
        /// <returns></returns>
        public static String GetFieldStr(Dictionary<String, object> map, String field)
        {
            foreach (KeyValuePair<String, object> pair in map)
            {
                if (pair.Key == field)
                {
                    return pair.Value.ToString();
                }
            }
            return null;
        }
        /// <summary>
        /// 获取 json 字符串中某一字段的值
        /// </summary>
        /// <param name="oneJsonString">json 字符串</param>
        /// <param name="field">字段名称</param>
        /// <returns></returns>
        public static Object GetField(String oneJsonString, String field)
        {
            var map = JsonParser.Parse(oneJsonString) as Dictionary<String, object>;
            foreach (KeyValuePair<String, object> pair in map)
            {
                if (pair.Key == field)
                {
                    return pair.Value;
                }
            }
            return null;
        }
        /// <summary>
        /// 获取 json 字符串中某一字段的值
        /// </summary>
        /// <param name="oneJsonString">json 字符串</param>
        /// <param name="field">字段名称</param>
        /// <returns></returns>
        public static String GetFieldStr(String oneJsonString, String field)
        {
            var map = JsonParser.Parse(oneJsonString) as Dictionary<String, object>;
            foreach (KeyValuePair<String, object> pair in map)
            {
                if (pair.Key == field)
                {
                    return pair.Value.ToString();
                }
            }
            return null;
        }
        /// <summary>
        /// 将 json 字符串反序列化为对象
        /// </summary>
        /// <param name="oneJsonString">json 字符串</param>
        /// <param name="t">目标类型</param>
        /// <returns></returns>
        public static Object ToObject(String oneJsonString, Type t)
        {
            var map = JsonParser.Parse(oneJsonString) as Dictionary<String, object>;
            return setValueToObject(t, map);
        }

        /// <summary>
        /// 将 json 字符串反序列化为对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="jsonString">json 字符串</param>
        /// <returns></returns>
        public static T ToObject<T>(String jsonString)
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(jsonString);
            //Object result = ToObject(jsonString, typeof(T));
            //return (T)result;
        }

        /// <summary>
        /// 将对象序列化为json字符串,支持子对象的序列化
        /// </summary>
        /// <param name="obj">序列化的对象</param>
        /// <returns></returns>
        public static String ToString(Object obj)
        {
            return Serialization.JsonString.Convert(obj);
        }
        /// <summary>
        /// 将对象序列化为json字符串,支持子对象的序列化
        /// </summary>
        /// <param name="obj">序列化的对象</param>
        /// <param name="kvs">序列化的对象</param>
        /// <returns></returns>
        public static String ToString(Object obj, params KeyValuePair<string, string>[] kvs)
        {
            string s = Serialization.JsonString.Convert(obj);
            if (kvs != null && kvs.Length > 0)
            {
                string strPx = "";
                foreach (var kv in kvs)
                {
                    strPx += "\"" + kv.Key + "\":\"" + kv.Value + "\",";
                }
                string temp = s.Substring(1).Trim();
                if (temp == "}")
                {
                    strPx = strPx.Substring(0, strPx.Length - 1);
                }
                s = "{" + strPx + temp;
            }
            return s;
        }
        /// <summary>
        /// 将对象集合序列化为json字符串,不支持子对象的序列化
        /// </summary>
        /// <param name="list">序列化的List对象</param>
        /// <returns></returns>
        public static String ToStringList(IList list)
        {
            return Serialization.SimpleJsonString.ConvertList(list);
        }

        /// <summary>
        /// 将 json 字符串反序列化为对象列表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="jsonString">json 字符串</param>
        /// <returns>返回对象列表</returns>
        public static List<T> ToList<T>(String jsonString)
        {
            List<T> list = new List<T>();
            if (strUtil.IsNullOrEmpty(jsonString)) return list;
            List<object> lists = Serialization.JsonParser.Parse(jsonString) as List<object>;
            if (lists != null)
            {
                foreach (Dictionary<String, object> map in lists)
                {
                    if (typeof(T) == map.GetType())
                    {
                        T t = (T)System.Convert.ChangeType(map, typeof(T));
                        list.Add(t);
                    }
                    else
                    {
                        Object result = Serialization.Json.setValueToObject(typeof(T), map);
                        list.Add((T)result);
                    }
                }
            }
            return list;
        }


        /// <summary>
        /// 将字典序列化为 json 字符串
        /// </summary>
        /// <param name="dic"></param>
        /// <returns></returns>
        public static String DicToString(Dictionary<String, object> dic)
        {
            return JsonString.ConvertDictionary(dic, false, "");
        }

        /// <summary>
        /// 将 json 字符串反序列化为字典对象
        /// </summary>
        /// <param name="JsonString"></param>
        /// <returns></returns>
        public static Dictionary<String, object> StringToDic(String JsonString)
        {
            String str = trimBeginEnd(JsonString, "[", "]");
            if (strUtil.IsNullOrEmpty(str))
            {
                return new Dictionary<String, object>();
            }
            return JsonParser.Parse(str) as Dictionary<String, object>;
        }

        /// <summary>
        /// 将 json 字符串反序列化为字典对象的列表
        /// </summary>
        /// <param name="jsonString"></param>
        /// <returns></returns>
        public static List<Dictionary<String, object>> StringToDicList(String jsonString)
        {


            List<object> list = JsonParser.Parse(jsonString) as List<object>;

            List<Dictionary<String, object>> results = new List<Dictionary<String, object>>();
            foreach (Object obj in list)
            {

                Dictionary<String, object> item = obj as Dictionary<String, object>;
                results.Add(item);
            }

            return results;
        }

        /// <summary>
        /// 去除 /* 类似的 */ 注释
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static String ClearStr(String str)
        {
            str = System.Text.RegularExpressions.Regex.Replace(str, @"\/\*.+\*\/", "");
            str = str.Replace("\r\n", "");
            str = str.Replace("\r", "");
            str = System.Text.RegularExpressions.Regex.Replace(str, "\\,\\s+\\\"", ",\"");
            return str;
        }

        internal static Object setValueToObject(Type t, Dictionary<String, object> map)
        {
            var result = Runtime.Reflection.GetInstance(t);
            if (map != null)
            {
                var properties = Runtime.Reflection.GetPropertyList(t);
                foreach (KeyValuePair<String, object> pair in map)
                {

                    String pName = pair.Key;
                    String pValue = pair.Value.ToString();


                    PropertyInfo info = getPropertyInfo(properties, pName);
                    //if ((info != null) && !info.IsDefined(typeof(NotSaveAttribute), false))
                    if (info != null)
                    {
                        Object objValue = null;

                        if (Runtime.Reflection.IsBaseType(info.PropertyType))
                        {
                            objValue = System.Convert.ChangeType(pValue, info.PropertyType);
                            Runtime.Reflection.SetPropertyValue(result, pName, objValue);
                        }
                        else if (info.PropertyType == typeof(Dictionary<String, object>))
                        {
                            objValue = pair.Value;
                            Runtime.Reflection.SetPropertyValue(result, pName, objValue);
                        }
                        else if (info.PropertyType == typeof(Dictionary<String, String>))
                        {
                            Dictionary<String, String> dic = new Dictionary<string, string>();
                            try
                            {
                                var keys = (Dictionary<String, object>.KeyCollection)Runtime.Reflection.GetPropertyValue(pair.Value, "Keys");
                                var values = (Dictionary<String, object>.ValueCollection)Runtime.Reflection.GetPropertyValue(pair.Value, "Values");
                                if (keys != null && values != null && keys.Count == values.Count)
                                {
                                    string[] _keys = new string[keys.Count];
                                    string[] _values = new string[values.Count];
                                    int k = 0;
                                    foreach (var key in keys)
                                    {
                                        _keys[k] = key;
                                        k++;
                                    }
                                    int v = 0;
                                    foreach (var value in values)
                                    {
                                        _values[v] = value.ToString();
                                        v++;
                                    }
                                    for (int i = 0; i < keys.Count; i++)
                                    {
                                        dic.Add(_keys[i], _values[i]);
                                    }
                                }
                            }
                            catch { }
                            objValue = dic;
                            Runtime.Reflection.SetPropertyValue(result, pName, objValue);
                        }
                        else if (Runtime.Reflection.IsInterface(info.PropertyType, typeof(IList)))
                        {
                            try
                            {
                                var list = (List<Object>)pair.Value;
                                Runtime.Reflection.SetPropertyValue(result, pName, list);
                            }
                            catch { }
                        }
                        else
                        {
                            try
                            {
                                objValue = Runtime.Reflection.GetInstance(info.PropertyType);
                                try
                                {
                                    var keys = (Dictionary<String, object>.KeyCollection)Runtime.Reflection.GetPropertyValue(pair.Value, "Keys");
                                    var values = (Dictionary<String, object>.ValueCollection)Runtime.Reflection.GetPropertyValue(pair.Value, "Values");
                                    if (keys != null && values != null && keys.Count == values.Count)
                                    {
                                        string[] _keys = new string[keys.Count];
                                        object[] _values = new object[values.Count];
                                        int k = 0;
                                        foreach (var key in keys)
                                        {
                                            _keys[k] = key;
                                            k++;
                                        }
                                        int v = 0;
                                        foreach (var value in values)
                                        {
                                            if (value is string)
                                            {
                                                _values[v] = value.ToString();
                                            }
                                            else
                                            {
                                                _values[v] = value;
                                            }
                                            v++;
                                        }
                                        for (int i = 0; i < keys.Count; i++)
                                        {
                                            Runtime.Reflection.SetPropertyValue(objValue, _keys[i], _values[i]);
                                        }
                                    }
                                }
                                catch { }
                                Runtime.Reflection.SetPropertyValue(result, pair.Key, objValue);
                            }
                            catch { }
                        }
                    }

                }
            }
            return result;
        }       


        private static PropertyInfo getPropertyInfo(PropertyInfo[] propertyList, String pName)
        {
            foreach (PropertyInfo info in propertyList)
            {
                if (info.Name.Equals(pName))
                {
                    return info;
                }
            }
            return null;
        }

        private static String trimBeginEnd(String str, String beginStr, String endStr)
        {
            str = str.Trim();
            str = strUtil.TrimStart(str, beginStr);
            str = strUtil.TrimEnd(str, endStr);
            str = str.Trim();
            return str;
        }

        /// <summary>
        /// 将引号、冒号、逗号进行编码
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static String Encode(String str)
        {
            return str.Replace("\"", "&quot;").Replace(":", "&#58;").Replace(",", "&#44;").Replace("'", "\\'");
        }

        /// <summary>
        /// 将引号、冒号、逗号进行解码
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static String Decode(String str)
        {
            return str.Replace("&quot;", "\"").Replace("&#58;", ":").Replace("&#44;", ",").Replace("\\'", "'");
        }
    }
}

