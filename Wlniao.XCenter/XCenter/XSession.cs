using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace Wlniao.XCenter
{
    /// <summary>
    /// 统一会话状态
    /// </summary>
    public class XSession
    {
        /// <summary>
        /// 
        /// </summary>
        public string sid = "";
        /// <summary>
        /// 
        /// </summary>
        public string wkey = "";
        /// <summary>
        /// 来自EmiContext
        /// </summary>
        private string apptoken = "";
        /// <summary>
        /// 过期时间
        /// </summary>
        public long ExpireTime { get; set; }
        /// <summary>
        /// 平台标识
        /// </summary>
        public string AppCode { get; set; }
        /// <summary>
        /// 租户标识
        /// </summary>
        public string OwnerId { get { return wkey; } set { wkey = value; } }
        /// <summary>
        /// 用户标识
        /// </summary>
        public string UserSid { get { return sid; } set { sid = value; } }
        /// <summary>
        /// 扩展数据
        /// </summary>
        public Dictionary<string, object> ExtData { get; set; }
        /// <summary>
        /// 认证是否有效
        /// </summary>
        public bool IsValid
        {
            get
            {
                if (ExpireTime > XCore.NowUnix && !string.IsNullOrEmpty(UserSid))
                {
                    return true;
                }
                return false;
            }
        }
        /// <summary>
        /// 用户姓名
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 登录账号
        /// </summary>
        public string Account { get; set; }
        /// <summary>
        /// 所在部门列表
        /// </summary>
        public string DepartmentIds { get; set; }

        /// <summary>
        /// 通过Token生成Session
        /// </summary>
        /// <param name="ctx"></param>
        public XSession(Context? ctx)
        {
            if (ctx != null)
            {
                this.AppCode = ctx.app;
                this.OwnerId = ctx.owner;
                this.apptoken = ctx.token;
            }
            this.ExtData = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 通过令牌生成Session
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="token"></param>
        /// <param name="ticket"></param>
        public XSession(Context ctx, string token, String ticket)
        {
            this.ExtData = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (ctx != null && !string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(ticket))
            {
                try
                {
                    var data = Encryptor.SM4DecryptECBFromHex(ticket, token).Split(',', StringSplitOptions.RemoveEmptyEntries);
                    if (data.Length > 4)
                    {
                        this.ExpireTime = cvt.ToLong(data[0]);
                        this.AppCode = data[1];
                        this.OwnerId = data[2];
                        this.UserSid = data[3];
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(ctx.message) && !string.IsNullOrEmpty(Context.XCenterApp) && !string.IsNullOrEmpty(Context.XCenterToken) && !string.IsNullOrEmpty(Context.XCenterOwner))
                        {
                            ctx.message = "程序配置错误，请检查XCenterToken相关配置";
                            log.Error("程序配置错误，建议检查XCenterApp与XCenterToken是否匹配");
                        }
                        else
                        {
                            ctx.message = "";
                        }
                    }
                    if (data.Length == 5)
                    {
                        var plain = strUtil.HexStringToUTF8(data[4]);
                        var kvs = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(plain, new System.Text.Json.JsonSerializerOptions { });
                        if (kvs != null)
                        {
                            foreach (var kv in kvs)
                            {
                                if (kv.Key == "n")
                                {
                                    this.Name = kv.Value;
                                }
                                else if (kv.Key == "u")
                                {
                                    this.Account = kv.Value;
                                }
                                else if (kv.Key == "d")
                                {
                                    this.DepartmentIds = kv.Value;
                                }
                                else
                                {
                                    this.ExtData.TryAdd(kv.Key, kv.Value);
                                }
                            }
                        }
                    }
                }
                catch { }
                if (this.Name == null)
                {
                    this.Name = "";
                }
                if (this.Account == null)
                {
                    this.Account = "";
                }
                if (this.AppCode == null)
                {
                    this.AppCode = "";
                }
                if (this.DepartmentIds == null)
                {
                    this.DepartmentIds = "";
                }
                if (string.IsNullOrEmpty(this.UserSid))
                {
                    this.ExpireTime = 0;
                }
            }
        }

        /// <summary>
        /// 生成用户登录凭据
        /// </summary>
        /// <param name="token"></param>
        /// <param name="exprie"></param>
        /// <returns></returns>
        public String BuildTicket(string token, int exprie = 7200)
        {
            var plain = (XCore.NowUnix + exprie) + "," + (string.IsNullOrEmpty(AppCode) ? "app" : AppCode) + "," + (string.IsNullOrEmpty(OwnerId) ? "000000000" : OwnerId);
            if (!string.IsNullOrEmpty(UserSid))
            {
                var obj = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrEmpty(this.Name))
                {
                    obj.Add("n", this.Name);
                }
                if (!string.IsNullOrEmpty(this.Account))
                {
                    obj.Add("u", this.Account);
                }
                if (!string.IsNullOrEmpty(this.DepartmentIds))
                {
                    obj.Add("d", this.DepartmentIds);
                }
                if (ExtData != null)
                {
                    foreach (var kv in ExtData)
                    {
                        obj.TryAdd(kv.Key, kv.Value);
                    }
                }
                var ext = System.Text.Json.JsonSerializer.Serialize(obj, new JsonSerializerOptions { Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) });
                plain += "," + UserSid + "," + strUtil.UTF8ToHexString(ext);
            }
            return Encryptor.SM4EncryptECBToHex(plain, token);
        }
    }
}