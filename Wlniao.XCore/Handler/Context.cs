/*-----------------------Copyright 2017 www.wlniao.com---------------------------
    文件名称：Wlniao\Handler\IContext.cs
    适用环境：NETCoreCLR 1.0/2.0
    最后修改：2017年12月12日 10:58:50
    功能描述：Wlniao统一约束接口

    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at

               http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.
------------------------------------------------------------------------------*/
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Wlniao.Handler
{
    /// <summary>
    /// WlniaoHandler统一上下文接口
    /// </summary>
    public class Context : IContext
    {
        /// <summary>
        /// 请求方式 GET/POST
        /// </summary>
        public string Method { get; set; }
        /// <summary>
        /// 接口域名
        /// </summary>
        public string ApiHost { get; set; }
        /// <summary>
        /// 接口路径
        /// </summary>
        public string ApiPath { get; set; }
        /// <summary>
        /// 请求内容
        /// </summary>
        public object RequestBody { get; set; }
        /// <summary>
        /// 输出内容
        /// </summary>
        public object ResponseBody { get; set; }
        /// <summary>
        /// 接口数据内容格式
        /// </summary>
        public string ContentType = "application/json";
        /// <summary>
        /// 请求的Headers参数
        /// </summary>
        public Dictionary<string, string> HttpRequestHeaders;
        /// <summary>
        /// 输出的Headers参数
        /// </summary>
        public Dictionary<string, string> HttpResponseHeaders;
        /// <summary>
        /// 接口数据编码格式
        /// </summary>
        public System.Text.Encoding Encoding = System.Text.Encoding.UTF8;
        /// <summary>
        /// 请求使用的证书
        /// </summary>
        public System.Security.Cryptography.X509Certificates.X509Certificate Certificate = null;

        /// <summary>
        /// 返回结果
        /// </summary>
        public ApiResult<IResponse> Result = null;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Task<string> Handle()
        {
            var rlt = Task.Run<string>(() => { return ""; });
            Result = new ApiResult<IResponse> { node = XCore.WebNode, code = "-1", success = false, message = "unkown error" };
            if (string.IsNullOrEmpty(ApiHost))
            {
                Result.code = "400";
                Result.message = "request host not set";
            }
            else if (string.IsNullOrEmpty(ApiPath))
            {
                Result.code = "400";
                Result.message = "request path not set";
            }
            else
            {
                System.Net.Http.HttpClient http = null;
                Task<System.Net.Http.HttpResponseMessage> task = null;
                if (Certificate == null)
                {
                    http = new System.Net.Http.HttpClient();
                }
                else
                {
                    var handler = new System.Net.Http.HttpClientHandler();
                    handler.ClientCertificateOptions = System.Net.Http.ClientCertificateOption.Manual;
                    handler.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;
                    handler.ClientCertificates.Add(Certificate);
                    http = new System.Net.Http.HttpClient(handler);
                }
                http.BaseAddress = new System.Uri(ApiHost);
                if (Method == "GET")
                {
                    var query = RequestBody as string;
                    if (!string.IsNullOrEmpty(query))
                    {
                        var link = ApiPath.IndexOf('?') < 0 ? '?' : '&';
                        ApiPath += query[0] == '?' || query[0] == '&' ? query : link + query;
                    }
                    if (HttpRequestHeaders != null)
                    {
                        foreach (var kv in HttpRequestHeaders)
                        {
                            http.DefaultRequestHeaders.Add(kv.Key, kv.Value);
                        }
                    }
                    task = http.GetAsync(ApiPath);
                }
                else
                {
                    System.Net.Http.HttpContent content = null;
                    var text = RequestBody as string;
                    var bytes = RequestBody as byte[];
                    if (bytes != null)
                    {
                        content = new System.Net.Http.ByteArrayContent(bytes);
                    }
                    else if (!string.IsNullOrEmpty(text))
                    {
                        content = new System.Net.Http.StringContent(text, Encoding, ContentType);
                    }
                    else if (RequestBody != null)
                    {
                        content = new System.Net.Http.StringContent(Json.ToString(RequestBody), Encoding, ContentType);
                    }
                    else
                    {
                        content = new System.Net.Http.ByteArrayContent(new byte[0]);
                    }
                    if (HttpRequestHeaders != null)
                    {
                        foreach (var kv in HttpRequestHeaders)
                        {
                            content.Headers.Add(kv.Key, kv.Value);
                        }
                    }
                    task = http.PostAsync(ApiPath, content);
                }

                task.Result.Content.ReadAsStringAsync().ContinueWith((res) =>
                {
                    ResponseBody = res.Result;
                    HttpResponseHeaders = new Dictionary<string, string>();
                    foreach (var item in task.Result.Headers)
                    {
                        var em = item.Value.GetEnumerator();
                        if (em.MoveNext())
                        {
                            HttpResponseHeaders.Add(item.Key.ToLower(), em.Current);
                        }
                    }
                    rlt = Task.Run<string>(() => { return res.Result; });
                }).Wait();
            }
            return rlt;
        }
    }
}