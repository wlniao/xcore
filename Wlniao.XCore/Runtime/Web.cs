/*==============================================================================
    文件名称：Web.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：Web程序相关状态
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
using System.Text;

namespace Wlniao.Runtime
{
    /// <summary>
    /// Web程序相关状态
    /// </summary>
    public class Web
    {
        private static string _cfgIsMobile = null;
        private static string _cfgIsWeiXin = null;
        private static string _cfgThisDns = null;
        /// <summary>
        /// 当前访客环境是否为Mobile
        /// </summary>
        public static bool IsMobile
        {
            get
            {
                if (_cfgIsMobile == null)
                {
                    _cfgIsMobile = Config.GetConfigs("IsMobile");
                }
                return _cfgIsMobile != null && _cfgIsMobile.ToLower() == "true";
            }
        }
        /// <summary>
        /// 当前访客环境是否为微信
        /// </summary>
        public static bool IsWeiXin
        {
            get
            {
                if (_cfgIsWeiXin == null)
                {
                    _cfgIsWeiXin = Config.GetConfigs("IsWeiXin");
                }
                return _cfgIsWeiXin != null && _cfgIsWeiXin.ToLower() == "true";
            }
        }

        /// <summary>
        /// 当前网站的地址
        /// </summary>
        public static String ThisDns
        {
            get
            {
                if (_cfgThisDns == null)
                {
                    _cfgThisDns = Config.GetConfigs("ThisDns");
                }
                return _cfgThisDns == null ?"": _cfgThisDns;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public class Page
        {
            /// <summary>
            /// 创建一个新的错误页
            /// </summary>
            /// <param name="Title">页面标题</param>
            /// <param name="Content">页面内容</param>
            /// <returns></returns>
            public static String Error(String Title, String Content)
            {
                return "<html><head><title>" + Title + "</title><meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0, user-scalable=no, minimum-scale=1.0, maximum-scale=1.0\" /></head><body><div style=\"text-align:center;padding:18%;\"><span style=\"color:#333;font-size:18px;\">" + Content + "</span><span style=\"color:#999; font-size:12px;\"></span></div></body></html>";
            }
        }
    }
}