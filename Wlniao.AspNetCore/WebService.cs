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
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.Linq;
namespace Wlniao
{
    /// <summary>
    /// XCore内部运行信息及状态
    /// </summary>
    public partial class WebService
    {
        /// <summary>
        /// 输出监听日志
        /// </summary>
        public static void ListenLogs()
        {
            try
            {
                var endpoints = new List<string> { "http://localhost:" + XCore.ListenPort };
                if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                {
                    foreach (var address in Dns.GetHostEntry(Dns.GetHostName()).AddressList.OrderBy(o => o.ToString()).OrderBy(o => o.AddressFamily == AddressFamily.InterNetworkV6))
                    {
                        var ip = address.ToString();
                        if (address.AddressFamily == AddressFamily.InterNetwork && (ip.StartsWith("10.") || ip.StartsWith("172.") || ip.StartsWith("192.")))
                        {
                            endpoints.Add("http://" + ip + ":" + XCore.ListenPort);
                        }
                        else if (address.AddressFamily == AddressFamily.InterNetworkV6 && !address.IsIPv6LinkLocal && !ip.StartsWith("fe80") && !ip.Contains("%"))
                        {
                            endpoints.Add("http://[" + ip + "]:" + XCore.ListenPort);
                        }
                    }
                }
                foreach (var endpoint in endpoints)
                {
                    Wlniao.Log.Loger.Console("Now listening on: " + endpoint, ConsoleColor.DarkGreen);
                }
            }
            catch
            {
                Wlniao.Log.Loger.Console("Now listening on: http://0.0.0.0:" + XCore.ListenPort, ConsoleColor.DarkGreen);
            }
        }

        /// <summary>
        /// 服务停用及停用消息
        /// </summary>
        /// <param name="node"></param>
        /// <param name="code"></param>
        /// <param name="message"></param>
        public static void ServiceStop(String node, String code = "302", String message = "服务器正在维护中 Server maintenance.")
        {
            var json = Json.ToString(new { node, code, success = false, message });
            new WebHostBuilder().UseKestrel(o => { o.Listen(System.Net.IPAddress.IPv6Any, XCore.ListenPort); }).Configure(app =>
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