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
        /// <summary>
        /// 当前日志输出等级，None为未设定，会根据配置文件进行初始化
        /// </summary>
        internal static LogLevel logLevel = LogLevel.None;
        /// <summary>
        /// 当前日志输出工具
        /// </summary>
        private static ILogProvider logProvider = null;
        /// <summary>
        /// 文件日志输出工具
        /// </summary>
		private static ILogProvider fileProvider = new FileLoger(LogLevel);
		/// <summary>
		/// 当前日志输出等级
		/// </summary>
		public static LogLevel LogLevel
        {
            get
            {
                if (logLevel == LogLevel.None)
                {
                    var level = Config.GetConfigs("WLN_LOG_LEVEL").ToLower();
                    if (level == "info")
                    {
                        logLevel = LogLevel.Information;
                    }
                    else if (level == "warn")
                    {
                        logLevel = LogLevel.Warning;
                    }
                    else if (level == "fatal")
                    {
                        logLevel = LogLevel.Critical;
                    }
                    else if (!Enum.TryParse<LogLevel>(strUtil.GetTitleCase(level), out logLevel))
                    {
                        logLevel = LogLevel.Information;
                    }
                }
                return logLevel;
            }
        }
        /// <summary>
        /// 当前日志输出工具
        /// </summary>
        /// <returns></returns>
        public static ILogProvider LogProvider
        {
            get
            {
                if (logProvider == null)
                {
                    var type = Config.GetConfigs("WLN_LOG_TYPE").ToLower();
                    var server = Config.GetConfigs("WLN_LOG_SERVER").ToLower();
                    if (type == "loki" || (string.IsNullOrEmpty(type) && !string.IsNullOrEmpty(server)))
                    {
                        logProvider = new LokiLoger(LogLevel, server);
                    }
                    else if (type == "file")
                    {
                        logProvider = new FileLoger(LogLevel);
                    }
                    else
                    {
                        logProvider = new ConsoleLoger(LogLevel);
                    }
                }
                return logProvider;
            }
        }

        /// <summary>
        /// 设置日志提供工具
        /// </summary>
        /// <param name="provider"></param>
        public static void SetLogger(ILogProvider provider)
        {
            logProvider = provider;
		}
		/// <summary>
		/// 直接在控制台打印内容
		/// </summary>
		/// <param name="message"></param>
		/// <param name="color"></param>
		public static void Console(String message, ConsoleColor color = ConsoleColor.White)
		{
			System.Console.ForegroundColor = color;
			System.Console.WriteLine(message);
			System.Console.ForegroundColor = ConsoleColor.White;
		}
		/// <summary>
		/// 直接在文件及控制台中输出日志
		/// </summary>
		/// <param name="topic"></param>
		/// <param name="message"></param>
		/// <param name="color"></param>
		public static void File(String topic, String message, ConsoleColor color = ConsoleColor.White)
        {
            fileProvider.Topic(topic, message);
        }
		/// <summary>
		/// 输出Debug级别的日志
		/// </summary>
		/// <param name="message"></param>
		public static void Debug(String message)
        {
            LogProvider.Debug(message);
        }
        /// <summary>
        /// 输出Info级别的日志
        /// </summary>
        /// <param name="message"></param>
        public static void Info(String message)
        {
            LogProvider.Info(message);
        }
        /// <summary>
        /// 输出Warn级别的日志
        /// </summary>
        /// <param name="message"></param>
        public static void Warn(String message)
        {
            LogProvider.Warn(message);
        }
        /// <summary>
        /// 输出Error级别的日志
        /// </summary>
        /// <param name="message"></param>
        public static void Error(String message)
        {
            LogProvider.Error(message);
        }
        /// <summary>
        /// 输出Fatal级别的日志
        /// </summary>
        /// <param name="message"></param>
        public static void Fatal(String message)
        {
            LogProvider.Fatal(message);
        }
        /// <summary>
        /// 输出自定义主题的日志
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="message"></param>
        public static void Topic(String topic, String message)
        {
            LogProvider.Topic(topic, message);
        }
    }
}