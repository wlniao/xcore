using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Wlniao;

namespace Wlniao.XServer
{
    /// <summary>
    /// TokenApi基础Controller
    /// </summary>
    public class TokenApiController : XCoreController
    {
        /// <summary>
        /// 加密密钥
        /// </summary>
#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
        protected String token = null;
#pragma warning restore CS8625 // 无法将 null 字面量转换为非 null 的引用类型。
        /// <summary>
        /// 默认返回对象
        /// </summary>
        protected ApiResult<Object> result = new() { node = XCore.WebNode, message = "未知错误" };
        /// <summary>
        /// 执行请求校验
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        [NonAction]
        public IActionResult? Check(Func<Dictionary<String, Object>, IActionResult> func)
        {
            try
            {
                var sign = PostRequest("sign");
                var data = PostRequest("data");
                var timestamp = PostRequest("timestamp");
                if (string.IsNullOrEmpty(token))
                {
                    result.data = "200";
                    result.message = "通讯密钥未配置，请先配置token";
                }
                else if (string.IsNullOrEmpty(data))
                {
                    result.data = "202";
                    result.message = "缺少参数，data不能为空";
                }
                else if (string.IsNullOrEmpty(sign))
                {
                    result.data = "202";
                    result.message = "缺少参数，sign不能为空";
                }
                else if (string.IsNullOrEmpty(timestamp))
                {
                    result.data = "202";
                    result.message = "缺少参数，timestamp不能为空";
                }
                else if (cvt.ToLong(timestamp) + 3600 < XCore.NowUnix)
                {
                    result.code = "203";
                    result.message = "请求已过期，请重新发起";
                }
                else if (Encryptor.SM3Encrypt(timestamp + data + token) != sign)
                {
                    result.code = "205";
                    result.message = "请求错误，签名验证失败";
                }
                else
                {
                    var json = Encryptor.SM4DecryptECBFromHex(data, token);
                    if (string.IsNullOrEmpty(json) && !string.IsNullOrEmpty(data))
                    {
                        result.code = "206";
                        result.message = "数据解密失败，请检查通讯密钥";
                    }
                    else
                    {
                        var req = new Dictionary<String, Object>();
                        try
                        {
                            foreach (var kv in Wlniao.Json.ToObject<Dictionary<String, Object>>(json))
                            {
                                req.TryAdd(kv.Key, kv.Value);
                            }
                        }
                        catch { }
                        if (req == null && !string.IsNullOrEmpty(data))
                        {
                            result.code = "207";
                            result.message = "远端收到请求，但反序列化失败";
                        }
                        else
                        {
                            return func?.Invoke(req ?? new Dictionary<string, object>());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.code = "401";
                result.message = ex.Message;
                log.Error(ex.Message);
            }
            return Json(result);
        }
        /// <summary>
        /// 输出数据并标记为成功
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        [NonAction]
        public IActionResult OutSuccess(Object obj)
        {
            result.code = "0";
            result.data = obj;
            result.success = true;
            result.message = "success";
            var txt = string.Empty;
            if (obj == null)
            {
                txt = "";
            }
            else if (obj is string)
            {
                txt = obj.ToString();
            }
            else
            {
                txt = Wlniao.Json.ToString(obj);
            }
            var dic = new Dictionary<string, object>();
            dic.Add("node", result.node);
            dic.Add("code", result.code);
            dic.Add("tips", result.tips);
            dic.Add("data", Encryptor.SM4EncryptECBToHex(txt, token));
            dic.Add("success", result.success);
            dic.Add("message", result.message);
            return Json(dic);
        }
        /// <summary>
        /// 输出默认result对象
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        [NonAction]
        public IActionResult OutSuccess(string str)
        {
            result.code = "0";
            result.success = true;
            result.message = "success";
            var dic = new Dictionary<string, object>();
            dic.Add("node", result.node);
            dic.Add("code", result.code);
            dic.Add("tips", result.tips);
            dic.Add("data", Encryptor.SM4EncryptECBToHex(str, token));
            dic.Add("success", result.success);
            dic.Add("message", result.message);
            return Json(dic);
        }

        /// <summary>
        /// 输出默认result对象
        /// </summary>
        /// <returns></returns>
        [NonAction]
        public IActionResult OutDefault()
        {
            var txt = string.Empty;
            if (result.data == null)
            {
                txt = "";
            }
            else if (result.data is string)
            {
                txt = result.data.ToString();
            }
            else
            {
                txt = Wlniao.Json.ToString(result.data);
            }
            if (string.IsNullOrEmpty(result.code))
            {
                result.code = "1";
            }
            var dic = new Dictionary<string, object>();
            dic.Add("node", result.node);
            dic.Add("code", result.code);
            dic.Add("tips", result.tips);
            dic.Add("data", Encryptor.SM4EncryptECBToHex(txt, token));
            dic.Add("success", result.success);
            dic.Add("message", result.message);
            return Json(dic);
        }

    }
}
