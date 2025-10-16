using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Wlniao.Text;

namespace Wlniao.XServer
{
    /// <summary>
    /// XServer基础Controller
    /// </summary>
    public class XController : ControllerBase
    {
        /// <summary>
        /// 当前客户端的App信息
        /// </summary>
#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
        protected XsApp App = null;
#pragma warning restore CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
        /// <summary>
        /// 默认返回的ApiResult对象
        /// </summary>
        protected ApiResult<string> Rlt = new();
//         /// <summary>
//         /// 
//         /// </summary>
//         /// <param name="filterContext"></param>
//         public override void OnActionExecuting(ActionExecutingContext filterContext)
//         {
//             _rlt.success = false;
//             _rlt.message = "";
//             _rlt.data = "";
//             var AppId = GetRequest("xsappid");
//             var TimeSpan = GetRequest("xstime");
//             if (string.IsNullOrEmpty(AppId))
//             {
//                 _rlt.message = "xsappid is missing";
//                 filterContext.Result = new ContentResult { Content = Wlniao.Json.Serialize(_rlt) };
//             }
//             else if (Convert.ToLong(TimeSpan) < DateTools.GetUnix() - 3600)
//             {
//                 _rlt.message = "request is expired";
//                 filterContext.Result = new ContentResult() { Content = Wlniao.Json.Serialize(_rlt) };
//             }
//             else
//             {
//                 if (_cache.ContainsKey(AppId))
//                 {
//                     app = _cache[AppId];
//                 }
//                 else
//                 {
//                     if (AppId == XServer.Common.AppId)
//                     {
//                         //使用同一对AppId及Secret
//                         app = new XsApp
//                         {
//                             appid = XServer.Common.AppId,
//                             secret = XServer.Common.Secret,
//                             domain = "",
//                             xclient = false,
//                             appname = "Default"
//                         };
//                     }
//                     else
//                     {
//                         var rlt = XServer.XsApp.GetById(AppId);
//                         if (rlt.success)
//                         {
//                             app = rlt.data;
// #pragma warning disable CS8602 // 解引用可能出现空引用。
//                             if (app.xclient && !filterContext.ActionDescriptor.FilterDescriptors.Any(a => a.Filter.ToString().Contains("Wlniao.Mvc.XClientAttribute")))
//                             {
//                                 _rlt.message = "not allow xclient app";
//                                 filterContext.Result = new ContentResult() { Content = Wlniao.Json.Serialize(_rlt) };
//                                 return;
//                             }
// #pragma warning restore CS8602 // 解引用可能出现空引用。
//                         }
//                     }
//                 }
//                 if (app == null || app.appid.IsNullOrEmpty())
//                 {
//                     _rlt.message = "xsappid is invalid";
//                     filterContext.Result = new ContentResult() { Content = Wlniao.Json.Serialize(_rlt) };
//                 }
//                 else
//                 {
//                     var list = new List<KeyValuePair<string, string>>();
//                     foreach (string key in Request.Query.Keys)
//                     {
//                         list.Add(new KeyValuePair<string, string>(key, Request.Query[key].ToString()));
//                     }
//                     list.Sort(delegate (KeyValuePair<string, string> small, KeyValuePair<string, string> big) { return small.Key.CompareTo(big.Key); });
//                     var values = new System.Text.StringBuilder();
//                     foreach (var param in list)
//                     {
//                         if (!string.IsNullOrEmpty(param.Value) && param.Key != "sig")
//                         {
//                             values.Append(param.Value);
//                         }
//                     }
//                     values.Append(app.secret);
//                     var md5_result = System.Security.Cryptography.MD5.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes(values.ToString()));
//                     var sig_builder = new System.Text.StringBuilder();
//                     foreach (byte b in md5_result)
//                     {
//                         sig_builder.Append(b.ToString("x2"));
//                     }
//                     if (GetRequest("sig") != sig_builder.ToString())
//                     {
//                         if (_cache.ContainsKey(AppId))
//                         {
//                             _cache.Remove(AppId);
//                         }
//                         _rlt.message = "signature error";
//                         filterContext.Result = new ContentResult() { Content = Wlniao.Json.Serialize(_rlt) };
//                     }
//                     else if (!_cache.ContainsKey(AppId))
//                     {
//                         _cache.Add(AppId, app);
//                     }
//                 }
//             }
//             base.OnActionExecuting(filterContext);
//         }
        /// <summary>
        /// Object输出
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [NonAction]
        public ActionResult Json(object? data)
        {
            var jsonStr = "";
            if (data != null)
            {
                jsonStr = data is string ? data.ToString() : Wlniao.Json.Serialize(data);
            }
            return Content(jsonStr ?? "", "text/json", System.Text.Encoding.UTF8);
        }
        /// <summary>
        /// Object输出
        /// </summary>
        /// <param name="data"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        [NonAction]
        public ActionResult Json(object? data, System.Text.Encoding? encoding)
        {
            var jsonStr = "";
            if (data != null)
            {
                jsonStr = data is string ? data.ToString() : Wlniao.Json.Serialize(data);
            }
            return Content(jsonStr ?? "", "text/json", encoding ?? System.Text.Encoding.UTF8);
        }
        /// <summary>
        /// Json字符串输出
        /// </summary>
        /// <param name="jsonStr"></param>
        /// <returns></returns>
        [NonAction]
        public ActionResult JsonStr(string jsonStr)
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
        public ActionResult JsonStr(string jsonStr, System.Text.Encoding? encoding)
        {
            return Content(jsonStr, "text/json", encoding ?? System.Text.Encoding.UTF8);
        }

