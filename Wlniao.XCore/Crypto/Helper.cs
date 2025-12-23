using System;
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
        /// 对Hex及Base64密钥自动解码
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte[] Decode(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return null;
            }
            else if (Regex.IsMatch(data, "^[0-9a-f]+$", RegexOptions.IgnoreCase))
            {
                return System.Convert.FromHexString(data);
            }
            else if(data.Length % 4 == 0 && Regex.IsMatch(data, "^[A-Za-z0-9+/]+(={0,2})$", RegexOptions.IgnoreCase))
            {
                return System.Convert.FromBase64String(data);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
		/// 对字节数组编码为Hex或Base64
		/// </summary>
        /// <param name="bytes"></param>
        /// <param name="hex"></param>
		/// <returns></returns>
		public static string Encode(byte[] bytes, bool hex = true)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return string.Empty;
            }
            return hex ? Wlniao.Convert.BytesToHexString(bytes) : System.Convert.ToBase64String(bytes);
        }
    }
}
