/*==============================================================================
    文件名称：Common.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：XServer请求工具
================================================================================
 
    Copyright 2014 XieChaoyi

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
using Org.BouncyCastle.Asn1.Ocsp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using Wlniao.Handler;

namespace Wlniao.Net
{
    /// <summary>
    /// HTTP客户端
    /// </summary>
    public class ApiClient
    {
        /// <summary>
        /// 发起Get请求
        /// </summary>
        /// <param name="url">请求的Url</param>
        /// <param name="webroxy">代理服务器地址</param>
        /// <returns></returns>
        public static string Get(string url, string webroxy = null)
        {
            var res = "";
            var uri = new Uri(url);
            if (string.IsNullOrEmpty(webroxy))
            {
                webroxy = XCore.Webroxy;
            }
            if (!string.IsNullOrEmpty(webroxy))
            {
                url = webroxy + uri.PathAndQuery;
            }
            var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = XCore.ServerCertificateCustomValidationCallback };
            using (var client = new System.Net.Http.HttpClient(handler))
            {
                var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, url);
                if (!string.IsNullOrEmpty(webroxy))
                {
                    request.Headers.TryAddWithoutValidation("X-Webroxy", uri.Host);
                }
                client.SendAsync(request).ContinueWith((requestTask) =>
                {
                    var response = requestTask.Result;
                    response.Content.ReadAsStringAsync().ContinueWith((readTask) =>
                    {
                        res = readTask.Result;
                    }).Wait();
                }).Wait();
            }
            return res;
        }
        /// <summary>
        /// 发起Post请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="postData"></param>
        /// <param name="contentType"></param>
        /// <param name="webroxy">代理服务器地址</param>
        /// <returns></returns>
        public static string Post(string url, string postData, string contentType = "application/json", string webroxy = null)
        {
            var uri = new Uri(url);
            if (string.IsNullOrEmpty(webroxy))
            {
                webroxy = XCore.Webroxy;
            }
            if (!string.IsNullOrEmpty(webroxy))
            {
                url = webroxy + uri.PathAndQuery;
            }
            var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = XCore.ServerCertificateCustomValidationCallback };
            using (var client = new HttpClient(handler))
            {
                var stream = Convert.ToStream(string.IsNullOrEmpty(postData) ? new byte[0] : Encoding.UTF8.GetBytes(postData));
                var content = new StreamContent(stream);
                client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Wlniao/XCore");
                client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", contentType);
                if (!string.IsNullOrEmpty(webroxy))
                {
                    client.DefaultRequestHeaders.TryAddWithoutValidation("X-Webroxy", uri.Host);
                }
                var response = client.PostAsync(url, content).GetAwaiter().GetResult();
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                }
                else
                {
                    throw new Exception("StatusCode:" + response.StatusCode + " " + response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <param name="stream"></param>
        /// <param name="webroxy">代理服务器地址</param>
        /// <returns></returns>
        public static string Post(string url, System.IO.Stream stream, string webroxy = null)
        {
            var str = "";
            var err = "";
            var uri = new Uri(url);
            if (string.IsNullOrEmpty(webroxy))
            {
                webroxy = XCore.Webroxy;
            }
            if (!string.IsNullOrEmpty(webroxy))
            {
                url = webroxy + uri.PathAndQuery;
            }
            var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = XCore.ServerCertificateCustomValidationCallback };
            using (var client = new HttpClient(handler))
            {
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Date = DateTime.UtcNow;
                request.Headers.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("Wlniao-XCore-XServer", "beta"));
                if (!string.IsNullOrEmpty(webroxy))
                {
                    request.Headers.TryAddWithoutValidation("X-Webroxy", uri.Host);
                }
                if (stream != null && stream.Length > 0)
                {
                    request.Content = new StreamContent(stream);
                }
                client.SendAsync(request).ContinueWith((requestTask) =>
                {
                    try
                    {
                        requestTask.Result.Content.ReadAsStringAsync().ContinueWith((readTask) =>
                        {
                            str = readTask.Result;
                        }).Wait();
                    }
                    catch (AggregateException aex)
                    {
                        if (aex.InnerException != null)
                        {
                            if (aex.InnerException.InnerException != null)
                            {
                                err = aex.InnerException.InnerException.Message;
                            }
                            else
                            {
                                err = aex.InnerException.Message;
                            }
                        }
                        else
                        {
                            err = aex.Message;
                        }
                    }
                }).Wait();
            }
            return str;
        }

    }
}