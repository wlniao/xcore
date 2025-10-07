/*==============================================================================
    文件名称：Json.cs
    适用环境：CoreCLR 5.0,.NET Framework 6.0/8.0/10.0
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

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
namespace Wlniao
{
    /// <summary>
    /// Json序列化、反序列化工具
    /// </summary>
    public class Json
    {
        /// <summary>
        /// 将 json 字符串反序列化为对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="jsonString">json 字符串</param>
        /// <returns></returns>
        public static T Deserialize<T>(string jsonString)
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(jsonString, XCore.JsonSerializerOptions);
        }
        
        /// <summary>
        /// 将 json 字符串反序列化为字典对象
        /// </summary>
        /// <param name="jsonString"></param>
        /// <returns></returns>
        public static Dictionary<string, object> DeserializeToDic(string jsonString)
        {
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString, XCore.JsonSerializerOptions);
        }
        
        /// <summary>
        /// 将 json 字符串反序列化为对象列表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="jsonString">json 字符串</param>
        /// <returns>返回对象列表</returns>
        public static List<T> DeserializeToList<T>(string jsonString)
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<T>>(jsonString, XCore.JsonSerializerOptions);
        }

        /// <summary>
        /// 将对象序列化为json字符串,支持子对象的序列化
        /// </summary>
        /// <param name="obj">序列化的对象</param>
        /// <returns></returns>
        public static string Serialize<T>(T obj)
        {
            return JsonSerializer.Serialize<T>(obj, XCore.JsonSerializerOptions);
        }
        
        /// <summary>
        /// 将对象序列化为json字符串,支持子对象的序列化
        /// </summary>
        /// <param name="obj">序列化的对象</param>
        /// <param name="kvs">序列化的对象</param>
        /// <returns></returns>
        public static string Serialize<T>(T obj, params KeyValuePair<string, string>[] kvs)
        {
            var s = Serialize(obj);
            if (kvs == null || kvs.Length == 0 || !s.StartsWith('{')) return s;
            var strPx = kvs.Aggregate("", (current, kv) => current + ("\"" + kv.Key + "\":\"" + kv.Value + "\","));
            var temp = s[1..].Trim();
            if (temp == "}")
            {
                strPx = strPx[..^1];
            }
            s = "{" + strPx + temp;
            return s;
        }



        /// <summary>
        /// 将引号、冒号、逗号进行解码
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static string Decode(string str)
        {
            return str.Replace("&quot;", "\"").Replace("&#58;", ":").Replace("&#44;", ",").Replace("\\'", "'");
        }
    }
}

