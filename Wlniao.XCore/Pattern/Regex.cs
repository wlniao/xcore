/*==============================================================================
    文件名称：Regex.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：常用正则表达式的整理
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
namespace Wlniao.Pattern
{
    /// <summary>
    /// 常用正则表达式的整理
    /// </summary>
    public class Regex
    {
        /// <summary>
        /// 检查字符串是否和指定的正则表达式匹配
        /// </summary>
        /// <param name="input">需要检查的字符串</param>
        /// <param name="pattern">正则表达式</param>
        /// <returns></returns>
        public static bool IsMatch(string input, string pattern)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(input, pattern);
        }
        /// <summary>
        /// 网址的正则表达式
        /// </summary>
        public const string Url = @"^(ht|f)tp(s?)\:\/\/[0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*(:(0-9)*)*(\/?)([a-zA-Z0-9\-\.\?\,\'\/\\\+&amp;%\$#_]*)?$";

        /// <summary>
        /// email 正则表达式
        /// </summary>
        public const string Email = @"^(?("")("".+?""@)|(([0-9a-zA-Z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-zA-Z])@))(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-zA-Z][-\w]*[0-9a-zA-Z]\.)+[a-zA-Z]{2,6}))$";

        /// <summary>
        /// 货币值(小数)的正则表达式
        /// </summary>
        public const string Currency = @"^\d+(\.\d\d)?$"; //1.00

        /// <summary>
        /// (负数)货币值(小数)的正则表达式
        /// </summary>
        public const string NegativeCurrency = @"^(-)?\d+(\.\d\d)?$"; //-1.20

        /// <summary>
        /// html 页面中图片的正则表达式，获取&lt;img src="" /&gt; 的src部分
        /// </summary>
        public const string Img = "(?<=<img.+?src\\s*?=\\s*?\"?)([^\\s\"]+?)(?=[\\s\"])";
    }
}
