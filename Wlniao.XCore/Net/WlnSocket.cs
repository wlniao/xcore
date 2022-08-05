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
        private static Object _lock = new object();
        private static List<WlnSocket> sockets = new List<WlnSocket>();
        /// <summary>
        /// 从连接池获取一个实例
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="TimeOutSeconds"></param>
        /// <returns></returns>
        public static WlnSocket GetSocket(String host, Int32 port, Int32 TimeOutSeconds = 10)
        {
            var ipaddress = strUtil.IsIP(host) ? System.Net.IPAddress.Parse(host) : new Net.Dns.DnsTool().GetIPAddressDefault(host);
            if (ipaddress.IsIPv4MappedToIPv6)
            {
                ipaddress = ipaddress.MapToIPv4();
            }
            var endpoint = new System.Net.IPEndPoint(ipaddress, port);
            lock (_lock)
            {
                try
                {
                beginCheck:
                    foreach (var socket in sockets.OrderBy(a => a.LastUse))
                    {
                        if (socket.Catch || !socket.Connected)
                        {
                            sockets.Remove(socket);
                            try
                            {
                                if (socket.Connected)
                                {
                                    socket.Shutdown(System.Net.Sockets.SocketShutdown.Both);
                                }
                                socket.Close();
                            }
                            catch { }
                            goto beginCheck;
                        }
                        if (!socket.Using && socket.RemoteEndPoint.ToString() == endpoint.ToString() && socket.Connected && socket.LastUse < XCore.NowUnix - 15)
                        {
                            socket.Using = true;
                            socket.LastUse = XCore.NowUnix;
                            return socket;
                        }
                    }
                    var newsocket = new WlnSocket(endpoint.AddressFamily, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
                    newsocket.Using = true;
                    newsocket.LastUse = XCore.NowUnix;
                    newsocket.Connect(endpoint);
                    newsocket.SendTimeout = TimeOutSeconds * 1000;  //10s
                    newsocket.ReceiveTimeout = TimeOutSeconds * 1000;  //10s
                    sockets.Add(newsocket);
                    return newsocket;
                }
                catch { return null; }
            }
        }
        /// <summary>
        /// 通过Socket发送Http请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="kvs"></param>
        /// <returns></returns>
        public static String HttpGet(String url, params KeyValuePair<String, String>[] kvs)
        {
            var sb = new System.Text.StringBuilder();
            var uri = new Uri(url);
            var hostSocket = Net.WlnSocket.GetSocket(uri.Host, uri.Port, 15);
            try
            {
                var reqStr = "";
                reqStr += "GET " + uri.PathAndQuery + " HTTP/1.1";
                reqStr += "\r\nHost: " + uri.Host;
                reqStr += "\r\nDate: " + DateTools.ConvertToGMT(XCore.NowUnix);
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
            catch { }
            return sb.ToString();
        }
    }
}