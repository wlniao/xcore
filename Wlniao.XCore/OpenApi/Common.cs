/*==============================================================================
    文件名称：Common.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：XCenter OpenApi请求工具
================================================================================
 
    Copyright 2016 XieChaoyi

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

namespace Wlniao.OpenApi
{
    /// <summary>
    /// XCenter OpenApi请求工具
    /// </summary>
    public class Common
    {
        /// <summary>
        /// Get请求
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="action"></param>
        /// <param name="kvs"></param>
        /// <returns></returns>
        public static string Get(string controller, string action, params KeyValuePair<string, string>[] kvs)
        {
            return XServer.Common.Get("openapi", controller, action, kvs);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="controller"></param>
        /// <param name="action"></param>
        /// <param name="kvs"></param>
        /// <returns></returns>
        public static ApiResult<T> Get<T>(string controller, string action, params KeyValuePair<string, string>[] kvs)
        {
            return XServer.Common.Get<T>("openapi", controller, action, kvs);
        }
        /// <summary>
        /// Get请求但只返回成功后的data部分
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="action"></param>
        /// <param name="kvs"></param>
        /// <returns></returns>
        public static string GetOnlyData(string controller, string action, params KeyValuePair<string, string>[] kvs)
        {
            return XServer.Common.GetOnlyData("openapi", controller, action, kvs);
        }
        /// <summary>
        /// Post请求（字符串）
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="action"></param>
        /// <param name="postData"></param>
        /// <param name="kvs"></param>
        /// <returns></returns>
        public static string Post(string controller, string action, string postData, params KeyValuePair<string, string>[] kvs)
        {
            return XServer.Common.Post("openapi", controller, action, postData, kvs);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="controller"></param>
        /// <param name="action"></param>
        /// <param name="postData"></param>
        /// <param name="kvs"></param>
        /// <returns></returns>
        public static ApiResult<T> Post<T>(string controller, string action, string postData, params KeyValuePair<string, string>[] kvs)
        {
            return XServer.Common.Post<T>("openapi", controller, action, postData, kvs);
        }

        /// <summary>
        /// Post请求（字节流）
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="action"></param>
        /// <param name="stream"></param>
        /// <param name="kvs"></param>
        /// <returns></returns>
        public static string Post(string controller, string action, System.IO.Stream stream, params KeyValuePair<string, string>[] kvs)
        {
            return XServer.Common.Post("openapi", controller, action, stream, kvs);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="controller"></param>
        /// <param name="action"></param>
        /// <param name="stream"></param>
        /// <param name="kvs"></param>
        /// <returns></returns>
        public static ApiResult<T> Post<T>(string controller, string action, System.IO.Stream stream, params KeyValuePair<string, string>[] kvs)
        {
            return XServer.Common.Post<T>("openapi", controller, action, stream, kvs);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        /// <param name="path"></param>
        /// <param name="kvs"></param>
        /// <returns></returns>
        public static string CreateUrl(string app, string path, params KeyValuePair<string, string>[] kvs)
        {
            return XServer.Common.CreateUrl(false, app, path, kvs);
        }
    }
}
