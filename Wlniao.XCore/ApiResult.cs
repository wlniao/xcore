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
using System;
using System.Collections.Generic;
using System.Text;
namespace Wlniao
{
    /// <summary>
    /// API返回结果类型
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ApiResult<T>
    {
        /// <summary>
        /// 结果是否有效
        /// </summary>
        public bool success { get; set; }

        /// <summary>
        /// 附带返回的消息
        /// </summary>
        public string message { get; set; }

        /// <summary>
        /// 状态码
        /// </summary>
        public string code { get; set; }

        #region 附带返回的数据
        private T _data = default(T);
        /// <summary>
        /// 附带返回的数据
        /// </summary>
        public T data
        {
            get
            {
                return _data;
            }
            set
            {
                _data = value;
            }
        }
        #endregion

        #region 附带返回的数据
        private List<ApiLog> _logs = null;
        /// <summary>
        /// 请求日志
        /// </summary>
        public List<ApiLog> logs
        {
            get
            {
                return _logs;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="apiLog"></param>
        public void PutLog(ApiLog apiLog)
        {
            if (apiLog != null && !string.IsNullOrEmpty(apiLog.apinode))
            {
                if (_logs == null)
                {
                    _logs = new List<ApiLog>();
                }
                _logs.Add(apiLog);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="apiLogs"></param>
        public void PutLog(List<ApiLog> apiLogs)
        {
            if (apiLogs != null && apiLogs.Count > 0)
            {
                if (_logs == null)
                {
                    _logs = new List<ApiLog>();
                }
                foreach (var apiLog in apiLogs)
                {
                    _logs.Add(apiLog);
                }
            }
        }
        #endregion
        /// <summary>
        /// 
        /// </summary>
        public ApiResult()
        {
            code = "";
            message = "";
            success = false;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="apiLog"></param>
        public ApiResult(ApiLog apiLog)
        {
            if (apiLog != null)
            {
                _logs = new List<ApiLog>();
                _logs.Add(apiLog);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Message"></param>
        /// <param name="Success"></param>
        public ApiResult(String Message, Boolean Success = false)
        {
            this.message = Message;
            this.success = Success;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Code"></param>
        /// <param name="Message"></param>
        /// <param name="Success"></param>
        public ApiResult(String Code, String Message, Boolean Success = false)
        {
            this.code = Code;
            this.message = Message;
            this.success = Success;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="apiLogs"></param>
        public ApiResult(List<ApiLog> apiLogs)
        {
            if (apiLogs != null && apiLogs.Count > 0)
            {
                if (_logs == null)
                {
                    _logs = new List<ApiLog>();
                }
                foreach (var apiLog in apiLogs)
                {
                    _logs.Add(apiLog);
                }
            }
        }
    }
}