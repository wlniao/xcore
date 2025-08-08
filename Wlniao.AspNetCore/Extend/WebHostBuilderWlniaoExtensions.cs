using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;

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
                Log.Loger.Error(ex.Message);
            }

            try
            {
                // 配置网站内容根目录
                builder = builder.UseContentRoot(Directory.GetCurrentDirectory());
            }
            catch (Exception ex)
            {
                Log.Loger.Error(ex.Message);
            }

            try
            {

                // 配置Kestrel监听地址及端口号
                builder = builder.UseKestrel(o =>
                {
                    if (System.IO.File.Exists(WebService.TlsCrt) && System.IO.File.Exists(WebService.TlsKey))
                    {
                        if (WebService.TlsPort != XCore.ListenPort)
                        {
                            o.ListenAnyIP(XCore.ListenPort);
                        }
                        o.ListenAnyIP(WebService.TlsPort, lo =>
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
                Log.Loger.Error(ex.Message);
            }
            return builder;
        }
    }
}