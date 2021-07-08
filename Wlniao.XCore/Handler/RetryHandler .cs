using System;
using System.Collections.Generic;
using System.Text;
namespace Wlniao.Handler
{
    /// <summary>
    /// 
    /// </summary>
    public class RetryHandler : PipelineHandler
    {
        private static int WLN_WEAPP_RETRY = -1;
        /// <summary>
        /// 重试次数
        /// </summary>
        private static int MaxRetryTimes
        {
            get
            {
                if (WLN_WEAPP_RETRY < 0)
                {
                    var _WLN_WEAPP_RETRY = Wlniao.Config.GetSetting("WLN_WEAPP_RETRY");
                    if (string.IsNullOrEmpty(_WLN_WEAPP_RETRY))
                    {
                        if (Wlniao.Config.GetSetting("WLN_RETRY") != "false")
                        {
                            WLN_WEAPP_RETRY = 3;
                        }
                        else
                        {
                            WLN_WEAPP_RETRY = 0;
                        }
                    }
                    else
                    {
                        WLN_WEAPP_RETRY = cvt.ToInt(_WLN_WEAPP_RETRY);
                    }
                }
                return WLN_WEAPP_RETRY;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public RetryHandler(PipelineHandler handler) : base(handler) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ctx"></param>
        public override void HandleBefore(IContext ctx)
        {
            inner.HandleBefore(ctx);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ctx"></param>
        public override void HandleAfter(IContext ctx)
        {
            var _ctx = (IContext)ctx;
            while (true)
            {
                Exception exceptionForRetry = null;
                try
                {
                    inner.HandleAfter(ctx);
                }
                catch (Exception exception)
                {
                    exceptionForRetry = exception;
                }
                if (_ctx.Retry >= MaxRetryTimes)
                {
                    if (exceptionForRetry != null)
                    {
                        throw exceptionForRetry;
                    }
                    return;
                }
                if (exceptionForRetry == null)
                {
                    return;
                }
                _ctx.Retry++;
                System.Threading.Tasks.Task.Delay(1000 * _ctx.Retry).Wait();
                inner.HandleBefore(_ctx);
            }
        }
    }
}