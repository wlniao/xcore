using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Wlniao.Log;

namespace Wlniao.Engine
{
    /// <summary>
    /// 请求处理上下文实现
    /// </summary>
    [BusinessService(LifeTime = ServiceLifetime.Transient)]
    public class Context : IContext
    {
    
        /// <summary>
        /// 
        /// </summary>
        public bool Ready  { get; private set; } = false;
        
        /// <summary>
        /// 
        /// </summary>
        public string SKey { get; private set; }
        
        /// <summary>
        /// 
        /// </summary>
        public string Host { get; private set; }
        
        /// <summary>
        /// 
        /// </summary>
        public string Body { get; private set; }
        
        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, string> Query { get; private set; }
        
        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, string> HeaderInput { get; private set;  }
        
        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, string> HeaderOutput { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        private ApiResult<object?> Output { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        public string Authorization { get; private set; }
        
        /// <summary>
        /// 
        /// </summary>
        public string ConsumerId { get; private set; }
        
        /// <summary>
        /// 
        /// </summary>
        public string ConsumerSecretKey { get; private set; }

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
            this.Output = new ApiResult<object?> { code = "-1" };
            this.HeaderInput = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            this.HeaderOutput = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            this.Authorization = string.Empty;
            this.ConsumerId = string.Empty;
            this.ConsumerSecretKey = string.Empty;
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
                // 执行加载租户信息的回调方法
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
            if (this.IdentityAuthentication == null)
            {
                this.Session.Decode(this.Authorization, this.ConsumerSecretKey, this.ConsumerId);
            }
            else
            {
                // 执行登录身份认证的回调方法
                try
                {
                    this.Session = this.IdentityAuthentication.Invoke(this.Request) ?? new EngineSession();
                }
                catch (Exception e)
                {
                    Loger.Error($"IdentityAuthentication: {e.Message}");
                    throw;
                }
            }

            if (string.IsNullOrEmpty(this.Session.UserId) || this.Session.ExpireTime < DateTools.GetUnix())
            {
                this.Ready = false;
                this.Response.Headers.TryAdd("Access-Control-Expose-Headers", new Microsoft.Extensions.Primitives.StringValues("*"));
                this.Response.Headers.TryAdd("Authify-State", new Microsoft.Extensions.Primitives.StringValues("false"));
                this.OutMessage("unauthorized", 401, false);
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
        public void OutSuccess(object? data, string code = "200")
        {
            this.Output.data = data;
            this.Output.tips = false;
            this.Output.code = string.IsNullOrEmpty(code) ? "200" : code;
            this.Output.success = string.IsNullOrEmpty(code) || code == "200" ? true : false;
            this.Output.message = "success";
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