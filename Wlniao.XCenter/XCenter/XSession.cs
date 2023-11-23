using System;
using System.Collections.Generic;
using System.Text;

namespace Wlniao.XCenter
{
    /// <summary>
    /// 统一会话状态
    /// </summary>
    public class XSession
    {
        /// <summary>
        /// 账号Sid
        /// </summary>
        public string sid = "";
        /// <summary>
        /// Session Key
        /// </summary>
        public string key = "";
        /// <summary>
        /// 系统Wkey
        /// </summary>
        public string wkey = "";
        /// <summary>
        /// 账号名称
        /// </summary>
        public string name = "";
        /// <summary>
        /// 登录账号
        /// </summary>
        public string account = "";
        /// <summary>
        /// 所在部门（多个）
        /// </summary>
        public string departments = "";
        /// <summary>
        /// 当前平台
        /// </summary>
        public string platform = "";
        /// <summary>
        /// 当前平台Id
        /// </summary>
        public string platformId = "";
        /// <summary>
        /// 是否登录
        /// </summary>
        public bool login = false;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="key"></param>
        public XSession(EmiContext ctx, String key)
        {
            this.key = key;
            if (ctx != null && !string.IsNullOrEmpty(key))
            {
                var jsonStr = Wlniao.Cache.Get("wsession_" + key);
                if (string.IsNullOrEmpty(jsonStr))
                {
                    jsonStr = ctx.EmiGet("app", "wsession", new KeyValuePair<string, string>("key", key));
                    Wlniao.Cache.Set("wsession_" + key, jsonStr, 60);
                }
                var res = Wlniao.Json.ToObject<Dictionary<string, object>>(jsonStr);
                if (res != null && res.GetBoolean("success"))
                {
                    this.sid = res.GetString("sid");
                    this.name = res.GetString("name");
                    this.account = res.GetString("account");
                    this.departments = res.GetString("departments");
                    this.platform = res.GetString("platform");
                    this.platformId = res.GetString("platformId");
                    this.wkey = ctx.owner;
                    this.login = true;
                }
            }
        }
    }
}