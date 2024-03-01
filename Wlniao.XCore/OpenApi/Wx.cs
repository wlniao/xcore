/*==============================================================================
    文件名称：Wx.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：调用OpenApi服务端提供的方法
================================================================================
 
    Copyright 2015 XieChaoyi

    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at

               http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.

===============================================================================*/
using System;
using System.Collections.Generic;
namespace Wlniao.OpenApi
{
    /// <summary>
    /// 微信公众号接口
    /// </summary>
    public class Wx
    {
        #region 包含的Model
        /// <summary>
        /// 错误信息
        /// </summary>
        public class ErrMsg
        {
            private string _errcode;
            private string _errmsg;
            private string _msgid;
            /// <summary>
            /// 
            /// </summary>
            public string errcode { get { return _errcode; } set { _errcode = value; } }
            /// <summary>
            /// 
            /// </summary>
            public string errmsg { get { return _errmsg; } set { _errmsg = value; } }
            /// <summary>
            /// 
            /// </summary>
            public string msgid { get { return _msgid; } set { _msgid = value; } }
        }

        /// <summary>
        /// AccessToken信息
        /// </summary>
        public class AccessToken
        {
            private string _access_token;
            private string _expires_in;
            /// <summary>
            /// /
            /// </summary>
            public string access_token { get { return _access_token; } set { _access_token = value; } }
            /// <summary>
            /// 
            /// </summary>
            public string expires_in { get { return _expires_in; } set { _expires_in = value; } }
        }
        /// <summary>
        /// AccessToken信息
        /// </summary>
        public class WxTicket
        {
            private string _ticket;
            private string _expires_in;
            /// <summary>
            /// 
            /// </summary>
            public string ticket { get { return _ticket; } set { _ticket = value; } }
            /// <summary>
            /// 
            /// </summary>
            public string expires_in { get { return _expires_in; } set { _expires_in = value; } }
        }

        /// <summary>
        /// UserInfo信息
        /// </summary>
        public class UserInfo
        {
            /// <summary>
            /// 用户是否订阅该公众号标识
            /// 为0时，用户没有关注该公众号，拉取不到其余信息。
            /// </summary>
            public int subscribe { get; set; }
            /// <summary>
            /// 用户的标识，对当前公众号唯一
            /// </summary>
            public string openid { get; set; }
            /// <summary>
            /// 用户昵称
            /// </summary>
            public string nickname { get; set; }
            /// <summary>
            /// 用户的性别，值为1时是男性，值为2时是女性，值为0时是未知
            /// </summary>
            public int sex { get; set; }
            /// <summary>
            /// 用户个人资料填写的省份
            /// </summary>
            public string province { get; set; }
            /// <summary>
            /// 普通用户个人资料填写的城市
            /// </summary>
            public string city { get; set; }
            /// <summary>
            /// 国家，如中国为CN
            /// </summary>
            public string country { get; set; }
            /// <summary>
            /// 用户的语言，简体中文为zh_CN
            /// </summary>
            public string language { get; set; }
            /// <summary>
            /// 用户头像，最后一个数值代表正方形头像大小（有0、46、64、96、132数值可选，0代表640*640正方形头像），用户没有头像时该项为空。若用户更换头像，原有头像URL将失效。
            /// </summary>
            public string headimgurl { get; set; }
            /// <summary>
            /// 用户关注时间（时间戳），如果用户曾多次关注，则取最后关注时间
            /// </summary>
            public string subscribe_time { get; set; }
            /// <summary>
            /// unionid
            /// </summary>
            public string unionid { get; set; }
            /// <summary>
            /// 公众号运营者对粉丝的备注
            /// </summary>
            public string remark { get; set; }
            /// <summary>
            /// 用户所在的分组ID
            /// </summary>
            public string groupid { get; set; }
        }
        /// <summary>
        /// UserInfo信息
        /// </summary>
        public class AuthUserInfo
        {
            /// <summary>
            /// 用户的唯一标识
            /// </summary>
            public string openid { get; set; }
            /// <summary>
            /// 用户昵称
            /// </summary>
            public string nickname { get; set; }
            /// <summary>
            /// 用户的性别，值为1时是男性，值为2时是女性，值为0时是未知
            /// </summary>
            public int sex { get; set; }
            /// <summary>
            /// 用户个人资料填写的省份
            /// </summary>
            public string province { get; set; }
            /// <summary>
            /// 普通用户个人资料填写的城市
            /// </summary>
            public string city { get; set; }
            /// <summary>
            /// 国家，如中国为CN
            /// </summary>
            public string country { get; set; }
            /// <summary>
            /// 用户头像，最后一个数值代表正方形头像大小（有0、46、64、96、132数值可选，0代表640*640正方形头像），用户没有头像时该项为空。若用户更换头像，原有头像URL将失效。
            /// </summary>
            public string headimgurl { get; set; }
            /// <summary>
            /// 用户特权信息，json 数组，如微信沃卡用户为（chinaunicom）
            /// </summary>
            public string privilege { get; set; }
        }
        /// <summary>
        /// 模板消息数据
        /// </summary>
        public class TemplateData
        {
            /// <summary>
            /// 内容
            /// </summary>
            public string value { get; set; }

