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
using Microsoft.VisualBasic;
using System;
namespace Wlniao.Log
{
    /// <summary>
    /// ͨ����־���������
    /// </summary>
    public class Loger
    {
        /// <summary>
        /// ��ǰ��־����ȼ���NoneΪδ�趨������������ļ����г�ʼ��
        /// </summary>
        internal static LogLevel logLevel = LogLevel.None;
        /// <summary>
        /// ��ǰ��־�������
        /// </summary>
        private static ILogProvider logProvider = null;
        /// <summary>
        /// �ļ���־�������
        /// </summary>
		private static ILogProvider fileProvider = new FileLoger(LogLevel);
        /// <summary>
        /// ������־�����ʽ
        /// </summary>
        private static string logLocal = null;

        /// <summary>
        /// ��ǰ��־����ȼ�
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
        /// ��ǰ��־�������
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
        /// ������־�����ʽ
        /// </summary>
        public static string LogLocal
        {
            get
            {
                if (string.IsNullOrEmpty(logLocal))
                {
                    logLocal = Config.GetConfigs("WLN_LOG_LOCAL").ToLower();
                    if (string.IsNullOrEmpty(logLocal))
                    {
                        logLocal = "console";
                    }
                }
                return logLocal;
            }
        }

        /// <summary>
        /// ������־�ṩ����
        /// </summary>
        /// <param name="provider"></param>
        public static void SetLogger(ILogProvider provider)
        {
            logProvider = provider;
        }

        /// <summary>
        /// ֱ���ڿ���̨��ӡ����
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
        /// ֱ�����ļ�������̨�������־
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="message"></param>
        /// <param name="color"></param>
        public static void File(string topic, string message, ConsoleColor color = ConsoleColor.White)
        {
            fileProvider.Topic(topic, message, LogLevel.Information);
        }

        /// <summary>
        /// ���Debug�������־
        /// </summary>
        /// <param name="message"></param>
        public static void Debug(string message)
        {
            LogProvider.Debug(message);
        }
        /// <summary>
        /// ���Info�������־
        /// </summary>
        /// <param name="message"></param>
        public static void Info(string message)
        {
            LogProvider.Info(message);
        }
        /// <summary>
        /// ���Warn�������־
        /// </summary>
        /// <param name="message"></param>
        public static void Warn(string message)
        {
            LogProvider.Warn(message);
        }
        /// <summary>
        /// ���Error�������־
        /// </summary>
        /// <param name="message"></param>
        public static void Error(string message)
        {
            LogProvider.Error(message);
        }
        /// <summary>
        /// ���Fatal�������־
        /// </summary>
        /// <param name="message"></param>
        public static void Fatal(string message)
        {
            LogProvider.Fatal(message);
        }
        /// <summary>
        /// ����Զ����������־
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