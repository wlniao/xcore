using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Wlniao.Text;
using Encoding = System.Text.Encoding;

namespace Wlniao.Mvc
{
    /// <summary>
    /// XCore扩展的Controller
    /// </summary>
    public class XCoreController : ControllerBase
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
        /// 当前请求域名
        /// </summary>
        private string domain = null;
        /// <summary>
        /// 链路追踪ID
        /// </summary>
        private string trace = null;
        /// <summary>
        /// 当前请求开始时间
        /// </summary>
        private DateTime start = DateTime.Now;
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
        internal const string ErrorHtml = "<html><head><title>{{errorTitle}}</title><link rel=\"icon\" href=\"data:image/ico;base64,aWNv\" /><meta charset=\"UTF-8\"/><meta name=\"viewport\" content=\"user-scalable=no,width=device-width,initial-scale=1.0\"/></head><body onselectstart=\"return false\" style=\"text-align:center;background:#f9f9f9\"><div><img src=\"{{errorIcon}}\" style=\"width:9rem;margin-top:9rem;\"></div><div style=\"color:#999999;font-family:Segoe UI, Segoe UI Midlevel, Segoe WP, Arial, Sans-Serif;padding:1rem\">{{errorMsg}}</div></body></html>";
        // /// <summary>
        // /// 
        // /// </summary>
        // /// <param name="filterContext"></param>
        // public override void OnActionExecuting(ActionExecutingContext filterContext)
        // {
        //     method = Request.Query.ContainsKey("do") ? Request.Query["do"].ToString() : "";
        //     base.OnActionExecuting(filterContext);
        // }
        // /// <summary>
        // /// 
        // /// </summary>
        // /// <param name="context"></param>
        // public override void OnActionExecuted(ActionExecutedContext context)
        // {
        //     if(context.HttpContext.WebSockets.IsWebSocketRequest)
        //     {
        //         return;
        //     }
        //     if (!RequestSecurity)
        //     {
        //         errorMsg = Config.GetConfigs("NoSecurityMessage");
        //     }
        //     if (!Response.HasStarted)
        //     {
        //         Response.Headers.TryAdd("X-Wlniao-UseTime", DateTime.Now.Subtract(start).TotalMilliseconds.ToString("F2") + "ms");
        //     }
        //     if (string.IsNullOrEmpty(errorMsg))
        //     {
        //         base.OnActionExecuted(context);
        //         if (string.IsNullOrEmpty(trace) && Request.Headers.ContainsKey("X-Wlniao-Trace"))
        //         {
        //             trace = Request.Headers["X-Wlniao-Trace"].ToString();
        //         }
        //         if (!string.IsNullOrEmpty(trace) && !Response.HasStarted)
        //         {
        //             Response.Headers.TryAdd("X-Wlniao-Trace", trace);
        //         }
        //         if (!string.IsNullOrEmpty(XCore.XServerId) && !Response.HasStarted)
        //         {
        //             Response.Headers.TryAdd("X-Wlniao-XServerId", XCore.XServerId);
        //         }
        //     }
        //     else if (string.IsNullOrEmpty(method))
        //     {
        //         context.Result = new ContentResult
        //         {
        //             ContentType = "text/html",
        //             Content = errorHtml.Replace("{{errorTitle}}", errorTitle).Replace("{{errorIcon}}", errorIcon).Replace("{{errorMsg}}", errorMsg)
        //         };
        //     }
        //     else
        //     {
        //         var jsonStr = Wlniao.Json.Serialize(new { success = false, message = errorMsg, data = "" });
        //         var errorPage = new ContentResult();
        //         if (string.IsNullOrEmpty(GetRequest("callback")))
        //         {
        //             errorPage.ContentType = "text/json";
        //             errorPage.Content = jsonStr;
        //         }
        //         else
        //         {
        //             errorPage.ContentType = "text/javascript";
        //             errorPage.Content = GetRequest("callback") + "(" + jsonStr + ")";
        //         }
        //         context.Result = errorPage;
        //     }
        // }
        /// <summary>
        /// 输出调试消息
        /// </summary>
        /// <param name="message"></param>
        [NonAction]
        public void DebugMessage(string message)
        {
            if (!string.IsNullOrEmpty(message) && !Response.Headers.ContainsKey("X-Wlniao-Debug"))
            {
                Response.Headers.TryAdd("X-Wlniao-Debug", message);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <param name="seconds"></param>
        /// <returns></returns>
        [NonAction]
        public IActionResult RedirectWait(string url, int seconds)
        {
            return Content("<html><head><link rel=\"icon\" href=\"data:image/ico;base64,aWNv\"><meta http-equiv=\"refresh\" content=\"" + seconds + ";url=" + url + "\"></head></html>", "text/html");
        }
        /// <summary>
        /// Object输出
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [NonAction]
        public ActionResult Json(object data)
        {
            var jsonStr = "";
            switch (data)
            {
                case null:
                    return Content(jsonStr ?? "", "application/json", Encoding.UTF8);
                case string:
                    jsonStr = data.ToString();
                    break;
                default:
                    jsonStr = JsonSerializer.Serialize(data, new JsonSerializerOptions
                    {
                        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) //Json序列化的时候对中文进行处理
                    });
                    break;
            }

            return Content(jsonStr ?? "", "application/json", Encoding.UTF8);
        }
        /// <summary>
        /// Object输出
        /// </summary>
        /// <param name="data"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        [NonAction]
        public ActionResult Json(object? data, Encoding encoding)
        {
            var jsonStr = "";
            switch (data)
            {
                case null:
                    return Content(jsonStr ?? "", "application/json", encoding ?? Encoding.UTF8);
                case string:
                    jsonStr = data.ToString();
                    break;
                default:
                    jsonStr = JsonSerializer.Serialize(data, new JsonSerializerOptions
                    {
                        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) //Json序列化的时候对中文进行处理
                    });
                    break;
            }

            return Content(jsonStr ?? "", "application/json", encoding ?? Encoding.UTF8);
        }
        /// <summary>
        /// Json字符串输出
        /// </summary>
        /// <param name="jsonStr"></param>
        /// <returns></returns>
        [NonAction]
        public ActionResult JsonStr(string jsonStr)
        {
            if (string.IsNullOrEmpty(GetRequest("callback")) || jsonStr.LastIndexOf(')') > jsonStr.LastIndexOf(':'))
            {
                return Content(jsonStr, "application/json", Encoding.UTF8);
            }
            else
            {
                return Content(GetRequest("callback") + "(" + jsonStr + ")", "application/json", Encoding.UTF8);
            }
        }
        /// <summary>
        /// Json字符串输出
        /// </summary>
        /// <param name="jsonStr"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        [NonAction]
        public ActionResult JsonStr(string jsonStr, Encoding encoding)
        {
            if (string.IsNullOrEmpty(GetRequest("callback")) || jsonStr.LastIndexOf(')') > jsonStr.LastIndexOf(':'))
            {
                return Content(jsonStr, "application/json", encoding ?? Encoding.UTF8);
            }
            else
            {
                return Content(GetRequest("callback") + "(" + jsonStr + ")", "application/json", encoding ?? Encoding.UTF8);
            }
        }
        /// <summary>
        /// 输出错误消息
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        [NonAction]
        public ActionResult ErrorMsg(string? message = null)
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
                return Content(Wlniao.Json.Serialize(new { success = false, message = message }), "application/json", Encoding.UTF8);
            }
            else
            {
                return Content(GetRequest("callback") + "(" + Wlniao.Json.Serialize(new { success = false, message = message }) + ")", "text/json", Encoding.UTF8);
            }
        }
        /// <summary>
        /// 获取Cookie指
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        [NonAction]
        public string GetCookies(string key)
        {
            key = key.ToLower();
            var item = Request.Cookies.FirstOrDefault(o => o.Key.ToLower() == key);
            return item.Value ?? "";
        }
        /// <summary>
        /// 获取请求参数Get及Post
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Default"></param>
        /// <returns></returns>
        [NonAction]
        protected string GetRequestNoSecurity(string Key, string Default = "")
        {
            var key = Key.ToLower();
            foreach (var item in Request.Query.Keys)
            {
                if (item.ToLower() != key || string.IsNullOrEmpty(Request.Query[key]))
                {
                    continue;
                }
                Default = Request.Query[item].ToString().Trim();
                if (!string.IsNullOrEmpty(Default) && Default.IndexOf('%') >= 0)
                {
                    Default = StringUtil.UrlDecode(Default);
                }
                return Default.Trim();
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
        protected string GetRequestSecurity(string Key, string Default = "")
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
        protected string GetRequest(string Key, string Default = "")
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
        protected string GetRequestDecode(string Key, string Default = "")
        {
            return GetRequest(Key, Default);
        }
        /// <summary>
        /// 获取请求参数
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        [NonAction]
        protected int GetRequestInt(string Key)
        {
            return Convert.ToInt(GetRequest(Key, "0"));
        }
        /// <summary>
        /// 获取Post的文本内容
        /// </summary>
        /// <returns></returns>
        [NonAction]
        protected string GetPostString()
        {
            if (Request.Method == "OPTIONS")
            {
                return string.Empty;
            }
            else if (_strPost == null && Request.Method == "POST" && Request.Body != null)
            {
                try
                {
                    _strPost = new StreamReader(Request.Body).ReadToEnd();
                    if (string.IsNullOrEmpty(_strPost))
                    {
                        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                        _strPost = reader.ReadToEnd();
                    }
                }
                catch
                {
                    var buffer = Array.Empty<byte>();
                    if (Request != null && Request.ContentLength > 0)
                    {
                        buffer = new byte[(int)Request.ContentLength];
                    }
                    Request.Body.Read(buffer, 0, buffer.Length);
                    _strPost = Encoding.UTF8.GetString(buffer);
                }
            }
            return _strPost ?? "";
        }
        /// <summary>
        /// 
        /// </summary>
        private string? _strPost = null;
        /// <summary>
        /// 
        /// </summary>
        private Dictionary<string, object>? _ctxPost = null;
        /// <summary>
        /// 获取请求参数（仅标记但不过滤非安全字符）
        /// </summary>
        /// <param name="key"></param>
        /// <param name="default"></param>
        /// <returns></returns>
        [NonAction]
        protected string PostRequest(string key, string @default = "")
        {
            if (_ctxPost == null)
            {
                try
                {
                    _ctxPost = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    if (Request.Method == "POST")
                    {
                        try
                        {
                            if (Request.ContentType != null && Request.ContentType.Contains("application/x-www-form-urlencoded"))
                            {
                                #region 请求为表单
                                foreach (var item in Request.Form.Keys)
                                {
                                    _ctxPost.Add(item.ToLower(), Request.Form[item].ToString().Trim());
                                }
                                _strPost = "";
                                #endregion 请求为表单
                            }
                            else if (Request.ContentType != null && Request.ContentType.Contains("multipart/form-data"))
                            {
                                #region 请求为文件上传
                                if (Request.Form != null && Request.Form.Keys != null)
                                {
                                    foreach (var item in Request.Form.Keys)
                                    {
                                        _ctxPost.Add(item.ToLower(), Request.Form[item].ToString().Trim());
                                    }
                                }
                                _strPost = "";
                                #endregion 请求为文件上传
                            }
                            else if (Request.ContentLength > 0)
                            {
                                #region 请求为其它类型
                                if (_strPost == null)
                                {
                                    try
                                    {
                                        _strPost = new StreamReader(Request.Body).ReadToEnd();
                                    }
                                    catch
                                    {
                                        //strPost=Request.BodyReader.ReadAsync().Result.ToString();
                                        var buffer = new byte[(int)Request.ContentLength];
                                        Request.Body.Read(buffer, 0, buffer.Length);
                                        _strPost = Encoding.UTF8.GetString(buffer);
                                    }
                                }
                                if (!string.IsNullOrEmpty(_strPost))
                                {
                                    var tmpPost = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(_strPost);
                                    if (tmpPost != null)
                                    {
                                        foreach (var kv in tmpPost)
                                        {
                                            if (kv.Value == null)
                                            {
                                                continue;
                                            }
                                            else
                                            {
                                                _ctxPost.TryAdd(kv.Key, kv.Value);
                                            }
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
                            _ctxPost.TryAdd(item, StringUtil.UrlDecode(Request.Query[item].ToString().Trim()));
                        }
                    }
                }
                catch { }
            }
            if (_ctxPost != null)
            {
                return _ctxPost.GetString(key, @default);
            }
            else if (string.IsNullOrEmpty(@default))
            {
                return "";
            }
            else
            {
                return @default.Trim();
            }
        }

        /// <summary>
        /// 获取请求参数
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        [NonAction]
        protected int PostRequestInt(string Key)
        {
            return Convert.ToInt(PostRequest(Key, "0"));
        }

        /// <summary>
        /// 获取请求请求头信息
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Default"></param>
        /// <returns></returns>
        [NonAction]
        protected string HeaderRequest(string Key, string Default = "")
        {
            var key = Key.ToLower();
            var value = new Microsoft.Extensions.Primitives.StringValues();
            if (Request.Headers.TryGetValue(key, out value))
            {
                Default = value.ToString();
            }
            return Default.Trim();
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
                else if (ua.Contains("alipay"))
                {
                    return "alipay";
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
                var clientIp = (Request.HttpContext.Connection.RemoteIpAddress != null && !Request.HttpContext.Connection.RemoteIpAddress.IsIPv4MappedToIPv6) ? Request.HttpContext.Connection.RemoteIpAddress?.ToString() : Request.HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString();
                if (Request.Headers.TryGetValue("x-forwarded-for", out var forwardedIP))
                {
                    // 通过代理网关部署时，获取"x-forwarded-for"传递的真实IP
                    foreach (var ip in forwardedIP.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (ip != "::1" && ip != "127.0.0.1" && StringUtil.IsIP(ip))
                        {
                            clientIp = ip;
                            break;
                        }
                    }
                }
                return clientIp ?? string.Empty;
            }
        }
        /// <summary>
        /// 当前请求Host（带协议头）
        /// </summary>
        public string UrlHost
        {
            get
            {
                if (string.IsNullOrEmpty(host))
                {
                    host = Config.GetConfigs("WLN_HOST");
                    if (string.IsNullOrEmpty(host))
                    {
                        var webroxy = new Microsoft.Extensions.Primitives.StringValues();
                        if (Request.Headers.TryGetValue("X-Webroxy", out webroxy))
                        {
                            host = webroxy.ToString();
                        }
                        if (string.IsNullOrEmpty(host))
                        {
                            host = Request.Host.Value;
                        }
                    }
                    if (host.IndexOf("://") < 0)
                    {
                        host = (IsHttps ? "https://" : "http://") + host;
                    }
                }
                return host;
            }
        }
        /// <summary>
        /// 当前请求域名
        /// </summary>
        public string UrlDomain
        {
            get
            {
                if (string.IsNullOrEmpty(domain))
                {
                    var webroxy = new Microsoft.Extensions.Primitives.StringValues();
                    if (Request.Headers.TryGetValue("X-Webroxy", out webroxy))
                    {
                        domain = webroxy.ToString();
                    }
                    if (string.IsNullOrEmpty(domain))
                    {
                        domain = Request.Host.Host;
                    }
                }
                return domain;
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
                        trace = Guid.NewGuid().ToString().Replace('-', '\0');
                    }
                }
                return trace;
            }
        }
    }
}