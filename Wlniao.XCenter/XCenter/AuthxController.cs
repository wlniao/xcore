using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Wlniao;

namespace Wlniao.XCenter
{
    /// <summary>
    /// 控制台：身份授权
    /// </summary>
    [ApiExplorerSettings(IgnoreApi = true)]
    public partial class AuthxController : Wlniao.XCenter.EmiController
    {
        /// <summary>
        /// 通用静态页发布
        /// </summary>
        /// <returns></returns>
        //[Route("{p1?}/{p2?}/{p3?}/{p4?}")]
        public IActionResult appx(string? p1, string? p2, string? p3, string? p4)
        {
            return CheckSession((xsession, ctx) =>
            {
                return ErrorMsg("App Pages");
            });
        }
        /// <summary>
        /// 授权命令
        /// </summary>
        /// <returns></returns>
        //[Route("{controller}/{action}")]
        public IActionResult token()
        {
            return BuildTicket();
        }
        /// <summary>
        /// 授权页面
        /// </summary>
        /// <returns></returns>
        //[Route("{controller}/{p1?}/{p2?}/{p3?}/{p4?}")]
        public IActionResult authx(string? p1, string? p2, string? p3, string? p4)
        {
            var xsession = GetRequest("xsession");
            if (string.IsNullOrEmpty(xsession))
            {
                return CheckAuth((ctx) =>
                {
                    Console.WriteLine(Request.Host);
                    var host = UrlReferer;
                    if (string.IsNullOrEmpty(host))
                    {
                        host = UrlHost;
                    }
                    return Content("<html><head><link rel=\"icon\" href=\"data:image/ico;base64,aWNv\"><script>location.href='" + ctx.EmiHost + "/app/" + ctx.app + "?back=' + location.origin + '" + Request.Path.Value + "'</script></head></html>", "text/html");
                });
            }
            else
            {
                return CheckSession((xsession, ctx) =>
                {
                    var path = "/" + (Request == null || Request.Path == null || string.IsNullOrEmpty(Request.Path.Value) ? "" : string.Join('/', Request.Path.Value.Split('/', StringSplitOptions.RemoveEmptyEntries).Skip(1)));
                    var html = "<!DOCTYPE html><html lang=zh-CN><head><meta charset=utf-8><script>var to = location.pathname.substr(1); var ticket = 'devtestticket'; if (ticket) { localStorage.setItem('ticket', ticket) } history.replaceState(null, null, to); setTimeout(() => { location.reload() }, 100)</script></html>";
                    html = html.Replace("'devtestticket'", "'" + xsession.BuildTicket() + "'");
                    if (!string.IsNullOrEmpty(path) && path.IndexOf('\'') < 0)
                    {
                        html = html.Replace("location.pathname.substr(1)", "'" + path + "'");
                    }
                    return Content(html, "text/html", System.Text.Encoding.UTF8);
                });
            }
        }
    }
}