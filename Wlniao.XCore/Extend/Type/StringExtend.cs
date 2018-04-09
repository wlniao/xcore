using System;
using System.Text;
using System.Text.RegularExpressions;

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
            return input != null && strUtil.IsNumber(input) && input.Length == 11 && input.StartsWith("1");
        }
        /// <summary>
        /// 是否为邮箱地址
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool IsEmail(this string input)
        {
            return Regex.IsMatch(input, @"^\w+@\w+\.\w+$");
        }
        /// <summary>
        /// 分割字符串（不包含空字符串）
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string[] SplitBy(this string input)
        {
            return input.SplitBy(new[] { "," });
        }
        /// <summary>
        /// 分割字符串（不包含空字符串）
        /// </summary>
        /// <param name="input"></param>
        /// <param name="separator">用指定字符分割</param>
        /// <returns></returns>
        public static string[] SplitBy(this string input, params String[] separator)
        {
            if (string.IsNullOrEmpty(input))
            {
                return new string[0];
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
            return Regex.Replace(input, new string(new char[] { (char)65279 }), "");
        }
    }
}