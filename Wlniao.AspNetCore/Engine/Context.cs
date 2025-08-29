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
        public bool Ready  { get; private set; } = false;
        
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
        public string Body { get; private set; }
        
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

                if (this.HeaderInput.TryGetValue("x-forwarded-proto", out var proto) && proto.ToLower() == "https")
                {
                    return true;
                }
                if (this.HeaderInput.TryGetValue("x-client-scheme", out var scheme) && scheme.ToLower() == "https")
                {
                    return true;
                }
                if (this.HeaderInput.TryGetValue("referer", out var referer) && referer.StartsWith("https"))
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
                if (!this.HeaderInput.TryGetValue("x-forwarded-for", out var forwardedIp) ||
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
        public Wlniao.Engine.EngineSession Session { get; private set; }
        
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
        /// 输出内容序列化托管
        /// </summary>
        public Func<HttpRequest, string>? SerializeOutput { get; set; }


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
            this.SKey = string.Empty;
            this.Host = string.Empty;
            this.Body = string.Empty;
            this.Query = new Dictionary<string, string>();
            this.Output = new ApiResult<object> { code = "-1" };
            this.HeaderInput = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            this.HeaderOutput = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            this.Authorization = string.Empty;
            this.ConsumerId = string.Empty;
            this.ConsumerSecretKey = string.Empty;
            this.ConsumerPublicKey = string.Empty;
            this.ConsumerPrivateKey = string.Empty;
            this.Session = new EngineSession();
        }

        /// <summary>
        /// /
        /// </summary>
        /// <param name="httpRequest"></param>
        public void Init(HttpRequest httpRequest)
        {
            if (this.Ready)
            {
                return; //重复初始化时直接跳过
            }
            if (httpRequest.Headers.Count > 0)
            {
                // 处理请求头内容
                foreach (var item in httpRequest.Headers)
                {
                    this.HeaderInput.TryAdd(item.Key, item.Value.FirstOrDefault()?.ToString() ?? string.Empty);
                }
            }
            
            this.Request = httpRequest;
            this.Response = httpRequest.HttpContext.Response;
            if (this.LoadConsumerInfo != null)
            {
                // 执行加载租户信息的回调方法
                try
                {
                    var consumer = this.LoadConsumerInfo.Invoke(this.Request) ?? new ConsumerInfo();
                    this.Host = consumer.Domain;
                    this.ConsumerId = consumer.Id;
                    this.ConsumerSecretKey = consumer.SecretKey;
                    this.ConsumerPublicKey = consumer.PublicKey;
                    this.ConsumerPrivateKey = consumer.PrivateKey;
                }
                catch (Exception e)
                {
                    Loger.Error($"LoadConsumerInfo: {e.Message}");
                    throw;
                }
            }

            if (string.IsNullOrEmpty(this.Host))
            {
                this.Host = this.HeaderInput.GetString("x-domain", httpRequest.Host.Host);
            }
            
            if (this.SafetyCertification != null)
            {
                // 执行加载安全认证的回调方法
                try
                {
                    this.SKey = this.SafetyCertification(this.Request) ?? string.Empty;
                }
                catch (Exception e)
                {
                    Loger.Error($"SafetyCertification: {e.Message}");
                    throw;
                }
            }
            else if (!string.IsNullOrEmpty(this.ConsumerPrivateKey) && this.HeaderInput.TryGetValue("sm2token", out string? sm2Token) && !string.IsNullOrEmpty(sm2Token))
            {
                // 执行加载安全认证的回调方法
                try
                {
                    this.SKey = Wlniao.Encryptor.SM2DecryptByPrivateKey(sm2Token, this.ConsumerPrivateKey);
                }
                catch (Exception e)
                {
                    Loger.Error($"Default SafetyCertification: {e.Message}");
                    throw;
                }
            }
            
            // 模式匹配进行判断
            if (httpRequest is { Method: "POST", ContentLength: > 0 })
            {
                try
                {
                    this.Body = new StreamReader(httpRequest.Body).ReadToEnd();
                    if (string.IsNullOrEmpty(this.Body))
                    {
                        using var reader = new StreamReader(httpRequest.Body, System.Text.Encoding.UTF8);
                        this.Body = reader.ReadToEnd();
                    }
                }
                catch
                {
                    var buffer = new byte[(int)httpRequest.ContentLength];
                    httpRequest.Body.ReadExactly(buffer, 0, buffer.Length);
                    this.Body = System.Text.Encoding.UTF8.GetString(buffer);
                }

                try
                {
                    // 兼容处理Form提交的内容
                    foreach (var item in httpRequest.Form!)
                    {
                        this.Query.TryAdd(item.Key, item.Value.FirstOrDefault()?.ToString() ?? string.Empty);
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
                    this.Query.TryAdd(item.Key, item.Value.FirstOrDefault()?.ToString() ?? string.Empty);
                }
            }
            
            // 取出身份验证令牌
            if (this.HeaderInput.TryGetValue("authorization", out var authorization) && !string.IsNullOrEmpty(authorization))
            {
                this.Authorization = authorization;
            }
            
            // 标记初始化已完成
            this.Ready = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool Auth()
        {
            var outmsg = true;
            if (this.IdentityAuthentication == null && !string.IsNullOrEmpty(this.Authorization))
            {
                try
                {
                    this.Session.Decode(this.Authorization, this.ConsumerSecretKey, this.ConsumerId);
                }
                catch (Exception e)
                {
                    Loger.Error($"EngineSession.Decode: {e.Message}[${this.ConsumerId}]");
                    this.OutMessage($"EngineSession.Decode: {e.Message}", 401, false);
                    outmsg = false;
                }
            }
            else if (this.IdentityAuthentication != null)
            {
                // 执行登录身份认证的回调方法
                try
                {
                    this.Session = this.IdentityAuthentication.Invoke(this.Request) ?? new EngineSession();
                }
                catch (Exception e)
                {
                    Loger.Error($"IdentityAuthentication: {e.Message}[${this.ConsumerId}]");
                    this.OutMessage($"IdentityAuthentication: {e.Message}", 401, false);
                    outmsg = false;
                }
            }

            if (string.IsNullOrEmpty(this.Session.UserId) || this.Session.ExpireTime < DateTools.GetUnix())
            {
                this.Ready = false;
                this.Response.Headers.TryAdd("Access-Control-Expose-Headers", new Microsoft.Extensions.Primitives.StringValues("*"));
                this.Response.Headers.TryAdd("Authify-State", new Microsoft.Extensions.Primitives.StringValues("false"));
                if(outmsg)
                {
                    this.OutMessage("unauthorized", 401, false);
                }
            }
            else
            {
                this.Ready = true;
            }

            return this.Ready;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T InputDeserialize<T>()
        {
            if (string.IsNullOrEmpty(this.Body))
            {
                return Activator.CreateInstance<T>();
            }

            if (string.IsNullOrEmpty(this.SKey))
            {
                return System.Text.Json.JsonSerializer.Deserialize<T>(this.Body, XCore.JsonSerializerOptions) ??
                       Activator.CreateInstance<T>();
            }
            else
            {
                var json = Wlniao.Encryptor.SM4DecryptECBFromHex(this.Body, this.SKey, true);
                return System.Text.Json.JsonSerializer.Deserialize<T>(string.IsNullOrEmpty(json) ? this.Body: json, XCore.JsonSerializerOptions) ??
                       Activator.CreateInstance<T>();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, object> InputDeserialize()
        {
            var kvs = new Dictionary<string, object>();
            foreach (var kv in this.Query)
            {
                //先放入GET及FORM参数
                kvs.TryAdd(kv.Key, kv.Value);
            }

            foreach (var kv in InputDeserialize<Dictionary<string, object>>())
            {
                //再尝试Body参数进行覆盖
                kvs.PutValue(kv.Key, kv.Value);
            }

            return kvs;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="code"></param>
        public void OutSuccess(object? data, int code = 200)
        {
            this.Output.data = data;
            this.Output.tips = false;
            this.Output.code = code.ToString();
            this.Output.success = code == 200 ? true : false;
            this.Output.message = "success";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="message"></param>
        public void OutSuccess(object? data, string message)
        {
            this.Output.data = data;
            this.Output.tips = false;
            this.Output.code = "200";
            this.Output.success = true;
            this.Output.message = string.IsNullOrEmpty(message) ? "success" : message;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="code"></param>
        /// <param name="tips"></param>
        public void OutMessage(string message, int code, bool tips = false)
        {
            this.Output.data = null;
            this.Output.tips = tips;
            this.Output.code = code.ToString();
            this.Output.success = code == 200;
            this.Output.message = message;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="code"></param>
        /// <param name="tips"></param>
        public void OutMessage(string message, string code = "", bool tips = false)
        {
            this.Output.data = null;
            this.Output.tips = tips;
            this.Output.code = string.IsNullOrEmpty(code) ? "" : code;
            this.Output.success = code == "200" ? true : false;
            this.Output.message = message;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string SerializeJsonOutput()
        {
            if (this.SerializeOutput != null)
            {
                return this.SerializeOutput.Invoke(this.Request);
            }
            var output = new Dictionary<string, object> { { "code", this.Output.code ?? "" } };
            if (this.Output.data != null)
            {
                output.Add("data", this.Output.data);
            }

            if (this.Output.tips)
            {
                //输出提示内容可直接引用的标记
                output.Add("tips", this.Output.tips);
            }

            output.Add("message", this.Output.message);
            if (this.Output.success || this.Output.code != "-1")
            {
                output.Add("success", this.Output.success);
            }

            return System.Text.Json.JsonSerializer.Serialize(output, XCore.JsonSerializerOptions);
        }
    }
}