using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Org.BouncyCastle.Asn1.GM;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.Encoders;

namespace Wlniao.Crypto
{

    /// <summary>
    /// 密文排列方式
    /// </summary>
    public enum SM2Mode
    {
        /// <summary>
        /// 
        /// </summary>
        C1C2C3,
        /// <summary>
        /// 
        /// </summary>
        C1C3C2
    }

    /// <summary>
    /// SM2椭圆曲线公钥密码算法
    /// </summary>
    public class SM2
    {
        KeyTool key;
        SM2Mode mode;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pubkey"></param>
        /// <param name="privkey"></param>
        /// <param name="mode"></param>
        public SM2(byte[] pubkey, byte[] privkey, SM2Mode mode)
        {
            this.mode = mode;
            this.key = new KeyTool(pubkey, privkey);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pubkey"></param>
        /// <param name="privkey"></param>
        /// <param name="type"></param>
        /// <param name="mode"></param>
        public SM2(string pubkey, string privkey, KeyType type, SM2Mode mode = SM2Mode.C1C2C3)
        {
            var _pubkey = Decode(pubkey);
            var _privkey = Decode(privkey);
            if (type == KeyType.Pkcs8)
            {
                _pubkey = ((ECPublicKeyParameters)PublicKeyFactory.CreateKey(System.Convert.FromBase64String(pubkey))).Q.GetEncoded();
                _privkey = ((ECPrivateKeyParameters)PrivateKeyFactory.CreateKey(System.Convert.FromBase64String(privkey))).D.ToByteArray();
            }
            this.mode = mode;
            this.key = new KeyTool(_pubkey, _privkey);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public byte[] Decrypt(byte[] data)
        {
            if (mode == SM2Mode.C1C3C2)
            {
                data = C132ToC123(data);
            }
            var sm2 = new SM2Engine(new SM3());
            sm2.Init(false, key.PrivateKeyParameters);
            return sm2.ProcessBlock(data, 0, data.Length);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public byte[] Decrypt(string data)
        {
            return Decrypt(Decode(data));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public byte[] Encrypt(byte[] data)
        {
            var sm2 = new SM2Engine(new SM3());
            sm2.Init(true, new ParametersWithRandom(key.PublicKeyParameters));
            data = sm2.ProcessBlock(data, 0, data.Length);
            if (mode == SM2Mode.C1C3C2) data = C123ToC132(data);
            return data;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public byte[] Encrypt(string data)
        {
            return Encrypt(System.Text.UTF8Encoding.UTF8.GetBytes(data));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public byte[] Sign(byte[] msg, byte[] id = null)
        {
            var sm2 = new SM2Signer(new SM3());
            ICipherParameters cp;
            if (id != null) cp = new ParametersWithID(new ParametersWithRandom(key.PrivateKeyParameters), id);
            else cp = new ParametersWithRandom(key.PrivateKeyParameters);
            sm2.Init(true, cp);
            sm2.BlockUpdate(msg, 0, msg.Length);
            return sm2.GenerateSignature();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="signature"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool VerifySign(byte[] msg, byte[] signature, byte[] id = null)
        {
            var sm2 = new SM2Signer(new SM3());
            ICipherParameters cp;
            if (id != null)
            {
                cp = new ParametersWithID(key.PublicKeyParameters, id);
            }
            else
            {
                cp = key.PublicKeyParameters;
            }
            sm2.Init(false, cp);
            sm2.BlockUpdate(msg, 0, msg.Length);
            return sm2.VerifySignature(signature);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="signature"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool VerifySign(byte[] msg, string signature, byte[] id = null)
        {
            return VerifySign(msg, Hex.DecodeStrict(signature), id);
        }


        static byte[] C123ToC132(byte[] c1c2c3)
        {
            var gn = GMNamedCurves.GetByName("SM2P256V1");
            int c1Len = (gn.Curve.FieldSize + 7) / 8 * 2 + 1; //sm2p256v1的这个固定65。可看GMNamedCurves、ECCurve代码。
            int c3Len = 32; //new SM3Digest().getDigestSize();
            byte[] result = new byte[c1c2c3.Length];
            Array.Copy(c1c2c3, 0, result, 0, c1Len); //c1
            Array.Copy(c1c2c3, c1c2c3.Length - c3Len, result, c1Len, c3Len); //c3
            Array.Copy(c1c2c3, c1Len, result, c1Len + c3Len, c1c2c3.Length - c1Len - c3Len); //c2
            return result;
        }
        static byte[] C132ToC123(byte[] c1c3c2)
        {
            var gn = GMNamedCurves.GetByName("SM2P256V1");
            int c1Len = (gn.Curve.FieldSize + 7) / 8 * 2 + 1;
            int c3Len = 32; //new SM3Digest().getDigestSize();
            byte[] result = new byte[c1c3c2.Length];
            Array.Copy(c1c3c2, 0, result, 0, c1Len); //c1: 0->65
            Array.Copy(c1c3c2, c1Len + c3Len, result, c1Len, c1c3c2.Length - c1Len - c3Len); //c2
            Array.Copy(c1c3c2, c1Len, result, c1c3c2.Length - c3Len, c3Len); //c3
            return result;
        }
        static byte[] Decode(string key)
        {
            return Regex.IsMatch(key, "^[0-9a-f]+$", RegexOptions.IgnoreCase) ? Hex.Decode(key) : System.Convert.FromBase64String(key);
        }

    }
}