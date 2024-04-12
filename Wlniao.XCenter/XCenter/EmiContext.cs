using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Reflection.Emit;
using System.Security.Principal;
using System.Text;
using Wlniao;
using static System.Net.Mime.MediaTypeNames;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Newtonsoft.Json.Linq;
using Wlniao.Serialization;
using System.Text.Json.Serialization;

namespace Wlniao.XCenter
{
    /// <summary>
    /// 交互状态
    /// </summary>
    public class EmiContext : Context
    {
        private static string emiDomain = "";
        private static string EmiToken = Wlniao.Config.GetConfigs("EmiToken");
        private static string EmiDomain
        {
            get
            {
                if (string.IsNullOrEmpty(emiDomain))
                {
                    emiDomain = Wlniao.Config.GetConfigs("EmiDomain");
                    if (!string.IsNullOrEmpty(emiDomain))
                    {
                        emiDomain = emiDomain.Substring(emiDomain.IndexOf("://") + 3);
                    }
                }
                return emiDomain;
            }
        }

        /// <summary>
        /// CDN服务地址
        /// </summary>
        public string cdn { get; set; }
        /// <summary>
        /// 是否支持Https
        /// </summary>
        public Boolean https { get; set; }
        /// <summary>
        /// 是否已经注册
        /// </summary>
        public Boolean install { get; set; }
        /// <summary>
        /// 下次注册时间
        /// </summary>
        public DateTime register { get; set; }
        /// <summary>
        /// CDN服务地址
        /// </summary>
        public string apptoken { get; set; }
        /// <summary>
        /// EMI服务器地址
        /// </summary>
        [JsonIgnore]
        public string EmiHost
        {
            get
            {
                if (string.IsNullOrEmpty(domain) && !string.IsNullOrEmpty(EmiDomain))
                {
                    domain = EmiDomain;
                }

                if (https)
                {
                    return "https://" + domain;
                }
                else
                {
                    return "http://" + domain;
                }
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    domain = value.Trim().Trim('/');
                    if (domain.IndexOf("://") > 0)
                    {
                        domain = domain.Substring(domain.IndexOf("://") + 3);
                    }
                }
            }
        }
        /// <summary>
        /// CDN服务前缀
        /// </summary>
        [JsonIgnore]
        public string CdnPrefix
        {
            get
            {
                if (string.IsNullOrEmpty(cdn))
                {
                    return EmiHost;
                }
                else
                {
                    return cdn;
                }
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    cdn = "";
                }
                else
                {
                    cdn = value.Trim().TrimEnd('/');
                }
            }
        }

        /// <summary>
        /// 根据host生成一个EmiContext对象
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public static EmiContext Load(Context ctx)
        {
            var emi = Cache.Get<EmiContext>("emi_context_" + ctx.domain);
            if (emi == null || !emi.install)
            {
                emi = new EmiContext()
                {
                    app = ctx.app,
                    name = ctx.name,
                    brand = ctx.brand,
                    owner = ctx.owner,
                    domain = ctx.domain,
                    message = ctx.message,
                    register = DateTime.MinValue,
                    apptoken = string.IsNullOrEmpty(ctx.token) ? EmiToken : Encryptor.Md5Encryptor16(ctx.token).ToLower(),
                    https = true
                };
                if (!string.IsNullOrEmpty(EmiDomain))
                {
                    emi.domain = EmiDomain;
                }
                if (!string.IsNullOrEmpty(ctx.message))
                {
                    emi.message = ctx.message;
                }
                else if (string.IsNullOrEmpty(ctx.app))
                {
                    emi.message = "参数XCenterApp未配置，请配置";
                }
                else
                {
                    var check = emi.EmiGet<Dictionary<string, object>>("app", "check", new KeyValuePair<string, string>("app", ctx.app));
                    if (check.success || check.message == "install")
                    {
                        emi.install = true;
                        emi.register = DateTime.Now.AddMinutes(5);
                        if (check.data != null)
                        {
                            emi.cdn = check.data.GetString("cdn");
                        }
                        Cache.Set("emi_context_" + ctx.domain, emi, 600);
                    }
                    else if (check.success)
                    {
                        emi.install = false;
                        emi.register = DateTime.MinValue;
                        emi.message = "模块未安装，请先安装";
                    }
                    else if (check.message == "request is expired")
                    {
                        emi.message = "请求超时，请检查服务器时间是否同步";
                    }
                    else if (check.message == "token not config")
                    {
                        emi.message = "EmiToken参数未配置，请先配置或注册";
                    }
                    else if (check.message.Contains("token error"))
                    {
                        emi.message = "EmiToken参数配置错误，请重新配置或注册";
                    }
                    else if (check.message == "request exception")
                    {
                        emi.message = "Emi服务器链接失败，请确保服务器已启动并检查您填写的地址是否正确!";
                    }
                    else
                    {
                        emi.message = check.message;
                    }
                }
            }
            return emi;
        }

        /// <summary>
        /// 生成访问连接
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="controller"></param>
        /// <param name="action"></param>
        /// <param name="kvList"></param>
        /// <returns></returns>
        private static String CreateUrl(EmiContext ctx, string controller, string action, List<KeyValuePair<String, String>> kvList)
        {
            var url = ctx.EmiHost + "/" + controller + "/" + action;
            #region 处理接口基本参数及签名
            if (!string.IsNullOrEmpty(ctx.apptoken))
            {
                kvList.Add(new KeyValuePair<String, String>("timespan", DateTools.GetUnix().ToString()));
                kvList = kvList.OrderBy(o => o.Key).ToList();
                var values = new System.Text.StringBuilder();
                foreach (var kv in kvList)
                {
                    if (!string.IsNullOrEmpty(kv.Key))
                    {
                        values.Append(kv.Value);
                    }
                }
                values.Append(ctx.apptoken);
                kvList.Add(new KeyValuePair<String, String>("sig", Wlniao.Encryptor.Md5Encryptor32(values.ToString())));
            }
            #endregion
            #region 拼接请求参数
            foreach (var kv in kvList)
            {
                url += url.IndexOf('?') > 0 ? "&" : "?";
                url += kv.Key + "=" + kv.Value;
            }
            #endregion
            return url;
        }


        /// <summary>
        /// Get请求Emi服务器
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="action"></param>
        /// <param name="kvs"></param>
        /// <returns>服务器返回的泛型实例</returns>
        public String EmiGet(string controller, string action, params KeyValuePair<string, string>[] kvs)
        {
            try
            {
                var rlt = new ApiResult<Object>();
                var list = new List<KeyValuePair<string, string>>();
                foreach (var kv in kvs)
                {
                    list.Add(kv);
                }
                if (kvs == null || !kvs.Where(o => o.Key.ToLower() == "app").Any())
                {
                    list.Add(new KeyValuePair<string, string>("app", this.app));
                }
                var start = DateTime.Now;
                var uri = new Uri(CreateUrl(this, controller, action, list));
                var json = XServer.Common.GetResponseString(uri.ToString());
                if (log.LogLevel <= Wlniao.Log.LogLevel.Debug)
                {
                    Wlniao.Log.Loger.Topic("emi", "msgid:" + strUtil.CreateLongId() + ", " + uri.Scheme + "://" + uri.Host + uri.AbsolutePath + ", usetime:" + DateTime.Now.Subtract(start).TotalMilliseconds.ToString("F2") + "ms\n >>> " + uri.Query + "[Get]\n <<< " + json);
                }
                return json;
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { code = "", success = false, message = "EMI服务器请求异常：" + ex.Message }, new JsonSerializerOptions { Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) });
            }
        }
        /// <summary>
        /// Get请求Emi服务器
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="controller"></param>
        /// <param name="action"></param>
        /// <param name="kvs"></param>
        /// <returns>服务器返回的泛型实例</returns>
        public ApiResult<T> EmiGet<T>(string controller, string action, params KeyValuePair<string, string>[] kvs)
        {
            try
            {
                var list = new List<KeyValuePair<string, string>>();
                foreach (var kv in kvs)
                {
                    list.Add(kv);
                }
                if (kvs == null || !kvs.Where(o => o.Key.ToLower() == "app").Any())
                {
                    list.Add(new KeyValuePair<string, string>("app", this.app));
                }
                var start = DateTime.Now;
                var uri = new Uri(CreateUrl(this, controller, action, list));
                var json = XServer.Common.GetResponseString(uri.ToString());
                if (log.LogLevel <= Wlniao.Log.LogLevel.Debug)
                {
                    Wlniao.Log.Loger.Topic("emi", uri.Scheme + "://" + uri.Host + uri.AbsolutePath + ", msgid:" + strUtil.CreateLongId() + ", usetime:" + DateTime.Now.Subtract(start).TotalMilliseconds.ToString("F2") + "ms\n >>> " + uri.Query + "[Get]\n <<< " + json);
                }
                return JsonSerializer.Deserialize<ApiResult<T>>(json, new JsonSerializerOptions { });
            }
            catch (Exception ex)
            {
                return new ApiResult<T> { message = "EMI服务器请求异常：" + ex.Message, data = default(T) };
            }
        }

        /// <summary>
        /// Get请求Emi服务器
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="controller"></param>
        /// <param name="action"></param>
        /// <param name="postdata"></param>
        /// <param name="kvs"></param>
        /// <returns>服务器返回的泛型实例</returns>
        public ApiResult<T> EmiPost<T>(string controller, string action, string postdata, params KeyValuePair<string, string>[] kvs)
        {
            try
            {
                var list = new List<KeyValuePair<string, string>>();
                foreach (var kv in kvs)
                {
                    list.Add(kv);
                }
                if (kvs == null || !kvs.Where(o => o.Key.ToLower() == "app").Any())
                {
                    list.Add(new KeyValuePair<string, string>("app", this.app));
                }
                var start = DateTime.Now;
                var uri = new Uri(CreateUrl(this, controller, action, list));
                var json = XServer.Common.PostResponseString(uri.ToString(), postdata);
                if (log.LogLevel <= Wlniao.Log.LogLevel.Debug)
                {
                    Wlniao.Log.Loger.Topic("emi", uri.Scheme + "://" + uri.Host + uri.AbsolutePath + ", msgid:" + strUtil.CreateLongId() + ", usetime:" + DateTime.Now.Subtract(start).TotalMilliseconds.ToString("F2") + "ms\n >>> " + postdata + "\n <<< " + json);
                }
                return JsonSerializer.Deserialize<ApiResult<T>>(json, new JsonSerializerOptions { });
            }
            catch (Exception ex)
            {
                return new ApiResult<T> { message = "EMI服务器请求异常：" + ex.Message, data = default(T) };
            }
        }

        /// <summary>
        /// 通过EMI上传文件
        /// </summary>
        /// <param name="data"></param>
        /// <param name="name"></param>
        /// <param name="ticket"></param>
        /// <returns></returns>
        public ApiResult<String> EmiUpload(byte[] data, string name, string ticket = null)
        {
            try
            {
                var url = this.EmiHost + "/upload";
                if (string.IsNullOrEmpty(ticket))
                {
                    url += ("?ticket=" + Encryptor.SM4EncryptECBToHex((XCore.NowUnix + 300).ToString(), this.apptoken));
                }
                else
                {
                    url += ("?ticket=" + ticket);
                }
                var content = new MultipartFormDataContent();
                content.Add(new ByteArrayContent(data), "file", name);
                var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = XCore.ServerCertificateCustomValidationCallback };
                using (var client = new System.Net.Http.HttpClient(handler))
                {
                    var response = client.PostAsync(url, content).GetAwaiter().GetResult();
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var res = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        var result = JsonSerializer.Deserialize<ApiResult<String>>(res, new JsonSerializerOptions { Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) });
                        if (result == null)
                        {
                            result = new ApiResult<String> { message = "上传结果返回无效，请稍后再试" };
                        }
                        return result;
                    }
                    else
                    {
                        throw new Exception("StatusCode:" + response.StatusCode + " " + response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                    }
                }
            }
            catch (Exception ex)
            {
                return new ApiResult<string> { message = ex.Message };
            }
        }

        /// <summary>
        /// 获取Label值
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="label">默认值</param>
        /// <returns></returns>
        public string GetLabel(String key, String label = "")
        {
            if (string.IsNullOrEmpty(key))
            {
                return "";
            }
            var val = Cache.Get("emi_" + this.owner + "_label_" + key);
            if (string.IsNullOrEmpty(val))
            {
                var rlt = EmiGet<String>("app", "getlabel", new KeyValuePair<string, string>("key", key), new KeyValuePair<string, string>("label", label));
                if (rlt.success && !string.IsNullOrEmpty(rlt.data))
                {
                    val = rlt.data;
                }
                else if (string.IsNullOrEmpty(label))
                {
                    val = key;
                }
                else
                {
                    val = label;
                }
                Cache.Set("emi_" + this.owner + "_label_" + key, val, 3600);
            }
            return val;
        }

        /// <summary>
        /// 获取配置信息
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">默认值</param>
        /// <returns></returns>
        public string GetSetting(String key, String value = "")
        {
            if (string.IsNullOrEmpty(key))
            {
                return "";
            }
            var val = Cache.Get("emi_" + this.owner + "_setting_" + key);
            if (string.IsNullOrEmpty(val))
            {
                var rlt = EmiGet<String>("app", "setting", new KeyValuePair<string, string>("key", key), new KeyValuePair<string, string>("value", value));
                if (rlt.success && !string.IsNullOrEmpty(rlt.data))
                {
                    val = rlt.data;
                    Cache.Set("emi_" + this.owner + "_setting_" + key, val, 180);
                }
                else if (string.IsNullOrEmpty(value))
                {
                    val = "";
                }
                else
                {
                    val = value;
                }
            }
            return val;
        }

        /// <summary>
        /// 根据类型获取枚举名称
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public String GetEnumName(String key)
        {
            var val = Cache.Get<String>("emi_" + this.owner + "_enumname_" + key);
            if (string.IsNullOrEmpty(val))
            {
                var rlt = EmiGet<Wlniao.XCenter.Models.Enum>("app", "getenum", new KeyValuePair<string, string>("key", key));
                if (rlt.success && rlt.data != null && !string.IsNullOrEmpty(rlt.data.label))
                {
                    val = rlt.data.label;
                    Cache.Set("emi_" + this.owner + "_enumname_" + key, val, 300);
                }
            }
            if (string.IsNullOrEmpty(val))
            {
                val = key;
            }
            return val;
        }
        /// <summary>
        /// 根据类型获取枚举列表
        /// </summary>
        /// <param name="parent"></param>
        /// <returns></returns>
        public List<Wlniao.XCenter.Models.Enum> GetEnumList(String parent)
        {
            var val = Cache.Get<List<Wlniao.XCenter.Models.Enum>>("emi_" + this.owner + "_enumlist_" + parent);
            if (val == null || val.Count == 0)
            {
                var rlt = EmiGet<List<Wlniao.XCenter.Models.Enum>>("app", "getenumlist", new KeyValuePair<string, string>("parent", parent));
                if (rlt.success && rlt.data != null && rlt.data.Count > 0)
                {
                    val = rlt.data;
                    Cache.Set("emi_" + this.owner + "_enumlist_" + parent, val, 300);
                }
            }
            if (val == null)
            {
                val = new List<Models.Enum>();
            }
            return val;
        }
        /// <summary>
        /// 获取一个账号姓名昵称
        /// </summary>
        /// <param name="eid"></param>
        /// <param name="mobile"></param>
        /// <returns></returns>
        public String GetAccountName(String eid, string mobile = "")
        {
            if (string.IsNullOrEmpty(eid))
            {
                return "";
            }
            var val = Wlniao.Cache.Get("emi_" + this.owner + "_accountname_" + eid);
            if (string.IsNullOrEmpty(val))
            {
                var rlt = EmiGet<Dictionary<string, object>>("app", "getaccount", new KeyValuePair<string, string>("sid", eid));
                if (rlt.success && rlt.data != null && rlt.data.ContainsKey("name"))
                {
                    val = rlt.data.GetString("name");
                }
                if (string.IsNullOrEmpty(val))
                {
                    val = mobile;
                }
                if (!string.IsNullOrEmpty(val))
                {
                    Wlniao.Cache.Set("emi_" + this.owner + "_accountname_" + eid, val, 3600);
                }
            }
            return val;
        }
        /// <summary>
        /// 检查用户权限
        /// </summary>
        /// <param name="Sid"></param>
        /// <param name="Code"></param>
        /// <returns></returns>
        public bool Permission(String Sid, String Code)
        {
            if (!string.IsNullOrEmpty(Code))
            {
                var rlt = EmiGet<Boolean>("app", "permission"
                    , new KeyValuePair<string, string>("sid", Sid)
                    , new KeyValuePair<string, string>("code", Code));
                return rlt.data;
            }
            return false;
        }
        /// <summary>
        /// 检查机构数据查看权限
        /// </summary>
        /// <param name="Sid"></param>
        /// <param name="Code"></param>
        /// <param name="Organ"></param>
        /// <returns></returns>
        public bool PermissionOrgan(String Sid, String Code, String Organ)
        {
            if (!string.IsNullOrEmpty(Code))
            {
                var rlt = EmiGet<Boolean>("app", "permissionorgan"
                    , new KeyValuePair<string, string>("sid", Sid)
                    , new KeyValuePair<string, string>("code", Code)
                    , new KeyValuePair<string, string>("organ", Organ));
                return rlt.data;
            }
            return false;
        }
        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="Sid">操作用户</param>
        /// <param name="Comments">日志内容</param>
        /// <param name="ClientIP">终端IP</param>
        /// <returns></returns>
        public bool Log(String Sid, String Comments, String ClientIP)
        {
            return Log("", "", "", "", Sid, Comments, ClientIP);
        }
        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="Organ">所属部门</param>
        /// <param name="Sid">操作用户</param>
        /// <param name="Comments">日志内容</param>
        /// <param name="ClientIP">终端IP</param>
        /// <returns></returns>
        public bool Log(String Organ, String Sid, String Comments, String ClientIP)
        {
            return Log("", "", "", Organ, Sid, Comments, ClientIP);
        }
        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="Model">模型表名</param>
        /// <param name="Key">模型主键</param>
        /// <param name="Method">操作方法</param>
        /// <param name="Organ">所属部门</param>
        /// <param name="Sid">操作用户</param>
        /// <param name="Comments">日志内容</param>
        /// <param name="ClientIP">终端IP</param>
        /// <returns></returns>
        public bool Log(String Model, String Key, String Method, String Organ, String Sid, String Comments, String ClientIP)
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                sid = Sid,
                key = Key,
                model = Model,
                method = Method,
                comments = Comments,
                clientip = ClientIP
            });
            return EmiPost<string>("app", "log", json).success;
        }
    }
}