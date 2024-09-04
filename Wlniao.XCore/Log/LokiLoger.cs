/*==============================================================================
    文件名称：LokiLoger.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：基于Loki服务的日志写入工具
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
using Org.BouncyCastle.Asn1.Ocsp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Wlniao.Caching;
using Wlniao.Handler;
using Wlniao.OpenApi;
using Wlniao.Serialization;
using static Wlniao.Log.LokiLoger;

namespace Wlniao.Log
{
    /// <summary>
    /// 基于Loki服务的日志写入工具
    /// </summary>
    public class LokiLoger : ILogProvider
    {
        private static readonly string[] levels = new string[] { "info", "warn", "debug", "error", "fatal" };
        /// <summary>
        /// 多租户标识
        /// </summary>
        private string orgId = null;
        /// <summary>
        /// 服务器地址
        /// </summary>
        private string serverHost = null;
        /// <summary>
        /// 待写入数据流
        /// </summary>
        private static Queue<LokiStream> queue = new Queue<LokiStream>();
        /// <summary>
        /// 日志输出级别
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
        /// 落盘时间间隔
        /// </summary>
        public int Interval = 0;

        private FileLoger flog = null;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="level">日志输出级别</param>
        /// <param name="server">服务器地址</param>
        /// <param name="interval">落盘时间间隔（秒）</param>
        /// <param name="org_id">多租户租户ID，默认为0</param>
        public LokiLoger(LogLevel level = LogLevel.Information, string server = null, int interval = 0, string org_id = null)
        {
            flog = new FileLoger(level);
            this.level = level;
            this.Interval = this.Interval > 0 ? this.Interval : cvt.ToInt(Config.GetConfigs("WLN_LOG_INTERVAL", "5"));
            if (string.IsNullOrEmpty(server))
            {
                if (serverHost == null)
                {
                    serverHost = Config.GetConfigs("WLN_LOG_SERVER").TrimEnd('/');
                }
                if (string.IsNullOrEmpty(serverHost))
                {
                    serverHost = "";
                    Loger.Console(string.Format("{0} => {1}", DateTools.Format(), "WLN_LOG_SERVER not configured, please set loki server."), ConsoleColor.Red);
                }
            }
            else
            {
                serverHost = server.TrimEnd('/');
            }
            if (string.IsNullOrEmpty(org_id))
            {
                if (orgId == null)
                {
                    orgId = Config.GetConfigs("WLN_LOG_ORGID").TrimEnd('/');
                }
                if (string.IsNullOrEmpty(orgId))
                {
                    orgId = "";
                }
            }
            else
            {
                orgId = org_id.Trim();
            }
            if (this.Interval > 0 && !string.IsNullOrEmpty(serverHost))
            {
                Task.Run(() =>
                {
                    while (true)
                    {
                        Write("", null, LogLevel.None, true);
                        Task.Delay(Interval * 1000).Wait();
                    }
                });
            }
        }

        /// <summary>
        /// 输出日志
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="entrie"></param>
        /// <param name="push">是否立即回写</param>
        /// <param name="level">日志写入级别</param>
        private void Write(String topic, LokiEntrie entrie, LogLevel level = LogLevel.None, Boolean push = false)
        {
            try
            {
                LokiStream item = null;
                if (entrie != null && !string.IsNullOrEmpty(topic))
                {
                    item = new LokiStream
                    {
                        stream = new Dictionary<string, string> { { "service_name", XCore.WebNode } },
                        values = new List<string[]> { new string[] { entrie.ts, entrie.line } }
                    };
                    if (levels.Contains(topic))
                    {
                        item.stream.Add("level", topic);
                    }
                    else
                    {
                        item.stream.Add("topic", topic);
                        if (level == LogLevel.Information)
                        {
                            item.stream.Add("level", "info");
                        }
                        else if (level == LogLevel.Warning)
                        {
                            item.stream.Add("level", "warn");
                        }
                        else if (level == LogLevel.Error)
                        {
                            item.stream.Add("level", "error");
                        }
                        else if (level == LogLevel.Critical)
                        {
                            item.stream.Add("level", "fatal");
                        }
                        else if (level == LogLevel.Debug)
                        {
                            item.stream.Add("level", "debug");
                        }
                    }
                }
                if (push || this.Interval <= 0)
                {
                    var dto = new LokiDto { streams = new List<LokiStream>() };
                    if (item != null)
                    {
                        // 实时推送时，写入当前日志流
                        dto.streams.Add(item);
                    }
                    lock (levels)
                    {
                        for (var i = 0; i < 20 && queue.Count > 0; i++)
                        {
                            // 同时写入队列中之前失败的日志流
                            dto.streams.Add(queue.Dequeue());
                        }
                    }
                    if (dto.streams.Count > 0)
                    {
                        // 存在要推送的数据时，调用接口推送
                        var err = false;
                        try
                        {
                            var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = XCore.ServerCertificateCustomValidationCallback };
                            using (var client = new HttpClient(handler))
                            {
                                var content = JsonSerializer.Serialize(dto);
                                var request = new HttpRequestMessage(HttpMethod.Post, serverHost + "/loki/api/v1/push");
                                if (!string.IsNullOrEmpty(orgId))
                                {
                                    request.Headers.TryAddWithoutValidation("X-Scope-OrgID", orgId);
                                }
                                request.Content = new StringContent(JsonSerializer.Serialize(dto), System.Text.Encoding.UTF8, "application/json");
                                var response = client.Send(request);
                                var errmsg = response.Content.ReadAsStringAsync().Result;
                                if (response.StatusCode != System.Net.HttpStatusCode.NoContent && response.StatusCode != System.Net.HttpStatusCode.OK)
                                {
                                    err = true;
                                    if (!string.IsNullOrEmpty(errmsg))
                                    {
                                        LokiErrorLog("Push Result:" + errmsg);
                                        if (errmsg.Contains("loghttp.LogProtoStream"))
                                        {
                                            client.GetAsync(serverHost + "/ready");
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            err = true;
                            LokiErrorLog("Exception:" + ex.Message);
                        }
                        if (err)
                        {
                            lock (levels)
                            {
                                foreach (var line in dto.streams)
                                {
                                    queue.Enqueue(line); //失败时把待写入数据全部放入队列
                                }
                            }
                        }
                        else if (queue.Count > 20)
                        {
                            Task.Run(() =>
                            {
                                Write("", null, LogLevel.None, true);
                            });
                        }
                    }
                }
                else if(item != null)
                {
                    //定时落盘时把待写入数据放入日志流队列
                    queue.Enqueue(item);
                }
            }
            catch (Exception ex)
            {
                LokiErrorLog("Exception:" + ex.Message);
            }
        }

        private string tmpmsg = null;
        /// <summary>
        /// Loki异常时输出日志
        /// </summary>
        /// <param name="message"></param>
        private void LokiErrorLog(string message)
        {
            if (message != tmpmsg)
            {
                tmpmsg = message;
                Loger.File("Loki", message, ConsoleColor.Red);
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
                var entrie = new LokiEntrie { line = message, time = DateTime.UtcNow };
                if (Loger.LogLocal == "console")
                {
                    Loger.Console(string.Format("{0} => {1}", DateTools.Format(entrie.time), entrie.line), ConsoleColor.White);
                }
                else if (Loger.LogLocal == "file")
                {
                    flog.Write("debug", message);
                }
                //Write("debug", entrie, LogLevel.None, false); //Debug级别日志不存储
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
                var entrie = new LokiEntrie { line = message, time = DateTime.UtcNow };
                if (Loger.LogLocal == "console")
                {
                    Loger.Console(string.Format("{0} => {1}", DateTools.Format(entrie.time), entrie.line), ConsoleColor.Gray);
                }
                else if (Loger.LogLocal == "file")
                {
                    flog.Write("info", message);
                }
                Write("info", entrie, LogLevel.None, false);
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
                var entrie = new LokiEntrie { line = message, time = DateTime.UtcNow };
                if (Loger.LogLocal == "console")
                {
                    Loger.Console(string.Format("{0} => {1}", DateTools.Format(entrie.time), entrie.line), ConsoleColor.DarkYellow);
                }
                else if (Loger.LogLocal == "file")
                {
                    flog.Write("warn", message);
                }
                Write("warn", entrie, LogLevel.None, false);
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
                var entrie = new LokiEntrie { line = message, time = DateTime.UtcNow };
                if (Loger.LogLocal == "console")
                {
                    Loger.Console(string.Format("{0} => {1}", DateTools.Format(entrie.time), entrie.line), ConsoleColor.Red);
                }
                else if (Loger.LogLocal == "file")
                {
                    flog.Write("error", message);
                }
                Write("error", entrie, LogLevel.None, true);
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
                var entrie = new LokiEntrie { line = message, time = DateTime.UtcNow };
                if (Loger.LogLocal == "console")
                {
                    Loger.Console(string.Format("{0} => {1}", DateTools.Format(entrie.time), entrie.line), ConsoleColor.Magenta);
                }
                else if (Loger.LogLocal == "file")
                {
                    flog.Write("fatal", message);
                }
                Write("fatal", entrie, LogLevel.None, true);
            }
        }

        /// <summary>
        /// 输出自定义主题的日志
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="message"></param>
        /// <param name="logLevel"></param>
        /// <param name="consoleLocal"></param>
        public void Topic(String topic, String message, LogLevel logLevel, Boolean consoleLocal = true)
        {
            var entrie = new LokiEntrie { line = message, time = DateTime.UtcNow };
            if (consoleLocal && Level <= logLevel)
            {
                if (Loger.LogLocal == "console")
                {
                    var color = ConsoleColor.DarkGray;
                    if (logLevel == LogLevel.Information)
                    {
                        color = ConsoleColor.Gray;
                    }
                    else if (logLevel == LogLevel.Debug)
                    {
                        color = ConsoleColor.White;
                    }
                    else if (logLevel == LogLevel.Error)
                    {
                        color = ConsoleColor.Red;
                    }
                    else if (logLevel == LogLevel.Warning)
                    {
                        color = ConsoleColor.DarkYellow;
                    }
                    else if (logLevel == LogLevel.Critical)
                    {
                        color = ConsoleColor.Magenta;
                    }
                    Loger.Console(string.Format("{0} => {1}", DateTools.Format(entrie.time), entrie.line), color);
                }
                else if (Loger.LogLocal == "file")
                {
                    flog.Write(topic, message);
                }
            }
            Write(topic, entrie, logLevel);
        }

        /// <summary>
        /// 传输对象
        /// </summary>
        public class LokiDto
        {
            /// <summary>
            /// 写入日志流集合
            /// </summary>
            public List<LokiStream> streams { get; set; }
        }

        /// <summary>
        /// 日志实体
        /// </summary>
        public class LokiEntrie
        {
            /// <summary>
            /// 日志时间
            /// </summary>
            [Serialization.NotSerialize]
            internal DateTime time { get; set; }
            /// <summary>
            /// 日志行
            /// </summary>
            public string line { get; set; }
            /// <summary>
            /// RFC3339Nano格式时间
            /// </summary>
            public string ts
            {
                get
                {
                    return time.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).Ticks.ToString() + "00";
                }
            }
        }

        /// <summary>
        /// 日志流对象
        /// </summary>
        public class LokiStream
        {
            /// <summary>
            /// 日志标记
            /// </summary>
            public Dictionary<string, string> stream { get; set; }
            /// <summary>
            /// 日志内容
            /// </summary>
            public List<string[]> values { get; set; }
        }
    }
}