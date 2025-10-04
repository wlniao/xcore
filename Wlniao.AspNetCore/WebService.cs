/*==============================================================================
    文件名称：XCore.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：XCore内部运行信息及状态
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
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
namespace Wlniao
{
    /// <summary>
    /// XCore内部运行信息及状态
    /// </summary>
    public abstract class WebService
    {
        private static int _tlsPort = 0;
        private static string _tlsCrt = null!;
        private static string _tlsKey = null!;

        /// <summary>
        /// 是否启用HTTPS服务
        /// </summary>
        internal static bool UseHttps = false;

        /// <summary>
        /// TLS服务端口号
        /// </summary>
        public static int TlsPort
        {
            get
            {
                if (_tlsPort != 0)
                {
                    return _tlsPort;
                }
                try
                {
                    // 从配置文件中获取TLS监听端口号，未配置时为默认端口号
                    _tlsPort = System.Convert.ToInt32(Wlniao.Config.GetConfigs("WLN_TLS_PORT", XCore.ListenPort.ToString()));
                }
                catch(Exception ex)
                {
                    Wlniao.Log.Loger.Error($"Error Config: WLN_TLS_PORT => {ex.Message}");
                }
                return _tlsPort;
            }
        }

        /// <summary>
        /// TLS服务证书
        /// </summary>
        internal static string TlsCrt
        {
            get
            {
                if (string.IsNullOrEmpty(_tlsCrt))
                {
                    _tlsCrt = Config.GetConfigs("WLN_TLS_CRT", IO.PathTool.Map("xcore", "server.crt"));
                }
                return _tlsCrt;
            }
        }

        /// <summary>
        /// TLS服务私钥
        /// </summary>
        internal static string TlsKey
        {
            get
            {
                if (string.IsNullOrEmpty(_tlsKey))
                {
                    _tlsKey = Config.GetConfigs("WLN_TLS_KEY", IO.PathTool.Map("xcore", "server.key"));
                }
                return _tlsKey;
            }
        }


        /// <summary>
        /// 输出监听日志
        /// </summary>
        public static void ListenLogs()
        {
            var scheme = UseHttps ? "https://" : "http://";
            var port = UseHttps && TlsPort > 0 ? TlsPort : XCore.ListenPort;
            var endpoints = new List<string> { scheme + "localhost:" + port };
            if (UseHttps && TlsPort != XCore.ListenPort)
            {
                endpoints.Insert(0, "http://localhost:" + XCore.ListenPort);
            }
            try
            {
                if (Environment.GetEnvironmentVariable("MicroservicesNode") != "true" && System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                {
                    foreach (var address in Dns.GetHostEntry(Dns.GetHostName()).AddressList.OrderBy(o => o.ToString()).ThenBy(o => o.AddressFamily == AddressFamily.InterNetworkV6))
                    {
                        var ip = address.ToString();
                        if (address.AddressFamily == AddressFamily.InterNetwork && (ip.StartsWith("10.") || ip.StartsWith("172.") || ip.StartsWith("192.")))
                        {
                            endpoints.Add(scheme + ip + ":" + port);
                        }
                        else if (address is { AddressFamily: AddressFamily.InterNetworkV6, IsIPv6LinkLocal: false } && !ip.StartsWith("fe80") && !ip.Contains("%"))
                        {
                            endpoints.Add(scheme + "[" + ip + "]:" + port);
                        }
                    }
                }
                foreach (var endpoint in endpoints)
                {
                    Log.Loger.Console("Now listening on: " + endpoint, ConsoleColor.DarkGreen);
                }
            }
            catch
            {
                Log.Loger.Console("Now listening on: " + scheme + "0.0.0.0:" + port, ConsoleColor.DarkGreen);
            }
        }

        /// <summary>
        /// 服务停用及停用消息
        /// </summary>
        /// <param name="node"></param>
        /// <param name="code"></param>
        /// <param name="message"></param>
        public static void ServiceStop(string node, string code = "302", string message = "服务器正在维护中 Server maintenance.")
        {
            var json = Json.Serialize(new { node, code, success = false, message });
            new WebHostBuilder().UseKestrel(o => { o.Listen(IPAddress.IPv6Any, XCore.ListenPort); }).Configure(app =>
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Node " + node + " on maintenance mode: " + code);
                Console.ForegroundColor = ConsoleColor.White;
                app.Run((HttpContext context) =>
                {
                    context.Response.Headers.TryAdd("Content-Type", "application/json");
                    return context.Response.WriteAsync(json);
                });
            }).Build().Run();
        }
	}
}