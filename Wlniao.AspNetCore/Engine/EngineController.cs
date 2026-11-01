using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Wlniao;
using Wlniao.Crypto;
using Wlniao.Log;

namespace Wlniao.Engine
{
    /// <summary>
    /// 
    /// </summary>
    public class EngineController : Mvc.XCoreController
    {
        private readonly IContext _ctx;
        /// <summary>
        /// 【常量】需要登录
        /// </summary>
        protected const bool MustAuthentication = true;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ctx"></param>
        public EngineController(IContext ctx) : base()
        {
            _ctx = ctx;
        }

        /// <summary>
        /// 通用请求执行内容
        /// </summary>
        /// <returns></returns>
        private async Task<IContext> Invoke()
        {
            try
            {
                await _ctx.InitAsync(Request);
            }
            catch (AggregateException e)
            {
                Loger.Error($"Init Context: {e.InnerException.Message}{Environment.NewLine}{e.InnerException.StackTrace}");
                throw e.InnerException!;
            }
            catch (EngineException)
            {
                throw;
            }
            catch (Exception e)
            {
                Loger.Error($"Init Context: {e.Message}{Environment.NewLine}{e.StackTrace}");
                throw;
            }

            if (_ctx.Continue)
            {
                //执行身份验证
                await _ctx.AuthAsync();
            }
            return _ctx;
        }


        /// <summary>
        /// 执行请求调用并返回通用内容
        /// </summary>
        /// <param name="func"></param>
        /// <param name="mustAuthentication"></param>
        /// <returns></returns>
        [NonAction]
        protected async Task<IActionResult> Invoke(Func<IContext, Task<IActionResult>> func, bool mustAuthentication = false)
        {
            try
            {
                await Invoke();
                if (!mustAuthentication || !_ctx.Session.NotValid)
                {
                    return await func(_ctx);
                }

                if (_ctx.AuthFailedCallback != null)
                {
                    return _ctx.AuthFailedCallback();
                }

                return NotAuthorized();
            }
            catch (Exception e)
            {
                return ErrorResult(400, e);
            }
        }

        /// <summary>
        /// 执行请求调用并返回格式内容
        /// </summary>
        /// <param name="func"></param>
        /// <param name="mustAuthentication"></param>
        /// <returns></returns>
        [NonAction]
        protected async Task<IActionResult> Invoke(Func<IContext, Task<object>> func, bool mustAuthentication = false)
        {
            try
            {
                await Invoke();
                if (mustAuthentication && _ctx.Session.NotValid)
                {
                    if (_ctx.AuthFailedCallback != null)
                    {
                        return _ctx.AuthFailedCallback();
                    }

                    return NotAuthorized();
                }

                //调用业务处理函数
                var obj = await func(_ctx);
                _ctx.Output = obj!;
                ApplyHeaderOutput();
                return _ctx.OutputSerialize(obj);
            }
            catch (Exception e)
            {
                return ErrorResult(400, e);
            }
        }

        /// <summary>
        /// 非开发环境对外统一的异常提示，避免内部异常信息泄露给客户端
        /// </summary>
        protected const string DefaultErrorMessage = "系统异常，稍后可尝试重新提交";

        /// <summary>
        /// 输出统一格式的错误内容（HTTP 200 + 业务错误码，沿用既有约定）
        /// 必须经过序列化器转义，直接拼接异常内容会破坏 JSON（异常消息含引号、换行、反斜杠时）
        /// </summary>
        /// <param name="code">业务错误码</param>
        /// <param name="e">异常对象</param>
        /// <returns></returns>
        [NonAction]
        protected IActionResult ErrorResult(int code, Exception e)
        {
            Loger.Error($"Engine.Invoke: {e.Message}{Environment.NewLine}{e.StackTrace}");
            // 开发环境回传原始异常消息便于排查，生产环境统一泛化，与 ErrorHandling 中间件保持一致；
            // 需要向客户端传达具体文案时，请使用 ErrorResult(code, message) 或返回业务对象
            var message = XCore.IsDevTest ? e.Message : DefaultErrorMessage;
            return ErrorResult(code, message, Microsoft.AspNetCore.Http.StatusCodes.Status200OK);
        }

        /// <summary>
        /// 输出统一格式的错误内容
        /// </summary>
        /// <param name="code">业务错误码</param>
        /// <param name="message">错误消息</param>
        /// <returns></returns>
        [NonAction]
        protected IActionResult ErrorResult(int code, string message)
        {
            return ErrorResult(code, message, Microsoft.AspNetCore.Http.StatusCodes.Status200OK);
        }

        /// <summary>
        /// 输出未认证内容
        /// </summary>
        /// <returns></returns>
        [NonAction]
        protected IActionResult NotAuthorized()
        {
            return ErrorResult(401, "unauthorized", Microsoft.AspNetCore.Http.StatusCodes.Status401Unauthorized);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="code"></param>
        /// <param name="message"></param>
        /// <param name="statusCode"></param>
        /// <returns></returns>
        [NonAction]
        private IActionResult ErrorResult(int code, string message, int statusCode)
        {
            ApplyHeaderOutput();
            // code 必须保持 JSON 数字类型（与既有响应体 {"code":401,...} 一致），不能退化为字符串，
            // 否则按数字解析 code 的客户端会直接失败；JsonObject 不依赖反射，AOT / 裁剪安全
            var payload = new System.Text.Json.Nodes.JsonObject
            {
                ["code"] = code,
                ["message"] = message ?? string.Empty
            };
            return new ContentResult
            {
                Content = payload.ToJsonString(),
                ContentType = "application/json",
                StatusCode = statusCode
            };
        }

        /// <summary>
        /// 回写业务设置的响应头
        /// </summary>
        [NonAction]
        private void ApplyHeaderOutput()
        {
            foreach (var kv in _ctx.HeaderOutput)
            {
                Response.Headers.TryAdd(kv.Key, kv.Value);
            }
        }

    }
}
