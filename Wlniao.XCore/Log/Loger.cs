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
using Wlniao.Text;

namespace Wlniao.Log
{
    /// <summary>
    /// 日志输出工具
    /// </summary>
    public class Loger
    {
        /// <summary>
        /// 为None时表示未初始化，将在初始化后可用
        /// </summary>
        internal static LogLevel logLevel = LogLevel.None;
        /// <summary>
        /// 
        /// </summary>
        private static ILogProvider _logProvider = null;
        /// <summary>
        /// 
        /// </summary>
		private static ILogProvider _fileProvider = new FileLoger(LogLevel);
        /// <summary>
        /// 
        /// </summary>
        private static string _logLocal = null;

        /// <summary>
        /// 当前日志输出级别
        /// </summary>
        public static LogLevel LogLevel
        {
            get
            {
                if (logLevel != LogLevel.None)
                {
                    return logLevel;
                }
                var level = Config.GetConfigs("WLN_LOG_LEVEL").ToLower();
                switch (level)
                {
                    case "info":
                        logLevel = LogLevel.Information;
                        break;
                    case "warn":
                        logLevel = LogLevel.Warning;
                        break;
                    case "fatal":
                        logLevel = LogLevel.Critical;
                        break;
                    default:
                    {
                        if (!Enum.TryParse<LogLevel>(StringUtil.GetTitleCase(level), out logLevel))
                        {
                            logLevel = LogLevel.Information;
                        }

                        break;
                    }
                }
                return logLevel;
            }
        }

        /// <summary>
        /// 当前日志输出提供程序
        /// </summary>
        /// <returns></returns>
        public static ILogProvider LogProvider
        {
            get
            {
                if (_logProvider != null)
                {
                    return _logProvider;
                }
                var type = Config.GetConfigs("WLN_LOG_TYPE").ToLower();
                var server = Config.GetConfigs("WLN_LOG_SERVER").ToLower();
                if (type == "loki" || (string.IsNullOrEmpty(type) && !string.IsNullOrEmpty(server)))
                {
                    _logProvider = new LokiLoger(LogLevel, server);
                }
                else if (type == "file")
                {
                    _logProvider = new FileLoger(LogLevel);
                }
                else
                {
                    _logProvider = new ConsoleLoger(LogLevel);
                }
                return _logProvider;
            }
        }

        /// <summary>
        /// 本地日志输出路径
        /// </summary>
        public static string LogLocal
        {
            get
            {
                if (!string.IsNullOrEmpty(_logLocal))
                {
                    return _logLocal;
                }
                _logLocal = Config.GetConfigs("WLN_LOG_LOCAL").ToLower();
                if (string.IsNullOrEmpty(_logLocal))
                {
                    _logLocal = "console";
                }
                return _logLocal;
            }
        }

        /// <summary>
        /// 设置日志响应程序
        /// </summary>
        /// <param name="provider"></param>
        public static void SetLogger(ILogProvider provider)
        {
            _logProvider = provider;
        }

        /// <summary>
        /// 输出日志到控制台
        /// </summary>
        /// <param name="message"></param>
        /// <param name="color"></param>
        public static void Console(string message, ConsoleColor color = ConsoleColor.White)
        {
            System.Console.ForegroundColor = color;
            System.Console.WriteLine(message);
            System.Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// ֱ输出日志到日志文件
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="message"></param>
        /// <param name="color"></param>
        public static void File(string topic, string message, ConsoleColor color = ConsoleColor.White)
        {
            _fileProvider.Topic(topic, message, LogLevel.Information);
        }

        /// <summary>
        /// 输出开发调试日志
        /// </summary>
        /// <param name="message"></param>
        public static void Debug(string message)
        {
            LogProvider.Debug(message);
        }
        /// <summary>
        /// 输出普通信息日志
        /// </summary>
        /// <param name="message"></param>
        public static void Info(string message)
        {
            LogProvider.Info(message);
        }
        /// <summary>
        /// 输出系统警告日志
        /// </summary>
        /// <param name="message"></param>
        public static void Warn(string message)
        {
            LogProvider.Warn(message);
        }
        /// <summary>
        /// 输出系统错误日志
        /// </summary>
        /// <param name="message"></param>
        public static void Error(string message)
        {
            LogProvider.Error(message);
        }
        /// <summary>
        /// 系统崩溃日志输出
        /// </summary>
        /// <param name="message"></param>
        public static void Fatal(string message)
        {
            LogProvider.Fatal(message);
        }
        /// <summary>
        /// 按主题输出日志
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="message"></param>
        /// <param name="logLevel"></param>
        /// <param name="consoleLocal"></param>
        public static void Topic(string topic, string message, LogLevel logLevel = LogLevel.Information, bool consoleLocal = true)
        {
            LogProvider.Topic(topic, message, logLevel, consoleLocal);
        }

    }
}