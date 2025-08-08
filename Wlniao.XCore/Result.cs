/*==============================================================================
    文件名称：Result.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
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
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Wlniao
{
    /// <summary>
    /// 对结果信息的封装(有效或错误)
    /// </summary>
    public class Result
    {
        private List<string> _errors;
        private object _info;
        /// <summary>
        /// 
        /// </summary>
        public Result()
        {
            _errors = new List<string>();
        }
        /// <summary>
        /// 根据错误信息构建 result
        /// </summary>
        /// <param name="errorMsg"></param>
        public Result(string errorMsg)
        {
            _errors = new List<string>();
            _errors.Add(errorMsg);
        }
        /// <summary>
        /// 添加错误信息
        /// </summary>
        /// <param name="errorMsg"></param>
        public void Add(string errorMsg)
        {
            _errors.Add(errorMsg);
        }
        /// <summary>
        /// 附带的对象
        /// </summary>
        public object Info
        {
            get { return _info; }
            set { _info = value; }
        }
        /// <summary>
        /// 获取错误信息列表中第一条记录，没有时返回null
        /// </summary>
        public string Error
        {
            get
            {
                if (_errors == null || _errors.Count == 0)
                {
                    return null;
                }
                else
                {
                    return _errors[0];
                }
            }
        }
        /// <summary>
        /// 获取所有错误信息的列表
        /// </summary>
        public List<string> Errors
        {
            get { return _errors; }
            set { _errors = value; }
        }
        /// <summary>
        /// 结果是否全部正确有效
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public bool IsValid
        {
            get { return _errors.Count == 0; }
        }
        /// <summary>
        /// 结果是否包含错误
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public bool HasErrors
        {
            get { return _errors.Count > 0; }
        }
        /// <summary>
        /// 合并结果信息
        /// </summary>
        /// <param name="result"></param>
        public void Join(Result result)
        {
            foreach (var str in result.Errors)
            {
                Add(str);
            }
            if (_info == null)
            {
                _info = result.Info;
            }
        }
    }
}