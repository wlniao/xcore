/*==============================================================================
    文件名称：ErrorHandling.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：异常处理中间件
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
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Wlniao.Log;
using Wlniao.Mvc;

namespace Wlniao.Middleware
{
    /// <summary>
    /// 异常处理中间件选项
    /// </summary>
    public class ErrorHandlingOptions
    {
        /// <summary>
        /// 状态码
        /// </summary>
        public int StatusCode { get; set; } = 502;
    }
    /// <summary>
    /// 异常处理中间件扩展
    /// </summary>
    public static class ErrorHandlingExtension
    {
        /// <summary>
        /// 配置使用异常处理中间件
        /// </summary>
        /// <param name="app"></param>
        /// <param name="configureOptions"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseErrorHandling(this IApplicationBuilder app, Action<ErrorHandlingOptions> configureOptions)
        {
            var options = new ErrorHandlingOptions();
            configureOptions(options);
            return app.UseMiddleware<ErrorHandling>(options);
        }
    }
    /// <summary>
    /// 异常处理中间件
    /// </summary>
    public class ErrorHandling
    {
        private readonly RequestDelegate next;
        private readonly ErrorHandlingOptions options;
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="next"></param>
        /// <param name="options"></param>
        public ErrorHandling(RequestDelegate next, ErrorHandlingOptions options = null)
        {
            this.next = next;
            this.options = options ?? new ErrorHandlingOptions();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Invoke(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                Loger.Error($"{ex.Message}{Environment.NewLine}{ex.StackTrace}");
                await HandleExceptionAsync(context, options, ex.Message);
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="options"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        private static Task HandleExceptionAsync(HttpContext context, ErrorHandlingOptions options, string message)
        {
            try
            {
                if (!context.Response.Headers.ContainsKey("X-Wlniao-Debug"))
                {
                    context.Response.Headers.TryAdd("X-Wlniao-Debug", message);
                }
            }
            catch
            {
                // ignored
            }

            if (context.Request.Method == "POST" || context.Request.Query.ContainsKey("do"))
            {
                // context.Response.ContentType = "text/json";
                context.Response.StatusCode = options.StatusCode;
                return context.Response.WriteAsync(Json.Serialize(new { success = false, message = "系统异常，稍后可尝试重新提交" }));
            }
            else
            {
                // context.Response.ContentType = "text/html;charset=utf-8";
                return context.Response.WriteAsync(XCoreController.ErrorHtml.Replace("{{errorTitle}}", "错误").Replace("{{errorIcon}}", XCoreController.ErrorIcon).Replace("{{errorMsg}}", "系统异常，稍后可尝试重新提交"));
            }
        }
    }
}