using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using System.Security.Cryptography.X509Certificates;

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
        /// <param name="options"></param>
        /// <returns></returns>
        public static IWebHostBuilder UseWlniao(this IWebHostBuilder builder, Action<KestrelServerOptions> options = null)
        {
            try
            {
                // 配置日志输出级别，隐藏默认日志
                builder = builder.ConfigureLogging(c => { c.AddFilter(level => level >= LogLevel.Error); });
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
            }

            try
            {
                // 配置网站内容根目录
                builder = builder.UseContentRoot(Directory.GetCurrentDirectory());
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
            }

            try
            {

                // 配置Kestrel监听地址及端口号
                builder = builder.UseKestrel(o =>
                {
                    if (System.IO.File.Exists(WebService.TlsCrt) && System.IO.File.Exists(WebService.TlsKey))
                    {
                        o.ListenAnyIP(XCore.ListenPort, lo =>
                        {
                            WebService.UseHttps = true;
                            lo.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
                            lo.UseHttps(Wlniao.Crypto.Cert.CrtToPfx(WebService.TlsCrt, WebService.TlsKey));
                        });
                    }
                    else
                    {
                        o.ListenAnyIP(XCore.ListenPort);
                    }
                    if (options != null)
                    {
                        options(o);
                    }
                    //输出监听终结点信息
                    WebService.ListenLogs();
                });
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
            }
            return builder;
        }
    }
}