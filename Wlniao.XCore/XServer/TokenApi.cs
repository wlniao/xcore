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
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;

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
        /// <param name="url"></param>
        /// <param name="token"></param>
        /// <param name="data"></param>
        /// <param name="traceid"></param>
        /// <returns></returns>
        public static Wlniao.ApiResult<T> Request<T>(String url, String token, Object data, String traceid = "")
        {
            if (string.IsNullOrEmpty(traceid))
            {
                traceid = System.Guid.NewGuid().ToString(); //生成默认传递链路ID
            }
            var now = DateTools.GetUnix().ToString();
            var rlt = new Wlniao.ApiResult<T>();
            if (string.IsNullOrEmpty(token))
            {
                rlt.code = "100";
                rlt.message = "本地通讯密钥未配置，无法发起API请求";
            }
            else
            {
                var txt = data is String ? data.ToString() : Newtonsoft.Json.JsonConvert.SerializeObject(data);
                var start = DateTime.Now;
                var usetime = "0ms";
                var encdata = Wlniao.Encryptor.SM4EncryptECBToHex(txt, token);
                var resStr = "";
                var reqStr = Newtonsoft.Json.JsonConvert.SerializeObject(new { sign = Encryptor.SM3Encrypt(now + encdata + token), data = encdata, trace = traceid, timestamp = now });
                try
                {
                    var stream = cvt.ToStream(System.Text.Encoding.UTF8.GetBytes(reqStr));
                    var handler = new System.Net.Http.HttpClientHandler();
                    if (System.Net.ServicePointManager.ServerCertificateValidationCallback != null)
                    {
                        handler.ServerCertificateCustomValidationCallback = XCore.ValidateServerCertificate;
                    }
                    using (var client = new System.Net.Http.HttpClient(handler))
					{
						log.Debug(url + " request:" + reqStr);
						var reqest = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, url);
                        reqest.Headers.Date = DateTime.Now;
                        reqest.Content = new System.Net.Http.StreamContent(stream);
                        reqest.Content.Headers.Add("Content-Type", "application/json");
                        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Wlniao/XCore");
                        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Wlniao-Trace", traceid);
                        var respose = client.Send(reqest);
                        resStr = respose.Content.ReadAsStringAsync().Result;
                        if (respose.Content.Headers.Contains("X-Wlniao-Trace"))
                        {
                            traceid = respose.Content.Headers.GetValues("X-Wlniao-Trace").FirstOrDefault();
                        }
                        if (respose.Headers.Contains("X-Wlniao-UseTime"))
                        {
                            usetime = respose.Headers.GetValues("X-Wlniao-UseTime").FirstOrDefault();
                        }
                        log.Debug(url + " response:" + resStr + "[" + DateTime.Now.Subtract(start).TotalMilliseconds.ToString("F2") + "ms]");
                    }
                }
                catch { }
                if (string.IsNullOrEmpty(resStr))
                {
                    rlt.tips = true;
                    rlt.code = "101";
                    rlt.debuger = url + "[Error]";
                    rlt.message = "通讯异常，网络异常或请求错误";
                    log.Warn(url + " response: " + rlt.message + "[" + DateTime.Now.Subtract(start).TotalMilliseconds.ToString("F2") + "ms]");
                }
                else
                {
                    Wlniao.ApiResult<String> resObj = null;
                    try
                    {
                        resObj = Newtonsoft.Json.JsonConvert.DeserializeObject<Wlniao.ApiResult<String>>(resStr);
                    }
                    catch { }
                    if (resObj == null || (string.IsNullOrEmpty(resObj.node) && string.IsNullOrEmpty(resObj.code)))
                    {
                        rlt.code = "104";
                        rlt.message = "远端输出格式不满足本地要求";
                        log.Debug(url + ": " + rlt.message);
                    }
                    else
                    {
                        rlt.node = resObj.node;
                        rlt.code = resObj.code;
                        rlt.tips = resObj.tips;
                        rlt.traceid = string.IsNullOrEmpty(resObj.traceid) ? traceid : resObj.traceid;
                        rlt.message = resObj.message;
                        rlt.debuger = resObj.debuger;
                        rlt.success = resObj.success;
                        var plaintext = Wlniao.Encryptor.SM4DecryptECBFromHex(resObj.data, token);
						if (string.IsNullOrEmpty(resObj.data))
						{
							log.Debug(url + ": 远端暂无返回内容");
						}
						else if (string.IsNullOrEmpty(plaintext))
						{
							rlt.code = "106";
							rlt.message = "远端返回内容无法解密";
							log.Warn(url + ": " + rlt.message);
						}
						else
						{
                            try
                            {
                                log.Info(url + " [traceid:" + traceid + ",usetime:" + usetime + "]\r\n >>> " + txt + "\r\n <<< " + plaintext + "\r\n");
                                if (typeof(T) == typeof(string))
                                {
                                    rlt.data = (T)System.Convert.ChangeType(plaintext, typeof(T));
                                    rlt.tips = resObj.tips;
                                }
                                else
                                {
                                    rlt.data = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(plaintext);
                                    rlt.tips = resObj.tips;
                                }
                            }
                            catch
                            {
                                rlt.code = "107";
                                rlt.message = "收到远端输出，但反序列化失败";
                            }
                        }
                    }
                }
            }
            return rlt;
        }
    }
}