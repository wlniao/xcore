/*==============================================================================
    文件名称：FileLogger.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：文件日志工具，所有日志会被写入本地文件
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
    /// 文件日志工具，所有日志会被写入本地文件
    /// </summary>
    internal class FileLogger
    {
        /// <summary>
        /// 输出Info级别的日志
        /// </summary>
        /// <param name="message"></param>
        public static void Info(String message)
        {
            if (XCore.LogLevel >= LogLevel.Info)
            {
                Write(new LogMessage { LogTime = DateTime.Now, Message = message, LogLevel = "info" });
            }
        }
        /// <summary>
        /// 输出Debug级别的日志
        /// </summary>
        /// <param name="message"></param>
        public static void Debug(String message)
        {
            if (XCore.LogLevel >= LogLevel.Debug)
            {
                Write(new LogMessage { LogTime = DateTime.Now, Message = message, LogLevel = "debug" });
            }
        }
        /// <summary>
        /// 输出Warn级别的日志
        /// </summary>
        /// <param name="message"></param>
        public static void Warn(String message)
        {
            if (XCore.LogLevel >= LogLevel.Warn)
            {
                Write(new LogMessage { LogTime = DateTime.Now, Message = message, LogLevel = "warn" });
            }
        }
        /// <summary>
        /// 输出Error级别的日志
        /// </summary>
        /// <param name="message"></param>
        public static void Error(String message)
        {
            if (XCore.LogLevel >= LogLevel.Error)
            {
                Write(new LogMessage { LogTime = DateTime.Now, Message = message, LogLevel = "error" });
            }
        }
        /// <summary>
        /// 输出Fatal级别的日志
        /// </summary>
        /// <param name="message"></param>
        public static void Fatal(String message)
        {
            if (XCore.LogLevel >= LogLevel.Fatal)
            {
                Write(new LogMessage { LogTime = DateTime.Now, Message = message, LogLevel = "fatal" });
            }
        }
        /// <summary>
        /// 写入日志
        /// </summary>
        /// <param name="msg"></param>
        public static void Write(LogMessage msg)
        {
            try
            {
                var fi = new System.IO.FileInfo(IO.PathTool.Map(XCore.LogPath, DateTools.Format("yyyy.MM.dd"), strUtil.GetTitleCase(msg.LogLevel) + ".log"));
                if (!fi.Exists)
                {
                    if (!fi.Directory.Exists)
                    {
                        fi.Directory.Create();
                    }
                    var fs = fi.CreateText();
                    fs.WriteLine(string.Format("{0} {1} {2} - {3}", msg.LogTime, msg.LogLevel, msg.Source, msg.Message));
                    fs.Flush();
                    fs.Dispose();
                }
            }
            catch { }
        }
    }
}