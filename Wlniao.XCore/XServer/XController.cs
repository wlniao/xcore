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
        private static Dictionary<String, XsApp> _cache = new Dictionary<String, XsApp>();
        /// <summary>
        /// 当前客户端的App信息
        /// </summary>
        protected XServer.XsApp app = null;
        /// <summary>
        /// 默认返回的ApiResult对象
        /// </summary>
        protected ApiResult<String> _rlt = new ApiResult<String>();
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
                var result = new ContentResult();
                result.Content = Wlniao.Json.ToString(_rlt);
                filterContext.Result = result;
            }
            else if (cvt.ToLong(TimeSpan) < DateTools.GetUnix() - 3600)
            {
                _rlt.message = "request is expired";
                var result = new ContentResult();
                result.Content = Wlniao.Json.ToString(_rlt);
                filterContext.Result = result;
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
                        app = new XsApp();
                        app.appid = XServer.Common.AppId;
                        app.secret = XServer.Common.Secret;
                        app.domain = "";
                        app.xclient = false;
                        app.appname = "Default";
                    }
                    else
                    {
                        var rlt = XServer.XsApp.GetById(AppId);
                        if (rlt.success)
                        {
                            app = rlt.data;
                            _rlt.PutLog(rlt.logs);
                            if (app.xclient && filterContext.ActionDescriptor.FilterDescriptors.Where(a => a.Filter.ToString().Contains("Wlniao.Mvc.XClientAttribute")).ToList().Count() == 0)
                            {
                                _rlt.message = "not allow xclient app";
                                var result = new ContentResult();
                                result.Content = Wlniao.Json.ToString(_rlt);
                                filterContext.Result = result;
                                return;
                            }
                        }
                    }
                }
                if (app == null || app.appid.IsNullOrEmpty())
                {
                    _rlt.message = "xsappid is invalid";
                    var result = new ContentResult();
                    result.Content = Wlniao.Json.ToString(_rlt);
                    filterContext.Result = result;
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
                        var result = new ContentResult();
                        result.Content = Wlniao.Json.ToString(_rlt);
                        filterContext.Result = result;
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
        /// 获取请求参数
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Default"></param>
        /// <returns></returns>
        [NonAction]
        protected String GetRequest(String Key, String Default = "")
        {
            try
            {
                var key = Key.ToLower();
                foreach (var item in Request.Query.Keys)
                {
                    if (item.ToLower() == key)
                    {
                        Default = Request.Query[item];
                    }
                }
                //try
                //{
                //    if (Request.Method.ToUpper() == "POST" && Request.Form.Keys != null && string.IsNullOrEmpty(Default))
                //    {
                //        foreach (var item in Request.Form.Keys)
                //        {
                //            if (item.ToLower() == key)
                //            {
                //                Default = Request.Form[item];
                //            }
                //        }
                //    }
                //    else
                //    {
                //        Default = System.Text.RegularExpressions.Regex.Replace(Default, "<[^>]*>", "");
                //    }
                //}
                //catch { }
            }
            catch { }
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
            var str = GetRequest(Key, Default);
            if (str.IsNullOrEmpty())
            {
                return Default;
            }
            else
            {
                return strUtil.UrlDecode(str);
            }
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
            if (Request.Method == "POST")
            {
                var buffer = new byte[(int)Request.ContentLength];
                Request.Body.Read(buffer, 0, buffer.Length);
                return System.Text.Encoding.UTF8.GetString(buffer);
            }
            return "";
        }
        /// <summary>
        /// 获取当前Cookie（非服务端可用）
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetCookies(String key)
        {
            key = key.ToLower();
            foreach (var item in Request.Query.Keys)
            {
                if (item.ToLower() == key)
                {
                    return Request.Query[item];
                }
            }
            return "";
        }
    }
}
