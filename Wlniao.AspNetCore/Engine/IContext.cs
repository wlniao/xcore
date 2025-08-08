using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Wlniao;

namespace Wlniao.Engine
{
    /// <summary>
    /// 请求处理上下文接口
    /// </summary>
    public interface IContext
    {
        /// <summary>
        /// 是否初始化
        /// </summary>
        public bool Ready  { get; }
        /// <summary>
        /// 当前请求域名
        /// </summary>
        public string Host { get; }
        /// <summary>
        /// 请求输入内容
        /// </summary>
        public string Body { get; }
        /// <summary>
        /// 请求输入参数
        /// </summary>
        public Dictionary<string, string> Query { get; }
        /// <summary>
        /// 请求输入头信息
        /// </summary>
        public Dictionary<string, string> HeaderInput { get; }
        /// <summary>
        /// 请求输出头信息
        /// </summary>
        public Dictionary<string, string> HeaderOutput { get; set; }
        /// <summary>
        /// 请求是否启用Https
        /// </summary>
        public bool IsHttps { get; }
        /// <summary>
        /// 请求客户端IP
        /// </summary>
        public string ClientIP { get; }
        /// <summary>
        /// 请求身份凭据
        /// </summary>
        public string Authorization { get; }
        /// <summary>
        /// 多租户系统标识
        /// </summary>
        public string ConsumerId { get; }
        /// <summary>
        /// 多租户系统安全密钥
        /// </summary>
        public string ConsumerSecretKey { get; }
        /// <summary>
        /// 当前登录认证信息
        /// </summary>
        public EngineSession Session { get; }

        /// <summary>
        /// 系统用户识别的回调方法，返回系统用户信息
        /// </summary>
        public Func<HttpRequest, ConsumerInfo>? LoadConsumerInfo { get; set; }
        
        /// <summary>
        /// 登录身份认证的回调方法，返回统一会话状态
        /// </summary>
        public Func<HttpRequest, EngineSession>? IdentityAuthentication { get; set; }
        
        /// <summary>
        /// 请求安全认证的回调方法，返回请求加密/解密密钥
        /// </summary>
        public Func<HttpRequest, string>? SafetyCertification { get; set; }
        
        /// <summary>
        /// 通过请求初始化实例内容
        /// </summary>
        /// <param name="httpRequest"></param>
        public void Init(HttpRequest httpRequest);
        
        /// <summary>
        /// 执行身份验证（未验证通过时需将Ready设置为false）
        /// </summary>
        public bool Auth();
    
        /// <summary>
        /// 请求内容解析
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T InputDeserialize<T>();
    
        /// <summary>
        /// 请求内容解析
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, object> InputDeserialize();
    
        /// <summary>
        /// 输出正确内容及状态码
        /// </summary>
        /// <param name="data"></param>
        /// <param name="code"></param>
        public void OutSuccess(object? data, string code = "200");
    
        /// <summary>
        /// 输出错误内容及状态码
        /// </summary>
        /// <param name="message"></param>
        /// <param name="code"></param>
        /// <param name="tips"></param>
        public void OutMessage(string message, int code, bool tips = false);
        
        /// <summary>
        /// 输出错误内容及状态码
        /// </summary>
        /// <param name="message"></param>
        /// <param name="code"></param>
        /// <param name="tips"></param>
        public void OutMessage(string message, string code = "", bool tips = false);
    
        /// <summary>
        /// 输出Json序列化内容
        /// </summary>
        /// <returns></returns>
        public string SerializeJsonOutput();
    }
}