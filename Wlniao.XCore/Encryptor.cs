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
using System;
using System.IO;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Wlniao.Crypto;
using Wlniao.Log;

namespace Wlniao
{
    /// <summary>
    /// HASH算法加密工具类
    /// </summary>
    public static class Encryptor
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
            foreach (var b in s)
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
        /// 使用RSA私钥解密
        /// </summary>
        /// <param name="data">要解密的数据（Base64格式）</param>
        /// <param name="privateKey">RSA私钥，查看方式：openssl pkcs12 -in cert.pfx -nocerts -nodes</param>
        /// <param name="encoding">字符编码，默认为UTF8</param>
        /// <returns></returns>
        public static string RsaDecryptWithPrivate(string data, string privateKey, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;
            var sData = System.Convert.FromBase64String(data);
            if (privateKey.Contains("BEGIN RSA PRIVATE KEY") && !privateKey.Contains("BEGIN PRIVATE KEY"))
            {
                // PKCS#1 私钥，按 PKCS#1 v1.5 填充解密。
                using var rsa = CreateRsaFromPem(privateKey);
                return encoding.GetString(rsa.Decrypt(sData, RSAEncryptionPadding.Pkcs1));
            }
            else
            {
                // PKCS#8 私钥 + 无填充（教科书 RSA），保持历史语义：只做 c^d mod n，输出定长模长字节
                privateKey = privateKey.Replace("-----BEGIN PRIVATE KEY-----", "").Replace("-----END PRIVATE KEY-----", "").Replace("\n", "").Replace("\r", "").Trim();
                var privateBytes = System.Convert.FromBase64String(privateKey);
                using var rsa = RSA.Create();
                rsa.ImportPkcs8PrivateKey(privateBytes, out _);
                var parameters = rsa.ExportParameters(true);
                return encoding.GetString(RsaNoPadding(parameters, sData, usePrivate: true));
            }
        }

        /// <summary>
        /// 使用RSA公钥加密
        /// </summary>
        /// <param name="data">加密的数据</param>
        /// <param name="publicKey">RSA公钥，查看方式：openssl x509 -in send.crt -pubkey</param>
        /// <param name="encoding">字符编码，默认为UTF8</param>
        /// <returns></returns>
        public static string RsaEncryptWithPublic(string data, string publicKey, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;
            if (publicKey.Contains("BEGIN CERTIFICATE"))
            {
                publicKey = publicKey.Replace("-----BEGIN CERTIFICATE-----", "").Replace("-----END CERTIFICATE-----", "").Replace("\n", "").Replace("\r", "").Trim();
            }
            var certBytes = System.Convert.FromBase64String(publicKey);
            using var rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(certBytes, out _);
            var sData = encoding.GetBytes(data);
            // 保持历史语义：无填充（教科书 RSA），输出定长模长字节
            return System.Convert.ToBase64String(RsaNoPadding(rsa.ExportParameters(false), sData, usePrivate: false));
        }

