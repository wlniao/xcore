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
        //[Route("website/{p1?}/{p2?}/{p3?}/{p4?}")]
        //[Route("console/{p1?}/{p2?}/{p3?}/{p4?}")]
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
            return AuthX();
        }
    }
}