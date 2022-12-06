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
            var now = XCore.NowUnix.ToString();
            var rlt = new Wlniao.ApiResult<T>();
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(data);
            var encdata = Wlniao.Encryptor.SM4EncryptECBToHex(json, token);
            var reqStr = Newtonsoft.Json.JsonConvert.SerializeObject(new { sign = Encryptor.SM3Encrypt(now + json + token), data = encdata, trace = traceid, timestamp = now });
            var stream = cvt.ToStream(System.Text.Encoding.UTF8.GetBytes(reqStr));
            var handler = new System.Net.Http.HttpClientHandler();
            if (System.Net.ServicePointManager.ServerCertificateValidationCallback != null)
            {
                handler.ServerCertificateCustomValidationCallback = XCore.ValidateServerCertificate;
            }
            using (var client = new System.Net.Http.HttpClient(handler))
            {
                var start = DateTime.Now;
                var reqest = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, url);
                reqest.Headers.Date = start;
                reqest.Content = new System.Net.Http.StreamContent(stream);
                reqest.Content.Headers.Add("Content-Type", "application/json");
                client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Wlniao/XCore");
                var resStr = client.Send(reqest).Content.ReadAsStringAsync().Result;
                var resObj = Newtonsoft.Json.JsonConvert.DeserializeObject<Wlniao.ApiResult<String>>(resStr);
                rlt.node = resObj.node;
                rlt.code = resObj.code;
                rlt.traceid = string.IsNullOrEmpty(resObj.traceid) ? traceid : resObj.traceid;
                rlt.message = resObj.message;
                rlt.success = resObj.success;
                if (resObj.success)
                {
                    try
                    {
                        json = Wlniao.Encryptor.SM4DecryptECBFromHex(resObj.data, token);
                        if (!string.IsNullOrEmpty(json))
                        {
                            try
                            {
                                rlt.data = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
                            }
                            catch
                            {
                                rlt.message = "解密内容格式不正确";
                            }
                        }
                    }
                    catch
                    {
                        rlt.message = "输出内容解密失败";
                    }
                }
            }
            return rlt;
        }
    }
}