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
    public static class Encodings
    {
        private static System.Text.Encoding gbk = null;
        private static System.Text.Encoding gb2312 = null;

        /// <summary>
        /// 缺少 CodePages 编码提供器时的提示文案
        /// </summary>
        private const string RegisterProviderTip = "Please register by \"System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);\" first!";

        /// <summary>
        /// ASCII编码
        /// </summary>
        public static System.Text.Encoding ASCII => System.Text.Encoding.ASCII;

        /// <summary>
        /// Unicode编码
        /// </summary>
        public static System.Text.Encoding Unicode => System.Text.Encoding.Unicode;

        /// <summary>
        /// UTF8编码
        /// </summary>
        public static System.Text.Encoding UTF8 => System.Text.Encoding.UTF8;

        /// <summary>
        /// UTF32编码
        /// </summary>
        public static System.Text.Encoding UTF32 => System.Text.Encoding.UTF32;

        /// <summary>
        /// GBK编码
        /// </summary>
        public static System.Text.Encoding GBK => gbk ??= GetEncodingCore("gbk");

        /// <summary>
        /// GB2312编码
        /// </summary>
        public static System.Text.Encoding GB2312 => gb2312 ??= GetEncodingCore("gb2312");

        /// <summary>
        /// 根据名称返回编码
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static System.Text.Encoding GetEncoding(string name)
        {
            return GetEncodingCore(name);
        }

        /// <summary>
        /// 编码获取的唯一实现：未注册 CodePages 提供器时先注册再重试一次，两次都失败则原样抛出原因。
        /// </summary>
        /// <param name="name">编码名称</param>
        /// <returns>对应的编码实例</returns>
        private static System.Text.Encoding GetEncodingCore(string name)
        {
            var testRegisterProvider = true;
            reTest:
            try
            {
                return System.Text.Encoding.GetEncoding(name);
            }
            catch (Exception ex)
            {
                // 只有「提供器未注册」这一种失败才值得注册后重试；
                // 其余失败（编码名拼错、平台不支持等）注册什么提供器都没用。
                if (testRegisterProvider && ex.Message.Contains("Encoding.RegisterProvider"))
                {
                    testRegisterProvider = false;
                    System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                    goto reTest;
                }
                // 抛出时保留原始错误信息，避免把「编码名不存在」误诊成「请先注册提供器」
                throw new ArgumentException($"{RegisterProviderTip} 原始错误：{ex.Message}", nameof(name), ex);
            }
        }
    }
}
