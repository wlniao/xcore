/*==============================================================================
    文件名称：ApiLog.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：API日志信息的封装
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
namespace Wlniao
{
    /// <summary>
    /// API日志
    /// </summary>
    public class ApiLog
    {
        #region 请求是否有效
        private bool _status = false;
        /// <summary>
        /// 结果是否有效
        /// </summary>
        public bool status
        {
            get
            {
                return _status;
            }
            set
            {
                _status = value;
            }
        }
        #endregion

        /// <summary>
        /// 链路追踪ID
        /// </summary>
        public string trace { get; set; }
        #region 所请求的API节点
        /// <summary>
        /// 所请求的API节点
        /// </summary>
        private string _apinode = "";
        /// <summary>
        /// 
        /// </summary>
        public string apinode
        {
            get
            {
                return _apinode;
            }
            set
            {
                _apinode = value;
            }
        }
        #endregion

        #region 所请求的API地址
        /// <summary>
        /// A所请求的API地址
        /// </summary>
        private string _apiurl = "";
        /// <summary>
        /// 
        /// </summary>
        public string apiurl
        {
            get
            {
                return _apiurl;
            }
            set
            {
                _apiurl = value;
            }
        }
        #endregion

        #region 请求结果
        /// <summary>
        /// 请求结果
        /// </summary>
        private string _message = "";
        /// <summary>
        /// 
        /// </summary>
        public string message
        {
            get
            {
                return _message;
            }
            set
            {
                _message = value;
            }
        }
        #endregion

        #region 请求所花时间
        private int _usetime = 0;
        /// <summary>
        /// 请求所花时间 单位ms
        /// </summary>
        public int usetime
        {
            get
            {
                return _usetime;
            }
            set
            {
                _usetime = value;
            }
        }
        #endregion

        internal DateTime _time = DateTime.Now;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ApiNode"></param>
        /// <param name="ApiUrl"></param>
        public ApiLog(String ApiNode, String ApiUrl)
        {
            _apinode = ApiNode;
            _apiurl = ApiUrl;
            _time = DateTime.Now;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ApiLog Success(String message = "")
        {
            _usetime = System.Convert.ToInt32((DateTime.Now - _time).TotalMilliseconds);
            _message = message;
            _status = true;
            return this;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public ApiLog Failed(String message = "")
        {
            _usetime = System.Convert.ToInt32((DateTime.Now - _time).TotalMilliseconds);
            _message = message;
            _status = false;
            return this;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public ApiLog Failed(Exception ex)
        {
            _usetime = System.Convert.ToInt32((DateTime.Now - _time).TotalMilliseconds);
            if (ex is System.Net.Http.HttpRequestException)
            {
                var _ex = ex as System.Net.Http.HttpRequestException;
                if (_ex.Message.ToLower().IndexOf("timeout") >= 0)
                {
                    _message = "timeout";
                }
                else
                {
                    _message = "net error";
                }
            }
            else if (ex is System.TimeoutException)
            {
                _message = "timeout";
            }
            else
            {
                _message = "exception";
            }
            _status = false;
            return this;
        }
    }
}