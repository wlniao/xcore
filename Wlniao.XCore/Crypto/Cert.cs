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
            var cert = X509Certificate2.CreateFromPemFile(crtfile, keyfile);
            var data = cert.Export(X509ContentType.Pfx, password);
            //if (string.IsNullOrEmpty(password))
            //{
            //    data = cert.Export(X509ContentType.Pfx);
            //}
            //else
            //{
            //    data = cert.Export(X509ContentType.Pfx, password);
            //}
            if(!string.IsNullOrEmpty(outfile))
            {
                System.IO.File.WriteAllBytes(outfile, data);
            }
            if (!string.IsNullOrEmpty(password))
            {
                data = cert.Export(X509ContentType.Pfx);
            }
            return new X509Certificate2(data);
        }
        /// <summary>
        /// Private Key Convert Pkcs1->Pkcs8
        /// </summary>
        /// <param name="privateKey"></param>
        /// <returns></returns>
        public static string PrivateKeyPkcs1ToPkcs8(string privateKey)
        {
            privateKey = Pkcs1PrivateKeyFormat(privateKey);
            PemReader pr = new PemReader(new StringReader(privateKey));
            AsymmetricCipherKeyPair? kp = pr.ReadObject() as AsymmetricCipherKeyPair;
            StringWriter sw = new StringWriter();
            PemWriter pWrt = new PemWriter(sw);
            Pkcs8Generator pkcs8 = new Pkcs8Generator(kp.Private);
            pWrt.WriteObject(pkcs8);
            pWrt.Writer.Close();
            string result = sw.ToString();
            return result;
        }
        /// <summary>
        /// Private Key Convert Pkcs8->Pkcs1
        /// </summary>
        /// <param name="privateKey"></param>
        /// <returns></returns>
        public static string PrivateKeyPkcs8ToPkcs1(string privateKey)
        {
            privateKey = Pkcs8PrivateKeyFormat(privateKey);
            PemReader pr = new PemReader(new StringReader(privateKey));
            RsaPrivateCrtKeyParameters? kp = pr.ReadObject() as RsaPrivateCrtKeyParameters;
            var keyParameter = PrivateKeyFactory.CreateKey(PrivateKeyInfoFactory.CreatePrivateKeyInfo(kp));
            StringWriter sw = new StringWriter();
            PemWriter pWrt = new PemWriter(sw);
            pWrt.WriteObject(keyParameter);
            pWrt.Writer.Close();
            string result = sw.ToString();
            return result;
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

            List<string> res = new List<string>();
            res.Add("-----BEGIN RSA PRIVATE KEY-----");

            int pos = 0;

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
            List<string> res = new List<string>();
            res.Add("-----BEGIN PRIVATE KEY-----");

            int pos = 0;

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
            string header = String.Format("-----BEGIN {0}-----", headerPEM);
            string footer = String.Format("-----END {0}-----", headerPEM);
            int start = pemString.IndexOf(header, StringComparison.Ordinal) + header.Length;
            int end = pemString.IndexOf(footer, start, StringComparison.Ordinal) - start;
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
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            byte[] Buffer = GetBytesFromPemFile(privateKey, "RSA PRIVATE KEY");
            System.Security.Cryptography.RSAParameters rsaParam = rsa.ExportParameters(false);
            rsaParam.Modulus = Buffer;
            rsa.ImportParameters(rsaParam);
            return rsa;
        }
    }
}