        /// <summary>
        /// 使用RSA公钥签名
        /// </summary>
        /// <param name="data">要签名的数据</param>
        /// <param name="privateKey">RSA私钥，查看方式：openssl pkcs12 -in cert.pfx -nocerts -nodes</param>
        /// <param name="encoding">字符编码，默认为UTF8</param>
        /// <returns></returns>
        public static string RsaSignWithPrivate(string data, string privateKey, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;
            var sData = encoding.GetBytes(data);
            using var rsa = CreateRsaFromPem(privateKey);
            // 与历史 SignerUtilities.GetSigner("SHA1withRSA") 对应：SHA1 + PKCS#1 v1.5
            var signature = rsa.SignData(sData, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
            return System.Convert.ToBase64String(signature);
        }

        /// <summary>
        /// 使用RSA公钥验签
        /// </summary>
        /// <param name="data">要验签的数据</param>
        /// <param name="sign">要验证的签名</param>
        /// <param name="publicKey">RSA公钥，查看方式：openssl x509 -in send.crt -pubkey</param>
        /// <param name="encoding">字符编码，默认为UTF8</param>
        /// <returns></returns>
        public static bool RsaVerifyWithPublicKey(string data, string sign, string publicKey, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;
            var sData = encoding.GetBytes(data);
            if (publicKey.Contains("BEGIN CERTIFICATE"))
            {
                publicKey = publicKey.Replace("-----BEGIN CERTIFICATE-----", "").Replace("-----END CERTIFICATE-----", "").Replace("\n", "").Replace("\r", "").Trim();
            }
            var certBytes = System.Convert.FromBase64String(publicKey);
            using var rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(certBytes, out _);
            return rsa.VerifyData(sData, System.Convert.FromBase64String(sign), HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
        }

        /// <summary>
        /// 从 PEM 文本构造 RSA 实例。支持 PKCS#1（RSA PRIVATE KEY）与 PKCS#8（PRIVATE KEY），
        /// 也兼顾历史调用方直接传入裸 Base64 正文（无头尾标记）的情形。
        /// </summary>
        private static RSA CreateRsaFromPem(string privateKey)
        {
            var pem = privateKey;
            if (privateKey.Contains("BEGIN RSA PRIVATE KEY") && !privateKey.Contains("BEGIN PRIVATE KEY"))
            {
                if (!privateKey.Contains("----"))
                {
                    pem = "-----BEGIN RSA PRIVATE KEY-----" + privateKey + "-----END RSA PRIVATE KEY-----";
                }
            }
            else if (!privateKey.Contains("-----"))
            {
                var body = privateKey.Replace("-----BEGIN PRIVATE KEY-----", "").Replace("-----END PRIVATE KEY-----", "")
                    .Replace("\n", "").Replace("\r", "").Trim();
                pem = "-----BEGIN PRIVATE KEY-----" + body + "-----END PRIVATE KEY-----";
            }
            var rsa = RSA.Create();
            try
            {
                rsa.ImportFromPem(pem);
                return rsa;
            }
            catch
            {
                rsa.Dispose();
                throw;
            }
        }

        /// <summary>
        /// 无填充（教科书）RSA 运算：result = input^exp mod n，输出固定模长的大端字节。
        /// .NET 的 RSAEncryptionPadding 只有 Pkcs1 与 Oaep，没有 NoPadding，因此这里直接做模幂。
        /// 安全性说明：本次改造只求与历史行为保持一致，该方法本身不具备语义安全性，不应在新代码中使用。
        /// </summary>
        private static byte[] RsaNoPadding(RSAParameters parameters, byte[] input, bool usePrivate)
        {
            var modulus = new BigInteger(parameters.Modulus, isUnsigned: true, isBigEndian: true);
            var exponentBytes = usePrivate ? parameters.D : parameters.Exponent;
            var exponent = new BigInteger(exponentBytes, isUnsigned: true, isBigEndian: true);
            var value = new BigInteger(input, isUnsigned: true, isBigEndian: true);

            if (value >= modulus)
            {
                throw new CryptographicException("RSA 输入数据长度不能超过模长");
            }

            var raw = BigInteger.ModPow(value, exponent, modulus)
                .ToByteArray(isUnsigned: true, isBigEndian: true);

            var modulusLength = parameters.Modulus.Length;
            if (raw.Length > modulusLength)
            {
                throw new CryptographicException("RSA 运算结果超过模长");
            }
            var output = new byte[modulusLength];
            Buffer.BlockCopy(raw, 0, output, modulusLength - raw.Length, raw.Length);
            return output;
        }

        /// <summary>
        /// 获取SHA1值
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string Sha1(string str)
        {
            return HashToHex(SHA1.Create(), str);
        }
        /// <summary>
        /// 获取SHA256值
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string Sha256(string str)
        {
            return HashToHex(SHA256.Create(), str);
        }
        /// <summary>
        /// 获取SHA512值
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string Sha512(string str)
        {
            return HashToHex(SHA512.Create(), str);
        }

        /// <summary>
        /// Sha1 / Sha256 / Sha512 的唯一实现：空串返回空串，其余按 ASCII 编码取摘要并转大写十六进制。
        /// </summary>
        /// <remarks>
        /// 保持 ASCII 编码（而非 UTF8）是刻意为之：改成 UTF8 会让含非 ASCII 字符的输入摘要值发生变化。
        /// </remarks>
        /// <param name="algorithm">哈希算法实例，由本方法负责释放</param>
        /// <param name="str">待摘要的字符串</param>
        /// <returns>大写十六进制摘要；入参为空时返回空串</returns>
        private static string HashToHex(HashAlgorithm algorithm, string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return "";
            }
            using (algorithm)
            {
                var dataToHash = Encoding.ASCII.GetBytes(str); //将str转换成byte[]
                var dataHashed = algorithm.ComputeHash(dataToHash);//Hash运算
                return BitConverter.ToString(dataHashed).Replace("-", "");//将运算结果转换成string
            }
        }

        /// <summary>
        /// Hmac SM3算法加密
        /// </summary>
        /// <param name="str"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string HmacSM3(string str, string key)
        {
            var sm3 = new SM3();
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var dataBytes = Encoding.UTF8.GetBytes(str);
            return Convert.BytesToHexString(sm3.Hmac(dataBytes, keyBytes));
        }

        /// <summary>
        /// HMAC SHA1加密
        /// </summary>
        /// <param name="str"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string HmacSHA1(string str, string key)
        {
            var enText = "";
            if (string.IsNullOrEmpty(str))
            {
                return enText;
            }
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var dataBytes = Encoding.UTF8.GetBytes(str);
            foreach (var b in HmacSHA1(dataBytes, keyBytes))
            {
                enText += b.ToString("x2");
            }
            return enText;
        }

        /// <summary>
        /// HMAC SHA1加密
        /// </summary>
        /// <param name="dataBytes"></param>
        /// <param name="keyBytes"></param>
        /// <returns></returns>
        public static byte[] HmacSHA1(byte[] dataBytes, byte[] keyBytes)
        {
            var hashAlgorithm = new HMACSHA1(keyBytes);
            return hashAlgorithm.ComputeHash(dataBytes);
        }

        /// <summary>
        /// Hmac SHA256算法加密
        /// </summary>
        /// <param name="data"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static byte[] HmacSHA256(byte[] data, byte[] key)
        {
            var hashAlgorithm = new HMACSHA256(key);
            var bytes = hashAlgorithm.ComputeHash(data);
            return bytes;
        }
        /// <summary>
        /// Hmac SHA256算法加密
        /// </summary>
        /// <param name="str"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string HmacSHA256Hex(string str, string key)
        {
            var hashAlgorithm = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            var bytes = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(str));
            return System.Convert.ToHexString(bytes).ToLower();
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
            return code.EndsWith('=') ? code[..^3] : code; // code.Substring(0, code.Length - 3);
        }

