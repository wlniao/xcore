using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Wlniao.Crypto
{
    /// <summary>
    /// 证书服务工具
    /// </summary>
    public class Cert
    {
        /// <summary>
        /// CRT证书转PFX证书
        /// </summary>
        /// <param name="crtfile"></param>
        /// <param name="keyfile"></param>
        /// <param name="outfile"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static X509Certificate2 CrtToPfx(string crtfile, string keyfile, string outfile = null, string password = null)
        {
            X509Certificate2 cert = null;
            try
            {
                cert = X509Certificate2.CreateFromPemFile(crtfile, keyfile);
                var data = cert.Export(X509ContentType.Pfx, password);
                if(!string.IsNullOrEmpty(outfile))
                {
                    System.IO.File.WriteAllBytes(outfile, data);
                }
                if (!string.IsNullOrEmpty(password))
                {
                    data = cert.Export(X509ContentType.Pfx);
                }
#if NET10_0_OR_GREATER
                // .NET 10+ 新写法（无警告、AOT 兼容）
                return X509CertificateLoader.LoadCertificate(data);
#else
                // .NET 8 及更早旧写法
                return new X509Certificate2(data);
#endif
            }
            finally
            {
                if (cert != null)
                {
                    cert.Dispose();
                }
            }
        }
        /// <summary>
        /// Private Key Convert Pkcs1->Pkcs8
        /// </summary>
        /// <param name="privateKey"></param>
        /// <returns></returns>
        public static string PrivateKeyPkcs1ToPkcs8(string privateKey)
        {
            privateKey = Pkcs1PrivateKeyFormat(privateKey);
            using var rsa = RSA.Create();
            rsa.ImportFromPem(privateKey);
            return ToPem("PRIVATE KEY", rsa.ExportPkcs8PrivateKey());
        }
        /// <summary>
        /// Private Key Convert Pkcs8->Pkcs1
        /// </summary>
        /// <param name="privateKey"></param>
        /// <returns></returns>
        public static string PrivateKeyPkcs8ToPkcs1(string privateKey)
        {
            privateKey = Pkcs8PrivateKeyFormat(privateKey);
            using var rsa = RSA.Create();
            rsa.ImportFromPem(privateKey);
            return ToPem("RSA PRIVATE KEY", rsa.ExportRSAPrivateKey());
        }

        /// <summary>
        /// 把 DER 字节封装成 PEM 文本：Base64 每 64 字符换行，头尾带 BEGIN/END 标记。
        /// </summary>
        /// <param name="label">标记名，如 PRIVATE KEY / RSA PRIVATE KEY</param>
        /// <param name="der">DER 字节</param>
        /// <returns>PEM 文本</returns>
        private static string ToPem(string label, byte[] der)
        {
            var base64 = System.Convert.ToBase64String(der);
            var builder = new StringBuilder(base64.Length + base64.Length / 64 * 2 + 64);
            builder.Append("-----BEGIN ").Append(label).Append("-----").Append(Environment.NewLine);
            for (var i = 0; i < base64.Length; i += 64)
            {
                builder.Append(base64, i, Math.Min(64, base64.Length - i)).Append(Environment.NewLine);
            }
            builder.Append("-----END ").Append(label).Append("-----").Append(Environment.NewLine);
            return builder.ToString();
        }
        /// <summary>
        /// Format Pkcs1 format private key
        /// Author:Zhiqiang Li
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string Pkcs1PrivateKeyFormat(string str)
        {
            if (str.StartsWith("-----BEGIN RSA PRIVATE KEY-----"))
            {
                return str;
            }

            var res = new List<string> { "-----BEGIN RSA PRIVATE KEY-----" };

            var pos = 0;

            while (pos < str.Length)
            {
                var count = str.Length - pos < 64 ? str.Length - pos : 64;
                res.Add(str.Substring(pos, count));
                pos += count;
            }

            res.Add("-----END RSA PRIVATE KEY-----");
            var resStr = string.Join(Environment.NewLine, res);
            return resStr;
        }

        /// <summary>
        /// Remove the Pkcs1 format private key format
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string Pkcs1PrivateKeyFormatRemove(string str)
        {
            if (!str.StartsWith("-----BEGIN RSA PRIVATE KEY-----"))
            {
                return str;
            }
            return str.Replace("-----BEGIN RSA PRIVATE KEY-----", "").Replace("-----END RSA PRIVATE KEY-----", "")
                .Replace(Environment.NewLine, "");
        }

        /// <summary>
        /// Format Pkcs8 format private key
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string Pkcs8PrivateKeyFormat(string str)
        {
            if (str.StartsWith("-----BEGIN PRIVATE KEY-----"))
            {
                return str;
            }
            var res = new List<string> { "-----BEGIN PRIVATE KEY-----" };

            var pos = 0;

            while (pos < str.Length)
            {
                var count = str.Length - pos < 64 ? str.Length - pos : 64;
                res.Add(str.Substring(pos, count));
                pos += count;
            }

            res.Add("-----END PRIVATE KEY-----");
            var resStr = string.Join(Environment.NewLine, res);
            return resStr;
        }


        /// <summary>
        /// Remove the Pkcs8 format private key format
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string Pkcs8PrivateKeyFormatRemove(string str)
        {
            if (!str.StartsWith("-----BEGIN PRIVATE KEY-----"))
            {
                return str;
            }
            return str.Replace("-----BEGIN PRIVATE KEY-----", "").Replace("-----END PRIVATE KEY-----", "")
                .Replace(Environment.NewLine, "");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pemString"></param>
        /// <param name="headerPEM"></param>
        /// <returns></returns>
        public static byte[] GetBytesFromPemFile(string pemString, string headerPEM)
        {
            var header = $"-----BEGIN {headerPEM}-----";
            var footer = $"-----END {headerPEM}-----";
            var start = pemString.IndexOf(header, StringComparison.Ordinal) + header.Length;
            var end = pemString.IndexOf(footer, start, StringComparison.Ordinal) - start;
            if (start < 0 || end < 0)
            {
                return null;
            }
            return System.Convert.FromBase64String(pemString.Substring(start, end));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="privateKey"></param>
        /// <returns></returns>
        public static RSACryptoServiceProvider GetRSACryptoServiceProvider(string privateKey)
        {
            if (!privateKey.Contains("RSA PRIVATE KEY"))
            {
                privateKey = PrivateKeyPkcs8ToPkcs1(privateKey);
            }
            var rsa = new RSACryptoServiceProvider();
            try
            {
                var buffer = GetBytesFromPemFile(privateKey, "RSA PRIVATE KEY");
                var rsaParam = rsa.ExportParameters(false);
                rsaParam.Modulus = buffer;
                rsa.ImportParameters(rsaParam);
                return rsa;
            }
            catch
            {
                rsa.Dispose();
                throw;
            }
        }
    }
}