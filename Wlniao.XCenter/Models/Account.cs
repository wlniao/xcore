using System;
using System.Collections.Generic;
using System.Text;

namespace Wlniao.XCenter.Models
{
    /// <summary>
    /// 
    /// </summary>
    public class Account
    {
        /// <summary>
        /// 
        /// </summary>
        public string sid { get; set; }
        /// <summary>
        /// 头像
        /// </summary>
        public string avatar { get; set; }
        /// <summary>
        /// 登录帐号
        /// </summary>
        public string account { get; set; }
        /// <summary>
        /// 姓名
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// 职位
        /// </summary>
        public string position { get; set; }
        /// <summary>
        /// 部门
        /// </summary>
        public string department { get; set; }
        /// <summary>
        /// 工号
        /// </summary>
        public string jobnumber { get; set; }
        /// <summary>
        /// 电话号码
        /// </summary>
        public string telphone { get; set; }
        /// <summary>
        /// 微信绑定Id
        /// </summary>
        public string wxopenid { get; set; }
        /// <summary>
        /// 企业微信Id
        /// </summary>
        public string wxuserid { get; set; }
        /// <summary>
        /// 钉钉绑定Id
        /// </summary>
        public string dinguserid { get; set; }

        /// <summary>
        /// 获取一个账号信息
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="key"></param>
        /// <param name="by"></param>
        /// <returns></returns>
        public static Account Get(EmiContext ctx, string key, string by = "sid")
        {
            if (string.IsNullOrEmpty(key) || ctx == null)
            {
                return null;
            }
            var rlt = ctx.EmiGet<Account>("app", "getaccount", new KeyValuePair<string, string>(by, key));
            if (rlt.success)
            {
                return rlt.data;
            }
            return null;
        }
        /// <summary>
        /// 根据微信OpenId获取
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="wxopenid"></param>
        /// <returns></returns>
        public static Account GetByOpenId(EmiContext ctx, string wxopenid)
        {
            return Get(ctx, wxopenid, "wxopenid");
        }
        /// <summary>
        /// 获取一个账号信息
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="sid"></param>
        /// <returns></returns>
        public static string GetName(EmiContext ctx, string sid)
        {
            if (string.IsNullOrEmpty(sid) || ctx == null)
            {
                return "";
            }
            var val = Caching.Cache.Get("sidname-" + ctx.owner + "-" + sid);
            if (string.IsNullOrEmpty(val))
            {
                var rlt = ctx.EmiGet<Account>("app", "getaccount", new KeyValuePair<string, string>("sid", sid));
                if (rlt.success && !string.IsNullOrEmpty(rlt.data.name))
                {
                    val = rlt.data.name;
                    Caching.Cache.Set("sidname-" + ctx.owner + "-" + sid, val, 3600);
                }
                else
                {
                    val = "";
                }
            }
            return val;
        }
    }
}