        /// <summary>
        /// 逐字编码文本内容
        /// </summary>
        /// <param name="txt"></param>
        /// <param name="crypt"></param>
        /// <returns></returns>
        public static string VerbatimEncrypt(string txt, int crypt = 6338)
        {
            if (string.IsNullOrEmpty(txt))
            {
                return "";
            }
            var buffer = Encoding.UTF8.GetBytes(txt);
            for (var i = 0; i < buffer.Length; i++)
            {
                buffer[i] = (byte)((uint)buffer[i] + crypt);
            }
            return System.Convert.ToBase64String(buffer);
        }
        /// <summary>
        /// 逐字解码文本内容
        /// </summary>
        /// <param name="txt"></param>
        /// <param name="crypt"></param>
        /// <returns></returns>
        public static string VerbatimDecrypt(string txt, int crypt = 6338)
        {
            if (string.IsNullOrEmpty(txt))
            {
                return txt;
            }
            var buffer = System.Convert.FromBase64String(txt);
            for (var i = 0; i < buffer.Length; i++)
            {
                buffer[i] = (byte)((uint)buffer[i] - crypt);
            }
            return Encoding.UTF8.GetString(buffer);
        }
        /// <summary>
        /// 构造 AES 实例，并把密钥 / IV 补齐到 32 / 16 字节。
        /// </summary>
        /// <remarks>
        /// 以下三条是既定格式约定，不是可随意优化的实现细节：
        /// <list type="bullet">
        /// <item>IV 传空串时补成 16 个 0x00（全零 IV）。CBC 下相同明文前缀会产生相同密文，<b>不具备语义安全性</b>；</item>
        /// <item>密钥不足 32 字节（AES-256）时用 '\0' 补齐。补齐不增加熵，短密钥的强度仍按原始长度计算；</item>
        /// <item>密钥与 IV 按 ASCII 取字节，非 ASCII 字符会退化为 '?'。</item>
        /// </list>
        /// 这三条决定了密文的字节形态，修改任意一条都会让已落库的数据和外部系统持有的密文无法解密。
        /// 新业务需要随机 IV 或强密钥派生时，请另起独立 API，不要改动本方法。
        /// </remarks>
        /// <param name="sKey">加密密钥</param>
        /// <param name="iv">偏移量</param>
        /// <returns>已配置好 Key / IV / Padding 的 AES 实例</returns>
        private static Aes CreateAes(string sKey, string iv)
        {
            var aes = Aes.Create();
            aes.Key = Encoding.ASCII.GetBytes(FitLength(sKey, 32));
            aes.IV = Encoding.ASCII.GetBytes(FitLength(iv, 16));
            aes.Padding = PaddingMode.PKCS7;
            return aes;
        }

