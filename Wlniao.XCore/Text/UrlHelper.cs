/*==============================================================================
    �ļ����ƣ�UrlHelper.cs
    ���û�����CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    ������������װ�� url �Ĳ���
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
using System.Collections;
using System.Collections.Generic;
namespace Wlniao.Text
{
    /// <summary>
    /// ��װ�� url �Ĳ���
    /// </summary>
    public class UrlHelper
    {
        private static bool IsFirstDomainEqual(string url1, string url2)
        {
            string host1 = new UriBuilder(url1).Host;
            string host2 = new UriBuilder(url2).Host;
            return host1.Equals(host2);
        }
        /// <summary>
        /// ���url�Ƿ�����(�Ƿ���http��ͷ������������ͷ)
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static bool IsFullUrl(string url)
        {
            if (strUtil.IsNullOrEmpty(url)) return false;
            if (url.Trim().StartsWith("/")) return false;
            if (url.Trim().StartsWith("http://")) return true;
            string[] arrItem = url.Split('/');
            if (arrItem.Length < 1) return false;
            int dotIndex = arrItem[0].IndexOf(".");
            if (dotIndex <= 0) return false;
            return hasCommonExt(arrItem[0]) == false;
        }
        private static readonly List<string> extList = getExtList();
        private static List<string> getExtList()
        {
            string[] exts = { "htm", "html", "xhtml", "txt", "json",
                                "jpg", "gif", "png", "jpg", "jpeg", "bmp",
                                "doc", "docx", "ppt", "pptx", "xls", "xlsx", "chm", "pdf",
                                "zip", "7z", "rar", "exe", "dll",
                                "mov", "wav", "mp3", "rm", "rmvb", "mkv", "avi",
                                "asp", "aspx", "php", "jsp"
                            };
            return new List<string>(exts);
        }
        /// <summary>
        /// �ж���ַ�Ƿ����������׺�������� .htm/.html/.aspx/.jpg/.doc/.avi ��
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static bool hasCommonExt(string str)
        {
            int dotIndex = str.LastIndexOf(".");
            string ext = str.Substring(dotIndex + 1, str.Length - dotIndex - 1);
            return extList.Contains(ext);
        }
        /// <summary>
        /// �ж���ַ�Ƿ������׺�������� xyzz/ab.htm ������my/xyz/dfae3 �򲻰���
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static bool UrlHasExt(string url)
        {
            if (strUtil.IsNullOrEmpty(url)) return false;
            string[] arrItem = url.Split('/');
            string lastPart = arrItem[arrItem.Length - 1];
            return lastPart.IndexOf(".") >= 0;
        }
        /// <summary>
        /// �޳��� url �ĺ�׺��
        /// </summary>
        /// <param name="rawUrl">ԭʼurl</param>
        /// <returns>���ر��޳�����׺���� url</returns>
        public static string TrimUrlExt(string rawUrl)
        {
            if (strUtil.IsNullOrEmpty(rawUrl)) return rawUrl;
            int dotIndex = rawUrl.IndexOf(".");
            if (dotIndex < 0) return rawUrl;
            string[] arrItem = rawUrl.Split('.');
            string ext = arrItem[arrItem.Length - 1];
            if (ext.IndexOf('/') > 0) return rawUrl;
            return strUtil.TrimEnd(rawUrl, ext).TrimEnd('.');
        }
        /// <summary>
        /// �ڲ����Ǻ�׺��������£��Ƚ�������ַ�Ƿ���ͬ
        /// </summary>
        /// <param name="url1"></param>
        /// <param name="url2"></param>
        /// <returns></returns>
        public static bool CompareUrlWithoutExt(string url1, string url2)
        {
            if (strUtil.IsNullOrEmpty(url1) && strUtil.IsNullOrEmpty(url2)) return true;
            if (strUtil.IsNullOrEmpty(url1) || strUtil.IsNullOrEmpty(url2)) return false;
            return TrimUrlExt(url1) == TrimUrlExt(url2);
        }
    }
}