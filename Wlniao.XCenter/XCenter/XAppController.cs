using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Hosting;

namespace Wlniao.XCenter
{
    /// <summary>
    /// XApp扩展的Controller
    /// </summary>
    public class XAppController : XCoreController
    {
        /// <summary>
        /// 主平台接口访问工具
        /// </summary>
        internal Context ctx = null;
        /// <summary>
        /// 主平台登录会话状态
        /// </summary>
        internal XSession xsession = null;
        /// <summary>
        /// 会话加密密钥
        /// </summary>
        internal String sm4key = null;


        /// <summary>
        /// 检查系统使用授权
        /// </summary>
        /// <param name="func"></param>
        /// <param name="fail"></param>
        /// <param name="host"></param>
        /// <returns></returns>
        [NonAction]
        public IActionResult CheckAuth(Func<Context, IActionResult> func, Func<IActionResult> fail = null, String host = null)
        {
            if (fail == null)
            {
                fail = new Func<IActionResult>(() =>
                {
                    if (Request.Method == "POST" || (Request.Query != null && Request.Query.ContainsKey("do")))
                    {
                        return OutputSerialize(new ApiResult<string> { message = ctx?.message });
                    }
                    else
                    {
                        var errorPage = new ContentResult();
                        errorPage.ContentType = "text/html;charset=utf-8";
                        errorPage.Content = errorHtml.Replace("{{errorMsg}}", ctx?.message).Replace("{{errorTitle}}", errorTitle).Replace("{{errorIcon}}", errorIcon);
                        return errorPage;
                    }
                });
            }
            if (string.IsNullOrEmpty(host) && string.IsNullOrEmpty(Context.XCenterDomain))
            {
                host = HeaderRequest("x-domain", UrlDomain);
            }
            ctx = Context.Load(host);
            if (ctx == null || !string.IsNullOrEmpty(ctx.message) || string.IsNullOrEmpty(ctx.owner) || (string.IsNullOrEmpty(ctx.app) && string.IsNullOrEmpty(ctx.domain)))
            {
                return fail.Invoke();
            }
            return func.Invoke(ctx);
        }

        /// <summary>
        /// 检查用户登录状态
        /// </summary>
        /// <param name="func"></param>
        /// <param name="fail"></param>
        /// <param name="ticket"></param>
        /// <param name="addHeader"></param>
        /// <returns></returns>
        [NonAction]
        public IActionResult CheckSession(Func<XSession, Context, IActionResult> func, Func<IActionResult> fail = null, String ticket = null, Boolean addHeader = true)
        {
            if (fail == null)
            {
                fail = new Func<IActionResult>(() =>
                {
                    // Authify平台授权加载失败时执行
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
            return CheckAuth((ctx) =>
            {
                if (string.IsNullOrEmpty(ticket))
                {
                    ticket = HeaderRequest("Authorization");
                }
                xsession = new XSession(ctx, ticket);
                if (xsession.IsValid && xsession.OwnerId == ctx.owner)
                {
                    return func.Invoke(xsession, ctx);
                }
                else
                {
                    if (string.IsNullOrEmpty(ticket))
                    {
                        errorMsg = "authorization is missing";
                    }
                    else if (xsession.ExpireTime < XCore.NowUnix && !string.IsNullOrEmpty(xsession.UserSid))
                    {
                        errorMsg = "authorization is expire";
                    }
                    else
                    {
                        errorMsg = "authorization is error";
                    }
                    if (addHeader)
                    {
                        AddHeaderWithLogout();
                    }
                    return fail.Invoke();
                }
            }, fail, null);
        }


        /// <summary>
        /// 检查用户登录状态
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="ticket"></param>
        /// <returns></returns>
        [NonAction]
        public XSession CheckSessionBack(Context? ctx, String ticket = null)
        {
            if (ctx == null)
            {
                var host = "";
                if (string.IsNullOrEmpty(host) && string.IsNullOrEmpty(Context.XCenterDomain))
                {
                    host = HeaderRequest("x-domain", UrlDomain);
                }
                ctx = Context.Load(host);
            }
            if (ctx == null || string.IsNullOrEmpty(ctx.owner) || !string.IsNullOrEmpty(ctx.message) || (string.IsNullOrEmpty(ctx.app) && string.IsNullOrEmpty(ctx.domain)))
            {
                return new XSession(ctx);
            }
            else
            {
                if (string.IsNullOrEmpty(ticket))
                {
                    ticket = HeaderRequest("Authorization");
                }
                return new XSession(ctx, ticket);
            }
        }
        /// <summary>
        /// 添加要求客户端登录的头信息
        /// </summary>
        [NonAction]
        public void AddHeaderWithLogout()
        {
            Response.Headers.TryAdd("Access-Control-Expose-Headers", new Microsoft.Extensions.Primitives.StringValues("*"));
            Response.Headers.TryAdd("Authify-State", new Microsoft.Extensions.Primitives.StringValues("false"));
        }


        /// <summary>
        /// 请求内容解析
        /// </summary>
        /// <param name="sm4key"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [NonAction]
        public Dictionary<String, Object> InputDeserialize(String sm4key = null)
        {
            var result = new Dictionary<String, Object>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrEmpty(sm4key))
            {
                this.sm4key = sm4key;
            }
            try
            {
                if (Request.Query != null)
                {
                    foreach (var item in Request.Query)
                    {
                        result.TryAdd(item.Key, item.Value);
                    }
                }
                if (Request.Form != null && Request.Form.Count > 0)
                {
                    foreach (var item in Request.Form)
                    {
                        result.TryAdd(item.Key, item.Value);
                    }
                }
            }
            catch { }
            var input = GetPostString();
            if (string.IsNullOrEmpty(input))
            {
                return result;
            }
            else if (string.IsNullOrEmpty(this.sm4key))
            {
                var sm2token = HeaderRequest("sm2token");
                if (!string.IsNullOrEmpty(sm2token) && Context.XCenterPrivkey != null)
                {
                    this.sm4key = Wlniao.Encryptor.SM2DecryptByPrivateKey(Crypto.Helper.Decode(sm2token), Context.XCenterPrivkey);
                }
            }
            if (!string.IsNullOrEmpty(this.sm4key))
            {
                input = Wlniao.Encryptor.SM4DecryptECBFromHex(input, this.sm4key, true);
                if (string.IsNullOrEmpty(input))
                {
                    DebugMessage("Request message decrypt faild!!");
                    throw new Exception("请求内容解密失败");
                }
            }
            try
            {
                var obj = JsonSerializer.Deserialize<Dictionary<String, Object>>(input, new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
                });
                if (obj != null)
                {
                    foreach (var kv in obj)
                    {
                        result.TryAdd(kv.Key, kv.Value);
                    }
                }
            }
            catch { }
            return result;
        }

        /// <summary>
        /// 请求内容反序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [NonAction]
        public T InputDeserialize<T>(String sm4key = null)
        {
            if (!string.IsNullOrEmpty(sm4key))
            {
                this.sm4key = sm4key;
            }
            var input = GetPostString();
            if (string.IsNullOrEmpty(input))
            {
                return default(T);
            }
            else if (string.IsNullOrEmpty(this.sm4key))
            {
                var sm2token = HeaderRequest("sm2token");
                if (!string.IsNullOrEmpty(sm2token) && Context.XCenterPrivkey != null)
                {
                    this.sm4key = Wlniao.Encryptor.SM2DecryptByPrivateKey(Crypto.Helper.Decode(sm2token), Context.XCenterPrivkey);
                }
            }
            if (!string.IsNullOrEmpty(this.sm4key))
            {
                input = Wlniao.Encryptor.SM4DecryptECBFromHex(input, this.sm4key, true);
                if (string.IsNullOrEmpty(input))
                {
                    DebugMessage("Request message decrypt faild!!");
                    throw new Exception("请求内容解密失败");
                }
            }
            return JsonSerializer.Deserialize<T>(input, new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
            });
        }