        /// <summary>
        /// 获取请求参数Get及Post
        /// </summary>
        /// <param name="key"></param>
        /// <param name="default"></param>
        /// <returns></returns>
        [NonAction]
        protected string GetRequestNoSecurity(string key, string @default = "")
        {
            var k = key.ToLower();
            foreach (var item in Request.Query.Keys)
            {
                if (item.ToLower() != k || string.IsNullOrEmpty(Request.Query[k]))
                {
                    continue;
                }
                @default = Request.Query[item].ToString().Trim();
                if (!string.IsNullOrEmpty(@default) && @default.Contains('%'))
                {
                    @default = StringUtil.UrlDecode(@default);
                }
                return @default;
            }
            return @default;
        }
        /// <summary>
        /// 获取请求参数（仅标记但不过滤非安全字符）
        /// </summary>
        /// <param name="key"></param>
        /// <param name="default"></param>
        /// <returns></returns>
        [NonAction]
        protected string GetRequest(string key, string @default = "")
        {
            var k = key.ToLower();
            foreach (var item in Request.Query.Keys)
            {
                if (item.ToLower() != k || string.IsNullOrEmpty(Request.Query[k]))
                {
                    continue;
                }
                @default = Request.Query[item].ToString().Trim();
                if (!string.IsNullOrEmpty(@default) && @default.Contains('%'))
                {
                    @default = StringUtil.UrlDecode(@default);
                }
                return @default.Trim();
            }
            return @default.Trim();
        }
        /// <summary>
        /// 获取请求参数
        /// </summary>
        /// <param name="key"></param>
        /// <param name="default"></param>
        /// <returns></returns>
        [NonAction]
        protected string GetRequestDecode(string key, string @default = "")
        {
            return GetRequest(key, @default);
        }
        /// <summary>
        /// 获取请求参数
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        [NonAction]
        protected int GetRequestInt(string key)
        {
            return Convert.ToInt(GetRequest(key, "0"));
        }
        /// <summary>
        /// 获取Post的文本内容
        /// </summary>
        /// <returns></returns>
        [NonAction]
        protected string GetPostString()
        {
            if (_strPost == null && Request.Method == "POST" && Request.ContentLength > 0 && (Request.ContentType == null || !Request.ContentType.Contains("form")))
            {
                try
                {
                    _strPost = new System.IO.StreamReader(Request.Body).ReadToEnd();
                }
                catch
                {
                    var buffer = new byte[(int)Request.ContentLength];
                    Request.Body.ReadExactly(buffer, 0, buffer.Length);
                    _strPost = System.Text.Encoding.UTF8.GetString(buffer);
                }
            }
            return _strPost ?? "";
        }
#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
        private string? _strPost = null;
#pragma warning restore CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
        private Dictionary<string, string>? _ctxPost = null;
#pragma warning restore CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
        /// <summary>
        /// 获取请求参数（仅标记但不过滤非安全字符）
        /// </summary>
        /// <param name="key"></param>
        /// <param name="default"></param>
        /// <returns></returns>
        [NonAction]
        protected string PostRequest(string key, string @default = "")
        {
            var k = key.ToLower();
            if (_ctxPost == null)
            {
                try
                {
                    _ctxPost = new Dictionary<string, string>();
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
                                        _strPost = new System.IO.StreamReader(Request.Body).ReadToEnd();
                                    }
                                    catch
                                    {
                                        var buffer = new byte[(int)Request.ContentLength];
                                        Request.Body.ReadExactly(buffer, 0, buffer.Length);
                                        _strPost = System.Text.Encoding.UTF8.GetString(buffer);
                                    }
                                }
                                if (!string.IsNullOrEmpty(_strPost))
                                {
                                    var tmpPost = Wlniao.Json.Deserialize<Dictionary<string, string>>(_strPost);
                                    if (tmpPost != null)
                                    {
                                        foreach (var kv in tmpPost)
                                        {
                                            _ctxPost.TryAdd(kv.Key.ToLower(), kv.Value == null ? "" : kv.Value.Trim());
                                        }
                                    }
                                }
                                #endregion 请求为其它类型
                            }
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                    if (Request.Query != null) //叠加URL传递的参数
                    {
                        foreach (var item in Request.Query.Keys)
                        {
                            _ctxPost.TryAdd(item.ToLower(), StringUtil.UrlDecode(Request.Query[item].ToString().Trim()));
                        }
                    }
                }
                catch
                {
                    // ignored
                }
            }
            if (_ctxPost != null && _ctxPost.ContainsKey(k) && !string.IsNullOrEmpty(_ctxPost[k]))
            {
                @default = _ctxPost[k];
            }
            return @default.Trim();
        }

        /// <summary>
        /// 获取请求参数
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        [NonAction]
        protected int PostRequestInt(string key)
        {
            return Convert.ToInt(PostRequest(key, "0"));
        }
    }
}
