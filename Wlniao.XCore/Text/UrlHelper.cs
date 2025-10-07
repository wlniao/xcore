/*==============================================================================
    文件名称：UrlHelper.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：封装了 url 的操作
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
namespace Wlniao.Text
{
    /// <summary>
    /// 封装了 url 的操作
    /// </summary>
    public class UrlHelper
    {
        private static bool IsFirstDomainEqual(string url1, string url2)
        {
            var host1 = new UriBuilder(url1).Host;
            var host2 = new UriBuilder(url2).Host;
            return host1.Equals(host2);
        }
        /// <summary>
        /// 检查url是否完整(是否以http开头或者以域名开头)
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static bool IsFullUrl(string url)
        {
            if (StringUtil.IsNullOrEmpty(url))
            {
                return false;
            }
            if (url.Trim().StartsWith("/"))
            {
                return false;
            }
            if (url.Trim().StartsWith("http://"))
            {
                return true;
            }
            if (url.Trim().StartsWith("https://"))
            {
                return true;
            }
            var arrItem = url.Split('/');
            if (arrItem.Length < 1)
            {
                return false;
            }
            var dotIndex = arrItem[0].IndexOf(".", StringComparison.Ordinal);
            if (dotIndex <= 0)
            {
                return false;
            }
            return !hasCommonExt(arrItem[0]);
        }
        private static readonly List<string> ExtList = getExtList();

        private static List<string> getExtList()
        {
            return new List<string>
            {
                "htm", "html", "xhtml", "txt", "json",
                "jpg", "gif", "png", "jpg", "jpeg", "bmp",
                "doc", "docx", "ppt", "pptx", "xls", "xlsx", "chm", "pdf",
                "zip", "7z", "rar", "exe", "dll",
                "mov", "wav", "mp3", "rm", "rmvb", "mkv", "avi",
                "asp", "aspx", "php", "jsp"
            };
        }

        /// <summary>
        /// 判断网址是否包含常见后缀名，比如 .htm/.html/.aspx/.jpg/.doc/.avi 等
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static bool hasCommonExt(string str)
        {
            var dotIndex = str.LastIndexOf(".", StringComparison.Ordinal);
            var ext = str.Substring(dotIndex + 1, str.Length - dotIndex - 1);
            return ExtList.Contains(ext);
        }
        /// <summary>
        /// 判断网址是否包含后缀名，比如 xyzz/ab.htm 包含，my/xyz/dfae3 则不包含
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static bool UrlHasExt(string url)
        {
            if (StringUtil.IsNullOrEmpty(url))
            {
                return false;
            }
            var arrItem = url.Split('/');
            var lastPart = arrItem[^1]; //arrItem[arrItem.Length - 1]
            return lastPart.IndexOf(".", StringComparison.Ordinal) >= 0;
        }
        /// <summary>
        /// 剔除掉 url 的后缀名
        /// </summary>
        /// <param name="rawUrl">原始url</param>
        /// <returns>返回被剔除掉后缀名的 url</returns>
        public static string TrimUrlExt(string rawUrl)
        {
            if (StringUtil.IsNullOrEmpty(rawUrl))
            {
                return rawUrl;
            }
            var dotIndex = rawUrl.IndexOf(".", StringComparison.Ordinal);
            if (dotIndex < 0)
            {
                return rawUrl;
            }
            var arrItem = rawUrl.Split('.');
            var ext = arrItem[^1]; //arrItem[arrItem.Length - 1]
            return ext.IndexOf('/') > 0 ? rawUrl : StringUtil.TrimEnd(rawUrl, ext).TrimEnd('.');
        }
        /// <summary>
        /// 在不考虑后缀名的情况下，比较两个网址是否相同
        /// </summary>
        /// <param name="url1"></param>
        /// <param name="url2"></param>
        /// <returns></returns>
        public static bool CompareUrlWithoutExt(string url1, string url2)
        {
            if (StringUtil.IsNullOrEmpty(url1) && StringUtil.IsNullOrEmpty(url2))
            {
                return true;
            }

            if (StringUtil.IsNullOrEmpty(url1) || StringUtil.IsNullOrEmpty(url2))
            {
                return false;
            }
            return TrimUrlExt(url1) == TrimUrlExt(url2);
        }
    }
}