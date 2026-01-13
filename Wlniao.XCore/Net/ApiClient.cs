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

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Wlniao.Net
{
    /// <summary>
    /// HTTP客户端
    /// </summary>
    public static class ApiClient
    {
        /// <summary>
        /// 发起Get请求
        /// </summary>
        /// <param name="url">请求的Url</param>
        /// <param name="webroxy">代理服务器地址</param>
        /// <returns></returns>
        public static async Task<string> GetAsync(string url, string webroxy = null)
        {
            var logs = "";
            try
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

                logs += "URL: " + url + Environment.NewLine;
                
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.TryAddWithoutValidation("User-Agent", "Wlniao/XCore");
                
                if (!string.IsNullOrEmpty(webroxy))
                {
                    request.Headers.TryAddWithoutValidation("X-Webroxy", uri.Host);
                }

                var response = await HttpClientManager.SharedInstance.SendAsync(request);
                foreach (var hd in response.Headers)
                {
                    logs += hd.Key + ": " + string.Join(",", hd.Value) + Environment.NewLine;
                }
                var content = await response.Content.ReadAsStringAsync();
                
                return content;
            }
            catch (AggregateException e)
            {
                logs += e.InnerException?.Message;
                Wlniao.Log.Loger.Debug(logs);
                throw e.InnerException!;
            }
            catch(Exception e)
            {
                logs += e.Message;
                Wlniao.Log.Loger.Debug(logs);
                throw;
            }
        }
        
        /// <summary>
        /// 发起Get请求
        /// </summary>
        /// <param name="url">请求的Url</param>
        /// <param name="webroxy">代理服务器地址</param>
        /// <returns></returns>
        public static string Get(string url, string webroxy = null)
        {
            return GetAsync(url, webroxy).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 发起Post请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="postData"></param>
        /// <param name="contentType"></param>
        /// <param name="webroxy">代理服务器地址</param>
        /// <returns></returns>
        public static async Task<string> PostAsync(string url, string postData, string contentType = "application/json", string webroxy = null)
        {
            var logs = "";
            try
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
                logs += "URL: " + url + Environment.NewLine;
                
                var stream = Wlniao.Convert.ToStream(string.IsNullOrEmpty(postData) ? Array.Empty<byte>() : Encoding.UTF8.GetBytes(postData));
                var content = new StreamContent(stream);
                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = content
                };
                request.Headers.TryAddWithoutValidation("User-Agent", "Wlniao/XCore");
                request.Headers.TryAddWithoutValidation("Content-Type", contentType);
                if (!string.IsNullOrEmpty(webroxy))
                {
                    request.Headers.TryAddWithoutValidation("X-Webroxy", uri.Host);
                }
                
                var response = await HttpClientManager.SharedInstance.SendAsync(request);
                foreach (var hd in response.Headers)
                {
                    logs += hd.Key + ": " + string.Join(",", hd.Value) + Environment.NewLine;
                }
                return response.StatusCode == System.Net.HttpStatusCode.OK 
                    ? await response.Content.ReadAsStringAsync() 
                    : throw new Exception("StatusCode:" + response.StatusCode + " " + await response.Content.ReadAsStringAsync());
            }
            catch (AggregateException e)
            {
                logs += e.InnerException?.Message;
                Wlniao.Log.Loger.Debug(logs);
                throw e.InnerException!;
            }
            catch(Exception e)
            {
                logs += e.Message;
                Wlniao.Log.Loger.Debug(logs);
                throw;
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <param name="stream"></param>
        /// <param name="webroxy">代理服务器地址</param>
        /// <returns></returns>
        public static async Task<string> PostAsync(string url, System.IO.Stream stream, string webroxy = null)
        {
            try
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
                
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Date = DateTime.UtcNow;
                request.Headers.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("Wlniao-XCore-XServer", "beta"));
                if (!string.IsNullOrEmpty(webroxy))
                {
                    request.Headers.TryAddWithoutValidation("X-Webroxy", uri.Host);
                }
                if (stream is { Length: > 0 })
                {
                    request.Content = new StreamContent(stream);
                }
                
                var response = await HttpClientManager.SharedInstance.SendAsync(request);
                return await response.Content.ReadAsStringAsync();
            }
            catch (AggregateException e)
            {
                throw e.InnerException!;
            }
            catch
            {
                throw;
            }
        }
        
        /// <summary>
        /// 发起Post请求 (同步版本)
        /// </summary>
        /// <param name="url"></param>
        /// <param name="postData"></param>
        /// <param name="contentType"></param>
        /// <param name="webroxy">代理服务器地址</param>
        /// <returns></returns>
        public static string Post(string url, string postData, string contentType = "application/json", string webroxy = null)
        {
            return PostAsync(url, postData, contentType, webroxy).GetAwaiter().GetResult();
        }
        
        /// <summary>
        /// (同步版本)
        /// </summary>
        /// <param name="url"></param>
        /// <param name="stream"></param>
        /// <param name="webroxy">代理服务器地址</param>
        /// <returns></returns>
        public static string Post(string url, System.IO.Stream stream, string webroxy = null)
        {
            return PostAsync(url, stream, webroxy).GetAwaiter().GetResult();
        }

    }
}