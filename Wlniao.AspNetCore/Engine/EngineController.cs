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
        /// 初始化上下文
        /// </summary>
        /// <returns></returns>
        protected IContext Invoke()
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

            if (_ctx.Continue)
            {
                //执行身份验证
                _ctx.Auth();
            }
            return _ctx;
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
            Invoke();
            if (mustAuthentication && _ctx.Session.NotValid)
            {
                if(_ctx.Continue)
                {
                    //前期处理就绪时，按默认输出处理
                    _ctx.OutMessage("unauthorized", 401, false);
                }
                if(_ctx.AuthFailed != null)
                {
                    //调用登录失败输出结果的回调方法，返回自定义输出
                    _ctx.AuthFailed(Request);
                }
                else 
                {
                    _ctx.HeaderOutput.TryAdd("Access-Control-Expose-Headers", "*");
                    _ctx.HeaderOutput.TryAdd("Authify-State", "false");
                }
                //身份未验证通过时，直接跳转到输出
                goto END;
            }
            //调用业务处理函数
            func(_ctx);
            END:

            foreach (var kv in _ctx.HeaderOutput)
            {
                Response.Headers.TryAdd(kv.Key, kv.Value);
            }
            return Content(_ctx.SerializeJsonOutput(), _ctx.HeaderOutput.GetString("Content-Type", "application/json"), Encoding.UTF8);
        }

    }
}
