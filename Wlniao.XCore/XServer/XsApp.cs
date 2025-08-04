/*==============================================================================
    文件名称：XsApp.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：XServer服务App信息实体
================================================================================
 
    Copyright 2014 XieChaoyi

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
using System.Text;

namespace Wlniao.XServer
{
    /// <summary>
    /// XServer服务App信息实体
    /// </summary>
    public class XsApp
    {
        /// <summary>
        /// AppId
        /// </summary>
        public string appid { get; set; }
        /// <summary>
        /// Secret
        /// </summary>
        public string secret { get; set; }
        /// <summary>
        /// Domain
        /// </summary>
        public string domain { get; set; }
        /// <summary>
        /// AppCode
        /// </summary>
        public string appcode { get; set; }
        /// <summary>
        /// App业务名称
        /// </summary>
        public string appname { get; set; }
        /// <summary>
        /// App是否为XClient
        /// </summary>
        public bool xclient { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="AppId"></param>
        /// <returns></returns>
        public static ApiResult<XsApp> GetById(string AppId)
        {
            return Common.Get<XsApp>("openapi", "xsapp", "get"
                , new KeyValuePair<string, string>("appid", AppId));
        }
        /// <summary>
        /// 通过AppCode获取服务实例列表
        /// </summary>
        /// <param name="AppCode">如datapi/control/website等</param>
        /// <returns></returns>
        public static ApiResult<Dictionary<string, int>> GetInstances(string AppCode)
        {
            var _rlt = Common.Get<Dictionary<string, object>>("openapi", "xsapp", "instances"
                , new KeyValuePair<string, string>("state", Config.GetConfigs("XServerState", "2"))
                , new KeyValuePair<string, string>("appcode", AppCode));
            var rlt = new ApiResult<Dictionary<string, int>>() { success = _rlt.success, message = _rlt.message, data = new Dictionary<string, int>() };
            if (_rlt.success && _rlt.data != null)
            {
                var em = _rlt.data.GetEnumerator();
                while (em.MoveNext())
                {
                    if (cvt.ToInt(em.Current.Value) > 0)
                    {
                        rlt.data.Add(em.Current.Key, cvt.ToInt(em.Current.Value));
                    }
                }
            }
            return rlt;
        }
        /// <summary>
        /// 通过AppId获取服务实例列表
        /// </summary>
        /// <param name="AppId"></param>
        /// <returns></returns>
        public static ApiResult<Dictionary<string, int>> GetInstancesByAppId(string AppId)
        {
            var _rlt = Common.Get<Dictionary<string, object>>("openapi", "xsapp", "instances"
                , new KeyValuePair<string, string>("state", Config.GetConfigs("XServerState", "2"))
                , new KeyValuePair<string, string>("appid", AppId));
            var rlt = new ApiResult<Dictionary<string, int>>() { success = _rlt.success, message = _rlt.message, data = new Dictionary<string, int>() };
            if (_rlt.success && _rlt.data != null)
            {
                var em = _rlt.data.GetEnumerator();
                while (em.MoveNext())
                {
                    if (cvt.ToInt(em.Current.Value) > 0)
                    {
                        rlt.data.Add(em.Current.Key, cvt.ToInt(em.Current.Value));
                    }
                }
            }
            return rlt;
        }
    }
}