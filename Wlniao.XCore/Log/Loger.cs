/*==============================================================================
    �ļ����ƣ�Loger.cs
    ���û�����CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    ����������ͨ����־���������
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
    /// ͨ����־���������
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
        /// ֱ���ڿ���̨��ӡ����
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
        /// ���Info�������־
        /// </summary>
        /// <param name="message"></param>
        public static void Info(String message)
        {
            if (XCore.LogProvider == "file")
            {
                FileLogger.Info(message);
            }
            else if (XCore.LogLevel >= LogLevel.Info)
            {
                callMethod("Info", message);
            }
        }
        /// <summary>
        /// ���Debug�������־
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
        /// ���Warn�������־
        /// </summary>
        /// <param name="message"></param>
        public static void Warn(String message)
        {
            if (XCore.LogProvider == "file")
            {
                FileLogger.Warn(message);
            }
            else if (XCore.LogLevel >= LogLevel.Warn)
            {
                callMethod("Warn", message);
            }
            Console(message, ConsoleColor.DarkYellow);
        }
        /// <summary>
        /// ���Error�������־
        /// </summary>
        /// <param name="message"></param>
        public static void Error(String message)
        {
            if (XCore.LogProvider == "file")
            {
                FileLogger.Error(message);
            }
            else if (XCore.LogLevel >= LogLevel.Error)
            {
                callMethod("Error", message);
            }
            Console(message, ConsoleColor.Red);
        }
        /// <summary>
        /// ���Fatal�������־
        /// </summary>
        /// <param name="message"></param>
        public static void Fatal(String message)
        {
            if (XCore.LogProvider == "file")
            {
                FileLogger.Fatal(message);
            }
            else if (XCore.LogLevel >= LogLevel.Fatal)
            {
                callMethod("Fatal", message);
            }
            Console(message, ConsoleColor.Magenta);
        }
        /// <summary>
        /// ����Զ����������־
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="message"></param>
        public static void Topic(String topic, String message)
        {
            if (XCore.LogProvider == "file")
            {
                FileLogger.Write(new LogMessage { LogTime = DateTime.Now, Message = message, LogLevel = topic });
            }
            else if (XCore.LogLevel >= LogLevel.Fatal)
            {
                callMethod("Write", topic, message);
            }
            Console(message, ConsoleColor.Magenta);
        }
    }
}