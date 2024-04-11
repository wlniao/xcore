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
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Wlniao;
using Wlniao.XServer;

namespace Wlniao.XCenter
{
    /// <summary>
    /// Emi应用基础Controller
    /// </summary>
    public partial class EmiController : XAppController
    {
        /// <summary>
        /// EMI主程序接口访问工具
        /// </summary>
        internal new EmiContext ctx = null;
        /// <summary>
        /// EMI主程序地址
        /// </summary>
        private String emihost = "";
        /// <summary>
        /// 无权限提示消息
        /// </summary>
        private String nopermissionMessage = "";
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            if (ctx != null)
            {
                var ehost = string.IsNullOrEmpty(emihost) ? ctx?.EmiHost : (ctx.https ? "https://" : "//") + emihost;
                if (string.IsNullOrEmpty(ViewBag.eHost))
                {
                    ViewBag.eHost = ehost;
                }
                if (string.IsNullOrEmpty(ViewBag.EmiHost))
                {
                    ViewBag.EmiHost = ehost;
                }
                if (string.IsNullOrEmpty(ViewBag.CdnHost))
                {
                    ViewBag.CdnHost = ctx?.CdnPrefix;
                }
            }
            base.OnActionExecuted(context);
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
                return Json(new { success = true, ctx.domain, ticket = xsession.BuildTicket() });
            });
        }

        /// <summary>
        /// 检查系统使用授权
        /// </summary>
        /// <param name="func"></param>
        /// <param name="fail"></param>
        /// <returns></returns>
        [NonAction]
        public IActionResult CheckAuth(Func<EmiContext, IActionResult> func, Func<IActionResult> fail = null)
        {
            emihost = GetCookies("ehost");
            if (Request.Query.Keys.Contains("ehost"))
            {
                emihost = GetRequestNoSecurity("ehost");
                Response.Cookies.Append("ehost", emihost, IsHttps ? new CookieOptions { Secure = true, SameSite = SameSiteMode.None } : new CookieOptions { });
            }
            else if (string.IsNullOrEmpty(emihost))
            {
                emihost = HeaderRequest("x-domain", EmiContext.XCenterDomain);
            }
            if (fail == null)
            {
                fail = new Func<IActionResult>(() =>
                {
                    if (string.IsNullOrEmpty(errorMsg))
                    {
                        // 显示基础授权流程中的错误提示
                        if (base.ctx != null && !string.IsNullOrEmpty(base.ctx.message))
                        {
                            errorMsg = base.ctx.message;
                        }
                        else if (ctx != null && !string.IsNullOrEmpty(ctx.message))
                        {
                            errorMsg = ctx.message;
                        }
                    }
                    Response.Headers.TryAdd("Access-Control-Expose-Headers", new Microsoft.Extensions.Primitives.StringValues("*"));
                    Response.Headers.TryAdd("Authify-State", new Microsoft.Extensions.Primitives.StringValues("false"));
                    if (Request.Method == "POST" || (Request.Query != null && Request.Query.ContainsKey("do")))
                    {
                        var err = new ApiResult<string> { message = errorMsg };
                        errorMsg = "";
                        return OutputSerialize(err);
                    }
                    else
                    {
                        return ErrorMsg(errorMsg);
                    }
                });
            }
            return base.CheckAuth((ct) =>
            {
                ctx = EmiContext.Load(ct);
                if (ctx.install)
                {
                    return func.Invoke(ctx);
                }
                else
                {
                    errorMsg = ctx.message;
                    return fail.Invoke();
                }
            }, fail, emihost);
        }

        /// <summary>
        /// 检查登录认证状态
        /// </summary>
        /// <param name="func"></param>
        /// <param name="fail"></param>
        /// <returns></returns>
        [NonAction]
        public IActionResult CheckSession(Func<XSession, EmiContext, IActionResult> func, Func<IActionResult> fail = null)
        {
            if (fail == null)
            {
                fail = new Func<IActionResult>(() =>
                {
                    errorTitle = "用户身份校验异常";
                    if (string.IsNullOrEmpty(errorMsg))
                    {
                        // 显示基础授权流程中的错误提示
                        if (base.ctx != null && !string.IsNullOrEmpty(base.ctx.message))
                        {
                            errorMsg = base.ctx.message;
                        }
                        else if (ctx != null && !string.IsNullOrEmpty(ctx.message))
                        {
                            errorMsg = ctx.message;
                        }
                    }
                    if (string.IsNullOrEmpty(errorMsg))
                    {
                        errorMsg = "暂未登录或已失效，请登录";
                    }
                    if (Request.Method == "POST" || (Request.Query != null && Request.Query.ContainsKey("do")))
                    {
                        var err = new ApiResult<string> { message = errorMsg };
                        errorMsg = "";
                        return OutputSerialize(err);
                    }
                    else
                    {
                        var errorPage = new ContentResult();
                        errorPage.ContentType = "text/html;charset=utf-8";
                        errorPage.Content = errorHtml.Replace("{{errorMsg}}", errorMsg).Replace("{{errorTitle}}", errorTitle).Replace("{{errorIcon}}", errorIcon);
                        errorMsg = "";
                        return errorPage;
                    }
                });
            }
            return this.CheckAuth((ctx) =>
            {
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
                    Response.Headers.TryAdd("Access-Control-Expose-Headers", new Microsoft.Extensions.Primitives.StringValues("*"));
                    Response.Headers.TryAdd("Authify-State", new Microsoft.Extensions.Primitives.StringValues("false"));
                    return fail.Invoke();
                }
            });
        }

        /// <summary>
        /// 返回无权限提示
        /// </summary>
        /// <param name="ajax"></param>
        /// <returns></returns>
        [NonAction]
        public IActionResult NoPermission(Boolean ajax = false)
        {
            errorTitle = "操作未授权";
            if (string.IsNullOrEmpty(nopermissionMessage))
            {
                nopermissionMessage = "您暂无执行当前操作的权限";
            }
            if (ajax || Request.Method == "POST" || !string.IsNullOrEmpty(method))
            {
                return OutputSerialize(new ApiResult<string> { message = nopermissionMessage });
            }
            else
            {
                var errorPage = new ContentResult();
                errorPage.ContentType = "text/html;charset=utf-8";
                errorPage.Content = errorHtml.Replace("{{errorMsg}}", nopermissionMessage).Replace("{{errorTitle}}", errorTitle).Replace("{{errorIcon}}", errorIcon);
                return errorPage;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="code"></param>
        /// <param name="func"></param>
        /// <param name="fail"></param>
        /// <returns></returns>
        [NonAction]
        public IActionResult? CheckPermission(String code, Func<IActionResult> func, Func<IActionResult> fail = null)
        {
            if (fail == null)
            {
                fail = new Func<IActionResult>(() =>
                {
                    return NoPermission();
                });
            }
            if (ctx == null || xsession == null)
            {
                nopermissionMessage = "请先调用“CheckSession”后再进行权限验证";
            }
            else if (string.IsNullOrEmpty(code))
            {
                nopermissionMessage = "要验证的权限无效，请检查权限编码是否正确";
            }
            else
            {
                var rlt = ctx.EmiGet<Boolean>("app", "permission"
                    , new KeyValuePair<string, string>("sid", xsession.UserSid)
                    , new KeyValuePair<string, string>("code", code));
                if (rlt.data)
                {
                    return func?.Invoke();
                }
                else
                {
                    nopermissionMessage = rlt.message;
                }
            }
            return fail?.Invoke();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="code"></param>
        /// <param name="organ"></param>
        /// <param name="func"></param>
        /// <param name="fail"></param>
        /// <returns></returns>
        [NonAction]
        public IActionResult? CheckPermission(String code, String organ, Func<IActionResult> func, Func<IActionResult> fail = null)
        {
            if (fail == null)
            {
                fail = new Func<IActionResult>(() =>
                {
                    return NoPermission();
                });
            }
            if (ctx == null || xsession == null)
            {
                nopermissionMessage = "请先调用“CheckSession”后再进行权限验证";
            }
            else if (string.IsNullOrEmpty(code))
            {
                nopermissionMessage = "要验证的权限无效，请检查权限编码是否正确";
            }
            else
            {
                var rlt = ctx.EmiGet<Boolean>("app", "permissionorgan"
                    , new KeyValuePair<string, string>("sid", xsession.UserSid)
                    , new KeyValuePair<string, string>("code", code)
                    , new KeyValuePair<string, string>("organ", organ));
                if (rlt.data)
                {
                    return func?.Invoke();
                }
                else
                {
                    nopermissionMessage = rlt.message;
                }
            }
            return fail?.Invoke();
        }

    }
}