using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Wlniao.Log;
using Wlniao.Text;

namespace Wlniao.Engine
{
    /// <summary>
    /// 请求处理上下文实现
    /// </summary>
    [BusinessService(LifeTime = ServiceLifetime.Transient)]
    public class Context : IContext
    {
    
        /// <summary>
        /// 是否初始化
        /// </summary>
        public bool Continue  { get; set; } = true;
        
        /// <summary>
        /// 当前请求会话密钥
        /// </summary>
        public string SKey { get; private set; }
        
        /// <summary>
        /// 当前请求域名
        /// </summary>
        public string Host { get; private set; }
        
        /// <summary>
        /// 请求输入内容
        /// </summary>
        public string Body { get; set; }
        
        /// <summary>
        /// 请求输入参数
        /// </summary>
        public Dictionary<string, string> Query { get; private set; }
        
        /// <summary>
        /// 请求输入头信息
        /// </summary>
        public Dictionary<string, string> HeaderInput { get; private set;  }
        
        /// <summary>
        /// 请求输出头信息
        /// </summary>
        public Dictionary<string, string> HeaderOutput { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        public ApiResult<object> Output { get; set; }

        /// <summary>
        /// 请求是否启用Https
        /// </summary>
        public bool IsHttps
        {
            get
            {
                if (HeaderInput.TryGetValue("x-forwarded-proto", out var proto) && proto.ToLower() == "https")
                {
                    return true;
                }
                if (HeaderInput.TryGetValue("x-client-scheme", out var scheme) && scheme.ToLower() == "https")
                {
                    return true;
                }
                if (HeaderInput.TryGetValue("referer", out var referer) && referer.StartsWith("https"))
                {
                    return true;
                }
                return Request.IsHttps;
            }
        }

        /// <summary>
        /// 请求客户端IP
        /// </summary>
        public string ClientIP
        {
            get
            {
                var clientIp =
                    (Request.HttpContext.Connection.RemoteIpAddress != null &&
                     !Request.HttpContext.Connection.RemoteIpAddress.IsIPv4MappedToIPv6)
                        ? Request.HttpContext.Connection.RemoteIpAddress?.ToString()
                        : Request.HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString();
                if (!HeaderInput.TryGetValue("x-forwarded-for", out var forwardedIp) ||
                    string.IsNullOrEmpty(forwardedIp))
                    return clientIp ?? string.Empty;
                // 通过代理网关部署时，获取"x-forwarded-for"传递的真实IP
                foreach (var ip in forwardedIp.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (ip == "::1" || ip == "127.0.0.1" || !StringUtil.IsIP(ip)) continue;
                    clientIp = ip;
                    break;
                }

                return clientIp ?? string.Empty;
            }
        }

        /// <summary>
        /// 请求身份凭据
        /// </summary>
        public string Authorization { get; private set; }

        /// <summary>
        /// 多租户系统标识
        /// </summary>
        public string ConsumerId { get; private set; }
        
        /// <summary>
        /// 多租户系统安全密钥
        /// </summary>
        public string ConsumerSecretKey { get; private set; }
        
        /// <summary>
        /// 多租户系统对外公钥
        /// </summary>
        public string ConsumerPublicKey { get; private set; }
    
        /// <summary>
        /// 多租户系统安全私钥
        /// </summary>
        public string ConsumerPrivateKey { get; private set; }

        /// <summary>
        /// 当前登录认证信息
        /// </summary>
        public EngineSession Session { get; private set; }
        
        /// <summary>
        /// 
        /// </summary>
        public Func<HttpRequest, ConsumerInfo>? LoadConsumerInfo { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Func<HttpRequest, EngineSession>? IdentityAuthentication { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Func<HttpRequest, string>? SafetyCertification { get; set; }

        /// <summary>
        /// 输出内容序列化回调方法
        /// </summary>
        public Func<HttpRequest, string>? SerializeOutput { get; set; }
        /// <summary>
        /// 认证失败时的回调方法
        /// </summary>
        public Action<HttpRequest>? AuthFailed { get; set; }


        /// <summary>
        /// 
        /// </summary>
        private HttpRequest Request { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        private HttpResponse Response { get; set; }


        /// <summary>
        /// 
        /// </summary>
        public Context()
        {
            SKey = string.Empty;
            Host = string.Empty;
            Body = string.Empty;
            Query = new Dictionary<string, string>();
            Output = new ApiResult<object> { code = "-1" };
            HeaderInput = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            HeaderOutput = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Authorization = string.Empty;
            ConsumerId = string.Empty;
            ConsumerSecretKey = string.Empty;
            ConsumerPublicKey = string.Empty;
            ConsumerPrivateKey = string.Empty;
            Session = new EngineSession();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="httpRequest"></param>
        public void Init(HttpRequest httpRequest)
        {
            if (this.HeaderInput.Count > 0)
            {
                return; //重复初始化时直接跳过
            }
            if (httpRequest.Headers.Count > 0)
            {
                // 处理请求头内容
                foreach (var item in httpRequest.Headers)
                {
                    HeaderInput.TryAdd(item.Key, item.Value.FirstOrDefault()?.ToString() ?? string.Empty);
                }
            }
            
            // 模式匹配进行判断
            if (httpRequest is { Method: "POST", ContentLength: > 0 })
            {
                try
                {
                    Body = new StreamReader(httpRequest.Body).ReadToEnd();
                    if (string.IsNullOrEmpty(Body))
                    {
                        using var reader = new StreamReader(httpRequest.Body, System.Text.Encoding.UTF8);
                        Body = reader.ReadToEnd();
                    }
                }
                catch
                {
                    var buffer = new byte[(int)httpRequest.ContentLength];
                    httpRequest.Body.ReadExactly(buffer, 0, buffer.Length);
                    Body = System.Text.Encoding.UTF8.GetString(buffer);
                }

                try
                {
                    // 兼容处理Form提交的内容
                    foreach (var item in httpRequest.Form!)
                    {
                        Query.TryAdd(item.Key, item.Value.FirstOrDefault()?.ToString() ?? string.Empty);
                    }
                }
                catch
                {
                    // ignored
                }
            }
            
            // 兼容处理Query提交的内容
            if (httpRequest.Query?.Count > 0)
            {
                foreach (var item in httpRequest.Query)
                {
                    Query.TryAdd(item.Key, item.Value.FirstOrDefault()?.ToString() ?? string.Empty);
                }
            }
            
            // 取出身份验证令牌
            if (HeaderInput.TryGetValue("authorization", out var authorization) && !string.IsNullOrEmpty(authorization))
            {
                Authorization = authorization;
            }
            else if (HeaderInput.TryGetValue("x-auth-token", out var xAuthToken) && !string.IsNullOrEmpty(xAuthToken))
            {
                Authorization = xAuthToken;
            }
            else if (HeaderInput.TryGetValue("x-token", out var xToken) && !string.IsNullOrEmpty(xToken))
            {
                Authorization = xToken;
            }

            Request = httpRequest;
            Response = httpRequest.HttpContext.Response;
            if (!this.Continue)
            {
                return;
            }
            if (LoadConsumerInfo != null)
            {
                // 执行加载租户信息的回调方法
                try
                {
                    var consumer = LoadConsumerInfo.Invoke(Request) ?? new ConsumerInfo();
                    Host = consumer.Domain;
                    ConsumerId = consumer.Id;
                    ConsumerSecretKey = consumer.SecretKey;
                    ConsumerPublicKey = consumer.PublicKey;
                    ConsumerPrivateKey = consumer.PrivateKey;
                }
                catch (Exception ex)
                {
                    Loger.Error($"LoadConsumerInfo: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
                    this.Continue = false;
                    throw;
                }
            }

            if (string.IsNullOrEmpty(Host))
            {
                Host = HeaderInput.GetString("x-domain", httpRequest.Host.Host);
            }

            if (!this.Continue) return;
            {
                if (SafetyCertification != null)
                {
                    // 执行加载安全认证的回调方法
                    try
                    {
                        SKey = SafetyCertification(Request) ?? string.Empty;
                    }
                    catch (Exception ex)
                    {
                        Loger.Error($"SafetyCertification: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
                        this.Continue = false;
                        throw;
                    }
                }
                else if (!string.IsNullOrEmpty(ConsumerPrivateKey) && HeaderInput.TryGetValue("sm2token", out var sm2Token) && !string.IsNullOrEmpty(sm2Token))
                {
                    // 执行加载安全认证的回调方法
                    try
                    {
                        SKey = Encryptor.Sm2DecryptByPrivateKey(sm2Token, ConsumerPrivateKey);
                    }
                    catch (Exception ex)
                    {
                        Loger.Error($"Default SafetyCertification: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
                        this.Continue = false;
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public void Auth()
        {
            if (IdentityAuthentication == null)
            {
                try
                {
                    if (string.IsNullOrEmpty(Authorization))
                    {
                        return;
                    }

                    Session.Decode(Authorization, ConsumerSecretKey, ConsumerId);
                }
                catch (Exception ex)
                {
                    Loger.Error($"EngineSession.Decode: {ex.Message}[${ConsumerId}]{Environment.NewLine}{ex.StackTrace}");
                }
            }
            else if (IdentityAuthentication != null)
            {
                // 执行登录身份认证的回调方法
                try
                {
                    Session = IdentityAuthentication.Invoke(Request) ?? new EngineSession();
                }
                catch (Exception ex)
                {
                    Loger.Error($"IdentityAuthentication: {ex.Message}[${ConsumerId}]{Environment.NewLine}{ex.StackTrace}");
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T InputDeserialize<T>()
        {
            if (string.IsNullOrEmpty(Body))
            {
                return Activator.CreateInstance<T>();
            }

            if (string.IsNullOrEmpty(SKey))
            {
                return System.Text.Json.JsonSerializer.Deserialize<T>(Body, XCore.JsonSerializerOptions) ??
                       Activator.CreateInstance<T>();
            }
            else
            {
                var json = Encryptor.Sm4DecryptEcbFromHex(Body, SKey, true);
                return System.Text.Json.JsonSerializer.Deserialize<T>(string.IsNullOrEmpty(json) ? Body: json, XCore.JsonSerializerOptions) ??
                       Activator.CreateInstance<T>();
            }
        }

        /// <summary>
        /// 输入内容缓存（避免重复反序列化）
        /// </summary>
        private Dictionary<string, object> _input = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// 返回输入内容
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, object> InputDeserialize()
        {
            lock (_input)
            {
                if (_input.Count > 0)
                {
                    return _input;
                }
                foreach (var kv in Query)
                {
                    //先放入GET及FORM参数
                    _input.TryAdd(kv.Key, kv.Value);
                }

                foreach (var kv in InputDeserialize<Dictionary<string, object>>())
                {
                    //再尝试Body参数进行覆盖
                    _input.PutValue(kv.Key, kv.Value);
                }
                return _input;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="code"></param>
        public void OutSuccess(object? data, int code = 200)
        {
            Output.data = data!;
            Output.tips = false;
            Output.code = code.ToString();
            Output.success = code == 200 ? true : false;
            Output.message = "success";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="message"></param>
        public void OutSuccess(object? data, string message)
        {
            Output.data = data!;
            Output.tips = false;
            Output.code = "200";
            Output.success = true;
            Output.message = string.IsNullOrEmpty(message) ? "success" : message;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="code"></param>
        /// <param name="tips"></param>
        public void OutMessage(string message, int code, bool tips = false)
        {
            Output.data = null;
            Output.tips = tips;
            Output.code = code.ToString();
            Output.success = code == 200;
            Output.message = message;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="code"></param>
        /// <param name="tips"></param>
        public void OutMessage(string message, string code = "", bool tips = false)
        {
            Output.data = null;
            Output.tips = tips;
            Output.code = string.IsNullOrEmpty(code) ? "" : code;
            Output.success = code == "200" ? true : false;
            Output.message = message;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string SerializeJsonOutput()
        {
            if (SerializeOutput != null)
            {
                return SerializeOutput.Invoke(Request);
            }
            var output = new Dictionary<string, object> { { "code", Output.code ?? "" } };
            if (Output.data != null)
            {
                output.Add("data", Output.data);
            }

            if (Output.tips)
            {
                //输出提示内容可直接引用的标记
                output.Add("tips", Output.tips);
            }

            output.Add("message", Output.message);
            if (Output.success || Output.code != "-1")
            {
                output.Add("success", Output.success);
            }

            return System.Text.Json.JsonSerializer.Serialize(output, XCore.JsonSerializerOptions);
        }
    }
}