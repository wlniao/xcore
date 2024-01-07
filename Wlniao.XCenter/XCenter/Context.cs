using Microsoft.Extensions.Hosting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using Wlniao.Serialization;
using static System.Net.Mime.MediaTypeNames;


namespace Wlniao.XCenter
{
    /// <summary>
    /// 
    /// </summary>
    public class Context
    {
        /// <summary>
        /// 
        /// </summary>
        public string app { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string brand { get; set; }
        /// <summary>
        /// 认证系统分配的租户标识
        /// </summary>
        public string owner { get; set; }
        /// <summary>
        /// 认证系统分配的交互密钥
        /// </summary>
        public string token { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string domain { get; set; }
        /// <summary>
        /// 来自平台的消息
        /// </summary>
        public string message { get; set; }

        internal static string XCenterApp = Wlniao.Config.GetSetting("XCenterApp");
        internal static string XCenterName = Wlniao.Config.GetConfigs("XCenterName");
        internal static string XCenterOwner = Wlniao.Config.GetSetting("XCenterOwner");
        internal static string XCenterToken = Wlniao.Config.GetSetting("XCenterToken");
        internal static string XCenterBrand = Wlniao.Config.GetConfigs("XCenterBrand");
        internal static string XCenterDomain = Wlniao.Config.GetSetting("XCenterDomain").Replace("https://", "").Replace("http://", "").Trim('/');
        private static string _XCenterApi = null;
        private static string _XCenterHost = null;
        private static string _XCenterSm4Key = null;
        private static string _XCenterCertSn = null;
        private static byte[] _XCenterPrivkey = null;
        private static byte[] _XCenterPublicKey = null;
        /// <summary>
        /// XCenter认证服务器Api地址
        /// </summary>
        internal static string XCenterApi
        {
            get
            {
                if (_XCenterApi == null)
                {
                    _XCenterApi = Wlniao.Config.GetConfigs("XCenterApi", "https://authify.cn");
                }
                return _XCenterApi;
            }
        }
        /// <summary>
        /// XCenter认证服务器地址
        /// </summary>
        internal static string XCenterHost
        {
            get
            {
                if (_XCenterHost == null)
                {
                    _XCenterHost = Wlniao.Config.GetConfigs("XCenterHost", "https://authify.cn");
                }
                return _XCenterHost;
            }
        }
        /// <summary>
        /// XCenter固定通讯密钥（密钥由本地指定，不设置时随机生成）
        /// </summary>
        private static string XCenterSm4Key
        {
            get
            {
                if (_XCenterSm4Key == null)
                {
                    _XCenterSm4Key = Wlniao.Config.GetConfigs("XCenterSm4Key");
                }
                return _XCenterSm4Key;
            }
        }
        /// <summary>
        /// XCenter分配的证书序列号
        /// </summary>
        internal static string XCenterCertSn
        {
            get
            {
                if (_XCenterCertSn == null)
                {
                    _XCenterCertSn = Wlniao.Config.GetSetting("XCenterCertSn");
                }
                return _XCenterCertSn;
            }
        }
        /// <summary>
        /// XCenter分配的证书私钥
        /// </summary>
        internal static byte[] XCenterPrivkey
        {
            get
            {
                if (_XCenterPrivkey == null)
                {
                    var tmp = Wlniao.Config.GetSetting("XCenterPrivkey");
                    if (string.IsNullOrEmpty(tmp))
                    {
                        _XCenterPrivkey = new byte[0];
                    }
                    else
                    {
                        _XCenterPrivkey = Wlniao.Crypto.Helper.Decode(tmp);
                    }
                }
                return _XCenterPrivkey;
            }
        }
        /// <summary>
        /// XCenter分配的证书公钥
        /// </summary>
        internal static byte[] XCenterPublicKey
        {
            get
            {
                if (_XCenterPublicKey == null)
                {
                    var tmp = Wlniao.Config.GetSetting("XCenterPublicKey");
                    if (string.IsNullOrEmpty(tmp))
                    {
                        _XCenterPublicKey = new byte[0];
                    }
                    else
                    {
                        _XCenterPublicKey = Wlniao.Crypto.Helper.Decode(tmp);
                    }
                }
                return _XCenterPublicKey;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ownerId"></param>
        public static Context Load(Int32 ownerId)
        {
            //需要使用公钥从服务器上加载应用信息
            var ctx = new Context { owner = ownerId.ToString(), token = XCenterToken, app = XCenterApp };
            try
            {
                var res = AppData<Dictionary<string, object>>("/app/getapp_byowner", new { owner = ctx.owner });
                if (res.success)
                {
                    ctx.app = res.data.GetString("app");
                    ctx.name = res.data.GetString("name");
                    ctx.brand = res.data.GetString("brand");
                    ctx.token = res.data.GetString("token");
                    ctx.domain = res.data.GetString("domain");
                    try
                    {
                        var sm2token = Wlniao.Encryptor.SM2DecryptByPrivateKey(Wlniao.Crypto.Helper.Decode(res.data.GetString("sm2token")), XCenterPrivkey);
                        if (!string.IsNullOrEmpty(sm2token))
                        {
                            ctx.token = sm2token;
                        }
                        Wlniao.Cache.Set("ctx_" + ctx.domain, ctx, 300);
                    }
                    catch
                    {
                        if (string.IsNullOrEmpty(ctx.token))
                        {
                            ctx.message = "XCenterToken加载失败";
                        }
                    }
                }
                else
                {
                    ctx.message = res.message;
                }
            }
            catch (Exception ex)
            {
                ctx.message = "XApp初始化异常:" + ex.Message;
            }
            return ctx;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="domain"></param>
        /// <returns>不为空</returns>
        public static Context Load(String domain)
        {
            if (string.IsNullOrEmpty(domain))
            {
                domain = XCenterDomain;
            }
            if (string.IsNullOrEmpty(domain))
            {
                return new Context { message = "当前域名无效，请重新指定" };
            }
            else if (string.IsNullOrEmpty(XCenterApp) || string.IsNullOrEmpty(XCenterOwner) || XCenterOwner.Length != 9)
            {
                //需要使用公钥从服务器上加载应用信息
                var ctx = new Context { domain = domain, token = XCenterToken, app = XCenterApp };
                try
                {
                    var res = AppData<Dictionary<string, object>>("/app/getapp_bydomain", new { domain = ctx.domain });
                    if (res.success)
                    {
                        ctx.app = res.data.GetString("app");
                        ctx.name = res.data.GetString("name");
                        ctx.brand = res.data.GetString("brand");
                        ctx.owner = res.data.GetString("owner");
                        if (string.IsNullOrEmpty(ctx.app))
                        {
                            ctx.app = XCenterApp;
                        }
                        if (string.IsNullOrEmpty(ctx.token))
                        {
                            try
                            {
                                var sm2token = Wlniao.Encryptor.SM2DecryptByPrivateKey(Wlniao.Crypto.Helper.Decode(res.data.GetString("sm2token")), XCenterPrivkey);
                                if (!string.IsNullOrEmpty(sm2token))
                                {
                                    ctx.token = sm2token;
                                }
                            }
                            catch
                            {
                                if (string.IsNullOrEmpty(ctx.token))
                                {
                                    ctx.message = "XCenterToken加载失败";
                                }
                            }
                        }
                    }
                    else
                    {
                        ctx.message = res.message;
                    }
                }
                catch (Exception ex)
                {
                    ctx.message = "XApp初始化异常:" + ex.Message;
                }
                return ctx;
            }
            else
            {
                return new Context
                {
                    app = XCenterApp,
                    name = XCenterName,
                    brand = XCenterBrand,
                    owner = XCenterOwner,
                    token = XCenterToken,
                    domain = domain,
                    message = ""
                };
            }
        }

        /// <summary>
        /// 获取平台接口数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Wlniao.ApiResult<T> AppData<T>(String path, Object data)
        {
            if (string.IsNullOrEmpty(XCenterCertSn))
            {
                return new ApiResult<T> { message = "参数“XCenterCertSn”未配置，请先配置" };
            }
            else if (XCenterPublicKey.Length == 0)
            {
                return new ApiResult<T> { message = "参数“XCenterPublicKey”未配置，请先配置" };
            }
            var now = Wlniao.XCore.NowUnix.ToString();
            var rlt = new Wlniao.ApiResult<T>();
            var utime = "";
            var start = DateTime.Now;
            var msgid = strUtil.CreateLongId();
            var token = XCenterSm4Key.Length == 16 ? XCenterSm4Key : strUtil.CreateRndStrE(16);
            var plainData = Wlniao.Json.ToString(data);
            var encdata = Wlniao.Encryptor.SM4EncryptECBToHex(plainData, token);
            var sm2token = Wlniao.Encryptor.SM2EncryptByPublicKey(ASCIIEncoding.ASCII.GetBytes(token), XCenterPublicKey);
            var sign = Wlniao.Encryptor.SM3Encrypt(encdata + now);
            var resStr = "";
            var reqStr = Wlniao.Json.ToString(new { sn = XCenterCertSn, token = sm2token, timestamp = now, data = encdata, sign });
            log.Info("msgid:" + msgid + "[authify:/" + path + "]\n >>> " + reqStr);
            try
            {
                var stream = cvt.ToStream(System.Text.Encoding.UTF8.GetBytes(reqStr));
                var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = XCore.ServerCertificateCustomValidationCallback };
                using (var client = new System.Net.Http.HttpClient(handler))
                {
                    var reqest = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, XCenterHost + path);
                    reqest.Headers.Date = DateTime.Now;
                    reqest.Content = new System.Net.Http.StreamContent(stream);
                    reqest.Content.Headers.Add("Content-Type", "application/json");
                    client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Wlniao/XCore");
                    client.DefaultRequestHeaders.TryAddWithoutValidation("X-Wlniao-Trace", msgid);
                    var respose = client.Send(reqest);
                    resStr = respose.Content.ReadAsStringAsync().Result;
                    if (respose.Headers.Contains("X-Wlniao-Trace"))
                    {
                        rlt.traceid = respose.Headers.GetValues("X-Wlniao-Trace").FirstOrDefault();
                    }
                    if (respose.Headers.Contains("X-Wlniao-UseTime"))
                    {
                        utime = respose.Headers.GetValues("X-Wlniao-UseTime").FirstOrDefault();
                    }
                }
            }
            catch { }
            log.Info("msgid:" + msgid + "[usetime:" + DateTime.Now.Subtract(start).TotalMilliseconds.ToString("F2") + "ms]\n <<< " + resStr);
            var logDebug = "msgid:" + msgid + "[authify:/" + path + ", usetime:" + utime + "]\n >>> " + plainData;
            try
            {
                var resObj = Wlniao.Json.ToObject<Wlniao.ApiResult<String>>(resStr);
                if (resObj != null)
                {
                    rlt.node = resObj.node;
                    rlt.code = resObj.code;
                    rlt.message = resObj.message;
                    rlt.success = resObj.success;
                    if (resObj.success)
                    {
                        var json = Wlniao.Encryptor.SM4DecryptECBFromHex(resObj.data, token);
                        if (string.IsNullOrEmpty(json))
                        {
                            logDebug += "\n <<< RESPONSE EMPTY";
                        }
                        else
                        {
                            try
                            {
                                if (typeof(T) == typeof(string))
                                {
                                    rlt.data = (T)System.Convert.ChangeType(json, typeof(T));
                                }
                                else
                                {
                                    rlt.data = Wlniao.Json.ToObject<T>(json);
                                }
                                logDebug += "\n <<< " + Wlniao.Json.ToString(rlt);
                            }
                            catch
                            {
                                logDebug += "\n <<< " + json;
                            }
                        }
                    }
                    else
                    {
                        logDebug += "\n <<< " + rlt.message;
                    }
                }
            }
            catch (Exception ex)
            {
                logDebug += "\n <<< Exception" + ex.Message;
            }
            log.Debug(logDebug + "\n");
            return rlt;
        }
        /// <summary>
        /// 获取应用API接口数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public Wlniao.ApiResult<T> ApiData<T>(String path, Object data)
        {
            var now = Wlniao.XCore.NowUnix.ToString();
            var rlt = new Wlniao.ApiResult<T>();
            var utime = "";
            var start = DateTime.Now;
            var msgid = strUtil.CreateLongId();
            var plainData = Wlniao.Json.ToString(data);
            var encdata = Wlniao.Encryptor.SM4EncryptECBToHex(plainData, token);
            var sign = Wlniao.Encryptor.SM3Encrypt(owner + encdata + now + token);
            var resStr = "";
            var reqStr = Wlniao.Json.ToString(new { appid = owner, sign, data = encdata, timestamp = now });
            log.Info("msgid:" + msgid + "[authify:/" + path + "]\n >>> " + reqStr);
            try
            {
                var stream = cvt.ToStream(System.Text.Encoding.UTF8.GetBytes(reqStr));
                var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = XCore.ServerCertificateCustomValidationCallback };
                using (var client = new System.Net.Http.HttpClient(handler))
                {
                    var reqest = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, XCenterHost + path);
                    reqest.Headers.Date = DateTime.Now;
                    reqest.Content = new System.Net.Http.StreamContent(stream);
                    reqest.Content.Headers.Add("Content-Type", "application/json");
                    client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Wlniao/XCore");
                    client.DefaultRequestHeaders.TryAddWithoutValidation("X-Wlniao-Trace", msgid);
                    var respose = client.Send(reqest);
                    resStr = respose.Content.ReadAsStringAsync().Result;
                    if (respose.Headers.Contains("X-Wlniao-Trace"))
                    {
                        rlt.traceid = respose.Headers.GetValues("X-Wlniao-Trace").FirstOrDefault();
                    }
                    if (respose.Headers.Contains("X-Wlniao-UseTime"))
                    {
                        utime = respose.Headers.GetValues("X-Wlniao-UseTime").FirstOrDefault();
                    }
                }
            }
            catch { }
            log.Info("msgid:" + msgid + "[usetime:" + DateTime.Now.Subtract(start).TotalMilliseconds.ToString("F2") + "ms]\n <<< " + resStr);
            var logDebug = "msgid:" + msgid + "[authify:/" + path + ", usetime:" + utime + "]\n >>> " + plainData;
            try
            {
                var resObj = Json.ToObject<Wlniao.ApiResult<String>>(resStr);
                if (resObj != null)
                {
                    rlt.node = resObj.node;
                    rlt.code = resObj.code;
                    rlt.message = resObj.message;
                    rlt.success = resObj.success;
                    if (resObj.success)
                    {
                        var json = Wlniao.Encryptor.SM4DecryptECBFromHex(resObj.data, token);
                        if (string.IsNullOrEmpty(json))
                        {
                            logDebug += "\n <<< RESPONSE EMPTY";
                        }
                        else
                        {
                            try
                            {
                                if (typeof(T) == typeof(string))
                                {
                                    rlt.data = (T)System.Convert.ChangeType(json, typeof(T));
                                }
                                else
                                {
                                    rlt.data = Wlniao.Json.ToObject<T>(json);
                                }
                                logDebug += "\n <<< " + Wlniao.Json.ToString(rlt);
                            }
                            catch
                            {
                                logDebug += "\n <<< " + json;
                            }
                        }
                    }
                    else
                    {
                        logDebug += "\n <<< " + rlt.message;
                    }
                }
                else
                {
                    rlt.message = "网络错误，返回异常";
                }
            }
            catch (Exception ex)
            {
                logDebug += "\n <<< Exception" + ex.Message;
            }
            log.Debug(logDebug + "\n");
            return rlt;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="mobile"></param>
        /// <returns></returns>
        public String ApiSid(String mobile)
        {
            if (string.IsNullOrEmpty(mobile))
            {
                return "";
            }
            else
            {
                var res = ApiData<String>("/api/get_sid", new { mobile = mobile });
                if (res.success)
                {
                    return res.data;
                }
            }
            return "";
        }
        /// <summary>
        /// 获取网点在机构内部编码
        /// </summary>
        /// <param name="point"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public String ApiOrganCode(String point, ref String message)
        {
            var val = "";
            if (!string.IsNullOrEmpty(point))
            {
                val = Wlniao.Caching.InMemory.Get("pointcode_" + point);
                var res = ApiData<String>("/api/get_pointcode", new { id = point, app = app });
                if (res.success)
                {
                    val = res.data;
                    Wlniao.Caching.InMemory.Set("pointcode_" + point, res.data, 600);
                }
                else if (res.code == "999")
                {
                    message = res.message;
                }
            }
            return val;
        }
        /// <summary>
        /// 获取网点名称
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public String ApiPointName(String point)
        {
            var val = "";
            if (!string.IsNullOrEmpty(point))
            {
                val = Wlniao.Caching.InMemory.Get("pointname_" + point);
                var res = ApiData<String>("/api/get_pointname", new { id = point });
                if (res.success)
                {
                    val = res.data;
                    Wlniao.Caching.InMemory.Set("pointname_" + point, res.data, 600);
                }
                else if (res.code == "999")
                {
                    message = res.message;
                }
            }
            return val;
        }
        /// <summary>
        /// 获取应用参数配置
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Dictionary<String, String> ApiSetting(String key)
        {
            Dictionary<String, String> obj = null;
            try
            {
                var val = Wlniao.Caching.InMemory.Get("appsetting_" + key);
                obj = string.IsNullOrEmpty(val) ? null : Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<String, String>>(val);
            }
            catch { }
            try
            {
                if (obj == null || obj.Count == 0)
                {
                    var res = ApiData<String>("/api/get_setting", new { key = key });
                    if (res.success)
                    {
                        obj = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<String, String>>(res.data);
                        if (obj != null && obj.Count > 0)
                        {
                            Wlniao.Caching.InMemory.Set("appsetting_" + key, res.data, 900);
                        }
                    }
                }

            }
            catch { }
            return obj ?? new Dictionary<string, string>();
        }
    }
}