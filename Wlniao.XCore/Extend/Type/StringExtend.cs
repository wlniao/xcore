using System;
using System.Text.RegularExpressions;
using Wlniao.Text;

namespace Wlniao
{
    /// <summary>
    /// String类常用方法扩展
    /// </summary>
    public static class StringExtend
    {
        /// <summary>
        /// 是否为空
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        /// <remarks>
        /// 注意：本方法语义等同 BCL 的 <see cref="string.IsNullOrEmpty(string)"/>（纯空格字符串视为非空）。
        /// 而 <see cref="StringUtil.IsNullOrEmpty"/> 会把纯空格也判定为空，两者不可混用。
        /// </remarks>
        public static bool IsNullOrEmpty(this string input)
        {
            return string.IsNullOrEmpty(input);
        }
        /// <summary>
        /// 是否不为空
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool IsNotNullAndEmpty(this string input)
        {
            return !string.IsNullOrEmpty(input);
        }
        /// <summary>
        /// 是否为手机号
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool IsMobile(this string input)
        {
            return StringUtil.IsMobile(input);
        }
        /// <summary>
        /// 是否为邮箱地址
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool IsEmail(this string input)
        {
            return StringUtil.IsEmail(input);
        }
        /// <summary>
        /// 分割字符串（不包含空字符串）
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string[] SplitBy(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return Array.Empty<string>();
            }
            return input.SplitBy(new[] { "," });
        }
        /// <summary>
        /// 分割字符串（不包含空字符串）
        /// </summary>
        /// <param name="input"></param>
        /// <param name="separator">用指定字符分割</param>
        /// <returns></returns>
        public static string[] SplitBy(this string input, params string[] separator)
        {
            if (string.IsNullOrEmpty(input))
            {
                return Array.Empty<string>();
            }
            if (separator == null || separator.Length == 0)
            {
                separator = new[] { "," };
            }
            return input.Split(separator, StringSplitOptions.RemoveEmptyEntries);
        }
        /// <summary>
        /// 移除字符串中的BOM（UTF）
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string RemoveBOM(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }
            return Regex.Replace(input, new string(new char[] { (char)65279 }), "");
        }
    }
}