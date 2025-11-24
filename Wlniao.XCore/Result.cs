/*==============================================================================
    文件名称：Result.cs
    适用环境：CoreCLR
    功能描述：对结果信息的封装(有效或错误)
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
using System.Text.Json.Serialization;
namespace Wlniao
{
    /// <summary>
    /// 接口返回实例
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Result<T>
    {
        
        /// <summary>
        /// 返回状态码（200表示成功）
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        // ReSharper disable once MemberCanBePrivate.Global
        public int Code { get; set; } = 0;
        
        /// <summary>
        /// 返回的数据
        /// </summary>
        // [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public T? Data { get; set; } = default;
        
        /// <summary>
        /// 返回的消息内容
        /// </summary>
        // [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 是否存在错误情况
        /// </summary>
        /// <returns></returns>
        // [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.Always)]
        public bool Success => Code == 200;

        /// <summary>
        /// 是否存在错误情况
        /// </summary>
        /// <returns></returns>
        [System.Text.Json.Serialization.JsonIgnore]
        public bool HasError => Code != 200;

        /// <summary>
        /// 设置返回内容
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public Result<T> SetData(T data)
        {
            this.Code = 200;
            this.Data = data;
            return this;
        }

        /// <summary>
        /// 设置返回消息
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public Result<T> SetMessage(string message)
        {
            this.Message = message;
            return this;
        }
        /// <summary>
        /// 设置返回消息
        /// </summary>
        /// <param name="message"></param>
        /// <param name="statusCode"></param>
        /// <returns></returns>
        public Result<T> SetMessage(string message, int statusCode)
        {
            this.Code = statusCode;
            this.Message = message;
            return this;
        }
        
        /// <summary>
        /// 设置返回状态
        /// </summary>
        /// <param name="statusCode"></param>
        /// <returns></returns>
        public Result<T> SetStatusCode(int statusCode)
        {
            this.Code = statusCode;
            return this;
        }
        /// <summary>
        /// 标记状态为成功
        /// </summary>
        /// <returns></returns>
        public Result<T> SetSuccess()
        {
            this.Code = 200;
            return this;
        }

        /// <summary>
        /// 返回成功结果
        /// </summary>
        /// <param name="data"></param>
        /// <param name="statusCode"></param>
        /// <returns></returns>
        public static Result<T> OutSuccess(T data, int statusCode = 200)
        {
            return new Result<T> { Code = statusCode, Data = data, Message = "success" };
        }
        
        /// <summary>
        /// 返回成功结果
        /// </summary>
        /// <param name="message"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Result<T> OutSuccess(string message, T data)
        {
            return new Result<T> { Code = 200, Data = data, Message = message };
        }
        
        /// <summary>
        /// 返回错误消息
        /// </summary>
        /// <param name="message"></param>
        /// <param name="statusCode"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Result<T> OutMessage(string message, int statusCode, T data = default(T)!)
        {
            return new Result<T> { Code = statusCode, Data = data, Message = message };
        }
    }
}