        /// <summary>
        /// 把字符串补齐（或截断）到指定长度：不足补 '\0'，超出截断。
        /// </summary>
        /// <remarks>
        /// 补 '\0' 且超长静默截断是既定约定：结果会直接作为 AES 密钥 / IV 的字节序列参与运算，
        /// 换成其他填充字符会让同一份密钥算出不同密文，导致存量数据无法解密。
        /// </remarks>
        /// <param name="value">源字符串</param>
        /// <param name="length">目标长度</param>
        /// <returns>定长字符数组</returns>
        private static char[] FitLength(string value, int length)
        {
            var buffer = new char[length];
            for (var i = 0; i < length && i < value.Length; i++)
            {
                buffer[i] = value[i];
            }
            return buffer;
        }

        /// <summary>
        /// 把 SM4 的密钥 / IV 补齐到 16 字节（不足补 '0'，超出截断）。
        /// </summary>
        /// <remarks>
        /// 补的是字符 '0'（0x30）而非零字节 '\0'，与 AES 那套补位规则不同，这是 SM4 接口的既定约定，
        /// 改动会让存量 SM4 密文和外部对接方的计算结果全部错位。
        /// 另需注意：补齐后按 UTF-8 取字节，SM4 只使用前 16 字节——密钥含非 ASCII 字符时会在多字节字符中间被截断，请只用 ASCII。
        /// </remarks>
        /// <param name="key">密钥或偏移量</param>
        /// <returns>16 字节的密钥材料</returns>
        private static byte[] FitKey16(string key)
        {
            return Encoding.UTF8.GetBytes((key + "0000000000000000")[..16]);
        }

