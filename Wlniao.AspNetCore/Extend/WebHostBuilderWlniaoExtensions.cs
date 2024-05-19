using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System.IO;

namespace Wlniao
{
    /// <summary>
    /// 
    /// </summary>
    public static class WebHostBuilderWlniaoExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IWebHostBuilder UseWlniao(this IWebHostBuilder builder)
        {
            // 配置日志输出级别，隐藏默认日志
            builder = builder.ConfigureLogging(c => { c.AddFilter(level => level >= LogLevel.Error); });

            // 配置网站内容根目录
            builder = builder.UseContentRoot(Directory.GetCurrentDirectory());

            // 配置Kestrel监听地址及端口号
            builder = builder.UseKestrel(o => { o.ListenAnyIP(XCore.ListenPort); });

            // 输出监听日志
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
            return builder;
        }
    }
}