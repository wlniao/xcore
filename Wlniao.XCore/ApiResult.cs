/*==============================================================================
    文件名称：ApiResult.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：API返回信息的封装
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

using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;
namespace Wlniao
{
    /// <summary>
    /// API返回结果类型
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ApiResult<T>
    {
        /// <summary>
        /// 业务返回数据
        /// </summary>
        private T _data = default(T);

        /// <summary>
        /// 服务端提示消息
        /// </summary>
        public string message { get; set; }

        /// <summary>
        /// 服务端调试消息
        /// </summary>
        [JsonIgnore]
        public string debuger { get; set; }

        /// <summary>
        /// 是否成功获得预期结果
        /// </summary>
        [DefaultValue(false)]
        public bool success { get; set; }

        /// <summary>
        /// 服务端消息提示可否直接输出
        /// </summary>
        [DefaultValue(false)]
        public bool tips { get; set; }

        /// <summary>
        /// 产生提示消息的服务端节点
        /// </summary>
        [JsonIgnore]
        public string node { get; set; }

        /// <summary>
        /// 业务受理状态码
        /// </summary>
        [DefaultValue("")]
        public string code { get; set; }
        /// <summary>
        /// 链路追踪ID
        /// </summary>
        public string traceid { get; set; }
        /// <summary>
        /// 扩展数据
        /// </summary>
        [JsonIgnore]
        public Dictionary<string, object> ExtendData = new Dictionary<string, object>();

        /// <summary>
        /// 业务返回数据
        /// </summary>
        public T data
        {
            get => _data;
            set => _data = value;
        }

        /// <summary>
        /// 
        /// </summary>
        public ApiResult()
        {
            node = XCore.WebNode;
            code = "";
            message = "";
            success = false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Message"></param>
        /// <param name="Success"></param>
        public ApiResult(string Message, bool Success = false)
        {
            this.code = "";
            this.node = XCore.WebNode;
            this.message = Message;
            this.success = Success;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Node"></param>
        /// <param name="Code"></param>
        /// <param name="Message"></param>
        /// <param name="Success"></param>
        public ApiResult(string Node, string Code, string Message, bool Success = false)
        {
            this.node = Node;
            this.code = Code;
            this.message = Message;
            this.success = Success;
        }
    }
}