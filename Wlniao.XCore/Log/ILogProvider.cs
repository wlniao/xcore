/*==============================================================================
    文件名称：ILogProvider.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：日志工具接口，可通过此接口扩展日志输出方式
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
    /// 
    /// </summary>
    public interface ILogProvider
    {
        /// <summary>
        /// 日志输出级别
        /// </summary>
        public LogLevel Level { get; }
        /// <summary>
        /// 输出Debug级别的日志
        /// </summary>
        /// <param name="message"></param>
        public void Debug(string message);

        /// <summary>
        /// 输出Info级别的日志
        /// </summary>
        /// <param name="message"></param>
        public void Info(string message);

        /// <summary>
        /// 输出Warn级别的日志
        /// </summary>
        /// <param name="message"></param>
        public void Warn(string message);

        /// <summary>
        /// 输出Error级别的日志
        /// </summary>
        /// <param name="message"></param>
        public void Error(string message);

        /// <summary>
        /// 输出Fatal级别的日志
        /// </summary>
        /// <param name="message"></param>
        public void Fatal(string message);

        /// <summary>
        /// 输出自定义主题的日志
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="message"></param>
        /// <param name="logLevel"></param>
        /// <param name="consoleWrite"></param>
        public void Topic(string topic, string message, LogLevel logLevel, bool consoleWrite = true);

    }
}