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
            else
            {
                return System.Convert.FromBase64String(data);
            }
        }

        /// <summary>
		/// 对Hex及Base64密钥自动编码
		/// </summary>
		/// <param name="bytes"></param>
		/// <returns></returns>
		public static string Encode(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return string.Empty;
            }
            var sb = new System.Text.StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
            {
                // 格式化为两位十六进制，不足补0（如0x1→"01"，0xAB→"AB"）
                sb.Append($"{b:x2}");
            }
            return sb.ToString();
        }
    }
}
