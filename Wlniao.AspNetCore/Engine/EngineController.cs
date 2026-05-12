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

                foreach (var kv in _ctx.HeaderOutput)
                {
                    Response.Headers.TryAdd(kv.Key, kv.Value);
                }

                return Content("{\"code\":401,\"message\":\"unauthorized\"}", "application/json");
            }
            catch (Exception e)
            {
                return Content("{\"code\":400,\"message\":\"" + e.Message + "\"}", "application/json");
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

                    foreach (var kv in _ctx.HeaderOutput)
                    {
                        Response.Headers.TryAdd(kv.Key, kv.Value);
                    }

                    Response.StatusCode = 401;
                    return Content("{\"code\":401,\"message\":\"unauthorized\"}", "application/json");
                }

                //调用业务处理函数
                var obj = await func(_ctx);
                _ctx.Output = obj!;
                foreach (var kv in _ctx.HeaderOutput)
                {
                    Response.Headers.TryAdd(kv.Key, kv.Value);
                }

                return _ctx.OutputSerialize(obj);
            }
            catch (Exception e)
            {
                return Content("{\"code\":400,\"message\":\"" + e.Message + "\"}", "application/json");
            }
        }

    }
}
