using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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
    public partial class EmiController : XCoreController
    {
        /// <summary>
        /// 主平台登录会话状态
        /// </summary>
        protected XSession xsession = null;
        /// <summary>
        /// EMI主程序接口访问工具
        /// </summary>
        protected EmiContext ctx = null;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            if (ctx != null && string.IsNullOrEmpty(ViewBag.eHost))
            {
                ViewBag.eHost = ctx.EmiHost;
            }
            base.OnActionExecuted(context);
        }

        //    #region 解析请求Session
        //    var ehost = GetCookies("ehost");
        //    var session = GetCookies("wsession");
        //    if (Request.Query.Keys.Contains("ehost"))
        //    {
        //        ehost = GetRequestNoSecurity("ehost");
        //        Response.Cookies.Append("ehost", ehost, IsHttps ? new CookieOptions { Secure = true, SameSite = SameSiteMode.None } : new CookieOptions { });
        //    }
        //    else if (string.IsNullOrEmpty(ehost))
        //    {
        //        ehost = UrlDomain;
        //    }
        //    if (Request.Query.Keys.Contains("wsession"))
        //    {
        //        session = GetRequestNoSecurity("wsession");
        //        Response.Cookies.Append("wsession", session, IsHttps ? new CookieOptions { Secure = true, SameSite = SameSiteMode.None } : new CookieOptions { });
        //    }
        //    ctx = EmiContext.Load(ehost);
        //    if (ctx.install || string.IsNullOrEmpty(ctx.app))
        //    {
        //        xsession = new XSession(ctx, session);
        //    }
        //    #endregion
        //    if (xsession != null && (xsession.login || filterContext.ActionDescriptor.FilterDescriptors.Where(a => a.Filter.ToString().Contains("Wlniao.Mvc.NoLoginAttribute")).ToList().Count() > 0))
        //    {
        //        ViewBag.eHost = ctx.EmiHost;
        //        base.OnActionExecuting(filterContext);
        //    }
        //    else
        //    {
        //        errorMsg = "您的访问已失效，请重新登录";
        //        if (ctx != null && !string.IsNullOrEmpty(ctx.message) && ctx.message != "install")
        //        {
        //            errorMsg = ctx.message;
        //        }
        //        var errorPage = new ContentResult();
        //        if (Request.Method == "POST" || Request.Query.ContainsKey("do"))
        //        {
        //            errorPage.ContentType = "text/json";
        //            errorPage.Content = Wlniao.Json.ToString(new { success = false, message = errorMsg });
        //        }
        //        else
        //        {
        //            errorPage.ContentType = "text/html;charset=utf-8";
        //            errorPage.Content = errorHtml.Replace("{{errorMsg}}", errorMsg).Replace("{{errorTitle}}", errorTitle).Replace("{{errorIcon}}", errorIcon);
        //        }
        //        filterContext.Result = errorPage;
        //    }
        //}


        /// <summary>
        /// 检查用户系统权限权限
        /// </summary>
        /// <param name="Code"></param>
        /// <param name="Sid"></param>
        /// <returns></returns>
        [NonAction]
        public bool Permission(String Code, String Sid = "")
        {
            if (ctx == null || xsession == null)
            {
                return false;
            }
            if (!string.IsNullOrEmpty(Code))
            {
                var rlt = ctx.EmiGet<Boolean>("app", "permission"
                    , new KeyValuePair<string, string>("sid", string.IsNullOrEmpty(Sid) ? xsession.sid : Sid)
                    , new KeyValuePair<string, string>("code", Code));
                if (rlt.data)
                {
                    return rlt.data;
                }
            }
            return false;
        }
        /// <summary>
        /// 检查机构数据查看权限
        /// </summary>
        /// <param name="Organ"></param>
        /// <param name="Code"></param>
        /// <param name="Sid"></param>
        /// <returns></returns>
        [NonAction]
        public bool PermissionOrgan(String Organ, String Code, String Sid = "")
        {
            if (ctx == null || xsession == null)
            {
                return false;
            }
            if (!string.IsNullOrEmpty(Organ) && !string.IsNullOrEmpty(Code))
            {
                var rlt = ctx.EmiGet<Boolean>("app", "permissionorgan"
                    , new KeyValuePair<string, string>("sid", string.IsNullOrEmpty(Sid) ? xsession.sid : Sid)
                    , new KeyValuePair<string, string>("code", Code)
                    , new KeyValuePair<string, string>("organ", Organ));
                if (rlt.data)
                {
                    return rlt.data;
                }
                errorMsg = "您暂无执行当前操作的权限";
            }
            return false;
        }

        /// <summary>
        /// 构造认证令牌
        /// </summary>
        /// <returns></returns>
        [NonAction]
        public IActionResult BuildTicket()
        {
            return CheckSession((xsession, ctx) =>
            {
                var ticket = Encryptor.SM4EncryptECBToHex(Wlniao.Json.ToString(new
                {
                    xsession.sid,
                    xsession.name,
                    xsession.authid,
                    xsession.account,
                    expire = XCore.NowUnix + 3600
                }), ctx.token, true);
                return Json(new { success = true, ctx.domain, ticket });
            }, true);
        }
        /// <summary>
        /// 检查系统使用授权
        /// </summary>
        /// <param name="func"></param>
        /// <param name="outJson"></param>
        /// <returns></returns>
        [NonAction]
        public IActionResult CheckAuth(Func<EmiContext, IActionResult> func, Boolean outJson = false)
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
                ehost = HeaderRequest("x-domain", UrlDomain);
            }
            if (string.IsNullOrEmpty(ehost))
            {
                msg = "验证失败，缺少授权参数";
            }
            else
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
            if (outJson)
            {
                return Json(new { success = false, message = msg });
            }
            else
            {
                return ErrorMsg(msg);
            }
        }
        /// <summary>
        /// 检查登录认证状态
        /// </summary>
        /// <param name="func"></param>
        /// <param name="outJson"></param>
        /// <returns></returns>
        [NonAction]
        public IActionResult CheckSession(Func<XSession, EmiContext, IActionResult> func, Boolean outJson = false)
        {
            return CheckAuth((ctx) =>
            {
                var msg = "";
                var session = GetCookies("wsession");
                if (Request.Query.Keys.Contains("wsession"))
                {
                    session = GetRequestNoSecurity("wsession");
                    Response.Cookies.Append("wsession", session, IsHttps ? new CookieOptions { Secure = true, SameSite = SameSiteMode.None } : new CookieOptions { });
                }
                if (string.IsNullOrEmpty(session))
                {
                    var authorization = HeaderRequest("Authorization");
                    if (!string.IsNullOrEmpty(authorization))
                    {
                        try
                        {
                            var plainData = Encryptor.SM4DecryptECBFromHex(authorization, ctx.token, true);
                            var auth = Wlniao.Json.ToObject<Dictionary<string, object>>(plainData);
                            if (auth.GetInt64("expire") > XCore.NowUnix)
                            {
                                xsession = new XSession(ctx) { login = true };
                                xsession.sid = auth.GetString("sid");
                                xsession.name = auth.GetString("name");
                                xsession.authid = auth.GetString("authid");
                                xsession.account = auth.GetString("account");
                            }
                        }
                        catch { }
                    }
                }
                else
                {
                    xsession = new XSession(ctx, session);
                }
                if (xsession != null && xsession.login && xsession.wkey == ctx.owner && !string.IsNullOrEmpty(xsession.sid))
                {
                    return func.Invoke(xsession, ctx);
                }
                else
                {
                    msg = "暂未登录或已失效，请登录";
                    if (outJson)
                    {
                        Response.Headers.TryAdd("Access-Control-Expose-Headers", new Microsoft.Extensions.Primitives.StringValues("*"));
                        Response.Headers.TryAdd("Authify-State", new Microsoft.Extensions.Primitives.StringValues("false"));
                        return Json(new { success = false, message = msg });
                    }
                    else
                    {
                        return ErrorMsg(msg);
                    }
                }
            }, outJson);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Code"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        [NonAction]
        public IActionResult? CheckPermission(String Code, Func<IActionResult> func)
        {
            if (ctx == null || xsession == null)
            {
                return Json(new { success = false, message = "请先调用“CheckSession”后再进行权限验证" });
            }
            else if (string.IsNullOrEmpty(Code))
            {
                if (!string.IsNullOrEmpty(method))
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
                    , new KeyValuePair<string, string>("sid", xsession.sid)
                    , new KeyValuePair<string, string>("code", Code));
                if (rlt.data)
                {
                    return func?.Invoke();
                }
                else if (string.IsNullOrEmpty(method))
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
                if (!string.IsNullOrEmpty(method))
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
                    , new KeyValuePair<string, string>("sid", xsession.sid)
                    , new KeyValuePair<string, string>("code", Code)
                    , new KeyValuePair<string, string>("organ", Organ));
                if (rlt.data)
                {
                    return func?.Invoke();
                }
                else if (string.IsNullOrEmpty(method))
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
        public ActionResult NoPermission(Boolean ajax = false)
        {
            if (ajax || !string.IsNullOrEmpty(method))
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
                        var auth = allow == "ALL" || allow == codes[i].ToUpper() ? true : ctx.Permission(xsession.sid, codes[i]);
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
                        var auth = allow == "ALL" || allow == codes[i].ToUpper() ? true : ctx.PermissionOrgan(xsession.sid, codes[i], organ);
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