using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        public object Output { get; set; }

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
        public string ClientIp
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
        public string ConsumerId { get; set; }
        
        /// <summary>
        /// 多租户系统安全密钥
        /// </summary>
        public string ConsumerSecretKey { get; set; }
        
        /// <summary>
        /// 多租户系统对外公钥
        /// </summary>
        public string ConsumerPublicKey { get; set; }
    
        /// <summary>
        /// 多租户系统安全私钥
        /// </summary>
        public string ConsumerPrivateKey { get; set; }

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
                Authorization = authorization.StartsWith("Bearer ") ? authorization[7..] : authorization;
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
                catch (Exception e)
                {
                    this.Continue = false;
                    Loger.Error($"LoadConsumerInfo: {e.Message}{Environment.NewLine}{e.StackTrace}");
                    throw new EngineException(e.Message, 804);
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
                    catch (Exception e)
                    {
                        this.Continue = false;
                        Loger.Error($"SafetyCertification: {e.Message}{Environment.NewLine}{e.StackTrace}");
                        throw new EngineException(e.Message, 805);
                    }
                }
                else if (!string.IsNullOrEmpty(ConsumerPrivateKey) && HeaderInput.TryGetValue("sm2token", out var sm2Token) && !string.IsNullOrEmpty(sm2Token))
                {
                    // 执行加载安全认证的回调方法
                    try
                    {
                        SKey = Encryptor.SM2DecryptByPrivateKey(sm2Token, ConsumerPrivateKey);
                    }
                    catch (Exception e)
                    {
                        this.Continue = false;
                        Loger.Error($"Default SafetyCertification: {e.Message}{Environment.NewLine}{e.StackTrace}");
                        throw new EngineException(e.Message, 805);
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
                catch (Exception e)
                {
                    Loger.Debug($"EngineSession.Decode: {e.Message}[{ConsumerId}]");
                }
            }
            else if (IdentityAuthentication != null)
            {
                // 执行登录身份认证的回调方法
                try
                {
                    Session = IdentityAuthentication.Invoke(Request) ?? new EngineSession();
                }
                catch (Exception e)
                {
                    Loger.Debug($"IdentityAuthentication: {e.Message}[{ConsumerId}]");
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
                var json = string.Empty;
                try
                {
                    json = Encryptor.SM4DecryptECBFromHex(Body, SKey, true);
                }
                catch
                {
                    // ignored
                }

                return System.Text.Json.JsonSerializer.Deserialize<T>(string.IsNullOrEmpty(json) ? Body: json, XCore.JsonSerializerOptions) ??
                       Activator.CreateInstance<T>();
            }
        }

        /// <summary>
        /// 输入内容缓存（避免重复反序列化）
        /// </summary>
        private readonly Dictionary<string, object> _input = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

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
        public Func<HttpResponse, IActionResult>? AuthFailed { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public IActionResult OutputSerialize<T>(T output)
        {
            foreach (var kv in HeaderOutput)
            {
                Response.Headers.TryAdd(kv.Key, kv.Value);
            }

            var json = output as string ?? Wlniao.Json.Serialize(output);
            if (OutputSerializeCallback != null)
            {
                return OutputSerializeCallback(json);
            }
            
            return string.IsNullOrEmpty(SKey) 
                ? new ContentResult { Content = json, ContentType = HeaderOutput.GetString("Content-Type", "application/json") } 
                : new ContentResult { Content = Wlniao.Encryptor.SM4EncryptECBToHex(json, SKey), ContentType = "text/plain" };
        }
        
        /// <summary>
        /// 
        /// </summary>
        public Func<IActionResult>? AuthFailedCallback { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        public Func<string, IActionResult>? OutputSerializeCallback { get; set; }
    }
}