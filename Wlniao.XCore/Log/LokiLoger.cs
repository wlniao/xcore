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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;

namespace Wlniao.Log
{
    /// <summary>
    /// 基于Loki服务的日志写入工具
    /// </summary>
    public class LokiLoger : ILogProvider
    {
        private static readonly string[] levels = new string[] { "info", "warn", "debug", "error", "fatal" };
        
        /// <summary>
        /// 专门的锁对象，避免使用levels数组
        /// </summary>
        private static readonly object _queueLock = new object();
        
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
        
        // 静态HttpClient实例，避免每次创建新实例
        private static readonly HttpClient _httpClient;
        
        // CancellationTokenSource用于取消后台任务
        private static CancellationTokenSource _cancellationTokenSource;
        
        // 最大批量大小
        private const int MaxBatchSize = 100;
        
        // 最大队列大小，防止内存溢出
        private const int MaxQueueSize = 10000;
        
        // 最大重试次数
        private const int MaxRetryCount = 3;
        
        static LokiLoger()
        {
            var handler = new HttpClientHandler 
            { 
                ServerCertificateCustomValidationCallback = XCore.ServerCertificateCustomValidationCallback,
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
            };
            _httpClient = new HttpClient(handler);
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Wlniao/XCore");
            _cancellationTokenSource = new CancellationTokenSource();
        }
        
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
            this.Interval = this.Interval > 0 ? this.Interval : Convert.ToInt(Config.GetConfigs("WLN_LOG_INTERVAL"));
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
                Task.Run(async () =>
                {
                    while (!_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        try
                        {
                            await WriteAsync("", null, LogLevel.None, true, _cancellationTokenSource.Token);
                            await Task.Delay(Interval * 1000, _cancellationTokenSource.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        catch (Exception ex)
                        {
                            LokiErrorLog("Background task exception: " + ex.Message);
                        }
                    }
                }, _cancellationTokenSource.Token);
            }
        }

