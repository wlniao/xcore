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
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Wlniao.Log;
using Wlniao.Text;

namespace Wlniao.XServer
{
    /// <summary>
    /// TokenApi服务端请求工具
    /// </summary>
    public class XApi
    {
        // 静态HttpClient实例，避免每次创建新实例
        private static readonly HttpClient _httpClient;
        
        static XApi()
        {
            var handler = new HttpClientHandler 
            { 
                ServerCertificateCustomValidationCallback = XCore.ServerCertificateCustomValidationCallback,
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
            };
            _httpClient = new HttpClient(handler);
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Wlniao/XCore");
        }
        
        /// <summary>
        /// 获取平台接口数据（AOT 安全：调用方需预序列化请求体并传入响应体源生成元数据）
        /// </summary>
        /// <typeparam name="T">响应数据载荷类型</typeparam>
        /// <param name="node"></param>
        /// <param name="url"></param>
        /// <param name="token"></param>
        /// <param name="dataJson">已由调用方序列化为 JSON 字符串的请求体</param>
        /// <param name="dataInfo">响应数据载荷 T 的源生成元数据</param>
        /// <param name="traceid"></param>
        /// <returns></returns>
        public static Wlniao.ApiResult<T> Request<T>(string node, string url, string token, string dataJson, JsonTypeInfo<T> dataInfo, string traceid = "")
        {
            return RequestAsync<T>(node, url, token, dataJson, dataInfo, traceid, CancellationToken.None).GetAwaiter().GetResult();
        }
        
        /// <summary>
        /// 异步获取平台接口数据（AOT 安全）
        /// </summary>
        /// <typeparam name="T">响应数据载荷类型</typeparam>
        /// <param name="node"></param>
        /// <param name="url"></param>
        /// <param name="token"></param>
        /// <param name="dataJson">已由调用方序列化为 JSON 字符串的请求体</param>
        /// <param name="dataInfo">响应数据载荷 T 的源生成元数据</param>
        /// <param name="traceid"></param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns></returns>
        public static async Task<Wlniao.ApiResult<T>> RequestAsync<T>(string node, string url, string token, string dataJson, JsonTypeInfo<T> dataInfo, string traceid = "", CancellationToken cancellationToken = default)
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
                    traceid = StringUtil.CreateLongId();
                }
                var uri = new Uri(url);
                var txt = dataJson;
                var utime = "";
                var start = DateTime.Now;
                var encdata = Wlniao.Encryptor.SM4EncryptECBToHex(txt, token);
                var resStr = "";
                var reqStr = new System.Text.Json.Nodes.JsonObject
                {
                    ["sign"] = Encryptor.SM3Encrypt(now + encdata + token),
                    ["data"] = encdata,
                    ["trace"] = traceid,
                    ["timestamp"] = now
                }.ToJsonString(XCore.JsonSerializerOptions);
                try
                {
                    Loger.Topic(node, "msgid:" + traceid + ", " + url + "\n >>> " + reqStr, Log.LogLevel.Information, false);
                    
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(TimeSpan.FromSeconds(30));
                    
                    var request = new HttpRequestMessage(HttpMethod.Post, uri);
                    request.Headers.Date = DateTime.Now;
                    request.Headers.TryAddWithoutValidation("X-Wlniao-Trace", traceid);
                    request.Content = new StringContent(reqStr, System.Text.Encoding.UTF8, "application/json");
                    
                    var response = await _httpClient.SendAsync(request, cts.Token).ConfigureAwait(false);
                    resStr = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    
                    if (response.Headers.Contains("X-Wlniao-Trace"))
                    {
                        rlt.traceid = response.Headers.GetValues("X-Wlniao-Trace").FirstOrDefault();
                    }
                    if (response.Headers.Contains("X-Wlniao-UseTime"))
                    {
                        utime = response.Headers.GetValues("X-Wlniao-UseTime").FirstOrDefault();
                    }
                    if (string.IsNullOrEmpty(utime))
                    {
                        utime = DateTime.Now.Subtract(start).TotalMilliseconds.ToString("F2") + "ms";
                    }
                    Loger.Topic(node, $"msgid:{traceid}, {url}[usetime:{utime}]{Environment.NewLine} <<< {resStr}", Log.LogLevel.Information, false);
                }
                catch (OperationCanceledException)
                {
                    utime = DateTime.Now.Subtract(start).TotalMilliseconds.ToString("F2") + "ms";
                    Loger.Topic(node, $"msgid:{traceid}, {url}[usetime:{utime}]{Environment.NewLine} <<< 请求超时", Wlniao.Log.LogLevel.Error, true);
                }
                catch (Exception ex)
                {
                    utime = DateTime.Now.Subtract(start).TotalMilliseconds.ToString("F2") + "ms";
                    Loger.Topic(node, $"msgid:{traceid}, {url}[usetime:{utime}]{Environment.NewLine} <<< 请确认接口访问是否正常： => {ex.Message}", Wlniao.Log.LogLevel.Error, true);
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
                    Wlniao.ApiResult<string> resObj = null;
                    try
                    {
                        resObj = System.Text.Json.JsonSerializer.Deserialize(resStr, XApiJsonContext.Default.GetTypeInfo(typeof(Wlniao.ApiResult<string>))) as Wlniao.ApiResult<string>;
                    }
                    catch
                    {
                        // ignored
                    }

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
                                }
                                else
                                {
                                    rlt.data = Json.Deserialize<T>(plaintext, dataInfo);
                                }

                                rlt.tips = resObj.tips;
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
                Loger.Topic(node, logs, Log.LogLevel.Debug, true);
            }
            return rlt;
        }
    }

    /// <summary>
    /// XApi 内部使用的源生成 JSON 元数据上下文（AOT 安全，零反射）。
    /// 仅声明响应信封 ApiResult&lt;string&gt; 的固定类型；开放泛型 T 的载荷由调用方通过 JsonTypeInfo&lt;T&gt; 传入。
    /// </summary>
    [JsonSourceGenerationOptions]
    [JsonSerializable(typeof(Wlniao.ApiResult<string>))]
    internal partial class XApiJsonContext : JsonSerializerContext
    {
    }
}