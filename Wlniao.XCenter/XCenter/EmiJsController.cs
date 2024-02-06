using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Wlniao;
using Wlniao.XServer;
using static System.Collections.Specialized.BitVector32;

namespace Wlniao.XCenter
{
    /// <summary>
    /// Emi应用基础Controller
    /// </summary>
    public partial class EmiJsController : XCoreController
    {
        /// <summary>
        /// EMI主程序接口访问工具
        /// </summary>
        private EmiContext ctx = null;
        /// <summary>
        /// 主平台登录会话状态
        /// </summary>
        private XSession xsession = null;
        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="context"></param>
        //public override void OnActionExecuted(ActionExecutedContext context)
        //{
        //    if (ctx != null && string.IsNullOrEmpty(ViewBag.eHost))
        //    {
        //        ViewBag.eHost = ctx?.EmiHost;
        //    }
        //    base.OnActionExecuted(context);
        //}

        /// <summary>
        /// 构造认证令牌
        /// </summary>
        /// <returns></returns>
        [NonAction]
        public IActionResult BuildTicket()
        {
            return CheckSession((xsession, ctx) =>
            {
                return Json(new { success = true, ctx.domain, ticket = xsession.BuildTicket() });
            });
        }
        /// <summary>
        /// 检查系统使用授权
        /// </summary>
        /// <param name="func"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        [NonAction]
        public IActionResult CheckAuth(Func<EmiContext, IActionResult> func, IActionResult result = null)
        {
            var msg = "";
            var ehost = GetCookies("ehost");
            if (Request.Query.Keys.Contains("ehost"))
            {
                ehost = GetRequestNoSecurity("ehost");
                Response.Cookies.Append("ehost", ehost, IsHttps ? new CookieOptions { Secure = true, SameSite = SameSiteMode.None } : new CookieOptions { });
            }
            else if (string.IsNullOrEmpty(ehost))
            {
                ehost = HeaderRequest("x-domain", EmiContext.XCenterDomain);
            }
            if (!string.IsNullOrEmpty(ehost))
            {
                ctx = EmiContext.Load(ehost);
                if (ctx.install)
                {
                    return func.Invoke(ctx);
                }
                else
                {
                    msg = ctx.message;
                }
            }
            else
            {
                msg = "访问无效，请求超时或已失效";
            }
            if (result == null)
            {
                if (Request.Method == "POST" || (Request.Query != null && Request.Query.ContainsKey("do")))
                {
                    Response.Headers.TryAdd("Access-Control-Expose-Headers", new Microsoft.Extensions.Primitives.StringValues("*"));
                    Response.Headers.TryAdd("Authify-State", new Microsoft.Extensions.Primitives.StringValues("false"));
                    result = Json(new { success = false, message = msg });
                }
                else
                {
                    result = ErrorMsg(msg);
                }
            }
            return result;
        }
        /// <summary>
        /// 检查登录认证状态
        /// </summary>
        /// <param name="func"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        [NonAction]
        public IActionResult CheckSession(Func<XSession, EmiContext, IActionResult> func, IActionResult result = null)
        {
            return CheckAuth((ctx) =>
            {
                var msg = "";
                var authorization = HeaderRequest("Authorization");
                if (Request.Query.Keys.Contains("xsession"))
                {
                    authorization = GetRequestNoSecurity("xsession");
                    Response.Cookies.Append("xs_" + ctx.app, authorization, IsHttps ? new CookieOptions { Secure = true, SameSite = SameSiteMode.None } : new CookieOptions { });
                }
                else if (string.IsNullOrEmpty(authorization))
                {
                    authorization = GetCookies("xs_" + ctx.app);
                }
                xsession = new XSession(ctx, authorization);
                if (xsession.IsValid && xsession.OwnerId == ctx.owner)
                {
                    return func.Invoke(xsession, ctx);
                }
                else
                {
                    if (result == null)
                    {
                        msg = "暂未登录或已失效，请登录";
                        if (Request.Method == "POST" || (Request.Query != null && Request.Query.ContainsKey("do")))
                        {
                            Response.Headers.TryAdd("Access-Control-Expose-Headers", new Microsoft.Extensions.Primitives.StringValues("*"));
                            Response.Headers.TryAdd("Authify-State", new Microsoft.Extensions.Primitives.StringValues("false"));
                            result = Json(new { success = false, message = msg });
                        }
                        else
                        {
                            result = ErrorMsg(msg);
                        }
                    }
                    return result;
                }
            }, result);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Code"></param>
        /// <param name="func"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        [NonAction]
        public IActionResult? CheckPermission(String Code, Func<IActionResult> func, IActionResult result = null)
        {
            if (ctx == null || xsession == null)
            {
                return Json(new { success = false, message = "请先调用“CheckSession”后再进行权限验证" });
            }
            else if (string.IsNullOrEmpty(Code))
            {
                if (Request.Method == "POST" || !string.IsNullOrEmpty(method))
                {
                    errorMsg = "";
                    return Json(new { success = false, message = "要验证的权限无效，请检查权限编码是否正确" });
                }
                else
                {
                    errorMsg = "要验证的权限无效，请检查权限编码是否正确";
                    errorTitle = "操作未授权";
                    var errorPage = new ContentResult();
                    errorPage.ContentType = "text/html;charset=utf-8";
                    errorPage.Content = errorHtml.Replace("{{errorMsg}}", errorMsg).Replace("{{errorTitle}}", errorTitle).Replace("{{errorIcon}}", errorIcon);
                    return errorPage;
                }
            }
            else
            {
                var rlt = ctx.EmiGet<Boolean>("app", "permission"
                    , new KeyValuePair<string, string>("sid", xsession.UserSid)
                    , new KeyValuePair<string, string>("code", Code));
                if (rlt.data)
                {
                    return func?.Invoke();
                }
                else if (Request.Method == "POST" || string.IsNullOrEmpty(method))
                {
                    errorMsg = rlt.message;
                    errorTitle = "操作未授权";
                    var errorPage = new ContentResult();
                    errorPage.ContentType = "text/html;charset=utf-8";
                    errorPage.Content = errorHtml.Replace("{{errorMsg}}", errorMsg).Replace("{{errorTitle}}", errorTitle).Replace("{{errorIcon}}", errorIcon);
                    return errorPage;
                }
                else
                {
                    errorMsg = "";
                    return Json(new { success = false, message = rlt.message });
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Code"></param>
        /// <param name="Organ"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        [NonAction]
        public IActionResult? CheckPermission(String Code, String Organ, Func<IActionResult> func)
        {
            if (ctx == null || xsession == null)
            {
                return Json(new { success = false, message = "请先调用“CheckSession”后再进行权限验证" });
            }
            else if (string.IsNullOrEmpty(Code))
            {
                if (Request.Method == "POST" || !string.IsNullOrEmpty(method))
                {
                    errorMsg = "";
                    return Json(new { success = false, message = "要验证的权限无效，请检查权限编码是否正确" });
                }
                else
                {
                    errorMsg = "要验证的权限无效，请检查权限编码是否正确";
                    errorTitle = "操作未授权";
                    var errorPage = new ContentResult();
                    errorPage.ContentType = "text/html;charset=utf-8";
                    errorPage.Content = errorHtml.Replace("{{errorMsg}}", errorMsg).Replace("{{errorTitle}}", errorTitle).Replace("{{errorIcon}}", errorIcon);
                    return errorPage;
                }
            }
            else
            {
                var rlt = ctx.EmiGet<Boolean>("app", "permissionorgan"
                    , new KeyValuePair<string, string>("sid", xsession.UserSid)
                    , new KeyValuePair<string, string>("code", Code)
                    , new KeyValuePair<string, string>("organ", Organ));
                if (rlt.data)
                {
                    return func?.Invoke();
                }
                else if (Request.Method == "POST" || string.IsNullOrEmpty(method))
                {
                    errorMsg = rlt.message;
                    errorTitle = "操作未授权";
                    var errorPage = new ContentResult();
                    errorPage.ContentType = "text/html;charset=utf-8";
                    errorPage.Content = errorHtml.Replace("{{errorMsg}}", errorMsg).Replace("{{errorTitle}}", errorTitle).Replace("{{errorIcon}}", errorIcon);
                    return errorPage;
                }
                else
                {
                    errorMsg = "";
                    return Json(new { success = false, message = rlt.message });
                }
            }
        }

        /// <summary>
        /// 返回无权限提示
        /// </summary>
        /// <param name="ajax"></param>
        /// <returns></returns>
        [NonAction]
        public IActionResult NoPermission(Boolean ajax = false)
        {
            if (ajax || Request.Method == "POST" || !string.IsNullOrEmpty(method))
            {
                errorMsg = "";
                return Json(new { success = false, message = "您暂无执行当前操作的权限" });
            }
            else
            {
                errorMsg = "您暂无执行当前操作的权限";
                errorTitle = "操作未授权";
                var errorPage = new ContentResult();
                errorPage.ContentType = "text/html;charset=utf-8";
                errorPage.Content = errorHtml.Replace("{{errorMsg}}", errorMsg).Replace("{{errorTitle}}", errorTitle).Replace("{{errorIcon}}", errorIcon);
                return errorPage;
            }
        }



        /// <summary>
        /// 获取常用日期段
        /// </summary>
        /// <returns></returns>
        public IActionResult dates()
        {
            var now = DateTools.GetNow();
            var quarter = now.Month / 3 * 3;
            if (now.Month % 3 == 0)
            {
                quarter -= 3;
            }
            var Today = now.ToString("yyyy-MM-dd");
            var Yeserday = now.AddDays(-1).ToString("yyyy-MM-dd");
            var MonthStart = now.ToString("yyyy-MM-01");
            var MonthEnd = DateTools.Convert(now.ToString("yyyy-MM-01 12:00:00")).AddMonths(1).AddDays(-1).ToString("yyyy-MM-dd");
            var QuarterStart = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Unspecified).AddMonths(quarter).ToString("yyyy-MM-01");
            var QuarterEnd = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Unspecified).AddMonths(quarter + 3).AddDays(-1).ToString("yyyy-MM-dd");
            var YearStart = now.ToString("yyyy-01-01");
            var YearEnd = now.ToString("yyyy-12-31");
            var PrevYearStart = now.AddYears(-1).ToString("yyyy-01-01");
            var PrevYearEnd = now.AddYears(-1).ToString("yyyy-12-31");
            var PrevMonthStart = now.AddMonths(-1).ToString("yyyy-MM-01");
            var PrevMonthEnd = DateTools.Convert(now.ToString("yyyy-MM-01 12:00:00")).AddDays(-1).ToString("yyyy-MM-dd");
            var PrevQuarterStart = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Unspecified).AddMonths(quarter - 3).ToString("yyyy-MM-01");
            var PrevQuarterEnd = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Unspecified).AddMonths(quarter).AddDays(-1).ToString("yyyy-MM-dd");
            return Json(new
            {
                Today,
                Yeserday,
                MonthStart,
                MonthEnd,
                QuarterStart,
                QuarterEnd,
                YearStart,
                YearEnd,
                PrevYearStart,
                PrevYearEnd,
                PrevMonthStart,
                PrevMonthEnd,
                PrevQuarterStart,
                PrevQuarterEnd,
                success = true
            });
        }
        /// <summary>
        /// 获取枚举数据
        /// </summary>
        /// <returns></returns>
        public IActionResult enums()
        {
            return CheckAuth(ctx =>
            {
                var key = GetRequest("key");
                var list = new List<Object>();
                var rlt = ctx.EmiGet<List<Dictionary<string, object>>>("app", "getenumlist", new KeyValuePair<string, string>("parent", key));
                if (rlt.success && rlt.data != null)
                {
                    foreach (var row in rlt.data)
                    {
                        list.Add(new { value = row.GetString("key"), label = row.GetString("label"), description = row.GetString("description") });
                    }
                }
                return Json(list);
            });
        }
        /// <summary>
        /// 获取UI标签
        /// </summary>
        /// <returns></returns>
        public IActionResult label()
        {
            return CheckAuth(ctx =>
            {
                var ht = new System.Collections.Hashtable();
                var keys = PostRequest("keys").SplitBy(",", "|");
                foreach (var key in keys)
                {
                    ht.Add(key, ctx.GetLabel(key));
                }
                ht.Add("success", true);
                return Json(ht);
            });
        }
        /// <summary>
        /// 权限检查
        /// </summary>
        /// <returns></returns>
        public IActionResult permission()
        {
            return CheckSession((xsession, ctx) =>
            {
                var ht = new System.Collections.Hashtable();
                var codes = PostRequest("code").SplitBy(",", "|");
                var organ = PostRequest("organ");
                var allow = Config.GetConfigs("AllowPermission").ToUpper();
                if (codes.Length == 0)
                {
                    ht.Add("success", false);
                }
                else if (string.IsNullOrEmpty(organ))
                {
                    for (var i = 0; i < codes.Length; i++)
                    {
                        var auth = allow == "ALL" || allow == codes[i].ToUpper() ? true : ctx.Permission(xsession.UserSid, codes[i]);
                        ht.Add(codes[i], auth);
                        if (i == 0)
                        {
                            ht.Add("success", auth);
                        }
                    }
                }
                else
                {
                    for (var i = 0; i < codes.Length; i++)
                    {
                        var auth = allow == "ALL" || allow == codes[i].ToUpper() ? true : ctx.PermissionOrgan(xsession.UserSid, codes[i], organ);
                        ht.Add(codes[i], auth);
                        if (i == 0)
                        {
                            ht.Add("success", auth);
                        }
                    }
                }
                return Json(ht);
            });
        }

    }
}