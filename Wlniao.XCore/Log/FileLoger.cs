/*==============================================================================
    文件名称：FileLoger.cs
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
using System.IO;
namespace Wlniao.Log
{
    /// <summary>
    /// 文件日志工具，所有日志会被写入本地文件
    /// </summary>
    public class FileLoger : ILogProvider
    {
        private string basePath = null;
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
        /// <param name="path"></param>
        public FileLoger(LogLevel level = LogLevel.Information, string path = null)
        {
            this.level = level;
            if (string.IsNullOrEmpty(path))
            {
                path = Config.GetConfigs("WLN_LOG_PATH");
                if (string.IsNullOrEmpty(path))
                {
                    basePath = XCore.StartupRoot + "/" + XCore.FrameworkRoot + "/logs/";
                }
            }
            else if (path.EndsWith("/") || path.EndsWith("\\"))
            {
                basePath = path;
            }
            else
            {
                basePath = path + "/";
            }
        }
        /// <summary>
        /// 输出Debug级别的日志
        /// </summary>
        /// <param name="message"></param>
        public void Debug(String message)
        {
            if (Level <= LogLevel.Debug)
            {
                Loger.Console(string.Format("{0} => {1}", DateTools.Format(), message), ConsoleColor.White);
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
                Write("info", message);
                Loger.Console(string.Format("{0} => {1}", DateTools.Format(), message), ConsoleColor.DarkGray);
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
                Write("warn", message);
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
                Write("error", message);
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
                Write("fatal", message);
                Loger.Console(string.Format("{0} => {1}", DateTools.Format(), message), ConsoleColor.Magenta);
            }
        }
        /// <summary>
        /// 输出日志
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="message"></param>
        public void Topic(String topic, String message)
        {
            Write(topic, message);
            Loger.Console(string.Format("[{0}]{1} => {2}", topic, DateTools.Format(), message), ConsoleColor.DarkGray);
        }
        /// <summary>
        /// 输出日志
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="message"></param>
        internal void Write(String topic, String message)
        {
            try
            {
                var fi = new System.IO.FileInfo(Path.GetFullPath(topic + ".log", basePath + DateTools.FormatDate()));
                if (!fi.Directory.Exists)
                {
                    fi.Directory.Create();
                }
                var fs = fi.Exists ? fi.AppendText() : fi.CreateText();
                fs.WriteLine(string.Format("{0} {1} - {2}", DateTools.Format(), topic, message));
                fs.Flush();
                fs.Dispose();
            }
            catch { }
        }
    }
}