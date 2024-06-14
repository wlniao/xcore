using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;

namespace Wlniao.Crypto
{
    /// <summary>
    /// 数组计算工具
    /// </summary>
    public class ByteArray
    {
        /// <summary>
        /// 两个数组进行异或运算
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static byte[] Xor(byte[] left, byte[] right)
        {
            byte[] val = new byte[left.Length];
            for (int i = 0; i < left.Length; i++)
            {
                val[i] = (byte)(left[i] ^ right[i]);
            }
            return val;
        }
        /// <summary>
        /// 对数组进行反转操作
        /// </summary>
        /// <param name="arr"></param>
        public static void Reverse(byte[] arr)
        {
            for (int i = 0; i < arr.Length / 2; i++)
            {
                byte tmp = arr[i];
                arr[i] = arr[arr.Length - i - 1];
                arr[arr.Length - i - 1] = tmp;
            }
        }
    }
}