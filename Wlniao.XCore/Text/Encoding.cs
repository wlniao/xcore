/*==============================================================================
    文件名称：Encoding.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：字符编码处理
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

namespace Wlniao.Text
{
    /// <summary>
    /// Encoding类常用方法扩展
    /// </summary>
    public static class Encoding
    {
        private static System.Text.Encoding gbk = null;
        private static System.Text.Encoding gb2312 = null;
        /// <summary>
        /// ASCII编码
        /// </summary>
        public static System.Text.Encoding ASCII
        {
            get
            {
                return System.Text.Encoding.ASCII;
            }
        }
        /// <summary>
        /// Unicode编码
        /// </summary>
        public static System.Text.Encoding Unicode
        {
            get
            {
                return System.Text.Encoding.Unicode;
            }
        }
        /// <summary>
        /// UTF8编码
        /// </summary>
        public static System.Text.Encoding UTF8
        {
            get
            {
                return System.Text.Encoding.UTF8;
            }
        }
        /// <summary>
        /// UTF32编码
        /// </summary>
        public static System.Text.Encoding UTF32
        {
            get
            {
                return System.Text.Encoding.UTF32;
            }
        }
        /// <summary>
        /// GBK编码
        /// </summary>
        public static System.Text.Encoding GBK
        {
            get
            {
                var testRegisterProvider = true;
            reTest:
                try
                {
                    if (gbk == null)
                    {
                        gbk = System.Text.Encoding.GetEncoding("gbk");
                    }
                }
                catch (Exception ex)
                {
                    if (testRegisterProvider && ex.Message.Contains("Encoding.RegisterProvider"))
                    {
                        testRegisterProvider = false;
                        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                        goto reTest;
                    }
                    else
                    {
                        throw new Exception("Please register by \"System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);\" first!");
                    }
                }
                return gbk;
            }
        }
        /// <summary>
        /// GB2312编码
        /// </summary>
        public static System.Text.Encoding GB2312
        {
            get
            {
                var testRegisterProvider = true;
            reTest:
                try
                {
                    if (gb2312 == null)
                    {
                        gb2312 = System.Text.Encoding.GetEncoding("gb2312");
                    }
                }
                catch (Exception ex)
                {
                    if (testRegisterProvider && ex.Message.Contains("Encoding.RegisterProvider"))
                    {
                        testRegisterProvider = false;
                        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                        goto reTest;
                    }
                    else
                    {
                        throw new Exception("Please register by \"System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);\" first!");
                    }
                }
                return gb2312;
            }
        }
        /// <summary>
        /// 根据名称返回编码
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static System.Text.Encoding GetEncoding(string name)
        {
            try
            {
                return System.Text.Encoding.GetEncoding(name);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Encoding.RegisterProvider"))
                {
                    System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                    return System.Text.Encoding.GetEncoding(name);
                }
                else
                {
                    throw new Exception("Please register by \"System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);\" first!");
                }
            }
        }
    }
}