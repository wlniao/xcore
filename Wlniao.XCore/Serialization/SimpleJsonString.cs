/*==============================================================================
    文件名称：SimpleJsonString.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：将简单的对象转换成 json 字符串，不支持子对象的序列化
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
using Wlniao.Runtime;

namespace Wlniao.Serialization
{
    /// <summary>
    /// 将简单的对象转换成 json 字符串，不支持子对象的序列化
    /// </summary>
    public partial class SimpleJsonString
    {

        /// <summary>
        /// 将对象列表转换成 json 字符串
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static string ConvertList(IList list)
        {
            StringBuilder sb = new StringBuilder("[");
            if (list != null && list.Count > 0)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    sb.Append(Serialization.JsonString.Convert(list[i]));
                    if (i < list.Count - 1)
                    {
                        sb.Append(",");
                    }
                }
            }
            sb.Append("]");
            return sb.ToString();
        }

        private static string EncodeQuoteAndClearLine(string src)
        {
            //return src.Replace( "\"", "\\\"" ).Replace( "\r\n", "" ).Replace( "\n", "" ).Replace( "\r", "" ).Replace( "\r\n", "" );
            return JsonString.ClearNewLine(src);
        }
        /// <summary>
        /// 将对象转换成 json 字符串
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string ConvertObject(object obj)
        {

            var builder = new StringBuilder();
            var properties = Reflection.GetPropertyList(obj.GetType());
            builder.Append("{");
            foreach (var info in properties)
            {
                if (info.IsDefined(typeof(NotSerializeAttribute), false))
                {
                    continue;
                }
                object propertyValue = Reflection.GetPropertyValue(obj, info.Name);
                if ((info.PropertyType == typeof(int)) || (info.PropertyType == typeof(long)) || (info.PropertyType == typeof(short)) || (info.PropertyType == typeof(float)) || (info.PropertyType == typeof(decimal)))
                {
                    builder.AppendFormat("\"{0}\":{1}", info.Name, propertyValue);
                }
                else if (info.PropertyType == typeof(bool))
                {
                    builder.AppendFormat("\"{0}\":{1}", info.Name, propertyValue.ToString().ToLower());
                }
                else if (Reflection.IsBaseType(info.PropertyType))
                {
                    builder.AppendFormat("\"{0}\":\"{1}\"", info.Name, EncodeQuoteAndClearLine(propertyValue == null ? string.Empty : propertyValue.ToString()));
                }
                else
                {
                    object str = Reflection.GetPropertyValue(propertyValue, "Id");
                    builder.AppendFormat("\"{0}\":\"{1}\"", info.Name, EncodeQuoteAndClearLine(str == null ? string.Empty : str.ToString()));
                }
                builder.Append(",");
            }
            return (builder.ToString().Trim().TrimEnd(',') + "}");
        }
    }
}