        /// <summary>
        /// 加密函数
        /// </summary>
        /// <param name="pToEncrypt">需要加密的字符串</param>
        /// <param name="sKey">加密密钥</param>
        /// <param name="IV">偏移量</param>
        /// <returns>返回加密后的密文</returns>
        public static string AesEncrypt(string pToEncrypt, string sKey, string IV = "")
        {
            if (string.IsNullOrEmpty(pToEncrypt))
            {
                return "";
            }

            try
            {
                var aes = CreateAes(sKey, IV);
                var inputByteArray = Encoding.UTF8.GetBytes(pToEncrypt);
                using var ms = new MemoryStream();
                using var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
                cs.Write(inputByteArray, 0, inputByteArray.Length);
                cs.FlushFinalBlock();
                return System.Convert.ToBase64String(ms.ToArray());
            }
            catch (Exception e)
            {
                Loger.Debug($"{e.Message}{Environment.NewLine}{e.StackTrace}");
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
            if (string.IsNullOrEmpty(pToDecrypt))
            {
                return "";
            }
            try
            {
                var aes = CreateAes(sKey, sIV);
                var inputByteArray = System.Convert.FromBase64String(pToDecrypt);
                using var ms = new MemoryStream();
                using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write);
                cs.Write(inputByteArray, 0, inputByteArray.Length);
                cs.FlushFinalBlock();
                return Encoding.UTF8.GetString(ms.ToArray());
            }
            catch (Exception e)
            {
                Loger.Debug($"{e.Message}{Environment.NewLine}{e.StackTrace}");
            }
            return "";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="plainText"></param>
        /// <param name="publicKey"></param>
        /// <param name="base64"></param>
        /// <returns></returns>
        public static string SM2EncryptByPublicKey(string plainText, string publicKey, bool base64 = false)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return "";
            }

