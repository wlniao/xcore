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
using Org.BouncyCastle.Security;
using System;
using System.IO;
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
                //RSA 格式私钥处理
                if (!privateKey.Contains("----"))
                {
                    privateKey = "-----BEGIN RSA PRIVATE KEY-----" + privateKey + "-----END RSA PRIVATE KEY-----";
                }
                var pemReader = new Org.BouncyCastle.OpenSsl.PemReader(new StringReader(privateKey));
                var keyParameter = ((Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair)pemReader.ReadObject()).Private;

                var cipher = CipherUtilities.GetCipher("SHA1withRSA");
                cipher.Init(false, keyParameter);
                return encoding.GetString(cipher.DoFinal(sData));
            }
            else
            {
                privateKey = privateKey.Replace("-----BEGIN PRIVATE KEY-----", "").Replace("-----END PRIVATE KEY-----", "").Replace("\n", "").Replace("\r", "").Trim();
                var privateBytes = System.Convert.FromBase64String(privateKey);
                var keyParameter = PrivateKeyFactory.CreateKey(privateBytes);

                var cipher = CipherUtilities.GetCipher("RSA/ECB/NoPadding");
                cipher.Init(false, keyParameter);
                return encoding.GetString(cipher.DoFinal(sData));
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
            var pubKey = PublicKeyFactory.CreateKey(certBytes);
            var cipher = CipherUtilities.GetCipher("RSA/ECB/NoPadding");
            cipher.Init(true, pubKey);
            var sData = encoding.GetBytes(data);
            return System.Convert.ToBase64String(cipher.DoFinal(sData));
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
            if (privateKey.Contains("BEGIN RSA PRIVATE KEY") && !privateKey.Contains("BEGIN PRIVATE KEY"))
            {
                //RSA 格式私钥处理
                if (!privateKey.Contains("----"))
                {
                    privateKey = "-----BEGIN RSA PRIVATE KEY-----" + privateKey + "-----END RSA PRIVATE KEY-----";
                }
                var pemReader = new Org.BouncyCastle.OpenSsl.PemReader(new StringReader(privateKey));
                var keyParameter = ((Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair)pemReader.ReadObject()).Private;

                var signer = SignerUtilities.GetSigner("SHA1withRSA");
                signer.Init(true, keyParameter);
                signer.BlockUpdate(sData, 0, sData.Length);
                sData = signer.GenerateSignature();
            }
            else
            {
                privateKey = privateKey.Replace("-----BEGIN PRIVATE KEY-----", "").Replace("-----END PRIVATE KEY-----", "").Replace("\n", "").Replace("\r", "").Trim();
                var privateBytes = System.Convert.FromBase64String(privateKey);
                var keyParameter = PrivateKeyFactory.CreateKey(privateBytes);

                var signer = SignerUtilities.GetSigner("SHA1withRSA");
                signer.Init(true, keyParameter);
                signer.BlockUpdate(sData, 0, sData.Length);
                sData = signer.GenerateSignature();
            }

            return System.Convert.ToBase64String(sData);
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
            var pubKey = PublicKeyFactory.CreateKey(certBytes);
            var signer = SignerUtilities.GetSigner("SHA1withRSA");
            signer.Init(false, pubKey);
            signer.BlockUpdate(sData, 0, sData.Length);
            var expectedSig = System.Convert.FromBase64String(sign);
            return signer.VerifySignature(expectedSig);
        }

        /// <summary>
        /// 获取SHA1值
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string Sha1(string str)
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
        /// 获取SHA256值
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string Sha256(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return "";
            }
            var dataToHash = Encoding.ASCII.GetBytes(str); //将str转换成byte[]
            var dataHashed = SHA256.Create().ComputeHash(dataToHash);
            return BitConverter.ToString(dataHashed).Replace("-", "");//将运算结果转换成string
        }
        /// <summary>
        /// 获取SHA512值
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string Sha512(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return "";
            }
            var dataToHash = Encoding.ASCII.GetBytes(str); //将str转换成byte[]
            var dataHashed = SHA512.Create().ComputeHash(dataToHash);
            return BitConverter.ToString(dataHashed).Replace("-", "");//将运算结果转换成string
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
                var aes = Aes.Create();
                var key = new char[32];
                for (var i = 0; i < key.Length && i < sKey.Length; i++)
                {
                    key[i] = sKey[i];
                }

                aes.Key = Encoding.ASCII.GetBytes(key);
                var iv = new char[16];
                for (var i = 0; i < iv.Length && i < IV.Length; i++)
                {
                    iv[i] = IV[i];
                }

                aes.IV = Encoding.ASCII.GetBytes(iv);
                aes.Padding = PaddingMode.PKCS7;
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
        /// <returns></returns>
        public static string SM2EncryptByPublicKey(string plainText, string publicKey)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return "";
            }
            var sm2 = new SM2(Helper.Decode(publicKey), null, SM2Mode.C1C3C2);
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encBytes = sm2.Encrypt(plainBytes);
            return Convert.BytesToHexString(encBytes);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="plainBytes"></param>
        /// <param name="publicKey"></param>
        /// <returns></returns>
        public static string SM2EncryptByPublicKey(byte[] plainBytes, byte[] publicKey)
        {
            if (plainBytes == null || publicKey == null)
            {
                return "";
            }
            var sm2 = new SM2(publicKey, null, SM2Mode.C1C3C2);
            var encBytes = sm2.Encrypt(plainBytes);
            return Convert.BytesToHexString(encBytes);
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
            var keyBytes = Encoding.UTF8.GetBytes((secretKey + "0000000000000000")[..16]);
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
            var keyBytes = Encoding.UTF8.GetBytes((secretKey + "0000000000000000")[..16]);
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
            try
            {
                var keyBytes = Encoding.UTF8.GetBytes((secretKey + "0000000000000000")[..16]);
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
                var keyBytes = Encoding.UTF8.GetBytes((secretKey + "0000000000000000")[..16]);
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
                var ivBytes = Encoding.UTF8.GetBytes((iv + "0000000000000000")[..16]);
                var keyBytes = Encoding.UTF8.GetBytes((secretKey + "0000000000000000")[..16]);
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
                var ivBytes = Encoding.UTF8.GetBytes((iv + "0000000000000000")[..16]);
                var keyBytes = Encoding.UTF8.GetBytes((secretKey + "0000000000000000")[..16]);
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
                var ivBytes = Encoding.UTF8.GetBytes((iv + "0000000000000000")[..16]);
                var keyBytes = Encoding.UTF8.GetBytes((secretKey + "0000000000000000")[..16]);
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
                var ivBytes = Encoding.UTF8.GetBytes((iv + "0000000000000000")[..16]);
                var keyBytes = Encoding.UTF8.GetBytes((secretKey + "0000000000000000")[..16]);
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