using System;
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
    public class EngineController : XCoreController
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
            this._ctx = ctx;
        }

        /// <summary>
        /// 执行请求调用
        /// </summary>
        /// <param name="func"></param>
        /// <param name="mustAuthentication"></param>
        /// <returns></returns>
        [NonAction]
        protected IActionResult Invoke(Action<IContext> func, bool mustAuthentication = false)
        {
            try
            {
                _ctx.Init(Request);
            }
            catch (Exception e)
            {
                Loger.Error($"Init Context: {e.Message}");
                throw;
            }
            if (_ctx.Ready && mustAuthentication && !_ctx.Auth())
            {
                //身份未验证通过时，直接跳转到输出
                goto END;
            }

            if (_ctx.Ready)
            {
                //调用业务处理函数
                func(_ctx);
            }
            END:
            return JsonStr(_ctx.SerializeJsonOutput());
        }

    }
}
