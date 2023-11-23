using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Org.BouncyCastle.Asn1.GM;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Security;

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
        public SM2(byte[] pubkey, byte[] privkey, SM2Mode mode = SM2Mode.C1C2C3)
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
            var _pubkey = Helper.Decode(pubkey);
            var _privkey = Helper.Decode(privkey);
            if (type == KeyType.Pkcs8)
            {
                _pubkey = _pubkey == null ? null : ((ECPublicKeyParameters)PublicKeyFactory.CreateKey(_pubkey)).Q.GetEncoded();
                _privkey = _privkey == null ? null : ((ECPrivateKeyParameters)PrivateKeyFactory.CreateKey(_privkey)).D.ToByteArray();
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
            var sm2 = new SM2Engine();
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
            return Decrypt(Helper.Decode(data));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public byte[] Encrypt(byte[] data)
        {
            var sm2 = new SM2Engine();
            sm2.Init(true, new ParametersWithRandom(key.PublicKeyParameters));
            data = sm2.ProcessBlock(data, 0, data.Length);
            if (mode == SM2Mode.C1C3C2)
            {
                data = C123ToC132(data);
            }
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
            var sm2 = new SM2Signer();
            ICipherParameters cp;
            if (id != null)
            {
                cp = new ParametersWithID(new ParametersWithRandom(key.PrivateKeyParameters), id);
            }
            else
            {
                cp = new ParametersWithRandom(key.PrivateKeyParameters);
            }
            sm2.Init(true, cp);
            sm2.BlockUpdate(msg, 0, msg.Length);
            return sm2.GenerateSignature();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public byte[] SignWithRsAsn1(byte[] msg, byte[] id = null)
        {
            var signer = SignerUtilities.GetSigner("SM3withSM2");
            ICipherParameters cp;
            if (id != null)
            {
                cp = new ParametersWithID(new ParametersWithRandom(key.PrivateKeyParameters), id);
            }
            else
            {
                cp = new ParametersWithRandom(key.PrivateKeyParameters);
            }
            signer.Init(true, cp);
            signer.BlockUpdate(msg, 0, msg.Length);
            return RsAsn1ToPlainByteArray(signer.GenerateSignature());
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
            var sm2 = new SM2Signer();
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
            return VerifySign(msg, Helper.Decode(signature), id);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="signature"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool VerifySignWithRsAsn1(byte[] msg, byte[] signature, byte[] id = null)
        {
            var signer = SignerUtilities.GetSigner("SM3withSM2");
            ICipherParameters cp;
            if (id != null)
            {
                cp = new ParametersWithID(new ParametersWithRandom(key.PrivateKeyParameters), id);
            }
            else
            {
                cp = new ParametersWithRandom(key.PrivateKeyParameters);
            }
            signer.Init(true, cp);
            signer.BlockUpdate(msg, 0, msg.Length);
            return signer.VerifySignature(RsPlainByteArrayToAsn1(signature));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="signature"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool VerifySignWithRsAsn1(byte[] msg, string signature, byte[] id = null)
        {
            return VerifySignWithRsAsn1(msg, Helper.Decode(signature), id);
        }



        private const int RS_LEN = 32;

        private static byte[] BigIntToFixexLengthBytes(Org.BouncyCastle.Math.BigInteger rOrS)
        {
            // for sm2p256v1, n is 00fffffffeffffffffffffffffffffffff7203df6b21c6052b53bbf40939d54123,
            // r and s are the result of mod n, so they should be less than n and have length<=32
            byte[] rs = rOrS.ToByteArray();
            if (rs.Length == RS_LEN) return rs;
            else if (rs.Length == RS_LEN + 1 && rs[0] == 0) return Org.BouncyCastle.Utilities.Arrays.CopyOfRange(rs, 1, RS_LEN + 1);
            else if (rs.Length < RS_LEN)
            {
                byte[] result = new byte[RS_LEN];
                Org.BouncyCastle.Utilities.Arrays.Fill(result, (byte)0);
                Buffer.BlockCopy(rs, 0, result, RS_LEN - rs.Length, rs.Length);
                return result;
            }
            else
            {
                throw new ArgumentException("err rs: " + Org.BouncyCastle.Utilities.Encoders.Hex.ToHexString(rs));
            }
        }

        /**
         * BC的SM3withSM2签名得到的结果的rs是asn1格式的，这个方法转化成直接拼接r||s
         * @param rsDer rs in asn1 format
         * @return sign result in plain byte array
         */
        public static byte[] RsAsn1ToPlainByteArray(byte[] rsDer)
        {
            var seq = Org.BouncyCastle.Asn1.Asn1Sequence.GetInstance(rsDer);
            byte[] r = BigIntToFixexLengthBytes(Org.BouncyCastle.Asn1.DerInteger.GetInstance(seq[0]).Value);
            byte[] s = BigIntToFixexLengthBytes(Org.BouncyCastle.Asn1.DerInteger.GetInstance(seq[1]).Value);
            byte[] result = new byte[RS_LEN * 2];
            Buffer.BlockCopy(r, 0, result, 0, r.Length);
            Buffer.BlockCopy(s, 0, result, RS_LEN, s.Length);
            return result;
        }

        /**
         * BC的SM3withSM2验签需要的rs是asn1格式的，这个方法将直接拼接r||s的字节数组转化成asn1格式
         * @param sign in plain byte array
         * @return rs result in asn1 format
         */
        public static byte[] RsPlainByteArrayToAsn1(byte[] sign)
        {
            if (sign.Length != RS_LEN * 2) throw new ArgumentException("err rs. ");
            var r = new Org.BouncyCastle.Math.BigInteger(1, Org.BouncyCastle.Utilities.Arrays.CopyOfRange(sign, 0, RS_LEN));
            var s = new Org.BouncyCastle.Math.BigInteger(1, Org.BouncyCastle.Utilities.Arrays.CopyOfRange(sign, RS_LEN, RS_LEN * 2));
            var v = new Org.BouncyCastle.Asn1.Asn1EncodableVector();
            v.Add(new Org.BouncyCastle.Asn1.DerInteger(r));
            v.Add(new Org.BouncyCastle.Asn1.DerInteger(s));
            try
            {
                return new Org.BouncyCastle.Asn1.DerSequence(v).GetEncoded("DER");
            }
            catch
            {
                return null;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="c1c2c3"></param>
        /// <returns></returns>
        public static byte[] C123ToC132(byte[] c1c2c3)
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="c1c3c2"></param>
        /// <returns></returns>
        public static byte[] C132ToC123(byte[] c1c3c2)
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

    }
}