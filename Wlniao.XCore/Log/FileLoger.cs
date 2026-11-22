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
using System.Text;
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
            }
            if (string.IsNullOrEmpty(path))
            {
                basePath = XCore.StartupRoot + "/" + XCore.FrameworkRoot + "/logs/";
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
        public void Debug(string message)
        {
            if (Level <= LogLevel.Debug)
            {
                Write("debug", message);
                if (Loger.LogLocal == "console")
                {
                    Loger.Console(string.Format("{0} => {1}", DateTools.Format(), message), ConsoleColor.White);
                }
            }
        }
        /// <summary>
        /// 输出Info级别的日志
        /// </summary>
        /// <param name="message"></param>
        public void Info(string message)
        {
            if (Level <= LogLevel.Information)
            {
                Write("info", message);
                if (Loger.LogLocal == "console")
                {
                    Loger.Console(string.Format("{0} => {1}", DateTools.Format(), message), ConsoleColor.Gray);
                }
            }
        }
        /// <summary>
        /// 输出Warn级别的日志
        /// </summary>
        /// <param name="message"></param>
        public void Warn(string message)
        {
            if (Level <= LogLevel.Warning)
            {
                Write("warn", message);
                if (Loger.LogLocal == "console")
                {
                    Loger.Console(string.Format("{0} => {1}", DateTools.Format(), message), ConsoleColor.DarkYellow);
                }
            }
        }
        /// <summary>
        /// 输出Error级别的日志
        /// </summary>
        /// <param name="message"></param>
        public void Error(string message)
        {
            if (Level <= LogLevel.Error)
            {
                Write("error", message);
                if (Loger.LogLocal == "console")
                {
                    Loger.Console(string.Format("{0} => {1}", DateTools.Format(), message), ConsoleColor.Red);
                }
            }
        }
        /// <summary>
        /// 输出Fatal级别的日志
        /// </summary>
        /// <param name="message"></param>
        public void Fatal(string message)
        {
            if (Level <= LogLevel.Critical)
            {
                Write("fatal", message);
                if (Loger.LogLocal == "console")
                {
                    Loger.Console(string.Format("{0} => {1}", DateTools.Format(), message), ConsoleColor.Magenta);
                }
            }
        }

        /// <summary>
        /// 输出自定义主题的日志
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="message"></param>
        /// <param name="logLevel"></param>
        /// <param name="consoleLocal"></param>
        public void Topic(string topic, string message, LogLevel logLevel, bool consoleLocal = true)
        {
            // 文件与控制台一样按级别过滤：否则 Debug 级敏感明文会无视 WLN_LOG_LEVEL 全部落盘
            if (Level <= logLevel)
            {
                Write(topic, message);
            }
            if (consoleLocal && Level <= logLevel && Loger.LogLocal == "console")
            {
                var color = Loger.GetLevelColor(logLevel);
                Loger.Console(string.Format("[{0}]{1} => {2}", topic, DateTools.Format(), message), color);
            }
        }

        /// <summary>
        /// 输出日志
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="message"></param>
        public void Write(string topic, string message)
        {
            // 持锁写入：同一文件并发 AppendText 会互相干扰导致丢日志；
            // 按「日期+topic」复用写句柄，避免每条日志开关一次文件。
            lock (writeLock)
            {
                try
                {
                    var day = DateTools.FormatDate();
                    var path = Path.GetFullPath(topic + ".log", basePath + day);
                    if (writer == null || writerPath != path || IsRotated(path))
                    {
                        DisposeWriter();
                        var fi = new System.IO.FileInfo(path);
                        if (fi.Directory != null && !fi.Directory.Exists)
                        {
                            fi.Directory.Create();
                        }
                        writer = fi.Exists ? fi.AppendText() : fi.CreateText();
                        writerPath = path;
                        // 记录打开时的文件长度；外部 logrotate 重命名/清空后据此重建句柄
                        writerLength = fi.Exists ? fi.Length : 0;
                    }
                    else
                    {
                        writerLength = new System.IO.FileInfo(path).Length;
                    }
                    writer.WriteLine(string.Format("{0} {1} - {2}", DateTools.Format(), topic, message));
                    writer.Flush();
                    writerLength = new System.IO.FileInfo(path).Length;
                }
                catch (Exception ex)
                {
                    // 写失败即丢弃句柄，下次强制重建（磁盘满/文件被占用等）
                    DisposeWriter();
                    try
                    {
                        System.Console.WriteLine($"日志文件写入异常: {ex.Message}, 堆栈: {ex.StackTrace}");
                    }
                    catch
                    {
                        // 如果连控制台都无法输出，则放弃记录
                    }
                }
            }
        }

        private readonly object writeLock = new();
        private StreamWriter writer;
        private string writerPath;
        private long writerLength;

        /// <summary>
        /// 释放当前文件日志句柄；替换 FileLoger 实例或应用退出前应调用
        /// </summary>
        public void Dispose()
        {
            lock (writeLock)
            {
                DisposeWriter();
            }
        }

        private bool IsRotated(string path)
        {
            // 文件被重命名/删除，或被外部工具清空/截断时，当前句柄应废弃重建
            var fi = new System.IO.FileInfo(path);
            return !fi.Exists || fi.Length < writerLength;
        }

        private void DisposeWriter()
        {
            if (writer != null)
            {
                try
                {
                    writer.Dispose();
                }
                catch
                {
                    // 释放失败时没有可用的兜底手段，忽略以避免掩盖上面的原始异常
                }
                writer = null;
                writerPath = null;
            }
        }
    }
}