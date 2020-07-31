/*==============================================================================
    文件名称：StringUtil.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：对常见字符串操作方法的封装
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
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Wlniao.Text
{
    /// <summary>
    /// 字符串操作工具类
    /// </summary>
    public class StringUtil
    {
        private static readonly Regex HtmlReg = new Regex("<[^>]*>");
        /// <summary>
        /// 检查字符串是否是 null 或者空白字符。不同于.net自带的string.IsNullOrEmpty，多个空格在这里也返回true。
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsNullOrEmpty(string str)
        {
            if (str != null)
            {
                return str.Trim().Length == 0;
            }
            return true;
        }
        /// <summary>
        /// 检查是否包含有效字符(空格等空白字符不算)
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool HasText(string str)
        {
            return !IsNullOrEmpty(str);
        }
        /// <summary>
        /// 比较两个字符串是否相等
        /// </summary>
        /// <param name="s1"></param>
        /// <param name="s2"></param>
        /// <returns></returns>
        public static bool Equals(string s1, string s2)
        {
            if (s1 == null && s2 == null) return true;
            if (s1 == null || s2 == null) return false;
            if (s2.Length != s1.Length) return false;
            return string.Compare(s1, 0, s2, 0, s2.Length) == 0;
        }
        /// <summary>
        /// 比较两个字符串是否相等(不区分大小写)
        /// </summary>
        /// <param name="s1"></param>
        /// <param name="s2"></param>
        /// <returns></returns>
        public static bool EqualsIgnoreCase(string s1, string s2)
        {
            if (s1 == null && s2 == null) return true;
            if (s1 == null || s2 == null) return false;
            if (s2.Length != s1.Length) return false;
            return string.Compare(s1, 0, s2, 0, s2.Length, System.StringComparison.OrdinalIgnoreCase) == 0;
        }
        /// <summary>
        /// 将 endString 附加到 srcString末尾，如果 srcString 末尾已包含 endString，则不再附加。
        /// </summary>
        /// <param name="srcString"></param>
        /// <param name="endString"></param>
        /// <returns></returns>
        public static string Append(string srcString, string endString)
        {
            if (IsNullOrEmpty(srcString)) return endString;
            if (IsNullOrEmpty(endString)) return srcString;
            if (srcString.EndsWith(endString)) return srcString;
            return srcString + endString;
        }
        /// <summary>
        /// 将对象转为字符串，如果对象为 null，则转为空字符串(string.Empty)
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string ConverToNotNull(object str)
        {
            if (str == null)
            {
                return string.Empty;
            }
            return str.ToString();
        }
        /// <summary>
        /// 从字符串中截取指定长度的一段，如果源字符串被截取了，则结果末尾出现省略号...
        /// </summary>
        /// <param name="str">源字符串</param>
        /// <param name="length">需要截取的长度</param>
        /// <returns></returns>
        public static string CutString(object str, int length)
        {
            return CutString(ConverToNotNull(str), length);
        }
        /// <summary>
        /// 从字符串中截取指定长度的一段，如果源字符串被截取了，则结果末尾出现省略号...
        /// </summary>
        /// <param name="str">源字符串</param>
        /// <param name="length">需要截取的长度</param>
        /// <returns></returns>
        public static string CutString(string str, int length)
        {
            if (str == null)
            {
                return null;
            }
            if (str.Length > length)
            {
                return string.Format("{0}...", str.Substring(0, length));
            }
            return str;
        }
        /// <summary>
        /// 将字符串转换为编辑器中可用的字符串(替换掉换行符号)
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string Edit(string str)
        {
            return str.Replace("\n", "").Replace("\r", "").Replace("'", "&#39;");
        }
        /// <summary>
        /// 对双引号进行编码
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static string EncodeQuote(string src)
        {
            return src.Replace("\"", "&quot;");
        }
        /// <summary>
        /// 让 html 在 textarea 中正常显示。替换尖括号和字符&amp;lt;与&amp;gt;
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public static string EncodeTextarea(string html)
        {
            if (html == null) return null;
            return html.Replace("&lt;", "&amp;lt;").Replace("&gt;", "&amp;gt;").Replace("<", "&lt;").Replace(">", "&gt;");
        }

        /// <summary>
        /// 获取 html 文档的标题内容
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        // ReSharper disable once UnusedMember.Global
        public string GetHtmlTitle(string html)
        {
            Match match = Regex.Match(html, "<title>(.*)</title>");
            if (match.Groups.Count == 2) return match.Groups[1].Value;
            return "(unknown)";
        }

        /// <summary>
        /// 将整数按照指定的长度转换为字符串，比如33转换为6位就是"000033"
        /// </summary>
        /// <param name="intValue"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string GetIntString(int intValue, int length)
        {
            // ReSharper disable once FormatStringProblem
            return string.Format("{0:D" + length + "}", intValue);
        }

        /// <summary>
        /// 得到字符串的 TitleCase 格式（首字母大写）
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string GetTitleCase(string str)
        {
            if (IsNullOrEmpty(str))
            {
                return str;
            }
            return str[0].ToString().ToUpper() + str.Substring(1);
        }

        /// <summary>
        /// 得到字符串的 CamelCase 格式
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string GetCamelCase(string str)
        {
            if (IsNullOrEmpty(str))
            {
                return str;
            }
            return str[0].ToString().ToLower() + str.Substring(1);
        }


        /// <summary>
        /// 从类型的全名中获取类型名称(不包括命名空间)
        /// </summary>
        /// <param name="typeFullName"></param>
        /// <returns></returns>
        public static string GetTypeName(string typeFullName)
        {
            var strArray = typeFullName.Split(new[] { '.' });
            return strArray[strArray.Length - 1];
        }

        /// <summary>
        /// 获取类型名称(主要针对泛型做特殊处理)。如果要获取内部元素信息，请使用t.GetGenericArguments
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static string GetTypeName(System.Type t)
        {
            if (t.IsGenericParameter||t.IsConstructedGenericType)
            {
                //类型为泛型
                return t.Name.Split('`')[0];
            }
            else
            {
                //普通类型
                return t.Name;
            }
        }

        /// <summary>
        /// 获取类型全名(主要针对泛型做特殊处理)，比如List&lt;String&gt;返回System.Collections.Generic.List。如果要获取内部元素信息，请使用t.GetGenericArguments
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static string GetTypeFullName(System.Type t)
        {
            if (t.IsGenericParameter || t.IsConstructedGenericType)
            {
                //类型为泛型
                return t.FullName.Split('`')[0];
            }
            else
            {
                //普通类型
                return t.FullName;
            }
        }

        /// <summary>
        /// 返回泛型的类型全名，包括元素名，比如System.Collections.Generic.List&lt;System.String&gt;
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static string GetGenericTypeWithArgs(System.Type t)
        {
            //System.Collections.Generic.Dictionary`2[System.Int32,System.String]
            string[] arr = t.ToString().Split('`');

            string[] arrArgs = arr[1].Split('[');
            string args = "<" + arrArgs[1].TrimEnd(']') + ">";

            return arr[0] + args;
        }

        /// <summary>
        /// 是否是英文字符和下划线
        /// </summary>
        /// <param name="rawString"></param>
        /// <returns></returns>
        public static bool IsLetter(string rawString)
        {
            if (IsNullOrEmpty(rawString)) return false;

            char[] arrChar = rawString.ToCharArray();
            foreach (char c in arrChar)
            {

                if ("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_".IndexOf(c) < 0)
                    return false;
            }
            return true;
        }
        /// <summary>
        /// 只能以英文开头，允许英文、数字、下划线；
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsLetterNumber(string str)
        {
            if (IsNullOrEmpty(str))
            {
                return false;
            }
            var arr = str.ToCharArray();
            if ("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(arr[0]) < 0)
            {
                return false;
            }
            for (int i = 1; i < arr.Length; i++)
            {
                if ("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_1234567890".IndexOf(arr[i]) < 0)
                {
                    return false;
                }
            }
            return true;
        }
        /// <summary>
        /// 是否是主机地址
        /// </summary>
        /// <param name="rawString"></param>
        /// <returns></returns>
        public static bool IsHost(string rawString)
        {
            if (IsNullOrEmpty(rawString))
            {
                return false;
            }
            var s = (rawString.IndexOf('?') > 0 ? rawString.Substring(0, rawString.IndexOf('?')) : rawString).Split(new[] { "://", "/" }, StringSplitOptions.RemoveEmptyEntries);
            var temp = rawString.IndexOf("://") > 0 ? (s[0] + "://" + strUtil.Join("/", s.Skip(1).ToArray()) + "/") : (strUtil.Join("/", s) + "/");
            return temp.StartsWith(rawString);
        }
        /// <summary>
        /// 是否是英文、数字和下划线，但不能以下划线开头
        /// </summary>
        /// <param name="rawString"></param>
        /// <returns></returns>
        public static bool IsUrlItem(string rawString)
        {
            if (IsNullOrEmpty(rawString)) return false;

            char[] arrChar = rawString.ToCharArray();
            if (arrChar[0] == '_') return false;

            foreach (char c in arrChar)
            {
                if ("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_1234567890".IndexOf(c) < 0)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 是否全部都是中文字符
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsChineseLetter(string str)
        {
            if (IsNullOrEmpty(str)) return false;
            char[] arr = str.ToCharArray();
            for (int i = 0; i < arr.Length; i++)
            {
                if (IsChineseLetter(str, i) == false) return false;
            }
            return true;
        }
        /// <summary>
        /// 只能以英文或中文开头，允许英文、数字、下划线和中文；
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsAbcNumberAndChineseLetter(string str)
        {
            if (IsNullOrEmpty(str)) return false;

            char[] arr = str.ToCharArray();
            if (IsAbcAndChinese(arr[0]) == false) return false;

            for (int i = 0; i < arr.Length; i++)
            {
                if ("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_1234567890".IndexOf(arr[i]) >= 0) continue;
                if (IsChineseLetter(str, i) == false) return false;
            }
            return true;
        }

        private static bool IsAbcAndChinese(char c)
        {
            if ("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(c) >= 0) return true;
            if (IsChineseLetter(c.ToString(), 0)) return true;
            return false;
        }
        /// <summary>
        /// 判断字符串的第N位是否中文字符
        /// </summary>
        /// <param name="input"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private static bool IsChineseLetter(string input, int index)
        {

            int chineseCharBegin = System.Convert.ToInt32(0x4e00);
            int chineseCharEnd = System.Convert.ToInt32(0x9fff);
            int code = System.Char.ConvertToUtf32(input, index);
            return (code >= chineseCharBegin && code <= chineseCharEnd);
        }

        /// <summary>
        /// 是否是有效的颜色值(3位或6位，全部由英文字符或数字组成)
        /// </summary>
        /// <param name="aColor"></param>
        /// <returns></returns>
        public static bool IsColorValue(string aColor)
        {
            if (IsNullOrEmpty(aColor)) return false;
            string color = aColor.Trim().TrimStart('#').Trim();
            if (color.Length != 3 && color.Length != 6) return false;

            char[] arr = color.ToCharArray();
            foreach (char c in arr)
            {
                if ("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".IndexOf(c) < 0) return false;
            }

            return true;
        }


        /// <summary>
        /// 检测是否有Sql危险字符
        /// </summary>
        /// <param name="str">要判断字符串</param>
        /// <returns>判断结果</returns>
        public static bool IsSafeSqlString(string str)
        {
            return !Regex.IsMatch(str, @"[;|\/|\(|\)|\[|\]|\}|\{|%|\*|!|\']");
        }
        /// <summary>
        /// 获取安全的Sql参数值
        /// </summary>
        /// <param name="str">要处理的字符串</param>
        /// <returns>安全的Sql参数值</returns>
        public static string GetSafeSqlString(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return str;
            }
            return Regex.Replace(str, @"[;|\/|\(|\)|\[|\]|\}|\{|%|\*|!|\']", "");
        }
        /// <summary>
        /// 获取安全的Sql参数值
        /// </summary>
        /// <param name="str"></param>
        /// <param name="tolower"></param>
        /// <returns></returns>
        public static string GetSafeSqlString(string str, bool tolower)
        {
            if (tolower)
                return Regex.Replace(str, @"[;|\/|\(|\)|\[|\]|\}|\{|%|\*|!|\']", "").ToLower();
            else
                return Regex.Replace(str, @"[;|\/|\(|\)|\[|\]|\}|\{|%|\*|!|\']", "");
        }
        /// <summary>
        /// 是否为ip
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        // ReSharper disable once UnusedMember.Global
        public static bool IsIP(string ip)
        {
            return Regex.IsMatch(ip, @"^((2[0-4]\d|25[0-5]|[01]?\d\d?)\.){3}(2[0-4]\d|25[0-5]|[01]?\d\d?)$");
        }
        /// <summary>
        /// 是否为数字（包含小数点、负号）
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsNumber(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return false;
            }
            else
            {
                str = str.Replace(".", "").Replace("-", "");
            }
            return Regex.IsMatch(str, @"^\d+$");
        }
        /// <summary>
        /// 是否为手机号
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsMobile(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return false;
            }
            return str.Length == 11 && IsNumber(str) && str.StartsWith("1");
        }
        /// <summary>
        /// 是否为18位身份证号
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsIdentity(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return false;
            }
            else if (str.Length != 18)
            {
                return false;
            }
            else if (!strUtil.IsNumber(str.Substring(0, 17)) || !(strUtil.IsNumber(str.Substring(17)) || str.Substring(17).ToUpper() == "X"))
            {
                return false;
            }
            else if (cvt.ToInt(str.Substring(6, 4)) < 1900 || cvt.ToInt(str.Substring(6, 4)) > DateTime.Now.Year)
            {
                return false;
            }
            else if (cvt.ToInt(str.Substring(10, 2)) == 0 || cvt.ToInt(str.Substring(10, 2)) > 12)
            {
                return false;
            }
            else if (cvt.ToInt(str.Substring(12, 2)) == 0 || cvt.ToInt(str.Substring(12, 2)) > 31)
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// 是否为SQL安全字符
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        public static bool IsUrlSafeChar(char ch)
        {
            if ((((ch < 'a') || (ch > 'z')) && ((ch < 'A') || (ch > 'Z'))) && ((ch < '0') || (ch > '9')))
            {
                switch (ch)
                {
                    case '(':
                    case ')':
                    case '*':
                    case '-':
                    case '.':
                    case '!':
                        break;
                    case '+':
                    case ',':
                        return false;
                    default:
                        if (ch != '_')
                        {
                            return false;
                        }
                        break;
                }
            }
            return true;
        }
        /// <summary>
        /// 返回 URL 字符串的编码结果
        /// </summary>
        /// <param name="str">字符串</param>
        /// <param name="encoding">编码类型，如（gb2312,utf-8,gbk），默认为utf-8</param>
        /// <returns>编码结果</returns>
        public static string UrlEncode(string str, string encoding)
        {
            var encode = Encoding.UTF8;
            if (!string.IsNullOrEmpty(encoding))
            {
                encode = System.Text.Encoding.GetEncoding(encoding);
            }
            var bytes = encode.GetBytes(str);
            int num = 0;
            int num2 = 0;
            for (int i = 0; i < bytes.Length; i++)
            {
                char ch = (char)bytes[i];
                if (ch == ' ')
                {
                    num++;
                }
                else if (!IsUrlSafeChar(ch))
                {
                    num2++;
                }
            }
            if (num > 0 || num2 > 0)
            {
                byte[] buffer = new byte[bytes.Length + (num2 * 2)];
                int num3 = 0;
                for (int j = 0; j < bytes.Length; j++)
                {
                    byte num6 = bytes[j];
                    char ch2 = (char)num6;
                    if (IsUrlSafeChar(ch2))
                    {
                        buffer[num3++] = num6;
                    }
                    else if (ch2 == ' ')
                    {
                        buffer[num3++] = 0x2b;
                    }
                    else
                    {
                        buffer[num3++] = 0x25;
                        int i1 = (num6 >> 4) & 15;
                        int i2 = num6 & 15;
                        if (i1 >= 10)
                        {
                            buffer[num3++] = (byte)(i1 + 55);
                        }
                        else
                        {
                            buffer[num3++] = (byte)(i1 + 48);
                        }
                        if (i2 >= 10)
                        {
                            buffer[num3++] = (byte)(i2 + 55);
                        }
                        else
                        {
                            buffer[num3++] = (byte)(i2 + 48);
                        }
                    }
                }
                bytes = buffer;
            }
            if (bytes == null)
            {
                return null;
            }
            str = encode.GetString(bytes, 0, bytes.Length);
            return str;
        }
        /// <summary>
        /// 返回 URL 字符串的编码结果
        /// </summary>
        /// <param name="str">字符串</param>
        /// <returns>编码结果</returns>
        public static string UrlEncode(string str)
        {
            return UrlEncode(str, "utf-8");
        }
        /// <summary>
        /// 返回 URL 字符串的解码结果
        /// </summary>
        /// <param name="str">字符串</param>
        /// <returns>解码结果</returns>
        /// <param name="encoding">编码类型，如（gb2312,utf-8,gbk），默认为utf-8</param>
        public static string UrlDecode(string str, string encoding)
        {
            if (string.IsNullOrEmpty(str))
            {
                return "";
            }
            var encode = Encoding.UTF8;
            if (!string.IsNullOrEmpty(encoding))
            {
                encode = Encoding.GetEncoding(encoding);
            }
            int length = str.Length;
            var decoder = new Text.UrlDecoder(length, encode);

            for (int i = 0; i < length; i++)
            {
                char ch = str[i];
                if (ch == '+')
                {
                    ch = ' ';
                }
                else if ((ch == '%') && (i < (length - 2)))
                {
                    if ((str[i + 1] == 'u') && (i < (length - 5)))
                    {
                        int num3 = cvt.HexToInt(str[i + 2]);
                        int num4 = cvt.HexToInt(str[i + 3]);
                        int num5 = cvt.HexToInt(str[i + 4]);
                        int num6 = cvt.HexToInt(str[i + 5]);
                        if (((num3 < 0) || (num4 < 0)) || ((num5 < 0) || (num6 < 0)))
                        {
                            goto Label_out;
                        }
                        ch = (char)((((num3 << 12) | (num4 << 8)) | (num5 << 4)) | num6);
                        i += 5;
                        decoder.AddChar(ch);
                        continue;
                    }
                    int num7 = cvt.HexToInt(str[i + 1]);
                    int num8 = cvt.HexToInt(str[i + 2]);
                    if ((num7 >= 0) && (num8 >= 0))
                    {
                        byte b = (byte)((num7 << 4) | num8);
                        i += 2;
                        decoder.AddByte(b);
                        continue;
                    }
                }
            Label_out:
                if ((ch & 0xff80) == 0)
                {
                    decoder.AddByte((byte)ch);
                }
                else
                {
                    decoder.AddChar(ch);
                }
            }

            str = decoder.GetString();
            int num = -1;
            for (int i = 0; i < str.Length; i++)
            {
                if (char.IsSurrogate(str[i]))
                {
                    num = i;
                    break;
                }
            }
            if (num < 0)
            {
                return str;
            }
            char[] chArray = str.ToCharArray();
            for (int j = num; j < chArray.Length; j++)
            {
                char c = chArray[j];
                if (char.IsLowSurrogate(c))
                {
                    chArray[j] = (char)0xfffd;
                }
                else if (char.IsHighSurrogate(c))
                {
                    if (((j + 1) < chArray.Length) && char.IsLowSurrogate(chArray[j + 1]))
                    {
                        j++;
                    }
                    else
                    {
                        chArray[j] = (char)0xfffd;
                    }
                }
            }
            return new string(chArray);

        }
        /// <summary>
        /// 返回 URL 字符串的解码结果
        /// </summary>
        /// <param name="str">字符串</param>
        /// <returns>解码结果</returns>
        public static string UrlDecode(string str)
        {
            return UrlDecode(str, "utf-8");
        }
        /// <summary>
        /// 生成一个指定长度的随机字符串（仅有数字）
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        static public string CreateRndStr(int length)
        {
            string valid = "0123456789";
            string res = "";
            var rnd = new Random();
            while (0 < length--)
                res += valid[rnd.Next(valid.Length)];
            return res;
        }
        /// <summary>
        /// 生成一个指定长度的随机字符串(包含字母)
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        static public string CreateRndStrE(int length)
        {
            const string valid = "0123456789abcdefghijklmnopqrstuvwxyz";
            string res = "";
            var rnd = new Random();
            while (0 < length--)
                res += valid[rnd.Next(valid.Length)];
            return res;
        }
        /// <summary>
        /// 对字符串数组进行排序
        /// </summary>
        /// <param name="strS"></param>
        /// <returns></returns>
        public static string[] Sort(string[] strS)
        {
            var temp = "";
            for (int i = 0; i < strS.Length - 1; i++)
            {
                //从比i大1的值开始遍历到结束
                //这里比较的总是比i大的值，因为之前的值已经冒泡完成
                for (int j = i + 1; j < strS.Length; j++)
                {
                    //如果前一个值大于后一个值，他们交换位置
                    if (strS[i].ToUpper().CompareTo(strS[j].ToUpper()) > 0)
                    {
                        //交换位置
                        temp = strS[i];
                        strS[i] = strS[j];
                        strS[j] = temp;
                    }
                }
            }
            return strS;
        }
        /// <summary>
        /// 用“,”并联一个字符串数组
        /// </summary>
        /// <param name="strS"></param>
        /// <returns></returns>
        public static string Join(string[] strS)
        {
            return Join(",", strS);
        }
        /// <summary>
        /// 并联一个字符串数组
        /// </summary>
        /// <param name="separator">分隔符</param>
        /// <param name="strS"></param>
        /// <returns></returns>
        public static string Join(string separator, string[] strS)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string str in strS)
            {
                if (sb.Length > 0)
                {
                    sb.Append(separator);
                } sb.Append(str);
            }
            return sb.ToString();
        }
        /// <summary>
        /// 用斜杠/拼接两个字符串
        /// </summary>
        /// <param name="strA"></param>
        /// <param name="strB"></param>
        /// <returns></returns>
        public static string Join(string strA, string strB)
        {
            return Join(strA, strB, "/");
        }

        /// <summary>
        /// 根据制定的分隔符拼接两个字符串
        /// </summary>
        /// <param name="strA"></param>
        /// <param name="strB"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string Join(string strA, string strB, string separator)
        {
            return (Append(strA, separator) + TrimStart(strB, separator));
        }

        /// <summary>
        /// 剔除 html 中的 tag
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public static string ParseHtml(Object html)
        {
            if (html == null) return string.Empty;
            return HtmlReg.Replace(html.ToString(), "").Replace(" ", " ");
        }

        /// <summary>
        /// 剔除 html 中的 tag，并返回指定长度的字符串
        /// </summary>
        /// <param name="html"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static String ParseHtml(Object html, int count)
        {
            return CutString(ParseHtml(html), count).Replace("　", "");
        }

        /// <summary>
        /// 从 html 中截取指定长度的一段，并关闭未结束的 html 标签
        /// </summary>
        /// <param name="html"></param>
        /// <param name="count">需要截取的长度(小于20个字符按20个字符计算)</param>
        /// <returns></returns>
        public static String CutHtmlAndColse(String html, int count)
        {
            if (html == null) return "";
            html = html.Trim();
            if (count <= 0) return "";
            if (count < 20) count = 20;
            String unclosedHtml = html.Length <= count ? html : html.Trim().Substring(0, count);
            return CloseHtml(unclosedHtml);
        }

        /// <summary>
        /// 关闭未结束的 html 标签
        /// (TODO 本方法临时使用，待重写)
        /// </summary>
        /// <param name="unClosedHtml"></param>
        /// <returns></returns>
        public static String CloseHtml(String unClosedHtml)
        {
            if (unClosedHtml == null) return "";
            var arrTags = new[] { "strong", "b", "i", "u", "em", "font", "span", "label", "pre", "td", "th", "tr", "tbody", "table", "li", "ul", "ol", "h1", "h2", "h3", "h4", "h5", "h6", "p", "div" };

            for (int i = 0; i < arrTags.Length; i++)
            {

                var re = new Regex("<" + arrTags[i] + "[^>]*>", RegexOptions.IgnoreCase);
                int openCount = re.Matches(unClosedHtml).Count;
                if (openCount == 0) continue;

                re = new Regex("</" + arrTags[i] + ">", RegexOptions.IgnoreCase);
                int closeCount = re.Matches(unClosedHtml).Count;

                int unClosedCount = openCount - closeCount;

                for (var k = 0; k < unClosedCount; k++)
                {
                    unClosedHtml += "</" + arrTags[i] + ">";
                }
            }

            return unClosedHtml;
        }

        /// <summary>
        /// 将字符串分割成数组
        /// </summary>
        /// <param name="srcString"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static String[] Split(String srcString, String separator)
        {
            if (srcString == null) return null;
            if (separator == null) throw new ArgumentNullException();
            return srcString.Split(new[] { separator }, StringSplitOptions.None);
        }

        /// <summary>
        /// 过滤掉 sql 语句中的单引号，并返回指定长度的结果
        /// </summary>
        /// <param name="rawSql"></param>
        /// <param name="number"></param>
        /// <returns></returns>
        public static String SqlClean(String rawSql, int number)
        {
            if (IsNullOrEmpty(rawSql)) return rawSql;
            return SubString(rawSql, number).Replace("'", "''");
        }

        /// <summary>
        /// 从字符串中截取指定长度的一段，结果末尾没有省略号
        /// </summary>
        /// <param name="str"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static String SubString(String str, int length)
        {
            if (str == null) return null;
            if (str.Length > length) return str.Substring(0, length);
            return str;
        }

        /// <summary>
        /// 将纯文本中的换行符转换成html中换行符
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static String Text2Html(String str)
        {
            return str.Replace("\n", "<br/>");
        }

        /// <summary>
        /// 将html中换行符转换成纯文本中的换行符
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static String Html2Text(string str)
        {
            return RemoveHtmlTag(HtmlDecode(str).Replace(" ", "").Replace("\n", "").Replace("\t", "").Replace("<br>", "\r\n").Replace("<br/>", "\r\n").Replace("</p><p>", "\r\n")).Trim();
        }
        /// <summary>
        /// 从 srcString 的末尾剔除掉 trimString
        /// </summary>
        /// <param name="srcString"></param>
        /// <param name="trimString"></param>
        /// <returns></returns>
        public static String TrimEnd(String srcString, String trimString)
        {
            if (IsNullOrEmpty(trimString)) return srcString;
            if (srcString.EndsWith(trimString) == false) return srcString;
            if (srcString.Equals(trimString)) return "";
            return srcString.Substring(0, srcString.Length - trimString.Length);
        }

        /// <summary>
        /// 从 srcString 的开头剔除掉 trimString
        /// </summary>
        /// <param name="srcString"></param>
        /// <param name="trimString"></param>
        /// <returns></returns>
        public static String TrimStart(String srcString, String trimString)
        {
            if (srcString == null) return null;
            if (trimString == null) return srcString;
            if (IsNullOrEmpty(srcString)) return String.Empty;
            if (srcString.StartsWith(trimString) == false) return srcString;
            return srcString.Substring(trimString.Length);
        }

        /// <summary>
        /// 将 html 中的脚本从各个部位，全部挪到页脚，以提高网页加载速度
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public static String ResetScript(String html)
        {

            Regex reg = new Regex("<script.*?</script>", RegexOptions.Singleline);

            MatchCollection mlist = reg.Matches(html);
            StringBuilder sb = new StringBuilder();
            sb.Append(reg.Replace(html, ""));

            for (int i = 0; i < mlist.Count; i++)
            {
                sb.Append(mlist[i].Value);
            }
            return sb.ToString();
        }

        /// <summary>
        /// 将字符串分割成平均的n等份，每份长度为count
        /// </summary>
        /// <param name="str"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static List<String> SplitByNum(String str, int count)
        {

            List<String> list = new List<string>();

            if (str == null) return list;
            if (str.Length == 0)
            {
                list.Add(str);
                return list;
            }

            if (count <= 0)
            {
                list.Add(str);
                return list;
            }

            int k = 0;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < str.Length; i++)
            {

                if (k == count)
                {
                    list.Add(sb.ToString());
                    k = 0;
                    sb = new StringBuilder();
                }

                sb.Append(str[i]);

                k++;
            }

            if (sb.Length > 0) list.Add(sb.ToString());

            return list;
        }

        /// <summary>
        /// 将 html 中空白字符和空白标记(&amp;nbsp;)剔除掉
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static String TrimHtml(String val)
        {

            if (val == null) return null;
            val = val.Trim();

            String text = ParseHtml(val);
            text = TrimHtmlBlank(text);
            if (IsNullOrEmpty(text) && HasNotImg(val) && HasNotFlash(val)) return "";

            val = TrimHtmlBlank(val);
            return val;
        }

        private static String TrimHtmlBlank(String text)
        {

            if (text == null) return null;
            text = text.Trim();

            if (text.StartsWith(HtmlBlank) || text.EndsWith(HtmlBlank))
            {
                while (true)
                {
                    text = TrimStart(text, HtmlBlank).Trim();
                    text = TrimEnd(text, HtmlBlank).Trim();
                    if (!text.StartsWith(HtmlBlank) && !text.EndsWith(HtmlBlank)) break;
                }
            }

            return text;
        }

        private static Boolean HasNotImg(String val)
        {
            if (val.ToLower().IndexOf("<img ", StringComparison.Ordinal) >= 0) return false;
            return true;
        }

        private static Boolean HasNotFlash(String val)
        {
            if (val.ToLower().IndexOf("x-shockwave-flash", StringComparison.Ordinal) >= 0) return false;
            return true;
        }

        private const String HtmlBlank = "&nbsp;";


        /// <summary>
        /// 截取字符串末尾的整数
        /// </summary>
        /// <param name="rawString"></param>
        /// <returns></returns>
        public static int GetEndNumber(String rawString)
        {
            if (IsNullOrEmpty(rawString)) return 0;
            char[] chArray = rawString.ToCharArray();
            int startIndex = -1;
            for (int i = chArray.Length - 1; i >= 0; i--)
            {
                if (!char.IsDigit(chArray[i])) break;
                startIndex = i;
            }
            if (startIndex == -1) return 0;
            return cvt.ToInt(rawString.Substring(startIndex));
        }

        /// <summary>
        /// 将Text字符串转换成HTML格式字符串
        /// </summary>
        /// <returns></returns>
        public static string ConvertToHTML(string str)
        {
            if (string.IsNullOrEmpty(str))
                return string.Empty;
            return str.Replace("\r\n", "<br>").Replace("\n", "<br>").Replace(" ", "&nbsp;");
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string HtmlDecode(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }
            else
            {
                str = str.Replace("&gt; ", "> ");
                str = str.Replace("&lt; ", " < ");
                str = str.Replace("&nbsp; ", ((char)32).ToString());
                str = str.Replace("&quot; ", ((char)34).ToString());
                str = str.Replace("&#39; ", ((char)39).ToString());
                str = str.Replace(" ", ((char)13).ToString());
                str = str.Replace(" </P> <P> ", ((char)13 & (char)10).ToString());
                str = str.Replace(" <BR> ", ((char)10).ToString());
                return str;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string HtmlEncode(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }
            else
            {
                str = str.Replace("> ", "&gt; ");
                str = str.Replace(" < ", "&lt; ");
                str = str.Replace(((char)13 & (char)10).ToString(), " </P> <P> ");
                str = str.Replace(((char)10 & (char)10).ToString(), " </P> <P> ");
                str = str.Replace(((char)32).ToString(), "&nbsp; ");
                str = str.Replace(((char)34).ToString(), "&quot; ");
                str = str.Replace(((char)39).ToString(), "&#39; ");
                str = str.Replace(((char)13).ToString(), " ");
                str = str.Replace(((char)32).ToString(), " <BR> ");
                return str;
            }
        }





        /// <summary>
        ///  将Text字符串转换成javascript格式字符串
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string ConvertToJSString(string str)
        {
            if (string.IsNullOrEmpty(str)) return string.Empty;
            return str.Replace(@"\", @"\\").Replace("'", @"\'").Replace("\"", "\\\"").Replace("\r\n", ""); ;
        }
        /// <summary>
        /// 将阿拉伯数字转换成中文数字
        /// </summary>
        /// <param name="number"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string ConvertToCHNNumber(int number, bool type)
        {
            var strNum = new[] { "壹", "贰", "叁", "肆", "伍", "陆", "柒", "捌", "玖" };
            if (type) strNum = new[] { "一", "二", "三", "四", "五", "六", "七", "八", "九" };
            var strN = number.ToString(CultureInfo.InvariantCulture);
            var bl = -1;
            var ch = true;
            var len = strN.Length;
            if (len > 24)
                throw new Exception("输入的数字过大，无法转换");
            var strResult = "";
            var strSz = new string[len];
            for (int i = 0; i < len; i++)
            {
                strSz[i] = strN.Substring(i, 1);
                if (!Regex.IsMatch(strSz[i], "^[0-9]$"))
                    throw new Exception("输入的数字含有非数字符号");
                if (strSz[0] == "0" && ch)//检验首位出现零的情况
                {
                    if (i != len - 1 && strSz[i] == "0" && strSz[i + 1] != "0")
                        bl = i;
                    else
                        ch = false;
                }
            }
            for (int i = 0; i < len; i++)
            {
                int num = len - i;
                if (strSz[i] != "0")
                {
                    strResult += strNum[System.Convert.ToInt32(strSz[i]) - 1];//将阿拉伯数字转换成中文大写数字                    
                    if (num % 4 == 2)
                        strResult += type ? "十" : "拾";//加上单位
                    if (num % 4 == 3)
                        strResult += type ? "百" : "佰";
                    if (num % 4 == 0)
                        strResult += type ? "千" : "仟";
                    if (num % 4 == 1)
                    {
                        if (num / 4 == 1)
                            strResult += type ? "万" : "萬";
                        if (num / 4 == 2)
                            strResult += "亿";
                        if (num / 4 == 3)
                            strResult += type ? "万" : "萬";
                        if (num / 4 == 4)
                            strResult += "亿";
                        if (num / 4 == 5)
                            strResult += type ? "万" : "萬";
                    }
                }
                else
                {
                    if (i > bl)
                    {
                        if ((i != len - 1 && strSz[i + 1] != "0" && (num - 1) % 4 != 0))
                        {
                            //此处判断“0”不是出现在末尾，且下一位也不是“0”；
                            //如 10012332 在此处读法应该为壹仟零壹萬贰仟叁佰叁拾贰,两个零只要读一个零
                            strResult += "零";
                        }
                        if (i != len - 1 && strSz[i + 1] != "0")
                        {
                            switch (num)
                            {
                                //此处出现的情况是如 10002332，“0”出现在万位上就应该加上一个“萬”读成壹仟萬零贰仟叁佰叁拾贰
                                case 5: strResult += type ? "万" : "萬";
                                    break;
                                case 9: strResult += "亿";
                                    break;
                                case 13: strResult += type ? "万" : "萬";
                                    break;
                            }
                        }
                        if (i != len - 1 && strSz[i + 1] != "0" && (num - 1) % 4 == 0)
                        {
                            //此处出现的情况是如 10002332，“0”出现在万位上就应该加上一个“零”读成壹仟萬零贰仟叁佰叁拾贰
                            strResult += "零";
                        }
                    }
                }
            }
            if (type && strResult.IndexOf("一十", StringComparison.Ordinal) == 0)
                strResult = strResult.Substring(1);
            return strResult;
        }
        /// <summary>
        /// 将数字转换成中文写法
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static string ConvertToCHNNumber(int number)
        {
            return ConvertToCHNNumber(number, false);
        }
        /// <summary>
        /// 按指定的长度截取字符串
        /// </summary>
        /// <param name="s"></param>
        /// <param name="l"></param>
        /// <param name="endStr"></param>
        /// <returns></returns>
        public static string Ellipsis(string s, int l, string endStr)
        {
            s = s.Trim();
            string temp = s.Substring(0, (s.Length < l + 1) ? s.Length : l + 1);
            byte[] encodedBytes = Encoding.GetEncoding("ASCII").GetBytes(temp);
            string outputStr = "";
            int count = 0;
            for (int i = 0; i < temp.Length; i++)
            {
                if (encodedBytes[i] == 63)
                    count += 2;
                else
                    count += 1;

                if (count <= l - endStr.Length)
                    outputStr += temp.Substring(i, 1);
                else if (count > l)
                    break;
            }
            if (count <= l)
            {
                outputStr = temp;
                endStr = "";
            }
            outputStr += endStr;
            return outputStr;
        }
        /// <summary>
        /// 移除Html标签
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public static string RemoveHtmlTag(string html)
        {
            if (!string.IsNullOrEmpty(html))
            {
                html = Regex.Replace(html, "<[^>]*>", "");
                html = html.Replace("&nbsp;", " ");
            }
            return html;
        }
        /// <summary>
        /// 移除Html标签(可保留部分)
        /// </summary>
        /// <param name="html"></param>
        /// <param name="holdTags">保留的 tag </param>
        /// <returns></returns>
        public static string RemoveHtmlTagWithHold(string html, params string[] holdTags)
        {
            if (!string.IsNullOrEmpty(html))
            {
                if (holdTags == null || holdTags.Length == 0)
                {
                    holdTags = new[] { "a", "img", "br", "strong", "b", "span", "li" };//保留的 tag 
                }
                // <(?!((/?\s?li\b)|(/?\s?ul\b)|(/?\s?a\b)|(/?\s?img\b)|(/?\s?br\b)|(/?\s?span\b)|(/?\s?b\b)))[^>]+> 
                string regStr = string.Format(@"<(?!((/?\s?{0})))[^>]+>", string.Join(@"\b)|(/?\s?", holdTags));
                var reg = new Regex(regStr, RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnoreCase);

                return reg.Replace(html, "");
            }
            return html;
        }
        /// <summary>
        /// 按字节长度截取字符串(支持截取带HTML代码样式的字符串)
        /// </summary>
        /// <param name="param">将要截取的字符串参数</param>
        /// <param name="length">截取的字节长度</param>
        /// <param name="end">字符串末尾补上的字符串</param>
        /// <returns>返回截取后的字符串</returns>
        public static string SubstringToHTML(string param, int length, string end)
        {
            string pattern = null;
            MatchCollection m = null;
            var result = new StringBuilder();
            var n = 0;
            var isCode = false; //是不是HTML代码
            var isHtml = false; //是不是HTML特殊字符,如&nbsp;
            var pchar = param.ToCharArray();
            for (int i = 0; i < pchar.Length; i++)
            {
                char temp = pchar[i];
                if (temp == '<')
                    isCode = true;
                else if (temp == '&')
                    isHtml = true;
                else if (temp == '>' && isCode)
                {
                    n--; isCode = false;
                }
                else if (temp == ';' && isHtml)
                    isHtml = false;
                if (!isCode && !isHtml)
                {
                    n = n + 1;
                    if (Encoding.Unicode.GetBytes(temp + "").Length > 1)
                        n = n + 1;//UNICODE码字符占两个字节 
                }
                result.Append(temp);
                if (n >= length)
                {
                    result.Append(end);
                    break;
                }
            }
            //取出截取字符串中的HTML标记
            string tempResult = result.ToString().Replace("(>)[^<>]*(<?)", "$1$2");
            //去掉不需要结素标记的HTML标记
            tempResult = tempResult.Replace(@"</?(AREA|BASE|BASEFONT|BODY|BR|COL|COLGROUP|DD|DT|FRAME|HEAD|HR|HTML|IMG|INPUT|ISINDEX|LI|LINK|META|OPTION|P|PARAM|TBODY|TD|TFOOT|TH|THEAD|TR|area|base|basefont|body|br|col|colgroup|dd|dt|frame|head|hr|html|img|input|isindex|li|link|meta|option|p|param|tbody|td|tfoot|th|thead|tr)[^<>]*/?>", "");
            //去掉成对的HTML标记
            tempResult = tempResult.Replace(@"<([a-zA-Z]+)[^<>]*>(.*?)</\1>", "$2");
            //用正则表达式取出标记
            pattern = ("<([a-zA-Z]+)[^<>]*>");
            m = Regex.Matches(tempResult, pattern);
            var endHtml = new System.Collections.Generic.List<String>();
            foreach (Match mt in m)
            {
                endHtml.Add(mt.Result("$1"));
            }
            //补全不成对的HTML标记
            for (int i = endHtml.Count - 1; i >= 0; i--)
            {
                result.Append("</");
                result.Append(endHtml[i]);
                result.Append(">");
            }
            return result.ToString();
        }
        /// <summary>
        /// 将字节数组读取为UTF8编码的字符串（其它编码自动转换）
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static string GetUTF8String(byte[] buffer)
        {
            if (buffer.Length < 2)
            {
                return "";
            }
            var encoding = Wlniao.IO.IdentifyEncoding.GetEncodingName(buffer);
            var asciiBytes = encoding == "UTF-8" ? buffer : System.Text.Encoding.Convert(Encoding.GetEncoding(encoding), Encoding.UTF8, buffer);
            var asciiChars = new char[System.Text.Encoding.UTF8.GetCharCount(asciiBytes, 0, asciiBytes.Length)];
            Encoding.UTF8.GetChars(asciiBytes, 0, asciiBytes.Length, asciiChars, 0);
            if (asciiChars[0] == 65279)
            {
                return new string(asciiChars.Skip(1).ToArray());
            }
            else
            {
                return new string(asciiChars);
            }
        }
        /// <summary>
        /// 将字节数组读取为UTF8编码的字符串（其它编码自动转换）
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static string GetUTF8String(byte[] buffer, int index, int count)
        {
            if (buffer.Length < 2)
            {
                return "";
            }
            var encoding = IO.IdentifyEncoding.GetEncodingName(buffer);
            var asciiBytes = encoding == "UTF-8" ? buffer : System.Text.Encoding.Convert(Encoding.GetEncoding(encoding), Encoding.UTF8, buffer, index, count);
            var asciiChars = new char[System.Text.Encoding.UTF8.GetCharCount(asciiBytes, 0, asciiBytes.Length)];
            Encoding.UTF8.GetChars(asciiBytes, 0, asciiBytes.Length, asciiChars, 0);
            if (asciiChars[0] == 65279)
            {
                return new string(asciiChars.Skip(1).ToArray());
            }
            else
            {
                return new string(asciiChars);
            }
        }
        /// <summary>
        /// Unicode转换为字符
        /// </summary>
        /// <param name="strUnicode">Unicode编码</param>
        /// <returns>字符</returns>
        public static string GetStringByUnicode(string strUnicode)
        {
            string dst = "";
            string[] src = strUnicode.Split(new[] { "\\u" }, Int32.MaxValue, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < src.Length; i++)
            {
                var bytes = new byte[2];
                bytes[1] = byte.Parse(int.Parse(src[i].Substring(0, 2), NumberStyles.HexNumber).ToString(CultureInfo.InvariantCulture));
                bytes[0] = byte.Parse(int.Parse(src[i].Substring(2, 2), NumberStyles.HexNumber).ToString(CultureInfo.InvariantCulture));
                dst += Encoding.Unicode.GetString(bytes, 0, bytes.Length);
            }
            return dst;
        }
        /// <summary>
        /// 字符转换为Unicode编码
        /// </summary>
        /// <param name="strInput">字符</param>
        /// <returns>Unicode编码</returns>
        public static string GetUnicodeByString(string strInput)
        {
            char[] src = strInput.ToCharArray();
            string dst = "";
            for (int i = 0; i < src.Length; i++)
            {
                byte[] utext = Encoding.Unicode.GetBytes(src[i].ToString());
                dst += @"\u" + utext[1].ToString("x2") + utext[0].ToString("x2");
            }
            return dst;
        }

        /// <summary>
        /// 汉字转换为拼音(首字母大写)
        /// </summary>
        /// <param name="ch">输入的汉字</param>
        /// <returns>输出的拼音</returns>
        public static string Chs2Pinyin(char ch)
        {
            return Pinyin.Get(ch);
        }
        /// <summary>
        /// 汉字转换为拼音(每个字的首字母大写)
        /// </summary>
        /// <param name="chrstr">输入的汉字</param>
        /// <returns>输出的拼音</returns>
        public static string Chs2Pinyin(string chrstr)
        {
            // 匹配中文字符
            var regex = new Regex("^[\u4e00-\u9fa5]$");
            var pyString = "";
            var noWChar = chrstr.ToCharArray();
            for (var j = 0; j < noWChar.Length; j++)
            {
                var ch = noWChar[j].ToString();
                if (regex.IsMatch(noWChar[j].ToString()))
                {
                    // 中文字符
                    ch = Pinyin.Get(ch);
                }
                pyString += ch;
            }
            return pyString;
        }
        /// <summary>
        /// 汉字转换为拼音
        /// </summary>
        /// <param name="chrstr">输入的汉字</param>
        /// <returns>输出的拼音</returns>
        public static string Chs2PinyinSplit(string chrstr)
        {
            // 匹配中文字符
            var regex = new Regex("^[\u4e00-\u9fa5]$");
            var pyString = "";
            var noWChar = chrstr.ToCharArray();
            for (int j = 0; j < noWChar.Length; j++)
            {
                var ch = noWChar[j].ToString();
                // 中文字符
                if (regex.IsMatch(noWChar[j].ToString()))
                {
                    ch = Pinyin.Get(ch);
                }
                if (IsNullOrEmpty(pyString))
                {
                    pyString = ch;
                }
                else
                {
                    pyString += " " + ch;
                }
            }
            return pyString;
        }
        /// <summary>
        /// 依次取得字符串中每个字符的拼音首字母
        /// </summary>
        /// <param name="strText"></param>
        /// <returns></returns>
        static public string Chs2PinyinSpell(string strText)
        {
            var myStr = "";
            if (!string.IsNullOrEmpty(strText))
            {
                var len = strText.Length;
                for (int i = 0; i < len; i++)
                {
                    myStr += Chs2Pinyin(strText.Substring(i, 1)).Substring(0, 1);
                }
            }
            return myStr;
        }
        /// <summary>
        /// 汉字转换为拼音（用于搜索）
        /// </summary>
        /// <param name="strText"></param>
        /// <returns></returns>
        static public string Chs2PinyinSearch(string strText)
        {
            var myStr = "";
            if (!string.IsNullOrEmpty(strText))
            {
                int len = strText.Length;
                myStr = Chs2Pinyin(strText) + " ";
                for (int i = 0; i < len; i++)
                {
                    myStr += Chs2Pinyin(strText.Substring(i, 1)).Substring(0, 1);
                }
            }
            return myStr;
        }
        /// <summary>
        /// 获取字符串数字
        /// </summary>
        /// <param name="strText"></param>
        /// <returns></returns>
        public static byte[] GetByteByStr(string strText)
        {
            return Encoding.UTF8.GetBytes(strText);
        }
        /// <summary>
        /// 取字符串中的大写字母
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static string GetOnlyUpper(string src)
        {
            string temp = "";
            foreach (char c in src)
            {
                int t = c;
                if (t > 65 && t <= 90)
                {
                    temp += (char)t;
                }
            }
            return temp;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static int GetTokenCharCount(string str)
        {
            if (string.IsNullOrEmpty(str))
                return 0;
            Regex seperatorReg = new Regex("[,;.!?'，。？：；‘’！“”—……、《》<>{}【】]", RegexOptions.IgnorePatternWhitespace);
            return seperatorReg.Matches(str).Count;
        }        
        /// <summary>
        /// utf-8 转换 gb2312
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string UTF8ToGB2312(string str)
        {
            try
            {
                byte[] temp = Encoding.UTF8.GetBytes(str);
                byte[] temp1 = System.Text.Encoding.Convert(Encoding.UTF8, Encoding.GB2312, temp);
                string result = Encoding.GB2312.GetString(temp1, 0, temp1.Length);
                return result;
            }
            catch
            {
                return null;
            }
        }



        /// <summary>
        /// gb2312 转换 utf-8
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string GB2312ToUTF8(string str)
        {
            try
            {
                byte[] unicodeBytes = Encoding.GB2312.GetBytes(str);
                byte[] asciiBytes = System.Text.Encoding.Convert(Encoding.GB2312, Encoding.UTF8, unicodeBytes);
                var asciiChars = new char[Encoding.UTF8.GetCharCount(asciiBytes, 0, asciiBytes.Length)];
                Encoding.UTF8.GetChars(asciiBytes, 0, asciiBytes.Length, asciiChars, 0);
                var result = new string(asciiChars);
                return result;
            }
            catch
            {
                return "";
            }
        }
        /// <summary>
        /// UNICODE字符转为中文，支持中英混排（编码\\uxxxx" 转换为"\uxxxx）
        /// </summary>
        /// <param name="unicodeString"></param>
        /// <returns></returns>
        public static string ConvertUnicodeStringToChinese(string unicodeString)
        {
            if (string.IsNullOrEmpty(unicodeString))
                return string.Empty;

            string outStr = unicodeString;

            Regex re = new Regex("\\\\u[0123456789abcdef]{4}", RegexOptions.IgnoreCase);
            MatchCollection mc = re.Matches(unicodeString);
            foreach (Match ma in mc)
            {
                outStr = outStr.Replace(ma.Value, ConverUnicodeStringToChar(ma.Value).ToString());
            }
            return outStr;
        }

        private static char ConverUnicodeStringToChar(string str)
        {
            char outStr = char.MinValue;
            outStr = (char)int.Parse(str.Remove(0, 2), System.Globalization.NumberStyles.HexNumber);
            return outStr;
        }

        static Regex reUnicode = new Regex(@"\\u([0-9a-fA-F]{4})", RegexOptions.CultureInvariant);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string ConvertStringToUnicode(string s)
        {
            return reUnicode.Replace(s, m =>
            {
                short c;
                if (short.TryParse(m.Groups[1].Value, System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture, out c))
                {
                    return "" + (char)c;
                }
                return m.Value;
            });
        }


        /// <summary>
        /// 检测敏感词（自动过滤符号，只支持中文）
        /// </summary>
        /// <param name="content">需要检测的内容，如：超G强抗t△干dst△扰敏■■感※◇词 kljb过＆滤jb一■＆№正■№则匹◎←配代crSBtr码（只……支{持^中#^文）</param>
        /// <param name="blackwords">需要检测的关键字，如：(超强|抗干扰|敏感词|过滤|正则匹配|代码|只支持|中文)</param>
        /// <returns>检测到敏感词时，Result.IsValid为false，且Result.Errors为命中关键字列表</returns>
        public static ApiResult<String> CheckSensitiveWordsZhcn(string content, string blackwords)
        {
            return CheckSensitiveWordsZhcn(content, blackwords, 0);
        }
        /// <summary>
        /// 检测敏感词（自动过滤符号，只支持中文）
        /// </summary>
        /// <param name="content">需要检测的内容，如：超G强抗t△干dst△扰敏■■感※◇词 kljb过＆滤jb一■＆№正■№则匹◎←配代crSBtr码（只……支{持^中#^文）</param>
        /// <param name="blackwords">需要检测的关键字，如：(超强|抗干扰|敏感词|过滤|正则匹配|代码|只支持|中文)</param>
        /// <param name="times">最低命中次数</param>
        /// <returns>检测到敏感词时，Result.IsValid为false，且Result.Errors为命中关键字列表</returns>
        public static ApiResult<String> CheckSensitiveWordsZhcn(string content, string blackwords, int times)
        {
            return CheckSensitiveWords(Regex.Replace(content, "[^\u4e00-\u9fa5]", ""), blackwords, times);
        }

        /// <summary>
        /// 检测敏感词（自动过滤符号）
        /// </summary>
        /// <param name="content">需要检测的内容</param>
        /// <param name="blackwords">需要检测的关键字</param>
        /// <returns>检测到敏感词时，Result.IsValid为false，且Result.Errors为命中关键字列表</returns>
        public static ApiResult<String> CheckSensitiveWords(string content, string blackwords)
        {
            return CheckSensitiveWords(content, blackwords, 0);
        }
        /// <summary>
        /// 检测敏感词（自动过滤符号）
        /// </summary>
        /// <param name="content">需要检测的内容</param>
        /// <param name="blackwords">需要检测的关键字</param>
        /// <param name="times">最低命中次数</param>
        /// <returns>检测到敏感词时，Result.IsValid为false，且Result.Errors为命中关键字列表</returns>
        public static ApiResult<String> CheckSensitiveWords(string content, string blackwords, int times)
        {
            var result = new ApiResult<String>();
            result.message = "无匹配字符";
            if (!string.IsNullOrEmpty(blackwords))
            {
                if (!blackwords.StartsWith("("))
                {
                    blackwords = "(" + blackwords;
                }
                if (!blackwords.EndsWith(")"))
                {
                    blackwords += ")";
                }
                var matches = Regex.Matches(content, blackwords);
                if (matches.Count > times)
                {
                    result.success = true;
                    result.message = "有匹配字符";
                    result.data = string.Empty;
                    var ie = matches.GetEnumerator();
                    while (ie.MoveNext())
                    {
                        if (ie.Current != null)
                        {
                            result.data+= ie.Current.ToString();
                        }
                    }
                }
            }
            return result;
        }

        private static string [] beReplacedStrs = new[] { ".com.cn", ".edu.cn", ".net.cn", ".org.cn", ".co.jp", ".gov.cn", ".co.uk", ".ac.cn", ".edu", ".tv", ".info", ".com", ".ac", ".ag", ".am", ".at", ".be", ".biz", ".bz", ".cc", ".cn", ".com", ".de", ".es", ".eu", ".fm", ".gs", ".hk", ".in", ".info", ".io", ".it", ".jp", ".la", ".md", ".ms", ".name", ".net", ".nl", ".nu", ".org", ".pl", ".ru", ".sc", ".se", ".sg", ".sh", ".tc", ".tk", ".tv", ".tw", ".us", ".co", ".uk", ".vc", ".vg", ".ws", ".il", ".li", ".nz" };


        /// <summary>
        /// 根据完整的URL获取域名
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string GetDomain(string url)
        {
            try
            {
                return new Uri(url).Host;
            }
            catch
            {
                return "";
            }
        }
        /// <summary>
        /// 获取域名主机和主域部分
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="host"></param>
        /// <param name="main"></param>
        /// <returns></returns>
        public static void GetDomainSplit(string domain, out string host, out string main)
        {
            host = "";
            main = "";
            foreach (string suffix in beReplacedStrs)
            {
                if (domain.EndsWith(suffix))
                {
                    var tmp = domain.Substring(0, domain.Length - suffix.Length);
                    main = tmp.Substring(tmp.LastIndexOf('.') + 1) + suffix;
                    if (tmp.LastIndexOf('.') > 0)
                    {
                        host = tmp.Substring(0, tmp.LastIndexOf('.'));
                    }
                    break;
                }
            }
        }
        /// <summary>
        /// 获取域名主域部分
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        public static string GetDomainMain(string domain)
        {
            foreach (string suffix in beReplacedStrs)
            {
                if (domain.EndsWith(suffix))
                {
                    var tmp = domain.Substring(0, domain.Length - suffix.Length);
                    if (tmp.LastIndexOf('.') > 0)
                    {
                        return tmp.Substring(tmp.LastIndexOf('.') + 1) + suffix;
                    }
                    else
                    {
                        return tmp + suffix;
                    }
                }
            }
            return "";
        }
        /// <summary>
        /// 获取域名主机部分
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        public static string GetDomainHost(string domain)
        {
            foreach (string suffix in beReplacedStrs)
            {
                if (domain.EndsWith(suffix))
                {
                    var tmp = domain.Substring(0, domain.Length - suffix.Length);
                    if (tmp.LastIndexOf('.') > 0)
                    {
                        tmp = tmp.Substring(0, tmp.LastIndexOf('.'));
                        return tmp;
                    }
                }
            }
            return "";
        }
        /// <summary>
        /// 获取主域名无后缀部分
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        public static string GetDomainMainNoSuffix(string domain)
        {
            var tmp = "";
            foreach (string suffix in beReplacedStrs)
            {
                if (domain.EndsWith(suffix))
                {
                    tmp = domain.Substring(0, domain.Length - suffix.Length);
                    if (tmp.LastIndexOf('.') > 0)
                    {
                        tmp = tmp.Substring(tmp.LastIndexOf('.') + 1);
                    }
                    break;
                }
            }
            return tmp;
        }

        /// <summary>
        /// 将查询字符串解析转换为名值集合.
        /// </summary>
        /// <param name="queryString"></param>
        /// <returns></returns>
        public static Dictionary<string, string> GetQueryString(string queryString)
        {
            return GetQueryString(queryString, true);
        }

        /// <summary>
        /// 将查询字符串解析转换为名值集合.
        /// </summary>
        /// <param name="queryString"></param>
        /// <param name="isEncoded"></param>
        /// <returns></returns>
        public static Dictionary<string, string> GetQueryString(string queryString, bool isEncoded)
        {
            queryString = queryString.Replace("?", "");
            var kvs = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(queryString))
            {
                int count = queryString.Length;
                for (int i = 0; i < count; i++)
                {
                    int startIndex = i;
                    int index = -1;
                    while (i < count)
                    {
                        char item = queryString[i];
                        if (item == '=')
                        {
                            if (index < 0)
                            {
                                index = i;
                            }
                        }
                        else if (item == '&')
                        {
                            break;
                        }
                        i++;
                    }
                    string key;
                    string value = null;
                    if (index >= 0)
                    {
                        key = queryString.Substring(startIndex, index - startIndex);
                        value = queryString.Substring(index + 1, (i - index) - 1);
                    }
                    else
                    {
                        key = queryString.Substring(startIndex, i - startIndex);
                    }
                    if (isEncoded)
                    {
                        kvs.Add(UrlDecode(key), UrlDecode(value));
                    }
                    else
                    {
                        kvs.Add(key, value);
                    }
                    if ((i == (count - 1)) && (queryString[i] == '&'))
                    {
                        kvs[key] = string.Empty;
                    }
                }
            }
            return kvs;
        }
    }
}

