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
        /// 检查字符串是否和指定的正则表达式匹配，并限制单次匹配的最长耗时
        /// </summary>
        /// <param name="input">需要检查的字符串</param>
        /// <param name="pattern">正则表达式</param>
        /// <param name="matchTimeout">单次匹配的超时时间，超时抛出 <see cref="System.Text.RegularExpressions.RegexMatchTimeoutException"/></param>
        /// <returns></returns>
        public static bool IsMatch(string input, string pattern, System.TimeSpan matchTimeout)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(input ?? "", pattern ?? "", System.Text.RegularExpressions.RegexOptions.None, matchTimeout);
        }

        /// <summary>
        /// 网址的正则表达式
        /// </summary>
        /// <remarks>
        /// <para>匹配 http / https / ftp / ftps 网址。主机名按「点分隔的标签」逐个匹配，每个标签以字母数字开头结尾、
        /// 中间允许字母数字下划线与连字符；端口最多 5 位数字；其后可跟以 / ? # 起始的路径与查询串。</para>
        /// <para><b>编写约束：不要改回 <c>([-.\w]*[0-9a-zA-Z])*</c> 这类嵌套量词写法。</b>
        /// 外层量词内部再包含可匹配空串的量词时，回溯路径数随输入长度指数增长，
        /// 一段刻意构造的畸形字符串即可让匹配耗时从微秒级升到数十秒（正则拒绝服务）。
        /// 当前写法的每次外层迭代都必须消耗一个字面量分隔符（<c>.</c>）或固定长度的端口段，
        /// 各迭代之间不存在歧义切分，整体复杂度为线性。</para>
        /// </remarks>
        public const string Url = @"^(ht|f)tps?://[0-9A-Za-z](?:[\w-]*[0-9A-Za-z])?(?:\.[0-9A-Za-z](?:[\w-]*[0-9A-Za-z])?)*(?::[0-9]{1,5})?(?:[/?#][A-Za-z0-9\-._~:/?#\[\]@!$&'()*+,;=%]*)?$";

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
