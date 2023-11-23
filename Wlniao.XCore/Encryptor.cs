/*==============================================================================
    文件名称：Encryptor.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：HASH算法加密工具类
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
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using System;
using System.IO;
using System.Security.Cryptography;
using Wlniao.Text;
using Wlniao.Crypto;
using System.Text.RegularExpressions;

namespace Wlniao
{
    /// <summary>
    /// HASH算法加密工具类
    /// </summary>
    public class Encryptor
    {
        /// <summary>
        /// 32位MD5算法加密（多次加密）
        /// </summary>
        /// <param name="str">需要加密的字符串</param>
        /// <param name="time">需要加密的次数</param>
        /// <returns>加密后的字符串</returns>
        public static string Md5Encryptor32(string str, int time)
        {
            do
            {
                str = Md5Encryptor32(str);
                time--;
            } while (time > 0);
            return str;
        }
        /// <summary>
        /// 32位MD5算法加密（大写）
        /// </summary>
        /// <param name="str">需要加密的字符串</param>
        /// <returns>加密后的字符串</returns>
        public static string Md5Encryptor32(string str)
        {
            var password = "";
            var s = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(str ?? ""));
            foreach (byte b in s)
            {
                password += b.ToString("X2");
            }
            return password;
        }
        /// <summary>
        /// 16位MD5算法加密
        /// </summary>
        /// <param name="str">需要加密的字符串</param>
        /// <returns>加密后的字符串</returns>
        public static string Md5Encryptor16(string str)
        {
            var s = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(str ?? ""));
            return BitConverter.ToString(s, 4, 8).Replace("-", "");
        }

        /// <summary>
        ///  HmacSHA256算法加密
        /// </summary>
        /// <param name="str"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string HmacSHA256Encryptor(string str, string key)
        {
            var hashAlgorithm = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            var bytes = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(str));
            return IO.Base64Encoder.Encoder.GetEncoded(bytes);
        }

        /// <summary>
        /// Base64编码
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string Base64Encrypt(string str)
        {
            return IO.Base64Encoder.Encoder.GetEncoded(Encoding.UTF8.GetBytes(str ?? ""));
        }
        /// <summary>
        /// Base64解码
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string Base64Decrypt(string str)
        {
            return Encoding.UTF8.GetString(IO.Base64Decoder.Decoder.GetDecoded(str ?? ""));
        }
        /// <summary>
        /// 逐字编码查询条件
        /// </summary>
        /// <param name="txt"></param>
        /// <param name="crypt"></param>
        /// <returns></returns>
        public static string VerbatimQuery(string txt, int crypt = 6338)
        {
            var code = VerbatimEncrypt(txt);
            if (code.EndsWith('='))
            {
                code = code.Substring(0, code.Length - 3);
            }
            return code;
        }
        /// <summary>
        /// 逐字编码文本内容
        /// </summary>
        /// <param name="txt"></param>
        /// <param name="crypt"></param>
        /// <returns></returns>
        public static string VerbatimEncrypt(string txt, int crypt = 6338)
        {
            if (!string.IsNullOrEmpty(txt))
            {
                var buffer = Encoding.UTF8.GetBytes(txt);
                for (var i = 0; i < buffer.Length; i++)
                {
                    buffer[i] = (byte)((uint)buffer[i] + crypt);
                }
                return System.Convert.ToBase64String(buffer);
            }
            return "";
        }
        /// <summary>
        /// 逐字解码文本内容
        /// </summary>
        /// <param name="txt"></param>
        /// <param name="crypt"></param>
        /// <returns></returns>
        public static string VerbatimDecrypt(string txt, int crypt = 6338)
        {
            if (!string.IsNullOrEmpty(txt))
            {
                var buffer = System.Convert.FromBase64String(txt);
                for (var i = 0; i < buffer.Length; i++)
                {
                    buffer[i] = (byte)((uint)buffer[i] - crypt);
                }
                return Encoding.UTF8.GetString(buffer);
            }
            return txt;
        }
        /// <summary>
        /// 加密函数
        /// </summary>
        /// <param name="pToEncrypt">需要加密的字符串</param>
        /// <param name="sKey">加密密钥</param>
        /// <param name="sIV">偏移量</param>
        /// <returns>返回加密后的密文</returns>
        public static string AesEncrypt(string pToEncrypt, string sKey, string sIV = "")
        {
            if (!string.IsNullOrEmpty(pToEncrypt))
            {
                try
                {
                    var aes = Aes.Create();
                    var key = new char[32];
                    for (var i = 0; i < key.Length && i < sKey.Length; i++)
                    {
                        key[i] = sKey[i];
                    }
                    aes.Key = Encoding.ASCII.GetBytes(key);
                    var iv = new char[16];
                    for (var i = 0; i < iv.Length && i < sIV.Length; i++)
                    {
                        iv[i] = sIV[i];
                    }
                    aes.IV = Encoding.ASCII.GetBytes(iv);
                    aes.Padding = PaddingMode.PKCS7;
                    var inputByteArray = Encoding.UTF8.GetBytes(pToEncrypt);
                    using (var ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(inputByteArray, 0, inputByteArray.Length);
                            cs.FlushFinalBlock();
                            return System.Convert.ToBase64String(ms.ToArray());
                        }
                    }
                }
                catch { }
            }
            return "";
        }
        /// <summary>
        /// 解密函数
        /// </summary>
        /// <param name="pToDecrypt">需要解密的字符串</param>
        /// <param name="sKey">加密密钥</param>
        /// <param name="sIV">偏移量</param>
        /// <returns>返回加密前的明文</returns>
        public static string AesDecrypt(string pToDecrypt, string sKey, string sIV = "")
        {
            if (!string.IsNullOrEmpty(pToDecrypt))
            {
                try
                {
                    var aes = Aes.Create();
                    var key = new char[32];
                    for (var i = 0; i < key.Length && i < sKey.Length; i++)
                    {
                        key[i] = sKey[i];
                    }
                    aes.Key = Encoding.ASCII.GetBytes(key);
                    var iv = new char[16];
                    for (var i = 0; i < iv.Length && i < sIV.Length; i++)
                    {
                        iv[i] = sIV[i];
                    }
                    aes.IV = Encoding.ASCII.GetBytes(iv);
                    aes.Padding = PaddingMode.PKCS7;
                    var inputByteArray = System.Convert.FromBase64String(pToDecrypt);
                    using (var ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(inputByteArray, 0, inputByteArray.Length);
                            cs.FlushFinalBlock();
                            return Encoding.UTF8.GetString(ms.ToArray());
                        }
                    }
                }
                catch { }
            }
            return "";
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="plainText"></param>
        /// <param name="publicKey"></param>
        /// <returns></returns>
        public static String SM2EncryptByPublicKey(string plainText, string publicKey)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return "";
            }
            var sm2 = new SM2(Helper.Decode(publicKey), null, SM2Mode.C1C3C2);
            var plainBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            var encryBytes = sm2.Encrypt(plainBytes);
            return cvt.BytesToHexString(encryBytes);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="plainBytes"></param>
        /// <param name="publicKey"></param>
        /// <returns></returns>
        public static String SM2EncryptByPublicKey(byte[] plainBytes, byte[] publicKey)
        {
            if (plainBytes == null || publicKey == null)
            {
                return "";
            }
            var sm2 = new SM2(publicKey, null, SM2Mode.C1C3C2);
            var encryBytes = sm2.Encrypt(plainBytes);
            return cvt.BytesToHexString(encryBytes);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="encryText"></param>
        /// <param name="privateKey"></param>
        /// <returns></returns>
        public static String SM2DecryptByPrivateKey(string encryText, string privateKey)
        {
            if (string.IsNullOrEmpty(encryText))
            {
                return "";
            }
            else if (encryText[0] != '0' && encryText[1] != '4' && Regex.IsMatch(encryText, "^[0-9a-f]+$", RegexOptions.IgnoreCase))
            {
                encryText = "04" + encryText;
            }
            var sm2 = new SM2(null, Helper.Decode(privateKey), SM2Mode.C1C3C2);
            var encryBytes = Helper.Decode(encryText);
            var plainBytes = sm2.Decrypt(encryBytes);
            return System.Text.Encoding.UTF8.GetString(plainBytes);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="encryBytes"></param>
        /// <param name="privateKey"></param>
        /// <returns></returns>
        public static String SM2DecryptByPrivateKey(byte[] encryBytes, byte[] privateKey)
        {
            if (encryBytes == null || privateKey == null)
            {
                return "";
            }
            var sm2 = new SM2(null, privateKey, SM2Mode.C1C3C2);
            var plainBytes = sm2.Decrypt(encryBytes);
            return System.Text.Encoding.UTF8.GetString(plainBytes);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="plainText"></param>
        /// <param name="secretKey"></param>
        /// <param name="isPadding"></param>
        /// <returns></returns>
        public static String SM4EncryptECBToHex(string plainText, string secretKey, bool isPadding = true)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return "";
            }
            var keyBytes = System.Text.Encoding.UTF8.GetBytes((secretKey + "0000000000000000").Substring(0, 16));
            var plainBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            var sm4 = new SM4();
            var encryBytes = sm4.EncryptECB(plainBytes, keyBytes, isPadding);
            return cvt.BytesToHexString(encryBytes);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="plainText"></param>
        /// <param name="secretKey"></param>
        /// <param name="isPadding"></param>
        /// <returns></returns>
        public static String SM4EncryptECBToBase64(string plainText, string secretKey, bool isPadding = true)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return "";
            }
            var keyBytes = System.Text.Encoding.UTF8.GetBytes((secretKey + "0000000000000000").Substring(0, 16));
            var plainBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            var sm4 = new SM4();
            var encryBytes = sm4.EncryptECB(plainBytes, keyBytes, isPadding);
            return System.Convert.ToBase64String(encryBytes);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="encryText"></param>
        /// <param name="secretKey"></param>
        /// <param name="isPadding"></param>
        /// <returns></returns>
        public static String SM4DecryptECBFromHex(string encryText, string secretKey, bool isPadding = true)
        {
            if (!string.IsNullOrEmpty(encryText))
            {
                try
                {
                    var keyBytes = System.Text.Encoding.UTF8.GetBytes((secretKey + "0000000000000000").Substring(0, 16));
                    var encryBytes = Helper.Decode(encryText);
                    var sm4 = new SM4();
                    var plainBytes = sm4.DecryptECB(encryBytes, keyBytes, isPadding);
                    return System.Text.Encoding.UTF8.GetString(plainBytes);
                }
                catch { }
            }
            return "";
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="encryText"></param>
        /// <param name="secretKey"></param>
        /// <param name="isPadding"></param>
        /// <returns></returns>
        public static String SM4DecryptECBFromBase64(string encryText, string secretKey, bool isPadding = true)
        {
            if (!string.IsNullOrEmpty(encryText))
            {
                try
                {
                    var keyBytes = System.Text.Encoding.UTF8.GetBytes((secretKey + "0000000000000000").Substring(0, 16));
                    var encryBytes = Helper.Decode(encryText);
                    var sm4 = new SM4();
                    var plainBytes = sm4.DecryptECB(encryBytes, keyBytes, isPadding);
                    return System.Text.Encoding.UTF8.GetString(plainBytes);
                }
                catch { }
            }
            return "";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="plainText"></param>
        /// <param name="secretKey"></param>
        /// <param name="iv"></param>
        /// <param name="isPadding"></param>
        /// <returns></returns>
        public static String SM4EncryptCBCToHex(string plainText, string secretKey, string iv, bool isPadding = true)
        {
            if (!string.IsNullOrEmpty(plainText))
            {
                try
                {
                    var ivBytes = System.Text.Encoding.UTF8.GetBytes((iv + "0000000000000000").Substring(0, 16));
                    var keyBytes = System.Text.Encoding.UTF8.GetBytes((secretKey + "0000000000000000").Substring(0, 16));
                    var plainBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
                    var sm4 = new SM4();
                    var encryBytes = sm4.EncryptCBC(plainBytes, keyBytes, ivBytes, isPadding);
                    return cvt.BytesToHexString(encryBytes);
                }
                catch { }
            }
            return "";
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="plainText"></param>
        /// <param name="secretKey"></param>
        /// <param name="iv"></param>
        /// <param name="isPadding"></param>
        /// <returns></returns>
        public static String SM4EncryptCBCToBase64(string plainText, string secretKey, string iv, bool isPadding = true)
        {
            if (!string.IsNullOrEmpty(plainText))
            {
                try
                {
                    var ivBytes = System.Text.Encoding.UTF8.GetBytes((iv + "0000000000000000").Substring(0, 16));
                    var keyBytes = System.Text.Encoding.UTF8.GetBytes((secretKey + "0000000000000000").Substring(0, 16));
                    var plainBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
                    var sm4 = new SM4();
                    var encryBytes = sm4.EncryptCBC(plainBytes, keyBytes, ivBytes, isPadding);
                    return System.Convert.ToBase64String(encryBytes);
                }
                catch { }
            }
            return "";
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="encryText"></param>
        /// <param name="secretKey"></param>
        /// <param name="iv"></param>
        /// <param name="isPadding"></param>
        /// <returns></returns>
        public static String SM4DecryptCBCFromHex(string encryText, string secretKey, string iv, bool isPadding = true)
        {
            if (!string.IsNullOrEmpty(encryText))
            {
                try
                {
                    var ivBytes = System.Text.Encoding.UTF8.GetBytes((iv + "0000000000000000").Substring(0, 16));
                    var keyBytes = System.Text.Encoding.UTF8.GetBytes((secretKey + "0000000000000000").Substring(0, 16));
                    var encryBytes = Helper.Decode(encryText);
                    var sm4 = new SM4();
                    var plainBytes = sm4.DecryptCBC(encryBytes, keyBytes, ivBytes, isPadding);
                    return System.Text.Encoding.UTF8.GetString(plainBytes);
                }
                catch { }
            }
            return "";
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="encryText"></param>
        /// <param name="secretKey"></param>
        /// <param name="iv"></param>
        /// <param name="isPadding"></param>
        /// <returns></returns>
        public static String SM4DecryptCBCFromBase64(string encryText, string secretKey, string iv, bool isPadding = true)
        {
            if (!string.IsNullOrEmpty(encryText))
            {
                try
                {
                    var ivBytes = System.Text.Encoding.UTF8.GetBytes((iv + "0000000000000000").Substring(0, 16));
                    var keyBytes = System.Text.Encoding.UTF8.GetBytes((secretKey + "0000000000000000").Substring(0, 16));
                    var encryBytes = Helper.Decode(encryText);
                    var sm4 = new SM4();
                    var plainBytes = sm4.DecryptCBC(encryBytes, keyBytes, ivBytes, isPadding);
                    return System.Text.Encoding.UTF8.GetString(plainBytes);
                }
                catch { }
            }
            return "";
        }

        /// <summary>
        /// 获取SM3值
        /// </summary>
        /// <param name="str">需要加密的字符串</param>
        /// <returns>加密后的字符串</returns>
        public static string SM3Encrypt(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return "";
            }
            var sm3 = new Crypto.SM3();
            var buffer = System.Text.Encoding.UTF8.GetBytes(str);
            sm3.BlockUpdate(buffer, 0, buffer.Length);
            buffer = sm3.DoFinal();
            return cvt.BytesToHexString(buffer);
        }
        /// <summary>
        /// 获取SHA1值
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string GetSHA1(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return "";
            }
            var dataToHash = Encoding.ASCII.GetBytes(str); //将str转换成byte[]
            var dataHashed = SHA1.Create().ComputeHash(dataToHash);//Hash运算
            return BitConverter.ToString(dataHashed).Replace("-", "");//将运算结果转换成string
        }
        /// <summary>
        /// HMACMD5加密
        /// </summary>
        /// <param name="str"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static byte[] GetHMACSHA1(string str, string key)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var hashAlgorithm = new HMACSHA1(keyBytes);
            return hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(str));
        }
        /// <summary>
        /// HMACMD5加密
        /// </summary>
        /// <param name="str"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string GetHMACSHA1String(string str, string key)
        {
            if (string.IsNullOrEmpty(str))
            {
                return "";
            }
            var enText = "";
            foreach (byte b in GetHMACSHA1(str, key))
            {
                enText += b.ToString("x2");
            }
            return enText;
        }
    }
}