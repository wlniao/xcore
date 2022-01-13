using Org.BouncyCastle.Utilities.Encoders;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Wlniao.Crypto
{
    /// <summary>
    /// 
    /// </summary>
    public class Helper
    {
        /// <summary>
        /// 使用指定的数字执行无符号按位右移
        /// </summary>
        /// <param name="number">要操作的编号</param>
        /// <param name="bits">要移位的比特数</param>
        /// <returns>移位操作产生的数字</returns>
        public static int URShift(int number, int bits)
        {
            if (number >= 0)
                return number >> bits;
            else
                return (number >> bits) + (2 << ~bits);
        }

        /// <summary>
        /// 使用指定的数字执行无符号按位右移
        /// </summary>
        /// <param name="number">要操作的编号</param>
        /// <param name="bits">要移位的比特数</param>
        /// <returns>移位操作产生的数字</returns>
        public static int URShift(int number, long bits)
        {
            return URShift(number, (int)bits);
        }

        /// <summary>
        /// 使用指定的数字执行无符号按位右移
        /// </summary>
        /// <param name="number">要操作的编号</param>
        /// <param name="bits">要移位的比特数</param>
        /// <returns>移位操作产生的数字</returns>
        public static long URShift(long number, int bits)
        {
            if (number >= 0)
                return number >> bits;
            else
                return (number >> bits) + (2L << ~bits);
        }

        /// <summary>
        /// 使用指定的数字执行无符号按位右移
        /// </summary>
        /// <param name="number">要操作的编号</param>
        /// <param name="bits">要移位的比特数</param>
        /// <returns>移位操作产生的数字</returns>
        public static long URShift(long number, long bits)
        {
            return URShift(number, (int)bits);
        }

        /// <summary>
        /// 对Hex及Base64密钥自动编码
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static byte[] Decode(string key)
        {
            return Regex.IsMatch(key, "^[0-9a-f]+$", RegexOptions.IgnoreCase) ? Hex.Decode(key) : System.Convert.FromBase64String(key);
        }
    }
}
