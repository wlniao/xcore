using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Net.Http.Headers;

namespace Wlniao
{
    /// <summary>
    /// XCore扩展的Controller
    /// </summary>
    public class XCoreController : Controller
    {
        /// <summary>
        /// 请求是否为Https
        /// </summary>
        private bool https = false;
        /// <summary>
        /// 当前请求Host
        /// </summary>
        private string host = null;
        /// <summary>
        /// 链路追踪ID
        /// </summary>
        private string trace = null;
        /// <summary>
        /// 当前执行的方法，参数：do=
        /// </summary>
        protected string method = "";
        /// <summary>
        /// 错误消息（不为空时输出）
        /// </summary>
        protected string errorMsg = "";
        /// <summary>
        /// 错误提醒页面标题
        /// </summary>
        protected string errorTitle = "错误";
        /// <summary>
        /// 错误提醒页面图标
        /// </summary>
        protected string errorIcon = ErrorIcon;
        /// <summary>
        /// 错误提醒页面模板
        /// </summary>
        protected string errorHtml = ErrorHtml;
        /// <summary>
        /// 当前请求是否安全
        /// </summary>
        protected bool RequestSecurity = true;
        /// <summary>
        /// 错误提醒页面图标
        /// </summary>
        internal const string ErrorIcon = "data:image/png;base64,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";
        /// <summary>
        /// 错误提醒页面模板
        /// </summary>
        internal const string ErrorHtml = "<html><head><title>{{errorTitle}}</title><link rel=\"icon\" href=\"data:image/ico;base64,aWNv\" /><meta charset=\"UTF-8\"/><meta name=\"viewport\" content=\"width=device-width,initial-scale=1,user-scalable=0\"/></head><body onselectstart=\"return false\" style=\"text-align:center;background:#f9f9f9\"><div><img src=\"{{errorIcon}}\" style=\"width:9rem;margin-top:9rem;\"></div><div style=\"color:#999999;font-family:Segoe UI, Segoe UI Midlevel, Segoe WP, Arial, Sans-Serif;padding:1rem\">{{errorMsg}}</div></body></html>";
        /// <summary>
        /// 
        /// </summary>
        /// <param name="filterContext"></param>
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            method = Request.Query.ContainsKey("do") ? Request.Query["do"].ToString() : "";
            Request.HttpContext.Items["startTime"] = DateTime.Now;
            base.OnActionExecuting(filterContext);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            if (!RequestSecurity)
            {
                errorMsg = Config.GetSetting("NoSecurityMessage");
            }
            if (string.IsNullOrEmpty(errorMsg))
            {
                base.OnActionExecuted(context);
                if (string.IsNullOrEmpty(trace) && Request.Headers.ContainsKey("X-Wlniao-Trace"))
                {
                    trace = Request.Headers["X-Wlniao-Trace"].ToString();
                }
                if (!string.IsNullOrEmpty(trace))
                {
                    Response.Headers.Add("X-Wlniao-Trace", trace);
                }
                if (Request.HttpContext.Items["startTime"] != null)
                {
                    var ts = DateTime.Now.Subtract(System.Convert.ToDateTime(Request.HttpContext.Items["startTime"]));
                    Response.Headers.Add("X-Wlniao-UseTime", ts.TotalMilliseconds.ToString("F2") + "ms");
                }
                if (!string.IsNullOrEmpty(XCore.XServerId))
                {
                    Response.Headers.Add("X-Wlniao-XServerId", XCore.XServerId);
                }
            }
            else if (string.IsNullOrEmpty(method))
            {
                var errorPage = new ContentResult();
                errorPage.ContentType = "text/html";
                errorPage.Content = errorHtml.Replace("{{errorTitle}}", errorTitle).Replace("{{errorIcon}}", errorIcon).Replace("{{errorMsg}}", errorMsg);
                context.Result = errorPage;
            }
            else
            {
                var jsonStr = Wlniao.Json.ToString(new { success = false, message = errorMsg, data = "" });
                var errorPage = new ContentResult();
                if (string.IsNullOrEmpty(GetRequest("callback")))
                {
                    errorPage.ContentType = "text/json";
                    errorPage.Content = jsonStr;
                }
                else
                {
                    errorPage.ContentType = "text/javascript";
                    errorPage.Content = GetRequest("callback") + "(" + jsonStr + ")";
                }
                context.Result = errorPage;
            }
        }
        /// <summary>
        /// 输出调试消息
        /// </summary>
        /// <param name="message"></param>
        [NonAction]
        public void DebugMessage(String message)
        {
            if (!Response.Headers.ContainsKey("X-Wlniao-Debug"))
            {
                Response.Headers.Add("X-Wlniao-Debug", message);
            }
        }
        /// <summary>
        /// Object输出
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [NonAction]
        public new ActionResult Json(Object data)
        {
            var jsonStr = "";
            if (data != null)
            {
                if (data is string)
                {
                    jsonStr = data.ToString();
                }
                else if (data != null)
                {
                    jsonStr = Wlniao.Json.ToString(data);
                }
            }
            if (Request.Query.ContainsKey("callback"))
            {
                return Content(GetRequest("callback") + "(" + jsonStr + ")", "text/javascript", System.Text.Encoding.UTF8);
            }
            else
            {
                return Content(jsonStr, "text/json", System.Text.Encoding.UTF8);
            }
        }
        /// <summary>
        /// Object输出
        /// </summary>
        /// <param name="data"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        [NonAction]
        public ActionResult Json(Object data, System.Text.Encoding encoding)
        {
            var jsonStr = "";
            if (data != null)
            {
                if (data is string)
                {
                    jsonStr = data.ToString();
                }
                else if (data != null)
                {
                    jsonStr = Wlniao.Json.ToString(data);
                }
            }
            if (Request.Query.ContainsKey("callback"))
            {
                return Content(GetRequest("callback") + "(" + jsonStr + ")", "text/javascript", encoding ?? System.Text.Encoding.UTF8);
            }
            else
            {
                return Content(jsonStr, "text/json", encoding ?? System.Text.Encoding.UTF8);
            }
        }
        /// <summary>
        /// Json字符串输出
        /// </summary>
        /// <param name="jsonStr"></param>
        /// <returns></returns>
        [NonAction]
        public ActionResult JsonStr(String jsonStr)
        {
            if (string.IsNullOrEmpty(GetRequest("callback")) || jsonStr.LastIndexOf(')') > jsonStr.LastIndexOf(':'))
            {
                return Content(jsonStr, "text/json", System.Text.Encoding.UTF8);
            }
            else
            {
                return Content(GetRequest("callback") + "(" + jsonStr + ")", "text/json", System.Text.Encoding.UTF8);
            }
        }
        /// <summary>
        /// Json字符串输出
        /// </summary>
        /// <param name="jsonStr"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        [NonAction]
        public ActionResult JsonStr(String jsonStr, System.Text.Encoding encoding)
        {
            if (string.IsNullOrEmpty(GetRequest("callback")) || jsonStr.LastIndexOf(')') > jsonStr.LastIndexOf(':'))
            {
                return Content(jsonStr, "text/json", encoding ?? System.Text.Encoding.UTF8);
            }
            else
            {
                return Content(GetRequest("callback") + "(" + jsonStr + ")", "text/json", encoding ?? System.Text.Encoding.UTF8);
            }
        }
        /// <summary>
        /// 输出错误消息
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        [NonAction]
        public ActionResult ErrorMsg(String message = null)
        {
            if (string.IsNullOrEmpty(message))
            {
                message = errorMsg;
            }
            if (string.IsNullOrEmpty(method))
            {
                return new ContentResult
                {
                    ContentType = "text/html",
                    Content = errorHtml.Replace("{{errorTitle}}", errorTitle).Replace("{{errorIcon}}", errorIcon).Replace("{{errorMsg}}", message)
                };
            }
            else if (string.IsNullOrEmpty(GetRequest("callback")))
            {
                return Content(Wlniao.Json.ToString(new { success = false, message = message, data = "" }), "text/json", System.Text.Encoding.UTF8);
            }
            else
            {
                return Content(GetRequest("callback") + "(" + Wlniao.Json.ToString(new { success = false, message = message, data = "" }) + ")", "text/json", System.Text.Encoding.UTF8);
            }
        }
        /// <summary>
        /// 获取Cookie指
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        [NonAction]
        public string GetCookies(String key)
        {
            key = key.ToLower();
            foreach (var item in Request.Cookies.Keys)
            {
                if (item.ToLower() == key)
                {
                    return Request.Cookies[item];
                }
            }
            return "";
        }
        /// <summary>
        /// 获取请求参数Get及Post
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Default"></param>
        /// <returns></returns>
        [NonAction]
        protected String GetRequestNoSecurity(String Key, String Default = "")
        {
            var key = Key.ToLower();
            foreach (var item in Request.Query.Keys)
            {
                if (item.ToLower() == key && !string.IsNullOrEmpty(Request.Query[key]))
                {
                    Default = Request.Query[item].ToString().Trim();
                    if (!string.IsNullOrEmpty(Default) && Default.IndexOf('%') >= 0)
                    {
                        Default = strUtil.UrlDecode(Default);
                    }
                    return Default.Trim();
                }
            }
            return Default.Trim();
        }
        /// <summary>
        /// 获取请求参数（过滤非安全字符）
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Default"></param>
        /// <returns></returns>
        [NonAction]
        protected String GetRequestSecurity(String Key, String Default = "")
        {
            Default = GetRequestNoSecurity(Key, Default);
            var str = System.Text.RegularExpressions.Regex.Replace(Default, @"[;|\/|\(|\)|\[|\]|\}|\{|%|\*|!|\'|\.|<|>]", "").Replace("\"", "");
            if (str != Default)
            {
                RequestSecurity = false;
            }
            return str;
        }
        /// <summary>
        /// 获取请求参数（仅标记但不过滤非安全字符）
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Default"></param>
        /// <returns></returns>
        [NonAction]
        protected String GetRequest(String Key, String Default = "")
        {
            Default = GetRequestNoSecurity(Key, Default);
            var str = System.Text.RegularExpressions.Regex.Replace(Default, @"[;|\/|\(|\)|\[|\]|\}|\{|%|\*|!|\'|\.|<|>]", "").Replace("\"", "");
            if (str != Default)
            {
                RequestSecurity = false;
            }
            return Default;
        }
        /// <summary>
        /// 获取请求参数
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Default"></param>
        /// <returns></returns>
        [NonAction]
        protected String GetRequestDecode(String Key, String Default = "")
        {
            return GetRequest(Key, Default);
        }
        /// <summary>
        /// 获取请求参数
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        [NonAction]
        protected Int32 GetRequestInt(String Key)
        {
            return cvt.ToInt(GetRequest(Key, "0"));
        }
        /// <summary>
        /// 获取Post的文本内容
        /// </summary>
        /// <returns></returns>
        [NonAction]
        protected String GetPostString()
        {
            if (strPost == null && Request.Method == "POST" && Request.ContentLength > 0 && (Request.ContentType == null || !Request.ContentType.Contains("form")))
            {
                try
                {
                    strPost = new System.IO.StreamReader(Request.Body).ReadToEnd();
                }
                catch
                {
                    var buffer = new byte[(int)Request.ContentLength];
                    Request.Body.Read(buffer, 0, buffer.Length);
                    strPost = System.Text.Encoding.UTF8.GetString(buffer);
                }
            }
            return strPost;
        }
        /// <summary>
        /// 
        /// </summary>
        private string strPost = null;
        /// <summary>
        /// 
        /// </summary>
        private Dictionary<string, string> ctxPost = null;
        /// <summary>
        /// 获取请求参数（仅标记但不过滤非安全字符）
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Default"></param>
        /// <returns></returns>
        [NonAction]
        protected String PostRequest(String Key, String Default = "")
        {
            var key = Key.ToLower();
            if (ctxPost == null)
            {
                try
                {
                    ctxPost = new Dictionary<string, string>();
                    if (Request.Method == "POST")
                    {
                        try
                        {
                            if (Request.ContentType != null && Request.ContentType.Contains("application/x-www-form-urlencoded"))
                            {
                                #region 请求为表单
                                foreach (var item in Request.Form.Keys)
                                {
                                    ctxPost.Add(item.ToLower(), Request.Form[item].ToString().Trim());
                                }
                                strPost = "";
                                #endregion 请求为表单
                            }
                            else if (Request.ContentType != null && Request.ContentType.Contains("multipart/form-data"))
                            {
                                #region 请求为文件上传
                                if (Request.Form != null && Request.Form.Keys != null)
                                {
                                    foreach (var item in Request.Form.Keys)
                                    {
                                        ctxPost.Add(item.ToLower(), Request.Form[item].ToString().Trim());
                                    }
                                }
                                strPost = "";
                                #endregion 请求为文件上传
                            }
                            else if (Request.ContentLength > 0)
                            {
                                #region 请求为其它类型
                                if (strPost == null)
                                {
                                    try
                                    {
                                        strPost = new System.IO.StreamReader(Request.Body).ReadToEnd();
                                    }
                                    catch
                                    {
                                        var buffer = new byte[(int)Request.ContentLength];
                                        Request.Body.Read(buffer, 0, buffer.Length);
                                        strPost = System.Text.Encoding.UTF8.GetString(buffer);
                                    }
                                }
                                if (!string.IsNullOrEmpty(strPost))
                                {
                                    var tmpPost = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<String, String>>(strPost);
                                    if (tmpPost != null)
                                    {
                                        foreach (var kv in tmpPost)
                                        {
                                            ctxPost.TryAdd(kv.Key.ToLower(), kv.Value == null ? "" : kv.Value.Trim());
                                        }
                                    }
                                }
                                #endregion 请求为其它类型
                            }
                        }
                        catch { }
                    }
                    if (Request.Query != null) //叠加URL传递的参数
                    {
                        foreach (var item in Request.Query.Keys)
                        {
                            ctxPost.TryAdd(item.ToLower(), strUtil.UrlDecode(Request.Query[item].ToString().Trim()));
                        }
                    }
                }
                catch { }
            }
            if (ctxPost.ContainsKey(key) && !string.IsNullOrEmpty(ctxPost[key]))
            {
                Default = ctxPost[key];
            }
            return Default.Trim();
        }

