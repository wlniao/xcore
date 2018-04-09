using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Wlniao.OpenApi
{
    /// <summary>
    /// 推送工具
    /// </summary>
    public class Push
    {
        /// <summary>
        /// 发送邮件
        /// </summary>
        /// <param name="to">收件人邮箱地址</param>
        /// <param name="title">邮件标题</param>
        /// <param name="body">邮件正文</param>
        /// <param name="from">发件人邮箱地址</param>
        /// <param name="user">发件箱登录帐号</param>
        /// <param name="pwd">发件箱登录密码</param>
        /// <param name="smtp">SMTP邮件服务器</param>
        /// <param name="useGet">是否使用GET方式发起请求</param>
        /// <returns></returns>
        public static Boolean SendEmail(String to, String title, String body
            , String from = "", String user = "", String pwd = "", String smtp = "", Boolean useGet = true)
        {
            if (useGet && body.Length < 300)
            {
                var rlt = XServer.Common.Get("openapi", "push", "email"
                    , new KeyValuePair<string, string>("to", to)
                    , new KeyValuePair<string, string>("title", strUtil.UrlEncode(title))
                    , new KeyValuePair<string, string>("body", strUtil.UrlEncode(body))
                    , new KeyValuePair<string, string>("from", strUtil.UrlEncode(from))
                    , new KeyValuePair<string, string>("user", strUtil.UrlEncode(user))
                    , new KeyValuePair<string, string>("pwd", strUtil.UrlEncode(pwd))
                    , new KeyValuePair<string, string>("smtp", strUtil.UrlEncode(smtp))
                    );
                return string.IsNullOrEmpty(rlt) ? false : rlt.ToLower() == "true";
            }
            else
            {
                var rlt = XServer.Common.Post("openapi", "push", "email", body
                    , new KeyValuePair<string, string>("to", to)
                    , new KeyValuePair<string, string>("title", strUtil.UrlEncode(title))
                    , new KeyValuePair<string, string>("from", strUtil.UrlEncode(from))
                    , new KeyValuePair<string, string>("user", strUtil.UrlEncode(user))
                    , new KeyValuePair<string, string>("pwd", strUtil.UrlEncode(pwd))
                    , new KeyValuePair<string, string>("smtp", strUtil.UrlEncode(smtp))
                    );
                return string.IsNullOrEmpty(rlt) ? false : rlt.ToLower() == "true";
            }
        }
    }
}