        /// <summary>
        /// 输出日志
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="entrie"></param>
        /// <param name="push">是否立即回写</param>
        /// <param name="level">日志写入级别</param>
        private void Write(string topic, LokiEntrie entrie, LogLevel level = LogLevel.None, bool push = false)
        {
            WriteAsync(topic, entrie, level, push, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 异步输出日志
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="entrie"></param>
        /// <param name="push">是否立即回写</param>
        /// <param name="level">日志写入级别</param>
        /// <param name="cancellationToken">取消令牌</param>
        private async Task WriteAsync(string topic, LokiEntrie entrie, LogLevel level = LogLevel.None, bool push = false, CancellationToken cancellationToken = default)
        {
            try
            {
                LokiStream item = null;
                if (entrie != null && !string.IsNullOrEmpty(topic))
                {
                    item = CreateLokiStream(topic, entrie, level);
                }
                
                if (push || this.Interval <= 0)
                {
                    await PushLogsAsync(item, cancellationToken).ConfigureAwait(false);
                }
                else if (item != null)
                {
                    EnqueueItem(item);
                }
            }
            catch (Exception ex)
            {
                LokiErrorLog("Exception:" + ex.Message);
            }
        }

        /// <summary>
        /// 创建LokiStream对象
        /// </summary>
        private LokiStream CreateLokiStream(string topic, LokiEntrie entrie, LogLevel level)
        {
            var item = new LokiStream
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
                item.stream.Add("level", GetLevelString(level));
            }
            
            return item;
        }

        /// <summary>
        /// 获取日志级别字符串
        /// </summary>
        private string GetLevelString(LogLevel level)
        {
            return level switch
            {
                LogLevel.Information => "info",
                LogLevel.Warning => "warn",
                LogLevel.Error => "error",
                LogLevel.Critical => "fatal",
                LogLevel.Debug => "debug",
                _ => "info"
            };
        }

        /// <summary>
        /// 将日志项加入队列
        /// </summary>
        private void EnqueueItem(LokiStream item)
        {
            lock (_queueLock)
            {
                if (queue.Count >= MaxQueueSize)
                {
                    // 队列已满，丢弃最旧的日志
                    queue.Dequeue();
                    LokiErrorLog("Queue overflow, discarding oldest log entry");
                }
                queue.Enqueue(item);
            }
        }

        /// <summary>
        /// 从队列中获取批量日志
        /// </summary>
        private List<LokiStream> DequeueBatch(int batchSize)
        {
            var batch = new List<LokiStream>();
            lock (_queueLock)
            {
                var count = Math.Min(batchSize, queue.Count);
                for (var i = 0; i < count; i++)
                {
                    batch.Add(queue.Dequeue());
                }
            }
            return batch;
        }

        /// <summary>
        /// 将日志重新加入队列（失败时）
        /// </summary>
        private void RequeueBatch(List<LokiStream> batch)
        {
            if (batch == null || batch.Count == 0) return;
            
            lock (_queueLock)
            {
                foreach (var item in batch)
                {
                    if (queue.Count >= MaxQueueSize)
                    {
                        LokiErrorLog("Queue overflow during requeue, discarding log entry");
                        break;
                    }
                    queue.Enqueue(item);
                }
            }
        }

        /// <summary>
        /// 推送日志到Loki
        /// </summary>
        private async Task PushLogsAsync(LokiStream currentItem, CancellationToken cancellationToken)
        {
            var batch = DequeueBatch(MaxBatchSize);
            
            if (currentItem != null)
            {
                batch.Insert(0, currentItem);
            }
            
            if (batch.Count == 0) return;
            
            var dto = new LokiDto { streams = batch };
            
            var success = false;
            var retryCount = 0;
            
            while (!success && retryCount < MaxRetryCount)
            {
                try
                {
                    success = await SendToLokiAsync(dto, cancellationToken).ConfigureAwait(false);
                    
                    if (!success)
                    {
                        retryCount++;
                        if (retryCount < MaxRetryCount)
                        {
                            // 指数退避
                            var delay = (int)Math.Pow(2, retryCount) * 100;
                            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    LokiErrorLog("Push cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    LokiErrorLog($"Push failed (attempt {retryCount}): {ex.Message}");
                    
                    if (retryCount < MaxRetryCount)
                    {
                        var delay = (int)Math.Pow(2, retryCount) * 100;
                        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            
            if (!success)
            {
                // 失败时重新加入队列
                RequeueBatch(batch);
            }
            else
            {
                // 如果队列还有较多数据，继续处理
                lock (_queueLock)
                {
                    if (queue.Count > MaxBatchSize / 2)
                    {
                        _ = Task.Run(async () =>
                        {
                            await WriteAsync("", null, LogLevel.None, true, cancellationToken).ConfigureAwait(false);
                        }, cancellationToken);
                    }
                }
            }
        }

        /// <summary>
        /// 发送日志到Loki服务器
        /// </summary>
        private async Task<bool> SendToLokiAsync(LokiDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var content = Json.Serialize(dto);
                
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(30));
                
                var request = new HttpRequestMessage(HttpMethod.Post, serverHost + "/loki/api/v1/push");
                if (!string.IsNullOrEmpty(orgId))
                {
                    request.Headers.TryAddWithoutValidation("X-Scope-OrgID", orgId);
                }
                
                // 使用Gzip压缩减少传输数据量
                var compressedContent = CompressContent(content);
                request.Content = new ByteArrayContent(compressedContent);
                request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                request.Content.Headers.ContentEncoding.Add("gzip");
                
                var response = await _httpClient.SendAsync(request, cts.Token).ConfigureAwait(false);
                
                if (response.StatusCode == System.Net.HttpStatusCode.NoContent || response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return true;
                }
                
                var errmsg = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                LokiErrorLog($"Push Result: {errmsg} [{dto.streams.Count}]");
                
                if (errmsg.Contains("loghttp.LogProtoStream"))
                {
                    await _httpClient.GetAsync(serverHost + "/ready", cts.Token).ConfigureAwait(false);
                }
                
                return false;
            }
            catch (Exception ex)
            {
                LokiErrorLog($"SendToLokiAsync exception: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gzip压缩内容
        /// </summary>
        private byte[] CompressContent(string content)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(content);
            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionLevel.Optimal))
            {
                gzip.Write(bytes, 0, bytes.Length);
            }
            return output.ToArray();
        }

        /// <summary>
        /// 记录Loki错误日志
        /// </summary>
        private void LokiErrorLog(string message)
        {
            try
            {
                flog.Error($"[LokiError] {message}");
            }
            catch
            {
                // 忽略文件日志错误
            }
        }

        /// <summary>
        /// 输出调试日志
        /// </summary>
        /// <param name="message"></param>
        public void Debug(string message)
        {
            if (level > LogLevel.Debug) return;
            var entrie = new LokiEntrie { line = message, time = DateTime.UtcNow };
            if (Loger.LogLocal == "console")
            {
                Loger.Console(string.Format("{0} => {1}", DateTools.Format(entrie.time), entrie.line), ConsoleColor.White);
            }
            else if (Loger.LogLocal == "file")
            {
                flog.Write("debug", message);
            }
            Write("debug", entrie, LogLevel.Debug, false);
        }

        /// <summary>
        /// 输出调试日志
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="message"></param>
        public void Debug(string topic, string message)
        {
            if (level > LogLevel.Debug) return;
            var entrie = new LokiEntrie { line = message, time = DateTime.UtcNow };
            if (Loger.LogLocal == "console")
            {
                Loger.Console(string.Format("{0} => {1}", DateTools.Format(entrie.time), entrie.line), ConsoleColor.White);
            }
            else if (Loger.LogLocal == "file")
            {
                flog.Write(topic, message);
            }
            Write(topic, entrie, LogLevel.Debug, false);
        }

        /// <summary>
        /// 输出信息日志
        /// </summary>
        /// <param name="message"></param>
        public void Info(string message)
        {
            if (level > LogLevel.Information) return;
            var entrie = new LokiEntrie { line = message, time = DateTime.UtcNow };
            if (Loger.LogLocal == "console")
            {
                Loger.Console(string.Format("{0} => {1}", DateTools.Format(entrie.time), entrie.line), ConsoleColor.Gray);
            }
            else if (Loger.LogLocal == "file")
            {
                flog.Write("info", message);
            }
            Write("info", entrie, LogLevel.Information, false);
        }

        /// <summary>
        /// 输出信息日志
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="message"></param>
        public void Info(string topic, string message)
        {
            if (level > LogLevel.Information) return;
            var entrie = new LokiEntrie { line = message, time = DateTime.UtcNow };
            if (Loger.LogLocal == "console")
            {
                Loger.Console(string.Format("{0} => {1}", DateTools.Format(entrie.time), entrie.line), ConsoleColor.Gray);
            }
            else if (Loger.LogLocal == "file")
            {
                flog.Write(topic, message);
            }
            Write(topic, entrie, LogLevel.Information, false);
        }

        /// <summary>
        /// 输出警告日志
        /// </summary>
        /// <param name="message"></param>
        public void Warn(string message)
        {
            if (level > LogLevel.Warning) return;
            var entrie = new LokiEntrie { line = message, time = DateTime.UtcNow };
            if (Loger.LogLocal == "console")
            {
                Loger.Console(string.Format("{0} => {1}", DateTools.Format(entrie.time), entrie.line), ConsoleColor.DarkYellow);
            }
            else if (Loger.LogLocal == "file")
            {
                flog.Write("warn", message);
            }
            Write("warn", entrie, LogLevel.Warning, false);
        }

        /// <summary>
        /// 输出警告日志
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="message"></param>
        public void Warn(string topic, string message)
        {
            if (level > LogLevel.Warning) return;
            var entrie = new LokiEntrie { line = message, time = DateTime.UtcNow };
            if (Loger.LogLocal == "console")
            {
                Loger.Console(string.Format("{0} => {1}", DateTools.Format(entrie.time), entrie.line), ConsoleColor.DarkYellow);
            }
            else if (Loger.LogLocal == "file")
            {
                flog.Write(topic, message);
            }
            Write(topic, entrie, LogLevel.Warning, false);
        }

        /// <summary>
        /// 输出错误日志
        /// </summary>
        /// <param name="message"></param>
        public void Error(string message)
        {
            if (level > LogLevel.Error) return;
            var entrie = new LokiEntrie { line = message, time = DateTime.UtcNow };
            if (Loger.LogLocal == "console")
            {
                Loger.Console(string.Format("{0} => {1}", DateTools.Format(entrie.time), entrie.line), ConsoleColor.Red);
            }
            else if (Loger.LogLocal == "file")
            {
                flog.Write("error", message);
            }
            Write("error", entrie, LogLevel.Error, true);
        }

        /// <summary>
        /// 输出错误日志
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="message"></param>
        public void Error(string topic, string message)
        {
            if (level > LogLevel.Error) return;
            var entrie = new LokiEntrie { line = message, time = DateTime.UtcNow };
            if (Loger.LogLocal == "console")
            {
                Loger.Console(string.Format("{0} => {1}", DateTools.Format(entrie.time), entrie.line), ConsoleColor.Red);
            }
            else if (Loger.LogLocal == "file")
            {
                flog.Write(topic, message);
            }
            Write(topic, entrie, LogLevel.Error, true);
        }

        /// <summary>
        /// 输出严重错误日志
        /// </summary>
        /// <param name="message"></param>
        public void Fatal(string message)
        {
            if (level > LogLevel.Critical) return;
            var entrie = new LokiEntrie { line = message, time = DateTime.UtcNow };
            if (Loger.LogLocal == "console")
            {
                Loger.Console(string.Format("{0} => {1}", DateTools.Format(entrie.time), entrie.line), ConsoleColor.Magenta);
            }
            else if (Loger.LogLocal == "file")
            {
                flog.Write("fatal", message);
            }
            Write("fatal", entrie, LogLevel.Critical, true);
        }

        /// <summary>
        /// 输出严重错误日志
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="message"></param>
        public void Fatal(string topic, string message)
        {
            if (level > LogLevel.Critical) return;
            var entrie = new LokiEntrie { line = message, time = DateTime.UtcNow };
            if (Loger.LogLocal == "console")
            {
                Loger.Console(string.Format("{0} => {1}", DateTools.Format(entrie.time), entrie.line), ConsoleColor.Magenta);
            }
            else if (Loger.LogLocal == "file")
            {
                flog.Write(topic, message);
            }
            Write(topic, entrie, LogLevel.Critical, true);
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
            var entrie = new LokiEntrie { line = message, time = DateTime.UtcNow };
            if (consoleLocal && Level <= logLevel)
            {
                if (Loger.LogLocal == "console")
                {
                    var color = logLevel switch
                    {
                        LogLevel.Information => ConsoleColor.Gray,
                        LogLevel.Debug => ConsoleColor.White,
                        LogLevel.Error => ConsoleColor.Red,
                        LogLevel.Warning => ConsoleColor.DarkYellow,
                        LogLevel.Critical => ConsoleColor.Magenta,
                        _ => ConsoleColor.DarkGray
                    };
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
        /// 立即回写
        /// </summary>
        public void Flush()
        {
            Write("", null, LogLevel.None, true);
        }

        /// <summary>
        /// 异步立即回写
        /// </summary>
        public async Task FlushAsync(CancellationToken cancellationToken = default)
        {
            await WriteAsync("", null, LogLevel.None, true, cancellationToken);
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
            [System.Text.Json.Serialization.JsonIgnore]
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