        /// <summary>
        /// 获取请求参数
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        [NonAction]
        protected Int32 PostRequestInt(String Key)
        {
            return cvt.ToInt(PostRequest(Key, "0"));
        }

        /// <summary>
        /// 客户端请求是否为HTTPS协议(兼容X-Forwarded-Proto属性)
        /// </summary>
        public bool IsHttps
        {
            get
            {
                if (https || Request.IsHttps)
                {
                    return true;
                }
                var val = new Microsoft.Extensions.Primitives.StringValues();
                if (Request.Headers.TryGetValue("x-forwarded-proto", out val) && val.ToString().ToLower() == "https")
                {
                    https = true;
                }
                if (!https && Request.Headers.TryGetValue("x-client-scheme", out val) && val.ToString().ToLower() == "https")
                {
                    https = true;
                }
                if (!https && Request.Headers.TryGetValue("referer", out val) && val.ToString().Contains("https"))
                {
                    https = true;
                }
                return https;
            }
        }
        /// <summary>
        /// 当前浏览器UserAgent
        /// </summary>
        /// <returns></returns>
        public string UserAgent
        {
            get
            {
                var ua = new Microsoft.Extensions.Primitives.StringValues();
                if (Request.Headers.TryGetValue("user-agent", out ua) && ua.Count > 0)
                {
                    return ua.ToString();
                }
                return "";
            }
        }
        /// <summary>
        /// 获取当前访问使用的平台
        /// </summary>
        /// <returns></returns>
        public string GetPlatform
        {
            get
            {
                var ua = UserAgent.ToLower();
                if (ua.Contains("wxwork"))
                {
                    return "wxwork";
                }
                else if (ua.Contains("micromessenger"))
                {
                    return "weixin";
                }
                else if (ua.Contains("dingtalk"))
                {
                    return "dingtalk";
                }
                else if (ua.Contains("wlniao"))
                {
                    return "wlniao";
                }
                else if (ua.Contains("wlnapp"))
                {
                    return "wlnapp";
                }
                else
                {
                    return "other";
                }
            }
        }
        /// <summary>
        /// 客户端IP地址
        /// </summary>
        public string ClientIP
        {
            get
            {
                var ip = "";
                var forwardedIP = new Microsoft.Extensions.Primitives.StringValues();
                if (Request.Headers.TryGetValue("x-forwarded-for", out forwardedIP))
                {
                    // 通过代理网关部署时，获取"x-forwarded-for"传递的真实IP
                    var ips = forwardedIP.ToString().Split(',', ':');
                    if (ips.Length > 0)
                    {
                        //通常多个IP中，第一个为访客IP，最后一个为代理服务器IP
                        ip = ips[0];
                    }
                }
                else
                {
                    ip = Request.HttpContext.Connection.RemoteIpAddress.ToString();
                    if (ip[0] == ':')
                    {
                        ip = "127.0.0.1";
                    }
                }
                return ip;
            }
        }
        /// <summary>
        /// 当前请求Host
        /// </summary>
        public string UrlHost
        {
            get
            {
                if (string.IsNullOrEmpty(host))
                {
                    if (!string.IsNullOrEmpty(XCore.WebHost) && strUtil.IsIP(Request.Host.Host))
                    {
                        host = XCore.WebHost;
                    }
                    else
                    {
                        host = (IsHttps ? "https://" : "http://") + Request.Host.Value;
                    }
                }
                return host;
            }
        }
        /// <summary>
        /// 页面引用地址
        /// </summary>
        public string UrlReferer
        {
            get
            {
                var referer = new Microsoft.Extensions.Primitives.StringValues();
                if (Request.Headers.TryGetValue("referer", out referer) && referer.Count > 0)
                {
                    return referer.ToString();
                }
                return "";
            }
        }
        /// <summary>
        /// 链路追踪ID
        /// </summary>
        public string TraceId
        {
            get
            {
                if (trace == null)
                {
                    var traceId = new Microsoft.Extensions.Primitives.StringValues();
                    if (Request.Headers.TryGetValue("wln-trace-id", out traceId) && traceId.Any())
                    {
                        trace = traceId.ToString();
                    }
                    else
                    {
                        trace = System.Guid.NewGuid().ToString().Replace('-', '\0');
                    }
                }
                return trace;
            }
        }
    }
}