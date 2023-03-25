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
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Wlniao.Middleware
{
    /// <summary>
    /// 异常处理中间件
    /// </summary>
    public class ErrorHandling
    {
        private readonly RequestDelegate next;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="next"></param>
        public ErrorHandling(RequestDelegate next)
        {
            this.next = next;
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
                Wlniao.log.Error(ex.Message);
                await HandleExceptionAsync(context, ex.Message);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static Task HandleExceptionAsync(HttpContext context, String message)
        {
            try
            {
                if (!context.Response.Headers.ContainsKey("X-Wlniao-Debug"))
                {
                    context.Response.Headers.Add("X-Wlniao-Debug", message);
                }
            }
            catch { }
            if (context.Request.Method == "POST" || context.Request.Query.ContainsKey("do"))
            {
                context.Response.ContentType = "text/json";
                return context.Response.WriteAsync(Wlniao.Json.ToString(new { success = false, message = "系统异常，稍后可尝试重新提交" }));
            }
            else
            {
                context.Response.ContentType = "text/html;charset=utf-8";
                return context.Response.WriteAsync(XCoreController.ErrorHtml.Replace("{{errorTitle}}", "错误").Replace("{{errorIcon}}", XCoreController.ErrorIcon).Replace("{{errorMsg}}", "系统异常，稍后可尝试重新提交"));
            }
        }
    }
}