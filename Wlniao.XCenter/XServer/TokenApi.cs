/*==============================================================================
    文件名称：TokenApi.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：TokenApi服务端请求工具
================================================================================
 
    Copyright 2022 XieChaoyi

    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at

               http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.

===============================================================================*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using Wlniao.Log;
using Wlniao.OpenApi;

namespace Wlniao.XServer
{
    /// <summary>
    /// TokenApi服务端请求工具
    /// </summary>
    public class TokenApi
    {
        /// <summary>
        /// 获取平台接口数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="apinode"></param>
        /// <param name="url"></param>
        /// <param name="token"></param>
        /// <param name="data"></param>
        /// <param name="traceid"></param>
        /// <returns></returns>
        public static Wlniao.ApiResult<T> Request<T>(String apinode, String url, String token, Object data, String traceid = "")
        {
            var now = DateTools.GetUnix().ToString();
            var rlt = new Wlniao.ApiResult<T>();
            if (string.IsNullOrEmpty(token))
            {
                rlt.code = "100";
                rlt.message = "本地通讯密钥未配置，无法发起API请求";
            }
            else
            {
                if (string.IsNullOrEmpty(traceid))
                {
                    traceid = strUtil.CreateLongId();
                }
                var uri = new Uri(url);
                var txt = data is String ? data.ToString() : Json.ToString(data);
                var utime = "";
                var start = DateTime.Now;
                var encdata = Wlniao.Encryptor.SM4EncryptECBToHex(txt, token);
                var resStr = "";
                var reqStr = Json.ToString(new { sign = Encryptor.SM3Encrypt(now + encdata + token), data = encdata, trace = traceid, timestamp = now });
                try
                {
                    var stream = cvt.ToStream(System.Text.Encoding.UTF8.GetBytes(reqStr));
                    var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = XCore.ServerCertificateCustomValidationCallback };
                    using (var client = new HttpClient(handler))
                    {
                        Loger.Topic(apinode, "msgid:" + traceid + ", " + url + "\n >>> " + reqStr, Log.LogLevel.Information, false);
                        var request = new HttpRequestMessage(HttpMethod.Post, uri);
                        request.Headers.Date = DateTime.Now;
                        request.Content = new StreamContent(stream);
                        request.Content.Headers.Add("Content-Type", "application/json");
                        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Wlniao/XCore");
                        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Wlniao-Trace", traceid);
                        var respose = client.Send(request);
                        resStr = respose.Content.ReadAsStringAsync().Result;
                        if (respose.Headers.Contains("X-Wlniao-Trace"))
                        {
                            rlt.traceid = respose.Headers.GetValues("X-Wlniao-Trace").FirstOrDefault();
                        }
                        if (respose.Headers.Contains("X-Wlniao-UseTime"))
                        {
                            utime = respose.Headers.GetValues("X-Wlniao-UseTime").FirstOrDefault();
                        }
                        if (string.IsNullOrEmpty(utime))
                        {
                            utime = DateTime.Now.Subtract(start).TotalMilliseconds.ToString("F2") + "ms";
                        }
                        Loger.Topic(apinode, $"msgid:{traceid}, {url}[usetime:{utime}]{Environment.NewLine} <<< {resStr}", Log.LogLevel.Information, false);
                    }
                }
                catch (Exception ex)
                {
                    utime = DateTime.Now.Subtract(start).TotalMilliseconds.ToString("F2") + "ms";
                    Loger.Topic(apinode, $"msgid:{traceid}, {url}[usetime:{utime}]{Environment.NewLine} <<< 请确认接口访问是否正常： => {ex.Message}", Wlniao.Log.LogLevel.Error, true);
                }
                if (string.IsNullOrEmpty(utime))
                {
                    utime = DateTime.Now.Subtract(start).TotalMilliseconds.ToString("F2") + "ms";
                }
                var logs = $"msgid:{traceid}, {uri.AbsolutePath}[usetime:{utime}]{Environment.NewLine} >>> {txt}";
                if (string.IsNullOrEmpty(resStr))
                {
                    rlt.tips = true;
                    rlt.code = "101";
                    rlt.debuger = url + "[Error]";
                    rlt.message = "通讯异常，网络异常或请求错误";
                    logs += "\n <<< " + rlt.message;
                }
                else
                {
                    Wlniao.ApiResult<String> resObj = null;
                    try
                    {
                        resObj = Json.ToObject<Wlniao.ApiResult<String>>(resStr);
                    }
                    catch { }
                    if (resObj == null || (string.IsNullOrEmpty(resObj.node) && string.IsNullOrEmpty(resObj.code)))
                    {
                        rlt.code = "104";
                        rlt.message = "远端输出格式不满足本地要求";
                        logs += "\n <<< " + rlt.message;
                        //log.Origin(apinode, "msgid:" + traceid + "," + uri.AbsolutePath + "[" + DateTime.Now.Subtract(start).TotalMilliseconds.ToString("F2") + "ms]\n <<< " + rlt.message);
                    }
                    else
                    {
                        rlt.node = resObj.node;
                        rlt.code = resObj.code;
                        rlt.tips = resObj.tips;
                        rlt.message = resObj.message;
                        rlt.debuger = resObj.debuger;
                        rlt.success = resObj.success;
                        if (!string.IsNullOrEmpty(resObj.traceid))
                        {
                            rlt.traceid = resObj.traceid;
                        }
                        var plaintext = string.IsNullOrEmpty(resObj.data) ? "" : Wlniao.Encryptor.SM4DecryptECBFromHex(resObj.data, token);
                        if (!string.IsNullOrEmpty(plaintext))
                        {
                            try
                            {
                                if (typeof(T) == typeof(string))
                                {
                                    rlt.data = (T)System.Convert.ChangeType(plaintext, typeof(T));
                                    rlt.tips = resObj.tips;
                                }
                                else
                                {
                                    rlt.data = Json.ToObject<T>(plaintext);
                                    rlt.tips = resObj.tips;
                                }
                            }
                            catch (Exception ex)
                            {
                                rlt.code = "107";
                                rlt.message = "收到远端输出，但反序列化失败：" + ex.Message;
                            }
                        }
                        else if (!string.IsNullOrEmpty(resObj.code) && !string.IsNullOrEmpty(resObj.data))
                        {
                            rlt.code = "106";
                            rlt.message = "远端返回内容无法解密";
                        }
                        else if (string.IsNullOrEmpty(resObj.code) && string.IsNullOrEmpty(resObj.data))
                        {
                            rlt.code = "105";
                            rlt.message = "远端暂无返回内容";
                        }
                        logs += "\r\n <<< {\"success\":" + rlt.success.ToString().ToLower() + ",\"message\":\"" + rlt.message + "\",\"code\":\"" + rlt.code + "\",\"data\":" + (string.IsNullOrEmpty(plaintext) ? "\"\"" : plaintext) + "}";
                    }
                }
                Loger.Topic(apinode, logs, Log.LogLevel.Debug, true);
            }
            return rlt;
        }
    }
}