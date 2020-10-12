/*==============================================================================
    文件名称：Common.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：XServer请求工具
================================================================================
 
    Copyright 2014 XieChaoyi

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
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
namespace Wlniao.XServer
{
    /// <summary>
    /// XServer请求工具
    /// </summary>
    public class Common
    {
        /// <summary>
        /// 开放接口主机地址
        /// </summary>
        public const string OpenHost = "https://openapi.wlniao.com";
        private static string _App = "";
        private static string _AppId = "";
        private static string _Secret = "";
        internal static bool console = true;
        internal static bool retry = Config.GetSetting("WLN_RETRY").ToLower() == "true";
        /// <summary>
        /// 
        /// </summary>
        internal static void Init()
        {
            _App = "";
            _AppId = "";
            _Secret = "";
            _AppInstances = new Dictionary<String, CommonApp>();
        }
        #region XServer访问
        /// <summary>
        /// App实例
        /// </summary>
        internal class Instance
        {
            /// <summary>
            /// 主机地址
            /// </summary>
            public String HostAddress { get; set; }
            public System.Net.IPAddress HostIP = null;
            /// <summary>
            /// 权重
            /// </summary>
            public int Weight { get; set; }
            /// <summary>
            /// 失败次数
            /// </summary>
            public int FailCount { get; set; }
            /// <summary>
            /// 实时压力指数
            /// </summary>
            public long Pressure { get; set; }

            /// <summary>
            /// 开始请求实例
            /// </summary>
            public void Start()
            {
                Pressure++;
            }
            /// <summary>
            /// 报告成功
            /// </summary>
            public void Success(long usetime = 0)
            {
                if (FailCount > 10)
                {
                    FailCount = 9;
                }
                else if (FailCount > 0)
                {
                    FailCount--;
                }
                //统计实例响应情况
            }
            /// <summary>
            /// 报告失败
            /// </summary>
            public void Failed()
            {
                FailCount++;
            }
        }
        /// <summary>
        /// App实例
        /// </summary>
        internal class CommonApp
        {
            private string _app;
            public string App
            {
                get
                {
                    return _app;
                }
            }
            private Dictionary<String, Instance> _instances;
            public Dictionary<String, Instance> Instances
            {
                get
                {
                    if (_instances == null || _instances.Count == 0)
                    {
                        lock (_instances)
                        {
                            _instances = new Dictionary<string, Instance>();
                            var host = Config.GetSetting(App + "Host", "");
                            if (string.IsNullOrEmpty(host))
                            {
                                var rlt = XsApp.GetInstances(App);
                                if (rlt.success && rlt.data.Count > 0)
                                {
                                    var em = rlt.data.GetEnumerator();
                                    while (em.MoveNext())
                                    {
                                        Add(em.Current.Key, em.Current.Value);
                                    }
                                }
                            }
                            else
                            {
                                Add(host, 100);
                            }
                            return _instances;
                        }
                    }
                    else if (_instances.Count > 1)
                    {
                        return _instances.OrderBy(o => o.Value.Pressure / o.Value.Weight).ToDictionary(o => o.Key, p => p.Value);
                    }
                    else
                    {
                        return _instances;
                    }
                }
            }
            /// <summary>
            /// 实例化一个APP
            /// </summary>
            /// <param name="App"></param>
            public CommonApp(String App)
            {
                _app = App;
                _instances = new Dictionary<String, Instance>();               
                if (App.ToLower() == "openapi")
                {
                    var host = Config.GetSetting(App + "Host");
                    if (string.IsNullOrEmpty(host))
                    {
                        if (_instances.Count == 0)
                        {
                            Add(OpenHost);
                        }
                    }
                    else
                    {
                        Add(host.ToLower());
                    }
                }
            }
            /// <summary>
            /// 实例化一个APP
            /// </summary>
            /// <param name="AppCode"></param>
            /// <param name="HostAddress"></param>
            public CommonApp(String AppCode, String HostAddress)
            {
                _app = AppCode;
                _instances = new Dictionary<String, Instance>();
                Add(HostAddress);
            }
            /// <summary>
            /// 添加一个本地实例
            /// </summary>
            /// <param name="HostAddress">实例地址</param>
            /// <param name="Weight">权重</param>
            public void Add(String HostAddress, Int32 Weight = 100)
            {
                if (Weight <= 0)
                {
                    return;
                }
                if (!string.IsNullOrEmpty(HostAddress))
                {
                    if (HostAddress.IndexOf("://") < 0)
                    {
                        HostAddress = "http://" + HostAddress.ToLower();
                    }
                    else
                    {
                        HostAddress.ToLower();
                    }
                    lock (_instances)
                    {
                        if (!_instances.ContainsKey(HostAddress))
                        {
                            _instances.Add(HostAddress, new Instance() { HostAddress = HostAddress, Weight = Weight, Pressure = 0, FailCount = 0 });
                        }
                        else
                        {
                            _instances[HostAddress].FailCount = 0;
                        }
                    }
                }
            }
            public void WriteLog(params ApiLog[] logs)
            {
                foreach (var item in logs)
                {
                    Log.Loger.Console(item.apiurl + "[" + item.usetime + "ms]", item.status ? ConsoleColor.DarkGreen : ConsoleColor.Red);
                    if (!string.IsNullOrEmpty(item.message))
                    {
                        Log.Loger.Console(item.message, ConsoleColor.White);
                    }
                }
            }
        }
        private static Dictionary<String, CommonApp> _AppInstances = new Dictionary<String, CommonApp>();
        /// <summary>
        /// App的实例
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        internal static CommonApp GetInstances(String app)
        {
            var _key = app.ToLower();
            if (_AppInstances.ContainsKey(_key))
            {
                return _AppInstances[_key];
            }
            else
            {
                var _app = new CommonApp(app);
                try { _AppInstances.Add(_key, _app); } catch { }
                return _app;
            }
        }
        /// <summary>
        /// 当前配置的AppCode
        /// </summary>
        public static string App
        {
            get
            {
                if (string.IsNullOrEmpty(_App))
                {
                    var _rlt = XsApp.GetById(AppId);
                    if (_rlt.success)
                    {
                        _App = _rlt.data.appcode;
                    }
                }
                return _AppId;
            }
        }
        /// <summary>
        /// 当前配置的XServerAppId
        /// </summary>
        public static string AppId
        {
            get
            {
                if (string.IsNullOrEmpty(_AppId))
                {
                    _AppId = Config.GetSetting("XServerAppId");
                }
                return _AppId;
            }
        }
        /// <summary>
        /// 当前配置的XServerSecret
        /// </summary>
        public static string Secret
        {
            get
            {
                if (string.IsNullOrEmpty(_Secret))
                {
                    _Secret = Config.GetSetting("XServerSecret");
                }
                return _Secret;
            }
        }
        /// <summary>
        /// 发起Http请求
        /// </summary>
        /// <param name="url">请求的Url</param>
        /// <param name="engine">引擎模式（0:httpclient 1:socket）</param>
        /// <returns></returns>
        public static string GetResponseString(String url, Int32 engine = 0)
        {
            var times = 3;
            while (times > 0)
            {
                var str = "";
                var uri = new Uri(url);
                if (engine == 1)
                {
                    #region
                    var hostSocket = Net.WlnSocket.GetSocket(uri.Host, uri.Port);
                    try
                    {
                        var reqStr = "";
                        reqStr += "GET " + uri.PathAndQuery + " HTTP/1.0";
                        reqStr += "\r\nHost: " + uri.Host;
                        reqStr += "\r\nDate: " + DateTools.ConvertToGMT(DateTools.GetUnix());
                        reqStr += "\r\nAccept: application/json";
                        reqStr += "\r\n";
                        reqStr += "\r\n";
                        var request = Encoding.UTF8.GetBytes(reqStr);
                        if (uri.Scheme == "https")
                        {
                            #region HTTPS请求
                            using (SslStream ssl = new SslStream(new NetworkStream(hostSocket, true), false, new RemoteCertificateValidationCallback(XCore.ValidateServerCertificate), null))
                            {
                                ssl.AuthenticateAsClientAsync(uri.Host).ContinueWith((_rlt) =>
                                {
                                    if (ssl.IsAuthenticated)
                                    {
                                        ssl.Write(request);
                                        ssl.Flush();
                                        var length = 0;
                                        var end = false;
                                        var start = false;
                                        var chunked = false;
                                        while (true)
                                        {
                                            var rev = new byte[65535];
                                            var index = ssl.Read(rev, 0, rev.Length);
                                            if (index == 0)
                                            {
                                                break;
                                            }
                                            var beffur = new byte[index];
                                            Buffer.BlockCopy(rev, 0, beffur, 0, index);
                                            var tempstr = strUtil.GetUTF8String(beffur);
                                            var lines = tempstr.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                                            index = 0;
                                            #region Headers处理
                                            if (!start && lines[0].StartsWith("HTTP"))
                                            {
                                                var ts = lines[0].Split(' ');
                                                if (ts[1] == "200")
                                                {
                                                    for (index = 1; index < lines.Length; index++)
                                                    {
                                                        if (lines[index].ToLower().StartsWith("content-length"))
                                                        {
                                                            ts = lines[index].Split(' ');
                                                            length = cvt.ToInt(ts[1]);
                                                        }
                                                        else if (lines[index].ToLower().StartsWith("transfer-encoding"))
                                                        {
                                                            chunked = lines[index].EndsWith("chunked");
                                                        }
                                                        if (string.IsNullOrEmpty(lines[index]))
                                                        {
                                                            index++;
                                                            start = true;
                                                            break;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    index = lines.Length;
                                                    break;
                                                }
                                            }
                                            #endregion
                                            #region 取文本内容
                                            for (; index < lines.Length; index++)
                                            {
                                                var line = lines[index];
                                                if (chunked)
                                                {
                                                    index++;
                                                    if (index < lines.Length)
                                                    {
                                                        var tempLength = cvt.DeHex(line, "0123456789abcdef");
                                                        if (tempLength > 0)
                                                        {
                                                            length += (int)tempLength;
                                                            line = lines[index];
                                                        }
                                                        else if (lines.Length == index + 2 && string.IsNullOrEmpty(lines[index + 1]))
                                                        {
                                                            end = true;
                                                            break;
                                                        }
                                                        else
                                                        {
                                                            break;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        break;
                                                    }
                                                }
                                                if (index == 0 || (chunked && index == 1) || str.Length == 0)
                                                {
                                                    str += line;
                                                }
                                                else
                                                {
                                                    str += "\r\n" + line;
                                                }
                                                if (!chunked && System.Text.Encoding.UTF8.GetBytes(str).Length >= length)
                                                {
                                                    end = true;
                                                }
                                            }
                                            if (end)
                                            {
                                                break;
                                            }
                                            #endregion
                                        }
                                    }
                                }).Wait();
                            }
                            #endregion
                        }
                        else
                        {
                            #region HTTP请求
                            if (hostSocket.Send(request, request.Length, System.Net.Sockets.SocketFlags.None) > 0)
                            {
                                var length = 0;
                                var end = false;
                                var start = false;
                                var chunked = false;
                                while (true)
                                {
                                    var rev = new byte[65535];
                                    var index = hostSocket.Receive(rev, rev.Length, System.Net.Sockets.SocketFlags.None);
                                    if (index == 0)
                                    {
                                        break;
                                    }
                                    var tempstr = strUtil.GetUTF8String(rev, 0, index);
                                    var lines = tempstr.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                                    index = 0;
                                    #region Headers处理
                                    if (!start && lines[0].StartsWith("HTTP"))
                                    {
                                        var ts = lines[0].Split(' ');
                                        if (ts[1] == "200")
                                        {
                                            for (index = 1; index < lines.Length; index++)
                                            {
                                                if (lines[index].ToLower().StartsWith("content-length"))
                                                {
                                                    ts = lines[index].Split(' ');
                                                    length = cvt.ToInt(ts[1]);
                                                }
                                                else if (lines[index].ToLower().StartsWith("transfer-encoding"))
                                                {
                                                    chunked = lines[index].EndsWith("chunked");
                                                }
                                                if (string.IsNullOrEmpty(lines[index]))
                                                {
                                                    index++;
                                                    start = true;
                                                    break;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            index = lines.Length;
                                            break;
                                        }
                                    }
                                    #endregion
                                    #region 取文本内容
                                    for (; index < lines.Length; index++)
                                    {
                                        var line = lines[index];
                                        if (chunked)
                                        {
                                            index++;
                                            if (index < lines.Length)
                                            {
                                                var tempLength = cvt.DeHex(line, "0123456789abcdef");
                                                if (tempLength > 0)
                                                {
                                                    length += (int)tempLength;
                                                    line = lines[index];
                                                }
                                                else if (lines.Length == index + 2 && string.IsNullOrEmpty(lines[index + 1]))
                                                {
                                                    end = true;
                                                    break;
                                                }
                                                else
                                                {
                                                    break;
                                                }
                                            }
                                            else
                                            {
                                                break;
                                            }
                                        }
                                        if (index == 0 || (chunked && index == 1) || str.Length == 0)
                                        {
                                            str += line;
                                        }
                                        else
                                        {
                                            str += "\r\n" + line;
                                        }
                                        if (!chunked && System.Text.Encoding.UTF8.GetBytes(str).Length >= length)
                                        {
                                            end = true;
                                        }
                                    }
                                    if (end)
                                    {
                                        break;
                                    }
                                    #endregion
                                }
                            }
                            #endregion
                        }
                        hostSocket.Using = false;
                    }
                    catch { }
                    #endregion
                }
                else
                {
                    #region
                    try
                    {
                        var handler = new System.Net.Http.HttpClientHandler();
                        if (System.Net.ServicePointManager.ServerCertificateValidationCallback != null)
                        {
                            handler.ServerCertificateCustomValidationCallback = XCore.ValidateServerCertificate;
                        }
                        var response = new System.Net.Http.HttpClient(handler).GetAsync(uri).GetAwaiter().GetResult();
                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            str = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        }
                        else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            log.Error("NotFound [" + uri.Scheme + "://" + uri.Authority + uri.AbsolutePath + "]");
                        }
                    }
                    catch (System.Net.Http.HttpRequestException ex)
                    {
                        log.Error(ex.Message + (ex.InnerException == null ? " " : " " + ex.InnerException.Message) + " [" + uri.Authority + "]");
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex.Message + " [" + uri.Authority + "]");
                    }
                    #endregion
                }
                if (string.IsNullOrEmpty(str))
                {
                    times--;
                    if (retry && times > 0)
                    {
                        System.Threading.Tasks.Task.Delay((int)Math.Pow(21, (4 - times))).Wait();
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    return str;
                }
            }
            return "";
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <param name="postData"></param>
        /// <returns></returns>
        public static string PostResponseString(String url, String postData)
        {
            var encode = System.Text.Encoding.UTF8;
            byte[] byteArray = encode.GetBytes(postData);
            var stream = cvt.ToStream(byteArray);
            var str = "";
            var err = "";
            var handler = new System.Net.Http.HttpClientHandler();
            if (System.Net.ServicePointManager.ServerCertificateValidationCallback != null)
            {
                handler.ServerCertificateCustomValidationCallback = XCore.ValidateServerCertificate;
            }
            using (var client = new System.Net.Http.HttpClient(handler))
            {
                var reqest = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, url);
                reqest.Headers.Date = DateTime.UtcNow;
                reqest.Headers.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("Wlniao-XCore-XServer", "beta"));
                if (stream != null && stream.Length > 0)
                {
                    reqest.Content = new System.Net.Http.StreamContent(stream);
                }
                client.SendAsync(reqest).ContinueWith((requestTask) =>
                {
                    try
                    {
                        var response = requestTask.Result;
                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            response.Content.ReadAsStringAsync().ContinueWith((readTask) =>
                            {
                                str = readTask.Result;
                            }).Wait();
                        }
                        else
                        {
                            response.Content.ReadAsStringAsync().ContinueWith((readTask) =>
                            {
                                str = readTask.Result;
                            }).Wait();
                        }
                    }
                    catch (AggregateException aex)
                    {
                        if (aex.InnerException != null)
                        {
                            if (aex.InnerException.InnerException != null)
                            {
                                err = aex.InnerException.InnerException.Message;
                            }
                            else
                            {
                                err = aex.InnerException.Message;
                            }
                        }
                        else
                        {
                            err = aex.Message;
                        }
                    }
                }).Wait();
            }
            return str;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static string PostResponseString(String url, System.IO.Stream stream)
        {
            var str = "";
            var err = "";
            var handler = new System.Net.Http.HttpClientHandler();
            if (System.Net.ServicePointManager.ServerCertificateValidationCallback != null)
            {
                handler.ServerCertificateCustomValidationCallback = XCore.ValidateServerCertificate;
            }
            using (var client = new System.Net.Http.HttpClient(handler))
            {
                var reqest = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, url);
                reqest.Headers.Date = DateTime.UtcNow;
                reqest.Headers.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("Wlniao-XCore-XServer", "beta"));
                if (stream != null && stream.Length > 0)
                {
                    reqest.Content = new System.Net.Http.StreamContent(stream);
                }
                client.SendAsync(reqest).ContinueWith((requestTask) =>
                {
                    try
                    {
                        requestTask.Result.Content.ReadAsStringAsync().ContinueWith((readTask) =>
                        {
                            str = readTask.Result;
                        }).Wait();
                    }
                    catch (AggregateException aex)
                    {
                        if (aex.InnerException != null)
                        {
                            if (aex.InnerException.InnerException != null)
                            {
                                err = aex.InnerException.InnerException.Message;
                            }
                            else
                            {
                                err = aex.InnerException.Message;
                            }
                        }
                        else
                        {
                            err = aex.Message;
                        }
                    }
                }).Wait();
            }
            return str;
        }
        /// <summary>
        /// 发起XServerGet请求（引擎通过HttpEngine配置）
        /// </summary>
        /// <param name="engine">引擎模式（0:restsharp 1:socket 2:httpclient）</param>
        /// <param name="common"></param>
        /// <param name="controller"></param>
        /// <param name="action"></param>
        /// <param name="logs"></param>
        /// <param name="kvs"></param>
        /// <returns></returns>
        private static string Get(Int32 engine, CommonApp common, string controller, string action, out List<ApiLog> logs, params KeyValuePair<String, String>[] kvs)
        {
            logs = new List<ApiLog>();
            var rlt = "";
            var data = "";
            var kvList = new List<KeyValuePair<String, String>>(kvs);
            #region 处理接口基本参数及签名
            if (!string.IsNullOrEmpty(Secret))
            {
                kvList.Add(new KeyValuePair<String, String>("xstime", DateTools.GetUnix().ToString()));
                if (!string.IsNullOrEmpty(AppId))
                {
                    kvList.Add(new KeyValuePair<String, String>("xsappid", AppId));
                }
                kvList = kvList.OrderBy(o => o.Key).ToList();
                var values = new System.Text.StringBuilder();
                foreach (var kv in kvList)
                {
                    if (!string.IsNullOrEmpty(kv.Key))
                    {
                        values.Append(kv.Value);
                    }
                }
                values.Append(Secret);
                var md5_result = System.Security.Cryptography.MD5.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes(values.ToString()));
                var sig_builder = new System.Text.StringBuilder();
                foreach (byte b in md5_result)
                {
                    sig_builder.Append(b.ToString("x2"));
                }
                kvList.Add(new KeyValuePair<String, String>("sig", sig_builder.ToString()));
            }
            #endregion
            foreach (var kv in kvList)
            {
                if (!string.IsNullOrEmpty(data))
                {
                    data += "&";
                }
                data += kv.Key + "=" + kv.Value;
            }
            var em = common.Instances.GetEnumerator();
            while (em.MoveNext())
            {
                var times = 3;
                while (times > 0)
                {
                    rlt = "";
                    var err = "";
                    var url = em.Current.Key + "/" + controller + "/" + action;
                    if (!string.IsNullOrEmpty(data))
                    {
                        url += "?" + data;
                    }
                    if (console && XCore.LogLevel == Log.LogLevel.Debug)
                    {
                        Log.Loger.Console(url, ConsoleColor.DarkGreen);
                    }
                    em.Current.Value.Start();
                    var uri = new Uri(url);
                    var apilog = new ApiLog(common.App, url);
                    if (engine == 1)
                    {
                        #region
                        var hostSocket = Net.WlnSocket.GetSocket(uri.Host, uri.Port);
                        try
                        {
                            var reqStr = "";
                            reqStr += "GET " + uri.PathAndQuery + " HTTP/1.0";
                            reqStr += "\r\nHost: " + uri.Host;
                            reqStr += "\r\nDate: " + DateTools.ConvertToGMT(DateTools.GetUnix());
                            reqStr += "\r\nAccept: application/json";
                            reqStr += "\r\n";
                            reqStr += "\r\n";
                            var request = Encoding.UTF8.GetBytes(reqStr);
                            if (uri.Scheme == "https")
                            {
                                #region HTTPS请求
                                using (SslStream ssl = new SslStream(new NetworkStream(hostSocket, true), false, new RemoteCertificateValidationCallback(XCore.ValidateServerCertificate), null))
                                {
                                    ssl.AuthenticateAsClientAsync(uri.Host).ContinueWith((_rlt) =>
                                    {
                                        if (ssl.IsAuthenticated)
                                        {
                                            ssl.Write(request);
                                            ssl.Flush();
                                            var length = 0;
                                            var end = false;
                                            var start = false;
                                            var chunked = false;
                                            while (true)
                                            {
                                                var rev = new byte[65535];
                                                var index = ssl.Read(rev, 0, rev.Length);
                                                if (index == 0)
                                                {
                                                    break;
                                                }
                                                var beffur = new byte[index];
                                                Buffer.BlockCopy(rev, 0, beffur, 0, index);
                                                var tempstr = strUtil.GetUTF8String(beffur);
                                                var lines = tempstr.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                                                index = 0;
                                                #region Headers处理
                                                if (!start && lines[0].StartsWith("HTTP"))
                                                {
                                                    var ts = lines[0].Split(' ');
                                                    if (ts[1] == "200")
                                                    {
                                                        for (index = 1; index < lines.Length; index++)
                                                        {
                                                            if (lines[index].ToLower().StartsWith("content-length"))
                                                            {
                                                                ts = lines[index].Split(' ');
                                                                length = cvt.ToInt(ts[1]);
                                                            }
                                                            else if (lines[index].ToLower().StartsWith("transfer-encoding"))
                                                            {
                                                                chunked = lines[index].EndsWith("chunked");
                                                            }
                                                            if (string.IsNullOrEmpty(lines[index]))
                                                            {
                                                                index++;
                                                                start = true;
                                                                break;
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        err = lines.LastOrDefault();
                                                        index = lines.Length;
                                                        break;
                                                    }
                                                }
                                                #endregion
                                                #region 取文本内容
                                                for (; index < lines.Length; index++)
                                                {
                                                    var line = lines[index];
                                                    if (chunked)
                                                    {
                                                        index++;
                                                        if (index < lines.Length)
                                                        {
                                                            var tempLength = cvt.DeHex(line, "0123456789abcdef");
                                                            if (tempLength > 0)
                                                            {
                                                                length += (int)tempLength;
                                                                line = lines[index];
                                                            }
                                                            else if (lines.Length == index + 2 && string.IsNullOrEmpty(lines[index + 1]))
                                                            {
                                                                end = true;
                                                                break;
                                                            }
                                                            else
                                                            {
                                                                break;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            break;
                                                        }
                                                    }
                                                    if (index == 0 || (chunked && index == 1) || rlt.Length == 0)
                                                    {
                                                        rlt += line;
                                                    }
                                                    else
                                                    {
                                                        rlt += "\r\n" + line;
                                                    }
                                                    if (!chunked && System.Text.Encoding.UTF8.GetBytes(rlt).Length >= length)
                                                    {
                                                        end = true;
                                                    }
                                                }
                                                if (end)
                                                {
                                                    break;
                                                }
                                                #endregion
                                            }
                                        }
                                    }).Wait();
                                }
                                #endregion
                            }
                            else
                            {
                                #region HTTP请求
                                if (hostSocket.Send(request, request.Length, System.Net.Sockets.SocketFlags.None) > 0)
                                {
                                    var length = 0;
                                    var end = false;
                                    var start = false;
                                    var chunked = false;
                                    while (true)
                                    {
                                        var rev = new byte[65535];
                                        var index = hostSocket.Receive(rev, rev.Length, System.Net.Sockets.SocketFlags.None);
                                        if (index == 0)
                                        {
                                            break;
                                        }
                                        var beffur = new byte[index];
                                        Buffer.BlockCopy(rev, 0, beffur, 0, index);
                                        var tempstr = strUtil.GetUTF8String(beffur);
                                        var lines = tempstr.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                                        index = 0;
                                        #region Headers处理
                                        if (!start && lines[0].StartsWith("HTTP"))
                                        {
                                            var ts = lines[0].Split(' ');
                                            if (ts[1] == "200")
                                            {
                                                for (index = 1; index < lines.Length; index++)
                                                {
                                                    if (lines[index].ToLower().StartsWith("content-length"))
                                                    {
                                                        ts = lines[index].Split(' ');
                                                        length = cvt.ToInt(ts[1]);
                                                    }
                                                    else if (lines[index].ToLower().StartsWith("transfer-encoding"))
                                                    {
                                                        chunked = lines[index].EndsWith("chunked");
                                                    }
                                                    if (string.IsNullOrEmpty(lines[index]))
                                                    {
                                                        index++;
                                                        start = true;
                                                        break;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                err = lines.LastOrDefault();
                                                index = lines.Length;
                                                break;
                                            }
                                        }
                                        #endregion
                                        #region 取文本内容
                                        for (; index < lines.Length; index++)
                                        {
                                            var line = lines[index];
                                            if (chunked)
                                            {
                                                index++;
                                                if (index < lines.Length)
                                                {
                                                    var tempLength = cvt.DeHex(line, "0123456789abcdef");
                                                    if (tempLength > 0)
                                                    {
                                                        length += (int)tempLength;
                                                        line = lines[index];
                                                    }
                                                    else if (lines.Length == index + 2 && string.IsNullOrEmpty(lines[index + 1]))
                                                    {
                                                        end = true;
                                                        break;
                                                    }
                                                    else
                                                    {
                                                        break;
                                                    }
                                                }
                                                else
                                                {
                                                    break;
                                                }
                                            }
                                            if (index == 0 || (chunked && index == 1) || rlt.Length == 0)
                                            {
                                                rlt += line;
                                            }
                                            else
                                            {
                                                rlt += "\r\n" + line;
                                            }
                                            if (!chunked && System.Text.Encoding.UTF8.GetBytes(rlt).Length >= length)
                                            {
                                                end = true;
                                            }
                                        }
                                        if (end)
                                        {
                                            break;
                                        }
                                        #endregion
                                    }
                                }
                                #endregion
                            }
                            if (string.IsNullOrEmpty(rlt))
                            {
                                em.Current.Value.Failed();
                                apilog.Failed(err);
                            }
                            else
                            {
                                em.Current.Value.Success();
                                apilog.Success(rlt);
                            }
                            hostSocket.Using = false;
                        }
                        catch (Exception ex)
                        {
                            em.Current.Value.Failed();
                            apilog.Failed(ex.Message);
                        }
                        #endregion
                    }
                    else
                    {
                        #region
                        try
                        {
                            var handler = new System.Net.Http.HttpClientHandler();
                            if (System.Net.ServicePointManager.ServerCertificateValidationCallback != null)
                            {
                                handler.ServerCertificateCustomValidationCallback = XCore.ValidateServerCertificate;
                            }
                            var response = new System.Net.Http.HttpClient(handler).GetAsync(uri).GetAwaiter().GetResult();
                            if (response.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                rlt = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                                em.Current.Value.Success();
                                apilog.Success(rlt);
                            }
                            else
                            {
                                em.Current.Value.Failed();
                                apilog.Failed(response.StatusCode.ToString());
                            }
                        }
                        catch (AggregateException aex)
                        {
                            em.Current.Value.Failed();
                            if (aex.InnerException != null)
                            {
                                if (aex.InnerException.InnerException != null)
                                {
                                    apilog.Failed(aex.InnerException.InnerException.Message);
                                }
                                else
                                {
                                    apilog.Failed(aex.InnerException.Message);
                                }
                            }
                            else
                            {
                                apilog.Failed(aex.Message);
                            }
                        }
                        #endregion
                    }
                    if (console && XCore.LogLevel == Log.LogLevel.Info)
                    {
                        common.WriteLog(apilog);
                    }
                    logs.Add(apilog);
                    if (string.IsNullOrEmpty(rlt))
                    {
                        times--;
                        if (retry && times > 0)
                        {
                            System.Threading.Tasks.Task.Delay((int)Math.Pow(15, (4 - times))).Wait();
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            return rlt;
        }
        /// <summary>
        /// 发起XServerGet请求（引擎通过HttpEngine配置）
        /// </summary>
        /// <param name="common"></param>
        /// <param name="controller"></param>
        /// <param name="action"></param>
        /// <param name="logs"></param>
        /// <param name="kvs"></param>
        /// <returns></returns>
        internal static string Get(CommonApp common, string controller, string action, out List<ApiLog> logs, params KeyValuePair<String, String>[] kvs)
        {
            return Get(0, common, controller, action, out logs, kvs);
        }
        /// <summary>
        /// 发起XServerGet请求
        /// </summary>
        /// <param name="app"></param>
        /// <param name="controller"></param>
        /// <param name="action"></param>
        /// <param name="kvs"></param>
        /// <returns></returns>
        public static string Get(string app, string controller, string action, params KeyValuePair<String, String>[] kvs)
        {
            List<ApiLog> logs;
            var common = GetInstances(app);
            return Get(common, controller, action, out logs, kvs);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        /// <param name="controller"></param>
        /// <param name="action"></param>
        /// <param name="logs"></param>
        /// <param name="kvs"></param>
        /// <returns></returns>
        public static string Get(string app, string controller, string action, out List<ApiLog> logs, params KeyValuePair<String, String>[] kvs)
        {
            var common = GetInstances(app);
            return Get(common, controller, action, out logs, kvs);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="app"></param>
        /// <param name="controller"></param>
        /// <param name="action"></param>
        /// <param name="kvs"></param>
        /// <returns></returns>
        public static ApiResult<T> Get<T>(string app, string controller, string action, params KeyValuePair<String, String>[] kvs)
        {
            List<ApiLog> logs;
            var common = GetInstances(app);
            var str = Get(common, controller, action, out logs, kvs);
            var rlt = Json.ToObject<ApiResult<T>>(str);
            rlt.PutLog(logs);
            return rlt;
        }
        /// <summary>
        /// Get请求但只返回成功后的data部分
        /// </summary>
        /// <param name="app"></param>
        /// <param name="controller"></param>
        /// <param name="action"></param>
        /// <param name="kvs"></param>
        /// <returns></returns>
        public static string GetOnlyData(string app, string controller, string action, params KeyValuePair<String, String>[] kvs)
        {
            List<ApiLog> logs;
            var common = GetInstances(app);
            var json = Get(common, controller, action, out logs, kvs);
            try
            {
                var temp = json.Substring(json.IndexOf("\"data\"") + 7);
                temp = temp.Substring(0, temp.IndexOf(",\"logs\":[") > 0 ? temp.IndexOf(",\"logs\":[") : temp.LastIndexOf("}"));
                if (temp.StartsWith("\""))
                {
                    json = json.Replace(temp, "");
                    temp = temp.Substring(1, temp.Length - 1);
                }
                else
                {
                    json = json.Replace(temp, "\"\"");
                }
                var rlt = Json.ToObject<ApiResult<String>>(json);
                if (rlt.success)
                {
                    return temp;
                }
            }
            catch { }
            return "";
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="common"></param>
        /// <param name="controller"></param>
        /// <param name="action"></param>
        /// <param name="postData"></param>
        /// <param name="logs"></param>
        /// <param name="kvs"></param>
        /// <returns></returns>
        internal static string Post(CommonApp common, string controller, string action, string postData, out List<ApiLog> logs, params KeyValuePair<String, String>[] kvs)
        {
            var data = "";
            var kvList = new List<KeyValuePair<String, String>>(kvs);
            #region 处理接口基本参数及签名
            if (!string.IsNullOrEmpty(Secret))
            {
                kvList.Add(new KeyValuePair<String, String>("xstime", DateTools.GetUnix().ToString()));
                if (!string.IsNullOrEmpty(AppId))
                {
                    kvList.Add(new KeyValuePair<String, String>("xsappid", AppId));
                }
                kvList = kvList.OrderBy(o => o.Key).ToList();
                var values = new System.Text.StringBuilder();
                foreach (var kv in kvList)
                {
                    if (!string.IsNullOrEmpty(kv.Key))
                    {
                        values.Append(kv.Value);
                    }
                }
                values.Append(Secret);
                //生成sig
                var md5_result = System.Security.Cryptography.MD5.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes(values.ToString()));
                var sig_builder = new System.Text.StringBuilder();
                foreach (byte b in md5_result)
                {
                    sig_builder.Append(b.ToString("x2"));
                }
                kvList.Add(new KeyValuePair<String, String>("sig", sig_builder.ToString()));
            }
            #endregion
            foreach (var kv in kvList)
            {
                if (!string.IsNullOrEmpty(data))
                {
                    data += "&";
                }
                data += kv.Key + "=" + kv.Value;
            }
            logs = new List<ApiLog>();
            var em = common.Instances.GetEnumerator();
            while (em.MoveNext())
            {
                var str = "";
                var url = em.Current.Key + "/" + controller + "/" + action;
                if (!string.IsNullOrEmpty(data))
                {
                    url += "?" + data;
                }
                var apilog = new ApiLog(common.App, url);
                if (XCore.LogLevel == Log.LogLevel.Debug)
                {
                    Log.Loger.Console(url, ConsoleColor.DarkGreen);
                }
                try
                {
                    em.Current.Value.Start();
                    var encode = System.Text.Encoding.UTF8;
                    var byteArray = encode.GetBytes(postData);
                    var stream = cvt.ToStream(byteArray);
                    var handler = new System.Net.Http.HttpClientHandler();
                    if (System.Net.ServicePointManager.ServerCertificateValidationCallback != null)
                    {
                        handler.ServerCertificateCustomValidationCallback = XCore.ValidateServerCertificate;
                    }
                    using (var client = new System.Net.Http.HttpClient(handler))
                    {
                        var reqest = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, url);
                        reqest.Headers.Date = DateTime.UtcNow;
                        reqest.Headers.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("Wlniao-XCore-XServer", "beta"));
                        if (stream != null && stream.Length > 0)
                        {
                            reqest.Content = new System.Net.Http.StreamContent(stream);
                        }
                        client.SendAsync(reqest).ContinueWith((requestTask) =>
                        {
                            try
                            {
                                var response = requestTask.Result;
                                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                                {
                                    response.Content.ReadAsStringAsync().ContinueWith((readTask) =>
                                    {
                                        str = readTask.Result;
                                    }).Wait();
                                }
                                else
                                {
                                    response.Content.ReadAsStringAsync().ContinueWith((readTask) =>
                                    {
                                        str = readTask.Result;
                                    }).Wait();
                                }
                            }
                            catch (AggregateException aex)
                            {
                                if (aex.InnerException != null)
                                {
                                    if (aex.InnerException.InnerException != null)
                                    {
                                        apilog.Failed(aex.InnerException.InnerException.Message);
                                    }
                                    else
                                    {
                                        apilog.Failed(aex.InnerException.Message);
                                    }
                                }
                                else
                                {
                                    apilog.Failed(aex.Message);
                                }
                            }
                        }).Wait();
                    }
                    if (string.IsNullOrEmpty(str))
                    {
                        em.Current.Value.Failed();
                    }
                    else
                    {
                        em.Current.Value.Success();
                        apilog.Success(str);
                    }
                }
                catch (Exception ex)
                {
                    em.Current.Value.Failed();
                    apilog.Failed(ex.Message);
                }
                if (console && XCore.LogLevel == Log.LogLevel.Info)
                {
                    if (!string.IsNullOrEmpty(str))
                    {
                        apilog.message = str;
                    }
                    common.WriteLog(apilog);
                }
                logs.Add(apilog);
                if (!string.IsNullOrEmpty(str))
                {
                    return str;
                }
            }
            if (logs.Count > 0)
            {
                var message = logs.LastOrDefault().message;
                if (string.IsNullOrEmpty(message))
                {
                    message = "request failed";
                }
                return "{\"success\":false,\"message\":\"" + message + "[" + App + "]\",\"data\":\"\"}";
            }
            else
            {
                return "{\"success\":false,\"message\":\"service host not config[" + App + "]\",\"data\":\"\"}";
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        /// <param name="controller"></param>
        /// <param name="action"></param>
        /// <param name="postData"></param>
        /// <param name="kvs"></param>
        /// <returns></returns>
        public static string Post(string app, string controller, string action, string postData, params KeyValuePair<String, String>[] kvs)
        {
            List<ApiLog> logs;
            var common = GetInstances(app);
            return Post(common, controller, action, postData, out logs, kvs);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        /// <param name="controller"></param>
        /// <param name="action"></param>
        /// <param name="postData"></param>
        /// <param name="logs"></param>
        /// <param name="kvs"></param>
        /// <returns></returns>
        public static string Post(string app, string controller, string action, string postData, out List<ApiLog> logs, params KeyValuePair<String, String>[] kvs)
        {
            var common = GetInstances(app);
            return Post(common, controller, action, postData, out logs, kvs);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="app"></param>
        /// <param name="controller"></param>
        /// <param name="action"></param>
        /// <param name="postData"></param>
        /// <param name="kvs"></param>
        /// <returns></returns>
        public static ApiResult<T> Post<T>(string app, string controller, string action, string postData, params KeyValuePair<String, String>[] kvs)
        {
            List<ApiLog> logs;
            var common = GetInstances(app);
            var rlt = Json.ToObject<ApiResult<T>>(Post(common, controller, action, postData, out logs, kvs));
            rlt.PutLog(logs);
            return rlt;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="common"></param>
        /// <param name="controller"></param>
        /// <param name="action"></param>
        /// <param name="stream"></param>
        /// <param name="logs"></param>
        /// <param name="kvs"></param>
        /// <returns></returns>
        internal static string Post(CommonApp common, string controller, string action, System.IO.Stream stream, out List<ApiLog> logs, params KeyValuePair<String, String>[] kvs)
        {
            var data = "";
            var kvList = new List<KeyValuePair<String, String>>(kvs);
            #region 处理接口基本参数及签名            
            if (!string.IsNullOrEmpty(Secret))
            {
                // 排序并拼接签名原始字符
                kvList.Add(new KeyValuePair<String, String>("xstime", DateTools.GetUnix().ToString()));
                if (!string.IsNullOrEmpty(AppId))
                {
                    kvList.Add(new KeyValuePair<String, String>("xsappid", AppId));
                }
                kvList = kvList.OrderBy(o => o.Key).ToList();
                var values = new System.Text.StringBuilder();
                foreach (var kv in kvList)
                {
                    if (!string.IsNullOrEmpty(kv.Key))
                    {
                        values.Append(kv.Value);
                    }
                }
                values.Append(Secret);          //指定VerifyCode
                //生成sig
                var md5_result = System.Security.Cryptography.MD5.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes(values.ToString()));
                var sig_builder = new System.Text.StringBuilder();
                foreach (byte b in md5_result)
                {
                    sig_builder.Append(b.ToString("x2"));
                }
                kvList.Add(new KeyValuePair<String, String>("sig", sig_builder.ToString()));
            }
            #endregion
            foreach (var kv in kvList)
            {
                if (!string.IsNullOrEmpty(data))
                {
                    data += "&";
                }
                data += kv.Key + "=" + kv.Value;
            }
            logs = new List<ApiLog>();
            var em = common.Instances.GetEnumerator();
            while (em.MoveNext())
            {
                var str = "";
                var url = em.Current.Key + "/" + controller + "/" + action;
                if (!string.IsNullOrEmpty(data))
                {
                    url += "?" + data;
                }
                var apilog = new ApiLog(common.App, url);
                if (XCore.LogLevel == Log.LogLevel.Debug)
                {
                    Log.Loger.Console(url, ConsoleColor.DarkGreen);
                }
                try
                {
                    em.Current.Value.Start();
                    var handler = new System.Net.Http.HttpClientHandler();
                    if (System.Net.ServicePointManager.ServerCertificateValidationCallback != null)
                    {
                        handler.ServerCertificateCustomValidationCallback = XCore.ValidateServerCertificate;
                    }
                    using (var client = new System.Net.Http.HttpClient(handler))
                    {
                        var reqest = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, url);
                        reqest.Headers.Date = DateTime.UtcNow;
                        reqest.Headers.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("Wlniao-XCore-XServer", "beta"));
                        if (stream != null && stream.Length > 0)
                        {
                            reqest.Content = new System.Net.Http.StreamContent(stream);
                        }
                        client.SendAsync(reqest).ContinueWith((requestTask) =>
                        {
                            try
                            {
                                var response = requestTask.Result;
                                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                                {
                                    response.Content.ReadAsStringAsync().ContinueWith((readTask) =>
                                    {
                                        str = readTask.Result;
                                    }).Wait();
                                }
                                else
                                {
                                    response.Content.ReadAsStringAsync().ContinueWith((readTask) =>
                                    {
                                        str = readTask.Result;
                                    }).Wait();
                                }
                            }
                            catch (AggregateException aex)
                            {
                                if (aex.InnerException != null)
                                {
                                    if (aex.InnerException.InnerException != null)
                                    {
                                        apilog.Failed(aex.InnerException.InnerException.Message);
                                    }
                                    else
                                    {
                                        apilog.Failed(aex.InnerException.Message);
                                    }
                                }
                                else
                                {
                                    apilog.Failed(aex.Message);
                                }
                            }
                        }).Wait();
                        if (string.IsNullOrEmpty(str))
                        {
                            em.Current.Value.Failed();
                        }
                        else
                        {
                            em.Current.Value.Success();
                            apilog.Success(str);
                        }
                    }
                }
                catch (Exception ex)
                {
                    em.Current.Value.Failed();
                    logs.Add(apilog.Failed(ex.Message));
                }
                if (console && XCore.LogLevel == Log.LogLevel.Info)
                {
                    common.WriteLog(apilog);
                }
                logs.Add(apilog);
                if (!string.IsNullOrEmpty(str))
                {
                    return str;
                }
            }
            if (logs.Count > 0)
            {
                var message = logs.LastOrDefault().message;
                if (string.IsNullOrEmpty(message))
                {
                    message = "request failed";
                }
                return "{\"success\":false,\"message\":\"" + message + "[" + App + "]\",\"data\":\"\"}";
            }
            else
            {
                return "{\"success\":false,\"message\":\"service host not config[" + App + "]\",\"data\":\"\"}";
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        /// <param name="controller"></param>
        /// <param name="action"></param>
        /// <param name="stream"></param>
        /// <param name="kvs"></param>
        /// <returns></returns>
        public static string Post(string app, string controller, string action, System.IO.Stream stream, params KeyValuePair<String, String>[] kvs)
        {
            List<ApiLog> logs;
            var common = GetInstances(app);
            return Post(common, controller, action, stream, out logs, kvs);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        /// <param name="controller"></param>
        /// <param name="action"></param>
        /// <param name="stream"></param>
        /// <param name="logs"></param>
        /// <param name="kvs"></param>
        /// <returns></returns>
        public static string Post(string app, string controller, string action, System.IO.Stream stream, out List<ApiLog> logs, params KeyValuePair<String, String>[] kvs)
        {
            var common = GetInstances(app);
            return Post(common, controller, action, stream, out logs, kvs);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="app"></param>
        /// <param name="controller"></param>
        /// <param name="action"></param>
        /// <param name="stream"></param>
        /// <param name="kvs"></param>
        /// <returns></returns>
        public static ApiResult<T> Post<T>(string app, string controller, string action, System.IO.Stream stream, params KeyValuePair<String, String>[] kvs)
        {
            List<ApiLog> logs;
            var common = GetInstances(app);
            var rlt = Json.ToObject<ApiResult<T>>(Post(common, controller, action, stream, out logs, kvs));
            rlt.PutLog(logs);
            return rlt;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        /// <param name="https"></param>
        /// <returns></returns>
        public static string GetAppHost(String app, Boolean https = false)
        {
            var em = GetInstances(App).Instances.GetEnumerator();
            while (em.MoveNext())
            {
                if (em.Current.Key.IndexOf("://") > 0)
                {
                    return em.Current.Key;
                }
                else if (https)
                {
                    return "https://" + em.Current.Key;
                }
                else
                {
                    return "http://" + em.Current.Key;
                }
            }
            return "";
        }
        /// <summary>
        /// 设置AppHost
        /// </summary>
        /// <param name="app"></param>
        /// <param name="host"></param>
        public static void SetAppHost(String app, String host)
        {
            var _app = app.ToLower();
            if (_AppInstances.ContainsKey(_app))
            {
                _AppInstances[_app] = new CommonApp(app, host);
            }
            else
            {
                _AppInstances.Add(_app, new CommonApp(app, host));
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sig"></param>
        /// <param name="app"></param>
        /// <param name="path"></param>
        /// <param name="kvs"></param>
        /// <returns></returns>
        public static string CreateUrl(bool sig, string app, string path, params KeyValuePair<String, String>[] kvs)
        {
            return CreateUrl(sig, app, "", path, kvs);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sig"></param>
        /// <param name="app"></param>
        /// <param name="host"></param>
        /// <param name="path"></param>
        /// <param name="kvs"></param>
        /// <returns></returns>
        public static string CreateUrl(bool sig, string app, string host, string path, params KeyValuePair<String, String>[] kvs)
        {
            var data = "";
            var common = GetInstances(app);
            var kvList = new List<KeyValuePair<String, String>>(kvs);
            #region 处理接口基本参数及签名            
            if (sig && !string.IsNullOrEmpty(Secret))
            {
                // 排序并拼接签名原始字符
                kvList.Add(new KeyValuePair<String, String>("xstime", DateTools.GetUnix().ToString()));
                if (!string.IsNullOrEmpty(AppId))
                {
                    kvList.Add(new KeyValuePair<String, String>("xsappid", AppId));
                }
                kvList = kvList.OrderBy(o => o.Key).ToList();
                var values = new System.Text.StringBuilder();
                foreach (var kv in kvList)
                {
                    if (!string.IsNullOrEmpty(kv.Key))
                    {
                        values.Append(kv.Value);
                    }
                }
                values.Append(Secret);          //指定VerifyCode
                //生成sig
                var md5_result = System.Security.Cryptography.MD5.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes(values.ToString()));
                var sig_builder = new System.Text.StringBuilder();
                foreach (byte b in md5_result)
                {
                    sig_builder.Append(b.ToString("x2"));
                }
                kvList.Add(new KeyValuePair<String, String>("sig", sig_builder.ToString()));
            }
            #endregion
            foreach (var kv in kvList)
            {
                if (!string.IsNullOrEmpty(data))
                {
                    data += "&";
                }
                data += kv.Key + "=" + kv.Value;
            }
            if (string.IsNullOrEmpty(host) && common.Instances.Count > 0)
            {
                host = common.Instances.FirstOrDefault().Key;
            }
            if (string.IsNullOrEmpty(host))
            {
                return "";
            }
            else if (host.IndexOf("://") < 0)
            {
                host = "http://" + host;
            }
            if (path.StartsWith("/"))
            {
                return host + path + (string.IsNullOrEmpty(data) ? "" : "?" + data);
            }
            else
            {
                return host + "/" + path + (string.IsNullOrEmpty(data) ? "" : "?" + data);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static ApiResult<String> CheckUrl(string url)
        {
            var rlt = new ApiResult<String>();
            var dic = strUtil.GetQueryString(url);
            if (dic.ContainsKey("xsappid"))
            {
                if (dic.ContainsKey("sig"))
                {
                    var _rlt = XsApp.GetById(dic["xsappid"]);
                    if (_rlt.success)
                    {
                        dic = dic.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
                        var values = new System.Text.StringBuilder();
                        var e = dic.GetEnumerator();
                        while (e.MoveNext())
                        {
                            if (!string.IsNullOrEmpty(e.Current.Key) && e.Current.Key != "sig")
                            {
                                values.Append(e.Current.Value);
                            }
                        }
                        values.Append(_rlt.data.secret);
                        var md5_result = System.Security.Cryptography.MD5.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes(values.ToString()));
                        var sig_builder = new System.Text.StringBuilder();
                        foreach (byte b in md5_result)
                        {
                            sig_builder.Append(b.ToString("x2"));
                        }
                        if (dic["sig"] == sig_builder.ToString())
                        {
                            rlt.success = true;
                            rlt.message = "签名正确";
                        }
                        else
                        {
                            rlt.message = "sig签名错误";
                        }
                    }
                    else
                    {
                        rlt.message = _rlt.message;
                    }
                }
                else
                {
                    rlt.message = "参数sig未指定";
                }
            }
            else
            {
                rlt.message = "参数xsappid未指定";
            }
            return rlt;
        }
        #endregion
    }
}