/*==============================================================================
    文件名称：WlnSocket.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：Socket类封装
================================================================================
 
    Copyright 2017 XieChaoyi

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
using System.Linq;
using System.Collections.Generic;
using Wlniao.Text;
using Encoding = System.Text.Encoding;

namespace Wlniao.Net
{
    /// <summary>
    /// 
    /// </summary>
    public class WlnSocket : System.Net.Sockets.Socket
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="addressFamily"></param>
        /// <param name="socketType"></param>
        /// <param name="protocolType"></param>
        public WlnSocket(System.Net.Sockets.AddressFamily addressFamily, System.Net.Sockets.SocketType socketType, System.Net.Sockets.ProtocolType protocolType)
            : base(addressFamily, socketType, protocolType)
        {

        }
        /// <summary>
        /// 此链接发生了异常
        /// </summary>
        public bool Catch { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool Using { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public long LastUse { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int TimeOutSeconds = 10;
        private static readonly object _lock = new object();
        private static readonly List<WlnSocket> _sockets = new List<WlnSocket>();
        
        // 定期清理过期连接的时间间隔（秒）
        private static readonly int CLEANUP_INTERVAL_SECONDS = 30;
        private static DateTime _lastCleanup = DateTime.UtcNow;
        
        // 最大重试次数
        private static readonly int MAX_RETRY_COUNT = 10;
        
        /// <summary>
        /// 从连接池获取一个实例
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="timeOutSeconds"></param>
        /// <returns></returns>
        public static WlnSocket GetSocket(string host, int port, int timeOutSeconds = 10)
        {
            var ipaddress = StringUtil.IsIP(host) ? System.Net.IPAddress.Parse(host) : new Net.Dns.DnsTool().GetIPAddressDefault(host);
            if (ipaddress.IsIPv4MappedToIPv6)
            {
                ipaddress = ipaddress.MapToIPv4();
            }
            var endpoint = new System.Net.IPEndPoint(ipaddress, port);
            
            // 先在锁内查找可用连接
            WlnSocket availableSocket = null;
            
            lock (_lock)
            {
                // 定期清理过期的连接
                if ((DateTime.UtcNow - _lastCleanup).TotalSeconds > CLEANUP_INTERVAL_SECONDS)
                {
                    CleanupUnusedSockets();
                    _lastCleanup = DateTime.UtcNow;
                }
                
                // 查找可用连接，使用重试机制避免无限循环
                var retryCount = 0;
                while (retryCount < MAX_RETRY_COUNT)
                {
                    var foundInvalid = false;
                    
                    // 使用ToList()避免集合修改导致的枚举异常
                    foreach (var socket in _sockets.Where(s => !s.Using).OrderBy(a => a.LastUse).ToList())
                    {
                        if (socket.Catch || !socket.Connected)
                        {
                            // 清理无效连接
                            _sockets.Remove(socket);
                            try
                            {
                                if (socket.Connected)
                                {
                                    socket.Shutdown(System.Net.Sockets.SocketShutdown.Both);
                                }
                                socket.Close();
                                socket.Dispose();
                            }
                            catch (Exception ex)
                            {
                                Wlniao.Log.Loger.Error($"Socket清理异常: {ex.Message}, 堆栈: {ex.StackTrace}");
                            }
                            foundInvalid = true;
                            break;
                        }
                        
                        if (!socket.Using && socket.RemoteEndPoint!.ToString() == endpoint.ToString() && socket.Connected && socket.LastUse < DateTools.GetUnix() - 15)
                        {
                            socket.Using = true;
                            socket.LastUse = DateTools.GetUnix();
                            availableSocket = socket;
                            break;
                        }
                    }
                    
                    if (availableSocket != null)
                    {
                        break;
                    }
                    
                    if (!foundInvalid)
                    {
                        break;
                    }
                    
                    retryCount++;
                }
            }
            
            if (availableSocket != null)
            {
                return availableSocket;
            }
            
            // 在锁外创建新连接（避免在锁内执行网络IO）
            var newsocket = new WlnSocket(endpoint.AddressFamily, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
            newsocket.Using = true;
            newsocket.LastUse = DateTools.GetUnix();
            
            try
            {
                newsocket.Connect(endpoint);
                newsocket.SendTimeout = timeOutSeconds * 1000;
                newsocket.ReceiveTimeout = timeOutSeconds * 1000;
                
                // 将新连接加入池
                lock (_lock)
                {
                    _sockets.Add(newsocket);
                }
                
                return newsocket;
            }
            catch (Exception ex)
            {
                // 确保异常情况下释放资源
                try
                {
                    if (newsocket.Connected)
                    {
                        newsocket.Shutdown(System.Net.Sockets.SocketShutdown.Both);
                    }
                    newsocket.Close();
                    newsocket.Dispose();
                }
                catch
                {
                    // 忽略清理异常
                }
                
                Wlniao.Log.Loger.Error($"Socket连接异常: {ex.Message}, 堆栈: {ex.StackTrace}");
                return null;
            }
        }
        
        /// <summary>
        /// 通过Socket发送Http请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="kvs"></param>
        /// <returns></returns>
        public static string HttpGet(string url, params KeyValuePair<string, string>[] kvs)
        {
            var sb = new System.Text.StringBuilder();
            var uri = new Uri(url);
            var hostSocket = Net.WlnSocket.GetSocket(uri.Host, uri.Port, 15);
            
            if (hostSocket == null)
            {
                Wlniao.Log.Loger.Error($"Socket连接失败: {uri.Host}:{uri.Port}");
                return "";
            }
            
            try
            {
                var reqStr = "";
                reqStr += "GET " + uri.PathAndQuery + " HTTP/1.1";
                reqStr += "\r\nHost: " + uri.Host;
                reqStr += "\r\nDate: " + DateTools.ConvertToGMT(DateTools.GetUnix());
                reqStr += "\r\nAccept: application/json";
                if (kvs != null)
                {
                    foreach (var kv in kvs)
                    {
                        reqStr += "\r\n" + kv.Key + ": " + kv.Value;
                    }
                }
                reqStr += "\r\n";
                reqStr += "\r\n";
                var request = Encoding.UTF8.GetBytes(reqStr);
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
                        var tempstr = Encoding.UTF8.GetString(rev, 0, index);
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
                                        length = Convert.ToInt(ts[1]);
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
                                    var tempLength = Convert.DeHex(line, "0123456789abcdef");
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
                            if (index == 0 || (chunked && index == 1) || sb.Length == 0)
                            {
                                sb.Append(line);
                            }
                            else
                            {
                                sb.Append("\r\n" + line);
                            }
                            if (!chunked && System.Text.Encoding.UTF8.GetBytes(sb.ToString()).Length >= length)
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
                hostSocket.Using = false;
            }
            catch (Exception ex)
            {
                // 确保异常情况下标记连接为不可用
                if (hostSocket != null)
                {
                    hostSocket.Catch = true;
                    hostSocket.Using = false;
                }
                Wlniao.Log.Loger.Error($"Socket HTTP请求异常: {ex.Message}, 堆栈: {ex.StackTrace}");
            }
            return sb.ToString();
        }
        
        /// <summary>
        /// 清理未使用的Socket连接
        /// </summary>
        private static void CleanupUnusedSockets()
        {
            var now = DateTools.GetUnix();
            var socketsToRemove = new List<WlnSocket>();
            
            foreach (var socket in _sockets)
            {
                // 清理超过5分钟未使用的连接
                if (!socket.Using && (now - socket.LastUse) > 300) // 300秒 = 5分钟
                {
                    socketsToRemove.Add(socket);
                }
                // 清理使用完毕但长时间未释放的连接
                else if (socket.Using && (now - socket.LastUse) > 600) // 600秒 = 10分钟
                {
                    socketsToRemove.Add(socket);
                }
            }
            
            foreach (var socket in socketsToRemove)
            {
                try
                {
                    _sockets.Remove(socket);
                    if (socket.Connected)
                    {
                        socket.Shutdown(System.Net.Sockets.SocketShutdown.Both);
                    }
                    socket.Close();
                    socket.Dispose();
                }
                catch (Exception ex)
                {
                    Wlniao.Log.Loger.Error($"Socket清理异常: {ex.Message}, 堆栈: {ex.StackTrace}");
                }
            }
        }
        
        /// <summary>
        /// 定期清理整个连接池
        /// </summary>
        public static void CleanupPool()
        {
            lock (_lock)
            {
                CleanupUnusedSockets();
                _lastCleanup = DateTime.UtcNow;
            }
        }
    }
}