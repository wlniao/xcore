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
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection.Emit;
using System.Security.Policy;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using static Wlniao.Log.LokiLoger;

namespace Wlniao.Log
{
    /// <summary>
    /// 基于Loki服务的日志写入工具
    /// </summary>
    public class LokiLoger : ILogProvider
    {
        /// <summary>
        /// 服务器地址
        /// </summary>
        private string serverHost = null;
		/// <summary>
		/// 
		/// </summary>
		private Dictionary<String, List<Entrie>> cache = null;
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
        /// 落盘时间间隔
        /// </summary>
        public int Interval = 10;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="level">日志输出级别</param>
        /// <param name="server">服务器地址</param>
        /// <param name="interval">落盘时间间隔（秒）</param>
        public LokiLoger(LogLevel level = LogLevel.Information, string server = null, int interval = 0)
		{
			this.level = level;
			this.Interval = interval > 0 ? interval : cvt.ToInt(Config.GetConfigs("WLN_LOG_INTERVAL ", "3"));
			if (string.IsNullOrEmpty(server))
			{
				serverHost = Config.GetConfigs("WLN_LOG_SERVER").TrimEnd('/');
				if (string.IsNullOrEmpty(serverHost))
				{
					Loger.Console(string.Format("{0} => {1}", DateTools.Format(), "WLN_LOG_SERVER not configured, please set loki server."), ConsoleColor.Red);
				}
			}
			else
			{
				serverHost = server.TrimEnd('/');
			}
			if (this.cache == null && !string.IsNullOrEmpty(serverHost))
			{
				this.cache = new Dictionary<String, List<Entrie>>();
				Task.Run(() =>
				{
					while (this.Interval > 0)
					{
						try
						{
							Task.Delay(Interval * 1000).Wait();
							var list = new List<object>();
							if (cache.Count == 0)
							{
								continue;
							}
							var content = new Dictionary<string, List<Entrie>>(cache);
							cache = new Dictionary<String, List<Entrie>>();
							foreach (var item in content)
							{
								if (item.Value.Count > 0)
								{
									list.Add(new
									{
										stream = new { topic = item.Key, node = XCore.WebNode },
										values = item.Value.Select(o => new[] { o.ts, o.line }).ToArray()
									});
								}
							}
							var handler = new HttpClientHandler();
							if (System.Net.ServicePointManager.ServerCertificateValidationCallback != null)
							{
								handler.ServerCertificateCustomValidationCallback = XCore.ValidateServerCertificate;
							}
							using (var client = new HttpClient(handler))
							{
								var start = DateTime.Now;
								var json = Newtonsoft.Json.JsonConvert.SerializeObject(new { streams = list.ToArray() });
								var reqest = new HttpRequestMessage(HttpMethod.Post, serverHost + "/loki/api/v1/push");
								reqest.Headers.Date = DateTime.Now;
								reqest.Content = new StreamContent(new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)));
								reqest.Content.Headers.Add("Content-Type", "application/json");
								client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Wlniao/XCore");
								var result = client.Send(reqest).Content.ReadAsStringAsync().Result;
								if (!string.IsNullOrEmpty(result))
								{
									Loger.Console(string.Format("{0} => {1}", DateTools.Format(), result), ConsoleColor.Red);
								}
							}
						}
						catch (Exception ex)
						{
							Loger.Console(string.Format("{0} => {1}", DateTools.Format(), "Loki Push:" + ex.Message), ConsoleColor.Red);
						}
					}
				});
			}
			else if (this.cache == null)
			{
				this.cache = new Dictionary<String, List<Entrie>>();
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
                var entrie = new Entrie { line = message, time = DateTime.UtcNow };
                Loger.Console(string.Format("{0} => {1}", DateTools.Format(entrie.time), entrie.line), ConsoleColor.DarkGray);
                Write("debug", entrie);
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
                var entrie = new Entrie { line = message, time = DateTime.UtcNow };
                Loger.Console(string.Format("{0} => {1}", DateTools.Format(entrie.time), entrie.line), ConsoleColor.White);
                Write("info", entrie);
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
                var entrie = new Entrie { line = message, time = DateTime.UtcNow };
                Loger.Console(string.Format("{0} => {1}", DateTools.Format(entrie.time), entrie.line), ConsoleColor.DarkYellow);
                Write("warn", entrie);
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
                var entrie = new Entrie { line = message, time = DateTime.Now };
                Loger.Console(string.Format("{0} => {1}", DateTools.Format(entrie.time), entrie.line), ConsoleColor.Red);
                Write("error", entrie, true);
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
                var entrie = new Entrie { line = message, time = DateTime.UtcNow };
                Loger.Console(string.Format("{0} => {1}", DateTools.Format(entrie.time), entrie.line), ConsoleColor.Magenta);
                Write("fatal", entrie, true);
            }
        }
        /// <summary>
        /// 输出日志
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="message"></param>
        public void Topic(String topic, String message)
        {
            var entrie = new Entrie { line = message, time = DateTime.UtcNow };
            Loger.Console(string.Format("[{0}]{1} => {2}", topic, DateTools.Format(entrie.time), entrie.line), ConsoleColor.White);
            Write(topic, entrie);
        }

        /// <summary>
        /// 日志主体
        /// </summary>
        public class Entrie
        {
            /// <summary>
            /// 日志时间
            /// </summary>
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
        /// 输出日志
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="entrie"></param>
        /// <param name="push">是否立即回写</param>
        private void Write(String topic, Entrie entrie, Boolean push = false)
        {
            try
            {
                if (push)
				{
                    // 实时推送日志
					var json = Newtonsoft.Json.JsonConvert.SerializeObject(new
					{
						streams = new[] {
								new {
									stream = new { topic = topic, node = XCore.WebNode },
									values = new[] { entrie }
								}
							}
					});
					var handler = new HttpClientHandler();
					if (System.Net.ServicePointManager.ServerCertificateValidationCallback != null)
					{
						handler.ServerCertificateCustomValidationCallback = XCore.ValidateServerCertificate;
					}
					using (var client = new HttpClient(handler))
					{
						var start = DateTime.Now;
						var reqest = new HttpRequestMessage(HttpMethod.Post, serverHost + "/loki/api/v1/push");
						reqest.Headers.Date = DateTime.Now;
						reqest.Content = new StreamContent(new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)));
						reqest.Content.Headers.Add("Content-Type", "application/json");
						client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Wlniao/XCore");
						var result = client.Send(reqest).Content.ReadAsStringAsync().Result;
						if (!string.IsNullOrEmpty(result))
						{
							Loger.Console(string.Format("{0} => {1}", DateTools.Format(), result), ConsoleColor.Red);
						}
					}
				}
				else if (cache.ContainsKey(topic))
				{
					cache[topic].Add(entrie);
				}
				else
				{
					cache.TryAdd(topic, new List<Entrie> { entrie });
				}
			}
			catch (Exception ex)
			{
				Loger.Console(string.Format("{0} => {1}", DateTools.Format(), "Loki Push:" + ex.Message), ConsoleColor.Red);
			}
		}

    }
}