            private string _color = "";
            /// <summary>
            /// 显示颜色
            /// </summary>
            public string color
            {
                get
                {
                    if (string.IsNullOrEmpty(_color))
                    {
                        return "#173177";
                    }
                    else
                    {
                        return _color;
                    }
                }
                set
                {
                    _color = value;
                }
            }
            /// <summary>
            /// 
            /// </summary>
            public TemplateData()
            {
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="value"></param>
            public TemplateData(string value)
            {
                this.value = value;
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="value"></param>
            /// <param name="color"></param>
            public TemplateData(string value, string color)
            {
                this.value = value;
                this.color = color;
            }
        }
        #endregion

        #region 包含的方法
        /// <summary>
        /// 获取公众号AccessToken
        /// </summary>
        /// <param name="appid"></param>
        /// <param name="appsecret"></param>
        /// <returns></returns>
        public static ApiResult<String> GetAccessToken(String appid, String appsecret)
        {
            var rlt = new ApiResult<String>();
            try
            {
                var json = XServer.Common.Get("openapi", "wx", "getaccesstoken"
                    , new KeyValuePair<string, string>("appid", appid)
                    , new KeyValuePair<string, string>("appsecret", appsecret));
                rlt = Json.ToObject<ApiResult<String>>(json);
                if (rlt == null)
                {
                    rlt = new ApiResult<String>();
                    rlt.success = false;
                    rlt.message = "解析Json结果失败";
                }
            }
            catch (Exception ex)
            {
                rlt.success = false;
                rlt.message = "内部异常：" + ex.Message;
                rlt.code = "5x04040601";
            }
            return rlt;
        }
        /// <summary>
        /// 本地获取公众号AccessToken（无缓存）
        /// </summary>
        /// <param name="appid"></param>
        /// <param name="appsecret"></param>
        /// <returns></returns>
        public static ApiResult<String> GetAccessTokenByLocal(String appid, String appsecret)
        {
            var rlt = new ApiResult<String>();
            try
            {
                #region 获取新的AccessToken
                var _response = "";
                var url = string.Format("https://api.weixin.qq.com/cgi-bin/token?grant_type=client_credential&appid={0}&secret={1}", appid, appsecret);
                using (var client = new System.Net.Http.HttpClient())
                {
                    client.SendAsync(new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, url)).ContinueWith((requestTask) =>
                    {
                        requestTask.Result.Content.ReadAsStringAsync().ContinueWith((readTask) =>
                        {
                            _response = readTask.Result;
                        }).Wait();
                    }).Wait();
                }
                try
                {
                    if (_response.IndexOf("access_token") > 0)
                    {
                        //var obj = Json.ToObject<Wlniao.OpenApi.Wx.AccessToken>(_response);
                        var obj = Json.ToObject<Dictionary<string, object>>(_response);
                        rlt.data = obj.GetString("access_token");
                        rlt.message = "expires in " + obj.GetString("expires_in");
                        rlt.success = obj != null && !string.IsNullOrEmpty(rlt.data);
                    }
                    else
                    {
                        var obj = Json.ToObject<Dictionary<string, object>>(_response);
                        rlt.message = obj.GetString("errmsg");
                    }
                }
                catch { }
                #endregion
            }
            catch (Exception ex)
            {
                rlt.success = false;
                rlt.message = "内部异常：" + ex.Message;
                rlt.code = "5x04040601";
            }
            return rlt;
        }
        /// <summary>
        /// 获取公众号GetTicketJsApi
        /// </summary>
        /// <param name="appid"></param>
        /// <param name="appsecret"></param>
        /// <returns></returns>
        public static ApiResult<String> GetTicketJsApi(String appid, String appsecret)
        {
            var rlt = new ApiResult<String>();
            try
            {
                var json = XServer.Common.Get("openapi", "wx", "getticketjsapi"
                    , new KeyValuePair<string, string>("appid", appid)
                    , new KeyValuePair<string, string>("appsecret", appsecret));
                rlt = Json.ToObject<ApiResult<String>>(json);
                if (rlt == null)
                {
                    rlt = new ApiResult<String>();
                    rlt.success = false;
                    rlt.code = "";
                    rlt.message = "解析Json结果失败";
                }
            }
            catch (Exception ex)
            {
                rlt.success = false;
                rlt.message = "发生异常：" + ex.Message;
                rlt.code = "5x04040602";
            }
            return rlt;
        }
        /// <summary>
        /// 通过OpenApi获取用户信息
        /// </summary>
        /// <param name="appid"></param>
        /// <param name="wxopenid"></param>
        /// <returns></returns>
        public static UserInfo GetUserInfoByOpenApi(String appid, String wxopenid)
        {
            var json = XServer.Common.Get("openapi", "wx", "getuserinfo"
                , new KeyValuePair<string, string>("appid", appid)
                , new KeyValuePair<string, string>("wxopenid", wxopenid));
            var rlt = Json.ToObject<ApiResult<UserInfo>>(json);
            if (rlt.success)
            {
                return rlt.data;
            }
            return new UserInfo();
        }
        /// <summary>
        /// 获取已关注用户的基本信息
        /// </summary>
        /// <param name="access_token"></param>
        /// <param name="wxopenid"></param>
        /// <returns></returns>
        public static UserInfo GetUserInfo(String access_token, String wxopenid)
        {
            string url = "https://api.weixin.qq.com/cgi-bin/user/info?access_token=" + access_token + "&openid=" + wxopenid + "&lang=zh_CN";
            string json = XServer.Common.GetResponseString(url);
            if (!string.IsNullOrEmpty(json))
            {
                return Json.ToObject<UserInfo>(json);
            }
            return null;
        }
        /// <summary>
        /// 发送模板消息
        /// </summary>
        /// <param name="access_token"></param>
        /// <param name="wxopenid"></param>
        /// <param name="template_id"></param>
        /// <param name="topcolor"></param>
        /// <param name="url"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static ApiResult<String> SendTemplateMsg(String access_token, String wxopenid, String template_id,String topcolor, String url, Dictionary<String,TemplateData> data)
        {
            var rlt = new ApiResult<String>();
            try
            {
                var json = Json.ToString(new
                {
                    touser = wxopenid
                    ,
                    template_id = template_id
                    ,
                    topcolor = topcolor
                    ,
                    url = topcolor
                    ,
                    data = data
                });
                var str = XServer.Common.PostResponseString("https://api.weixin.qq.com/cgi-bin/message/template/send?access_token=" + access_token, json);
                if (!string.IsNullOrEmpty(str))
                {
                    var msg = Json.ToObject<ErrMsg>(str);
                    if (msg.errcode == "0")
                    {
                        rlt.success = true;
                        rlt.message = msg.errmsg;
                        rlt.data = msg.msgid;
                    }
                    else
                    {
                        rlt.message = msg.errmsg;
                    }
                }
            }
            catch (Exception ex)
            {
                rlt.success = false;
                rlt.message = ex.Message;
                rlt.code = "5x04040603";
            }
            return rlt;
        }
        /// <summary>
        /// 获取通过网页授权后的用户信息
        /// </summary>
        /// <param name="appid"></param>
        /// <param name="wxopenid"></param>
        /// <returns></returns>
        public static ApiResult<AuthUserInfo> GetAuthUserInfoByOpenApi(String appid, String wxopenid)
        {
            ApiResult<AuthUserInfo> rlt = null;
            try
            {
                var json = XServer.Common.Get("openapi", "wx", "getuserinfo"
                    , new KeyValuePair<string, string>("appid", appid)
                    , new KeyValuePair<string, string>("wxopenid", wxopenid));
                rlt = Json.ToObject<ApiResult<AuthUserInfo>>(json);
                if (rlt == null)
                {
                    rlt = new ApiResult<AuthUserInfo>();
                    rlt.success = false;
                    rlt.message = "解析Json结果失败";
                }
            }
            catch (Exception ex)
            {
                rlt = new ApiResult<AuthUserInfo>();
                rlt.success = false;
                rlt.message = ex.Message;
                rlt.code = "5x04040604";
            }
            return rlt;
        }
        /// <summary>
        /// 获取通过网页授权后的用户信息
        /// </summary>
        /// <param name="appid"></param>
        /// <param name="wxopenid"></param>
        /// <param name="access_token"></param>
        /// <returns></returns>
        public static ApiResult<AuthUserInfo> GetAuthUserInfo(String appid, String wxopenid, String access_token)
        {
            var rlt = new ApiResult<AuthUserInfo>();
            try
            {
                var url = string.Format("https://api.weixin.qq.com/sns/userinfo?access_token={0}&openid={1}&lang=zh_CN", access_token, wxopenid);
                var userJson = XServer.Common.GetResponseString(url);
                var ui = Json.ToObject<AuthUserInfo>(userJson);
                if (!string.IsNullOrEmpty(ui.nickname))
                {
                    rlt.success = true;
                    rlt.data = ui;
                    rlt.message = "微信用户信息获取成功";
                }
                else
                {
                    rlt.success = false;
                    rlt.data = null;
                    rlt.message = "解析Json结果失败";
                }
            }
            catch (Exception ex)
            {
                rlt = new ApiResult<AuthUserInfo>();
                rlt.success = false;
                rlt.message = ex.Message;
                rlt.code = "5x04040605";
            }
            return rlt;
        }

        /// <summary>
        /// 根据参数和密码生成签名字符串
        /// </summary>
        /// <param name="kvs">API参数</param>
        /// <param name="verifycode">密码</param>
        /// <returns>签名字符串</returns>
        public static String GetSignatureStr(KeyValuePair<String, String>[] kvs, String verifycode = "")
        {
            var values = new System.Text.StringBuilder();
            var kvList = new List<KeyValuePair<String, String>>(kvs);
            #region 排序并拼接签名原始字符
            kvList.Sort();
            foreach (var kv in kvList)
            {
                if (kv.Key == "sig" || string.IsNullOrEmpty(kv.Value))
                {
                    continue;
                }
                values.Append(kv.Value);
            }
            #endregion
            values.Append(verifycode);
            return values.ToString();
        }
        #endregion

    }
}
