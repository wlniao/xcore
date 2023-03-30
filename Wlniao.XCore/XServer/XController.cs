using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Wlniao;

namespace Wlniao.XServer
{
    /// <summary>
    /// XServer基础Controller
    /// </summary>
    public class XController : Controller
    {
        private static readonly Dictionary<String, XsApp> _cache = new();
        /// <summary>
        /// 当前客户端的App信息
        /// </summary>
        protected XServer.XsApp app = null;
        /// <summary>
        /// 默认返回的ApiResult对象
        /// </summary>
        protected ApiResult<String> _rlt = new();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="filterContext"></param>
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            _rlt.success = false;
            _rlt.message = "";
            _rlt.data = "";
            var AppId = GetRequest("xsappid");
            var TimeSpan = GetRequest("xstime");
            if (string.IsNullOrEmpty(AppId))
            {
                _rlt.message = "xsappid is missing";
                filterContext.Result = new ContentResult { Content = Wlniao.Json.ToString(_rlt) };
            }
            else if (cvt.ToLong(TimeSpan) < XCore.NowUnix - 3600)
            {
                _rlt.message = "request is expired";
                filterContext.Result = new ContentResult() { Content = Wlniao.Json.ToString(_rlt) };
            }
            else
            {
                if (_cache.ContainsKey(AppId))
                {
                    app = _cache[AppId];
                }
                else
                {
                    if (AppId == XServer.Common.AppId)
                    {
                        //使用同一对AppId及Secret
                        app = new XsApp
                        {
                            appid = XServer.Common.AppId,
                            secret = XServer.Common.Secret,
                            domain = "",
                            xclient = false,
                            appname = "Default"
                        };
                    }
                    else
                    {
                        var rlt = XServer.XsApp.GetById(AppId);
                        if (rlt.success)
                        {
                            app = rlt.data;
                            _rlt.PutLog(rlt.logs);
                            if (app.xclient && !filterContext.ActionDescriptor.FilterDescriptors.Any(a => a.Filter.ToString().Contains("Wlniao.Mvc.XClientAttribute")))
                            {
                                _rlt.message = "not allow xclient app";
                                filterContext.Result = new ContentResult() { Content = Wlniao.Json.ToString(_rlt) };
                                return;
                            }
                        }
                    }
                }
                if (app == null || app.appid.IsNullOrEmpty())
                {
                    _rlt.message = "xsappid is invalid";
                    filterContext.Result = new ContentResult() { Content = Wlniao.Json.ToString(_rlt) };
                }
                else
                {
                    var list = new List<KeyValuePair<String, String>>();
                    foreach (string key in Request.Query.Keys)
                    {
                        list.Add(new KeyValuePair<string, string>(key, Request.Query[key]));
                    }
                    list.Sort(delegate (KeyValuePair<String, String> small, KeyValuePair<String, String> big) { return small.Key.CompareTo(big.Key); });
                    var values = new System.Text.StringBuilder();
                    foreach (var param in list)
                    {
                        if (!string.IsNullOrEmpty(param.Value) && param.Key != "sig")
                        {
                            values.Append(param.Value);
                        }
                    }
                    values.Append(app.secret);
                    var md5_result = System.Security.Cryptography.MD5.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes(values.ToString()));
                    var sig_builder = new System.Text.StringBuilder();
                    foreach (byte b in md5_result)
                    {
                        sig_builder.Append(b.ToString("x2"));
                    }
                    if (GetRequest("sig") != sig_builder.ToString())
                    {
                        if (_cache.ContainsKey(AppId))
                        {
                            _cache.Remove(AppId);
                        }
                        _rlt.message = "signature error";
                        filterContext.Result = new ContentResult() { Content = Wlniao.Json.ToString(_rlt) };
                    }
                    else if (!_cache.ContainsKey(AppId))
                    {
                        _cache.Add(AppId, app);
                    }
                }
            }
            base.OnActionExecuting(filterContext);
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
            if (string.IsNullOrEmpty(GetRequest("callback")))
            {
                return Content(jsonStr, "text/json", System.Text.Encoding.UTF8);
            }
            else
            {
                return Content(GetRequest("callback") + "(" + jsonStr + ")", "text/javascript", System.Text.Encoding.UTF8);
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
            if (string.IsNullOrEmpty(GetRequest("callback")))
            {
                return Content(jsonStr, "text/json", encoding ?? System.Text.Encoding.UTF8);
            }
            else
            {
                return Content(GetRequest("callback") + "(" + jsonStr + ")", "text/javascript", encoding ?? System.Text.Encoding.UTF8);
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
            return Content(jsonStr, "text/json", System.Text.Encoding.UTF8);
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
            return Content(jsonStr, "text/json", encoding ?? System.Text.Encoding.UTF8);
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
                    if (!string.IsNullOrEmpty(Default) && Default.Contains('%'))
                    {
                        Default = strUtil.UrlDecode(Default);
                    }
                    return Default;
                }
            }
            return Default;
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
            var key = Key.ToLower();
            foreach (var item in Request.Query.Keys)
            {
                if (item.ToLower() == key && !string.IsNullOrEmpty(Request.Query[key]))
                {
                    Default = Request.Query[item].ToString().Trim();
                    if (!string.IsNullOrEmpty(Default) && Default.Contains('%'))
                    {
                        Default = strUtil.UrlDecode(Default);
                    }
                    return Default.Trim();
                }
            }
            return Default.Trim();
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
        private string strPost = null;
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
    }
}
