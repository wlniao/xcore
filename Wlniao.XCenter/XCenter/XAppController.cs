using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Net.Http.Headers;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Newtonsoft.Json.Linq;
using Wlniao.Crypto;
using Microsoft.AspNetCore.Http;

namespace Wlniao.XCenter
{
    /// <summary>
    /// XApp扩展的Controller
    /// </summary>
    public class XAppController : XCoreController
    {
        /// <summary>
        /// 临时通讯密钥
        /// </summary>
        private string sm2key = "";
        /// <summary>
        /// 
        /// </summary>
        protected Context ctx = null;
        /// <summary>
        /// 
        /// </summary>
        protected ApiResult<Object> res = new ApiResult<Object> { code = XCore.WebNode, message = "未知错误" };
        /// <summary>
        /// 
        /// </summary>
        protected Dictionary<string, object> auth = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// 页面加载前事件
        /// </summary>
        /// <param name="filterContext"></param>
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var key = "ctx_" + UrlDomain;
            try
            {
                ctx = Wlniao.Cache.Get<XCenter.Context>(key);
                if (ctx == null || string.IsNullOrEmpty(ctx.owner))
                {
                    ctx = Context.Load(UrlDomain);
                    if(!string.IsNullOrEmpty(ctx.owner))
                    {
                        Wlniao.Cache.Set(key, ctx, 300);
                    }
                }
            }
            catch (Exception ex)
            {
                ctx = new Context { message = ex.Message };
            }
            if (string.IsNullOrEmpty(ctx.message))
            {
                base.OnActionExecuting(filterContext);
            }
            else
            {
                var errorPage = new ContentResult();
                if (Request.Method == "POST" || Request.Query.ContainsKey("do"))
                {
                    errorPage.ContentType = "text/json";
                    errorPage.Content = Wlniao.Json.ToString(new { success = false, message = ctx.message });
                }
                else
                {
                    errorPage.ContentType = "text/html;charset=utf-8";
                    errorPage.Content = errorHtml.Replace("{{errorMsg}}", ctx.message).Replace("{{errorTitle}}", errorTitle).Replace("{{errorIcon}}", errorIcon);
                }
                filterContext.Result = errorPage;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [NonAction]
        public IActionResult OutDefault()
        {
            var dic = new Dictionary<string, object>();
            dic.Add("success", res.success);
            dic.Add("message", res.message);
            if (!string.IsNullOrEmpty(res.code))
            {
                dic.Add("code", res.code);
            }
            if (res.data != null)
            {
                if (string.IsNullOrEmpty(sm2key))
                {
                    dic.Add("data", res.data);
                }
                else if (res.data is string)
                {
                    dic.Add("data", Encryptor.SM4EncryptECBToHex(res.data.ToString(), sm2key, true));
                    dic.Add("encrypt", true);
                }
                else
                {
                    dic.Add("data", Encryptor.SM4EncryptECBToHex(JsonSerializer.Serialize(res.data, new JsonSerializerOptions
                    {
                        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) //Json序列化的时候对中文进行处理
                    }), sm2key, true));
                }
            }
            return Json(dic);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        [NonAction]
        public IActionResult Handle(Action<Dictionary<String, Object>> func)
        {
            var sm2token = HeaderRequest("sm2token");
            if (!string.IsNullOrEmpty(sm2token) && Context.XCenterPublicKey != null)
            {
                sm2key = Wlniao.Encryptor.SM2DecryptByPrivateKey(Helper.Decode(sm2token), Context.XCenterPublicKey);
            }
            if (!string.IsNullOrEmpty(sm2key) && sm2key.Length != 16)
            {
                sm2key = null;
                res.code = "103";
                res.message = "server sm2token private key error";
            }
            else
            {
                var obj = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                try
                {
                    var body = GetPostString();
                    if (!string.IsNullOrEmpty(sm2key))
                    {
                        body = Encryptor.SM4DecryptECBFromHex(body, sm2key, true);
                    }
                    if (!string.IsNullOrEmpty(body))
                    {
                        foreach (var kv in Wlniao.Json.ToObject<Dictionary<String, Object>>(body))
                        {
                            obj.TryAdd(kv.Key, kv.Value);
                        }
                    }
                }
                catch { }
                func?.Invoke(obj);
            }
            return OutDefault();
        }

        /// <summary>
        /// 验证登录状态并生成调用参数
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        [NonAction]
        public IActionResult HandleByLogin(Action<Dictionary<String, Object>> func)
        {
            var authorization = HeaderRequest("Authorization");
            if (string.IsNullOrEmpty(authorization))
            {
                authorization = PostRequest("authorization");
            }
            try
            {
                var plainData = Encryptor.SM4DecryptECBFromHex(authorization, ctx.token, true);
                if (!string.IsNullOrEmpty(plainData))
                {
                    foreach (var kv in Wlniao.Json.ToObject<Dictionary<string, object>>(plainData))
                    {
                        auth.TryAdd(kv.Key, kv.Value);
                    }
                }
            }
            catch { }
            if (auth.Count == 0 || auth.GetInt64("expire") < XCore.NowUnix)
            {
                if (auth.Count == 0)
                {
                    res.code = "102";
                    res.message = string.IsNullOrEmpty(authorization) ? "authorization is missing" : "authorization is error";
                }
                else
                {
                    res.code = "103";
                    res.message = "authorization is expire";
                }
                Response.Headers.TryAdd("Access-Control-Expose-Headers", new Microsoft.Extensions.Primitives.StringValues("Authify-State"));
                Response.Headers.TryAdd("Authify-State", new Microsoft.Extensions.Primitives.StringValues("false"));
                return OutDefault();
            }
            else
            {
                return Handle(func);
            }
        }
    }
}