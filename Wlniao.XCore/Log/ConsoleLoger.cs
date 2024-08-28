/*==============================================================================
    文件名称：ConsoleLoger.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：控制台日志工具，所有日志会被写入控制台
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
    /// 控制台日志工具，所有日志会被写入控制台
    /// </summary>
    public class ConsoleLoger : ILogProvider
    {
        /// <summary>
        /// 
        /// </summary>
        private LogLevel level = Loger.LogLevel;
        /// <summary>
        /// 
        /// </summary>
        public LogLevel Level
        {
            get
            {
                return level;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="level"></param>
        public ConsoleLoger(LogLevel level = LogLevel.Information)
        {
            this.level = level;
        }
        /// <summary>
        /// 输出Debug级别的日志
        /// </summary>
        /// <param name="message"></param>
        public void Debug(String message)
        {
            if (Level <= LogLevel.Debug)
            {
                Loger.Console(string.Format("{0} => {1}", DateTools.Format(), message), ConsoleColor.DarkBlue);
            }
        }
        /// <summary>
        /// 输出Info级别的日志
        /// </summary>
        /// <param name="message"></param>
        public void Info(String message)
        {
            if (Level <= LogLevel.Information)
            {
                Loger.Console(string.Format("{0} => {1}", DateTools.Format(), message), ConsoleColor.White);
            }
        }
        /// <summary>
        /// 输出Warn级别的日志
        /// </summary>
        /// <param name="message"></param>
        public void Warn(String message)
        {
            if (Level <= LogLevel.Warning)
            {
                Loger.Console(string.Format("{0} => {1}", DateTools.Format(), message), ConsoleColor.DarkYellow);
            }
        }
        /// <summary>
        /// 输出Error级别的日志
        /// </summary>
        /// <param name="message"></param>
        public void Error(String message)
        {
            if (Level <= LogLevel.Error)
            {
                Loger.Console(string.Format("{0} => {1}", DateTools.Format(), message), ConsoleColor.Red);
            }
        }
        /// <summary>
        /// 输出Fatal级别的日志
        /// </summary>
        /// <param name="message"></param>
        public void Fatal(String message)
        {
            if (Level <= LogLevel.Critical)
            {
                Loger.Console(string.Format("{0} => {1}", DateTools.Format(), message), ConsoleColor.Magenta);
            }
        }

        /// <summary>
        /// 输出自定义主题的日志
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="message"></param>
        /// <param name="log_level"></param>
        public void Topic(String topic, String message, LogLevel log_level)
        {
            var color = ConsoleColor.DarkGray;
            if (log_level == LogLevel.Information)
            {
                color = ConsoleColor.Gray;
            }
            else if (log_level == LogLevel.Debug)
            {
                color = ConsoleColor.White;
            }
            else if (log_level == LogLevel.Error)
            {
                color = ConsoleColor.Red;
            }
            else if (log_level == LogLevel.Warning)
            {
                color = ConsoleColor.DarkYellow;
            }
            else if (log_level == LogLevel.Critical)
            {
                color = ConsoleColor.Magenta;
            }
            Loger.Console(string.Format("{0} => {2} => {1}", DateTools.Format(), message, topic), color);

        }
    }
}