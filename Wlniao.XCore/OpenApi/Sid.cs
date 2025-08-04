/*==============================================================================
    文件名称：Sid.cs
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
using System.Linq;
using System.Threading.Tasks;

namespace Wlniao.OpenApi
{
    /// <summary>
    /// 调用OpenApi服务端提供的方法
    /// </summary>
    public class Sid
    {
        /// <summary>
        /// 根据手机号生成Sid
        /// </summary>
        /// <returns></returns>
        public static string Get(string Mobile)
        {
            if (strUtil.IsMobile(Mobile))
            {
                var _rlt = Wlniao.OpenApi.Common.Get<string>("sid", "get", new KeyValuePair<string, string>("mobile", Mobile));
                if (_rlt.success)
                {
                    return _rlt.data;
                }
                else
                {
                    var r = new Random();
                    var ts = cvt.ToHex(DateTools.GetUnix(), "0123456789abcdef");
                    var ms = cvt.ToHex(long.Parse("86" + Mobile), "0123456789abcdef");
                    var v1 = cvt.ToHex(r.Next(4096, 26214), "0123456789abcdef");
                    var v2 = cvt.ToHex(r.Next(26214, 43690), "0123456789abcdef");
                    var v3 = cvt.ToHex(r.Next(43690, 65535), "0123456789abcdef");
                    return ts + "-" + v1 + "-" + v2 + "-" + v3 + "-" + ms;
                }
            }
            return System.Guid.NewGuid().ToString();
        }
        /// <summary>
        /// 根据Sid解析手机号
        /// </summary>
        /// <param name="Sid"></param>
        /// <returns></returns>
        public static string GetMobile(string Sid)
        {
            try
            {
                var arr = Sid.Split('-');
                var mobile = cvt.DeHex(arr[arr.Length - 1], "0123456789abcdef").ToString().Substring(2);
                if (strUtil.IsMobile(mobile))
                {
                    return mobile;
                }
            }
            catch { }
            return "";
        }
    }
}