            var sm2 = new SM2(Helper.Decode(publicKey), null, SM2Mode.C1C3C2);
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encBytes = sm2.Encrypt(plainBytes);
            return base64 ? System.Convert.ToBase64String(encBytes) : Convert.BytesToHexString(encBytes);
        }
        

        /// <summary>
        /// 
        /// </summary>
        /// <param name="plainBytes"></param>
        /// <param name="publicKey"></param>
        /// <param name="base64"></param>
        /// <returns></returns>
        public static string SM2EncryptByPublicKey(byte[] plainBytes, byte[] publicKey, bool base64 = false)
        {
            if (plainBytes == null || publicKey == null)
            {
                return "";
            }
            var sm2 = new SM2(publicKey, null, SM2Mode.C1C3C2);
            var encBytes = sm2.Encrypt(plainBytes);
            return base64 ? System.Convert.ToBase64String(encBytes) : Convert.BytesToHexString(encBytes);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="encText"></param>
        /// <param name="privateKey"></param>
        /// <returns></returns>
        public static string SM2DecryptByPrivateKey(string encText, string privateKey)
        {
            if (string.IsNullOrEmpty(encText))
            {
                return "";
            }
            else if (encText[0] != '0' && encText[1] != '4' && Regex.IsMatch(encText, "^[0-9a-f]+$", RegexOptions.IgnoreCase))
            {
                encText = "04" + encText;
            }
            var pks = Helper.Decode(privateKey);
            var sm2 = new SM2(null, pks, SM2Mode.C1C3C2);
            var encBytes = Helper.Decode(encText);
            var plainBytes = sm2.Decrypt(encBytes);
            return Encoding.UTF8.GetString(plainBytes);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="encBytes"></param>
        /// <param name="privateKey"></param>
        /// <returns></returns>
        public static string SM2DecryptByPrivateKey(byte[] encBytes, byte[] privateKey)
        {
            if (encBytes == null || privateKey == null)
            {
                return "";
            }
            var sm2 = new SM2(null, privateKey, SM2Mode.C1C3C2);
            var plainBytes = sm2.Decrypt(encBytes);
            return Encoding.UTF8.GetString(plainBytes);
        }
        
        /// <summary>
        /// 使用SM2私钥进行签名
        /// </summary>
        /// <param name="text"></param>
        /// <param name="privateKey"></param>
        /// <returns></returns>
        public static string Sm2SignWithPrivateKey(string text, byte[] privateKey)
        {
            var sm2 = new SM2(Array.Empty<byte>(), privateKey, SM2Mode.C1C3C2);
            return Helper.Encode(sm2.Sign(Encoding.UTF8.GetBytes(text)));
        }
        
        /// <summary>
        /// 使用SM2私钥进行签名
        /// </summary>
        /// <param name="text"></param>
        /// <param name="privateKey"></param>
        /// <returns></returns>
        public static string Sm2SignWithPrivateKey(string text, string privateKey)
        {
            var sm2 = new SM2(Array.Empty<byte>(), Helper.Decode(privateKey), SM2Mode.C1C3C2);
            return Helper.Encode(sm2.Sign(Encoding.UTF8.GetBytes(text)));
        }
        
        /// <summary>
        /// 使用SM2公钥进行签名验证
        /// </summary>
        /// <param name="text"></param>
        /// <param name="sign"></param>
        /// <param name="publicKey"></param>
        /// <returns></returns>
        public static bool Sm2VerifyWithPublicKey(string text, string sign, byte[] publicKey)
        {
            try
            {
                var sm2 = new SM2(publicKey, Array.Empty<byte>(), SM2Mode.C1C3C2);
                return sm2.VerifySign(Encoding.UTF8.GetBytes(text), Helper.Decode(sign));
            }
            catch (Exception e)
            {
                Loger.Debug($"{e.Message}{Environment.NewLine}{e.StackTrace}");
                return false;
            }
        }
        
        /// <summary>
        /// 使用SM2公钥进行签名验证
        /// </summary>
        /// <param name="text"></param>
        /// <param name="sign"></param>
        /// <param name="publicKey"></param>
        /// <returns></returns>
        public static bool Sm2VerifyWithPublicKey(string text, string sign, string publicKey)
        {
            try
            {
                if(publicKey.Length == 128 && Regex.IsMatch(publicKey, "^[0-9a-f]+$", RegexOptions.IgnoreCase))
                {
                    publicKey = "04" + publicKey; //未压缩的公钥，补齐前缀：04-未压缩 02-已压缩 03-已压缩
                }
                var sm2 = new SM2(Helper.Decode(publicKey), Array.Empty<byte>(), SM2Mode.C1C3C2);
                return sm2.VerifySign(Encoding.UTF8.GetBytes(text), Helper.Decode(sign));
            }
            catch (Exception e)
            {
                Loger.Debug($"{e.Message}{Environment.NewLine}{e.StackTrace}");
                return false;
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="plainText"></param>
        /// <param name="secretKey"></param>
        /// <param name="isPadding"></param>
        /// <returns></returns>
        public static string SM4EncryptECBToHex(string plainText, string secretKey, bool isPadding = true)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return "";
            }
            var keyBytes = FitKey16(secretKey);
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encBytes = new SM4().EncryptECB(plainBytes, keyBytes, isPadding);
            return Convert.BytesToHexString(encBytes);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="plainText"></param>
        /// <param name="secretKey"></param>
        /// <param name="isPadding"></param>
        /// <returns></returns>
        public static string SM4EncryptECBToBase64(string plainText, string secretKey, bool isPadding = true)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return "";
            }
            var keyBytes = FitKey16(secretKey);
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encBytes = new SM4().EncryptECB(plainBytes, keyBytes, isPadding);
            return System.Convert.ToBase64String(encBytes);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="encText"></param>
        /// <param name="secretKey"></param>
        /// <param name="isPadding"></param>
        /// <returns></returns>
        public static string SM4DecryptECBFromHex(string encText, string secretKey, bool isPadding = true)
        {
            if (string.IsNullOrEmpty(encText))
            {
                return "";
            }
            else if (!Regex.IsMatch(encText, "^[0-9a-f]+$", RegexOptions.IgnoreCase))
            {
                return "";
            }
            try
            {
                var keyBytes = FitKey16(secretKey);
                var encBytes = Helper.Decode(encText);
                var plainBytes = new SM4().DecryptECB(encBytes, keyBytes, isPadding);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch (Exception e)
            {
                Loger.Debug($"{e.Message}{Environment.NewLine}{e.StackTrace}");
            }
            return "";
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="encText"></param>
        /// <param name="secretKey"></param>
        /// <param name="isPadding"></param>
        /// <returns></returns>
        public static string SM4DecryptECBFromBase64(string encText, string secretKey, bool isPadding = true)
        {
            if (string.IsNullOrEmpty(encText))
            {
                return "";
            }
            try
            {
                var keyBytes = FitKey16(secretKey);
                var encBytes = Helper.Decode(encText);
                var sm4 = new SM4();
                var plainBytes = sm4.DecryptECB(encBytes, keyBytes, isPadding);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch (Exception e)
            {
                Loger.Debug($"{e.Message}{Environment.NewLine}{e.StackTrace}");
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
        public static string SM4EncryptCBCToHex(string plainText, string secretKey, string iv, bool isPadding = true)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return "";
            }
            try
            {
                var ivBytes = FitKey16(iv);
                var keyBytes = FitKey16(secretKey);
                var plainBytes = Encoding.UTF8.GetBytes(plainText);
                var sm4 = new SM4();
                var encBytes = sm4.EncryptCBC(plainBytes, keyBytes, ivBytes, isPadding);
                return Convert.BytesToHexString(encBytes);
            }
            catch (Exception e)
            {
                Loger.Debug($"{e.Message}{Environment.NewLine}{e.StackTrace}");
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
        public static string SM4EncryptCBCToBase64(string plainText, string secretKey, string iv, bool isPadding = true)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return "";
            }
            try
            {
                var ivBytes = FitKey16(iv);
                var keyBytes = FitKey16(secretKey);
                var plainBytes = Encoding.UTF8.GetBytes(plainText);
                var sm4 = new SM4();
                var encBytes = sm4.EncryptCBC(plainBytes, keyBytes, ivBytes, isPadding);
                return System.Convert.ToBase64String(encBytes);
            }
            catch (Exception e)
            {
                Loger.Debug($"{e.Message}{Environment.NewLine}{e.StackTrace}");
            }
            return "";
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="encText"></param>
        /// <param name="secretKey"></param>
        /// <param name="iv"></param>
        /// <param name="isPadding"></param>
        /// <returns></returns>
        public static string SM4DecryptCBCFromHex(string encText, string secretKey, string iv, bool isPadding = true)
        {
            if (string.IsNullOrEmpty(encText))
            {
                return "";
            }
            try
            {
                var ivBytes = FitKey16(iv);
                var keyBytes = FitKey16(secretKey);
                var encBytes = Helper.Decode(encText);
                var sm4 = new SM4();
                var plainBytes = sm4.DecryptCBC(encBytes, keyBytes, ivBytes, isPadding);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch (Exception e)
            {
                Loger.Debug($"{e.Message}{Environment.NewLine}{e.StackTrace}");
            }
            return "";
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="encText"></param>
        /// <param name="secretKey"></param>
        /// <param name="iv"></param>
        /// <param name="isPadding"></param>
        /// <returns></returns>
        public static string SM4DecryptCBCFromBase64(string encText, string secretKey, string iv, bool isPadding = true)
        {
            if (string.IsNullOrEmpty(encText))
            {
                return "";
            }
            try
            {
                var ivBytes = FitKey16(iv);
                var keyBytes = FitKey16(secretKey);
                var encBytes = Helper.Decode(encText);
                var sm4 = new SM4();
                var plainBytes = sm4.DecryptCBC(encBytes, keyBytes, ivBytes, isPadding);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch (Exception e)
            {
                Loger.Debug($"{e.Message}{Environment.NewLine}{e.StackTrace}");
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
            var sm3 = new SM3();
            var buffer = Encoding.UTF8.GetBytes(str);
            sm3.BlockUpdate(buffer, 0, buffer.Length);
            buffer = sm3.DoFinal();
            return Convert.BytesToHexString(buffer);
        }


    }
}