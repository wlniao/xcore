/*==============================================================================
    文件名称：LogMessage.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：日志消息实体
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
namespace Wlniao.Log
{
    /// <summary>
    /// 日志消息实体
    /// </summary>
    public class LogMessage
    {
        /// <summary>
        /// 日志的等级
        /// </summary>
        public String LogLevel { get; set; }
        /// <summary>
        /// 日志的时间
        /// </summary>
        public DateTime LogTime { get; set; }
        /// <summary>
        /// 日志的来源
        /// </summary>
        public String Source { get; set; }
        /// <summary>
        /// 日志的内容
        /// </summary>
        public String Message { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format("{0} {1} {2} - {3} \r\n", LogTime.ToString("HH:mm:ss"), strUtil.GetTitleCase(LogLevel), Source, Message);
        }
    }
}