        /// <summary>
        /// 输出内容序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [NonAction]
        public IActionResult OutputSerialize<T>(ApiResult<T> result)
        {
            var output = "";
            var option = new JsonSerializerOptions { Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) };
            if (string.IsNullOrEmpty(this.sm4key))
            {
                output = JsonSerializer.Serialize<ApiResult<T>>(result, option);
            }
            else
            {
                var tmp = new ApiResult<String>
                {
                    code = result.code,
                    node = result.node,
                    tips = result.tips,
                    traceid = result.traceid,
                    message = result.message,
                    success = result.success,
                };
                if (result.data is string)
                {
                    tmp.data = Encryptor.SM4EncryptECBToHex(result.data.ToString(), this.sm4key, true);
                }
                else if (result.data != null)
                {
                    var json = JsonSerializer.Serialize<T>(result.data, option);
                    tmp.data = Encryptor.SM4EncryptECBToHex(json, this.sm4key, true);
                }
                output = JsonSerializer.Serialize<ApiResult<String>>(tmp, option);
            }
            if (result != null && !string.IsNullOrEmpty(result.debuger))
            {
                DebugMessage(result.debuger);
            }
            return JsonStr(output);
        }


        /// <summary>
        /// 输出内容序列化消息内容
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="result"></param>
        /// <param name="message"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        [NonAction]
        public IActionResult OutputMessage<T>(ApiResult<T> result, String message, String code = null)
        {
            result.tips = true;
            result.message = message;
            if (!string.IsNullOrEmpty(code))
            {
                result.code = code;
            }

            var output = "";
            var option = new JsonSerializerOptions { Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) };
            if (string.IsNullOrEmpty(this.sm4key))
            {
                output = JsonSerializer.Serialize<ApiResult<T>>(result, option);
            }
            else
            {
                var tmp = new ApiResult<String>
                {
                    code = result.code,
                    node = result.node,
                    tips = result.tips,
                    traceid = result.traceid,
                    message = result.message,
                    success = result.success,
                };
                if (result.data is string)
                {
                    tmp.data = Encryptor.SM4EncryptECBToHex(result.data.ToString(), this.sm4key, true);
                }
                else if (result.data != null)
                {
                    var json = JsonSerializer.Serialize<T>(result.data, option);
                    tmp.data = Encryptor.SM4EncryptECBToHex(json, this.sm4key, true);
                }
                output = JsonSerializer.Serialize<ApiResult<String>>(tmp, option);
            }
            if (result != null && !string.IsNullOrEmpty(result.debuger))
            {
                DebugMessage(result.debuger);
            }
            return JsonStr(output);
        }

    }
}