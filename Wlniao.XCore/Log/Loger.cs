/*==============================================================================
    文件名称：Loger.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：通用日志输出工具类
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
    /// 通用日志输出工具类
    /// </summary>
    public class Loger
    {
        private static Object logObj = null;
        private static void callMethod(String method, params String[] args)
        {
            if (logObj == null)
            {
                var typeName = "Wlniao.Log." + strUtil.GetTitleCase(XCore.LogProvider) + "Logger";
                var asmName = "Wlniao.XCore." + strUtil.GetTitleCase(XCore.LogProvider);
                logObj = rft.GetInstance(asmName, typeName);
            }
            if (logObj != null)
            {
                rft.CallMethod(logObj, method, args);
            }
        }
        /// <summary>
        /// 直接在控制台打印内容
        /// </summary>
        /// <param name="message"></param>
        /// <param name="color"></param>
        public static void Console(String message, ConsoleColor color = ConsoleColor.White)
        {
            System.Console.ForegroundColor = color;
            System.Console.Write(" => " + message + "\n");
            System.Console.ForegroundColor = ConsoleColor.White;
        }
        /// <summary>
        /// 输出Info级别的日志
        /// </summary>
        /// <param name="message"></param>
        public static void Info(String message)
        {
            if (XCore.LogLevel >= LogLevel.Info)
            {
                if (XCore.LogProvider == "file")
                {
                    FileLogger.Info(message);
                }
                else
                {
                    callMethod("Info", message);
                }
                Console(message, ConsoleColor.White);
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
                if (XCore.LogProvider == "file")
                {
                    FileLogger.Debug(message);
                }
                else
                {
                    callMethod("Debug", message);
                }
                Console(message, ConsoleColor.DarkGreen);
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
                if (XCore.LogProvider == "file")
                {
                    FileLogger.Warn(message);
                }
                else
                {
                    callMethod("Warn", message);
                }
                Console(message, ConsoleColor.DarkYellow);
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
                if (XCore.LogProvider == "file")
                {
                    FileLogger.Error(message);
                }
                else
                {
                    callMethod("Error", message);
                }
                Console(message, ConsoleColor.Red);
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
                if (XCore.LogProvider == "file")
                {
                    FileLogger.Fatal(message);
                }
                else
                {
                    callMethod("Fatal", message);
                }
                Console(message, ConsoleColor.Magenta);
            }
        }
        /// <summary>
        /// 输出自定义主题的日志
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="message"></param>
        public static void Topic(String topic, String message)
        {
            if (XCore.LogProvider == "file")
            {
                FileLogger.Write(new LogMessage { LogTime = DateTime.Now, Message = message, LogLevel = topic });
            }
            else
            {
                callMethod("Write", topic, message);
            }
            Console(message, ConsoleColor.Magenta);
        }
    }
}