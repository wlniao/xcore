/*==============================================================================
    文件名称：LogLevel.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：日志的等级
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
namespace Wlniao.Log
{
    /// <summary>
    /// 日志实体
    /// </summary>
    public class LogEntrie
    {
        /// <summary>
        /// 日志标签
        /// </summary>
        public IDictionary<string, string> tags = new Dictionary<string, string>();

        /// <summary>
        /// 日志时间
        /// </summary>
        public DateTime time { get; set; }

        /// <summary>
        /// 日志内容
        /// </summary>
        public string content { get; set; }

        /// <summary>
        /// RFC3339Nano格式时间
        /// </summary>
        public string ts
        {
            get
            {
                return time.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).Ticks.ToString() + "00";
            }
        }
    }
}