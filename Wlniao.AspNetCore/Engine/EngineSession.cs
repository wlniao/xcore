using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Wlniao.Text;

namespace Wlniao.Engine
{
    /// <summary>
    /// 统一会话状态
    /// </summary>
    public class EngineSession
    {
        /// <summary>
        /// 认证是否有效
        /// </summary>
        public bool IsValid => ExpireTime > DateTools.GetUnix() && !string.IsNullOrEmpty(UserId);
        
        /// <summary>
        /// 过期时间
        /// </summary>
        public long ExpireTime { get; set; }
        
        /// <summary>
        /// 用户标识
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// 用户姓名
        /// </summary>
        public string? Name { get; set; }
        
        /// <summary>
        /// 登录账号
        /// </summary>
        public string? Account { get; set; }

        /// <summary>
        /// 所在部门列表
        /// </summary>
        public string? DepartmentIds { get; set; } = "";

        /// <summary>
        /// 扩展数据
        /// </summary>
        public Dictionary<string, string> ExtData { get; set; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 对当前状态进行编码加密
        /// </summary>
        /// <param name="consumerSecretKey"></param>
        /// <param name="currentConsumerId"></param>
        /// <param name="exprieSeconds"></param>
        /// <returns></returns>
        public string Encode(string consumerSecretKey, string currentConsumerId = null, int exprieSeconds = 7200)
        {
            var extend = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(this.Name))
            {
                extend.Add("n", this.Name);
            }

            if (!string.IsNullOrEmpty(this.Account))
            {
                extend.Add("a", this.Account);
            }

            if (!string.IsNullOrEmpty(this.DepartmentIds))
            {
                extend.Add("d", this.DepartmentIds);
            }

            foreach (var kv in (this.ExtData ?? new Dictionary<string, string>()))
            {
                extend.TryAdd(kv.Key, kv.Value);
            }

            this.ExpireTime = DateTools.GetUnix() + exprieSeconds;
            var extStr = System.Text.Json.JsonSerializer.Serialize(extend, XCore.JsonSerializerOptions);
            var extHex = StringUtil.UTF8ToHexString(extStr);
            var plain = $"{this.ExpireTime},{this.UserId},{currentConsumerId},{extHex}".Trim(',');
            return Encryptor.SM4EncryptECBToHex(plain, consumerSecretKey, true);
        }

        /// <summary>
        /// 从身份令牌解密编码当前状态
        /// </summary>
        /// <param name="authorization"></param>
        /// <param name="consumerSecretKey"></param>
        /// <param name="currentConsumerId"></param>
        public void Decode(string authorization, string consumerSecretKey, string currentConsumerId = null)
        {
            var data = Encryptor.SM4DecryptECBFromHex(authorization, consumerSecretKey).Split(',');
            if (data.Length < 4 || string.IsNullOrEmpty(data[1]) || (!string.IsNullOrEmpty(data[2]) && !string.IsNullOrEmpty(currentConsumerId) && data[2] != currentConsumerId))
            {
                throw new Exception("Authorization error");
            }
            else
            {
                this.ExpireTime = Wlniao.Convert.ToLong(data[0]);
                this.UserId = data[1];
                if (!string.IsNullOrEmpty(data.LastOrDefault()))
                {
                    var plain = StringUtil.HexStringToUTF8(data.LastOrDefault());
                    var kvs = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(plain, XCore.JsonSerializerOptions);
                    foreach (var kv in kvs ?? new Dictionary<string, string>())
                    {
                        switch (kv.Key)
                        {
                            case "n":
                                this.Name = kv.Value;
                                break;
                            case "u":
                                this.Account = kv.Value;
                                break;
                            case "d":
                                this.DepartmentIds = kv.Value;
                                break;
                            default:
                                this.ExtData.TryAdd(kv.Key, kv.Value);
                                break;
                        }
                    }
                }
                
                if (string.IsNullOrEmpty(this.Name))
                {
                    this.Name = "";
                }
                if (string.IsNullOrEmpty(this.Account))
                {
                    this.Account = "";
                }
                if (string.IsNullOrEmpty(this.UserId))
                {
                    this.ExpireTime = 0;
                }
            }
            
        }

    }
}