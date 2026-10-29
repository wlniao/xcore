/*==============================================================================
    文件名称：SM2.cs
    适用环境：CoreCLR (.NET 8 / .NET 10, NativeAOT)
    功能描述：SM2椭圆曲线公钥密码算法（签名、验签、加解密）
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

===============================================================================
    算法实现见 Sm2Core.cs / Sm2Curve.cs，依据 GB/T 32918 系列标准。
    本文件只负责编码格式与密文排列，公开签名保持与改造前一致：
      · Sign()      返回 ASN.1 DER 编码的 (r,s)，不是 r||s 拼接
      · VerifySign()  接受 ASN.1 DER 编码的签名
      · SignWithRsAsn1() / VerifySignWithRsAsn1()  以 r||s（64 字节）为交换格式
===============================================================================*/

using System;
using System.Numerics;

namespace Wlniao.Crypto
{

    /// <summary>
    /// 密文排列方式
    /// </summary>
    public enum SM2Mode
    {
        /// <summary>
        /// C1 || C2 || C3
        /// </summary>
        C1C2C3,
        /// <summary>
        /// C1 || C3 || C2
        /// </summary>
        C1C3C2
    }

    /// <summary>
    /// SM2椭圆曲线公钥密码算法
    /// </summary>
    public class SM2
    {
        private readonly KeyTool key;
        private readonly SM2Mode mode;

        /// <summary>
        /// 用字节形式的公钥、私钥构造
        /// </summary>
        /// <param name="pubkey">公钥 04||X||Y</param>
        /// <param name="privkey">私钥</param>
        /// <param name="mode">密文排列方式</param>
        public SM2(byte[] pubkey, byte[] privkey, SM2Mode mode = SM2Mode.C1C2C3)
        {
            this.mode = mode;
            this.key = new KeyTool(pubkey, privkey);
        }

        /// <summary>
        /// 用字符串形式的公钥、私钥构造
        /// </summary>
        /// <param name="pubkey">公钥字符串</param>
        /// <param name="privkey">私钥字符串</param>
        /// <param name="type">密钥类型</param>
        /// <param name="mode">密文排列方式</param>
        public SM2(string pubkey, string privkey, KeyType type, SM2Mode mode = SM2Mode.C1C2C3)
        {
            this.mode = mode;
            this.key = new KeyTool(pubkey, privkey, type);
        }

        /// <summary>
        /// 解密
        /// </summary>
        /// <param name="data">密文</param>
        /// <returns>明文</returns>
        public byte[] Decrypt(byte[] data)
        {
            if (mode == SM2Mode.C1C3C2)
            {
                data = C132ToC123(data);
            }
            return Sm2Core.Decrypt(key.PrivateValue, data);
        }

        /// <summary>
        /// 解密
        /// </summary>
        /// <param name="data">密文（Hex 或 Base64）</param>
        /// <returns>明文</returns>
        public byte[] Decrypt(string data)
        {
            return Decrypt(Helper.Decode(data));
        }

        /// <summary>
        /// 加密
        /// </summary>
        /// <param name="data">明文</param>
        /// <returns>密文</returns>
        public byte[] Encrypt(byte[] data)
        {
            var cipher = Sm2Core.Encrypt(key.PublicPoint, data);
            return mode == SM2Mode.C1C3C2 ? C123ToC132(cipher) : cipher;
        }

        /// <summary>
        /// 加密
        /// </summary>
        /// <param name="data">明文</param>
        /// <returns>密文</returns>
        public byte[] Encrypt(string data)
        {
            return Encrypt(System.Text.Encoding.UTF8.GetBytes(data));
        }

        /// <summary>
        /// 签名，返回 ASN.1 DER 编码的 (r,s)
        /// </summary>
        /// <param name="msg">待签名数据</param>
        /// <param name="id">用户标识，为 null 时使用默认值 1234567812345678</param>
        /// <returns>DER 编码的签名</returns>
        public byte[] Sign(byte[] msg, byte[] id = null)
        {
            var (r, s) = Sm2Core.Sign(key.PrivateValue, key.PublicPoint, msg, id);
            return DerEncoder.Sequence(DerEncoder.Integer(r), DerEncoder.Integer(s));
        }

        /// <summary>
        /// 签名，返回 r||s 拼接的 64 字节。
        /// 方法名沿用历史命名（Asn1），但返回值实际是去掉 ASN.1 包装后的定长拼接格式，
        /// 与 BouncyCastle 的 SM3withSM2 默认输出格式对应。
        /// </summary>
        /// <param name="msg">待签名数据</param>
        /// <param name="id">用户标识</param>
        /// <returns>r||s 共 64 字节</returns>
        public byte[] SignWithRsAsn1(byte[] msg, byte[] id = null)
        {
            return RsAsn1ToPlainByteArray(Sign(msg, id));
        }

        /// <summary>
        /// 验签
        /// </summary>
        /// <param name="msg">原始数据</param>
        /// <param name="signature">DER 编码的签名</param>
        /// <param name="id">用户标识，为 null 时使用默认值 1234567812345678</param>
        /// <returns>是否通过</returns>
        public bool VerifySign(byte[] msg, byte[] signature, byte[] id = null)
        {
            if (signature == null || !TryDecodeDerSignature(signature, out var r, out var s))
            {
                // 签名格式非法一律判定为验签失败，不向上抛异常，避免把解析错误当成运行时故障
                return false;
            }
            return Sm2Core.Verify(key.PublicPoint, msg, r, s, id);
        }

        /// <summary>
        /// 验签
        /// </summary>
        /// <param name="msg">原始数据</param>
        /// <param name="signature">DER 编码的签名（Hex 或 Base64）</param>
        /// <param name="id">用户标识</param>
        /// <returns>是否通过</returns>
        public bool VerifySign(byte[] msg, string signature, byte[] id = null)
        {
            return VerifySign(msg, Helper.Decode(signature), id);
        }

        /// <summary>
        /// 验签，签名以 r||s（64 字节）格式传入。
        /// 历史实现此处误用私钥以签名模式初始化后调用验签，导致该重载实际不可用；
        /// 改造后按正确语义使用公钥验签，入参与返回值的约定保持不变。
        /// </summary>
        /// <param name="msg">原始数据</param>
        /// <param name="signature">r||s 共 64 字节</param>
        /// <param name="id">用户标识</param>
        /// <returns>是否通过</returns>
        public bool VerifySignWithRsAsn1(byte[] msg, byte[] signature, byte[] id = null)
        {
            byte[] der;
            try
            {
                der = RsPlainByteArrayToAsn1(signature);
            }
            catch (ArgumentException)
            {
                return false;
            }
            return VerifySign(msg, der, id);
        }

        /// <summary>
        /// 验签，签名以 r||s（64 字节）格式传入
        /// </summary>
        /// <param name="msg">原始数据</param>
        /// <param name="signature">r||s 共 64 字节（Hex 或 Base64）</param>
        /// <param name="id">用户标识</param>
        /// <returns>是否通过</returns>
        public bool VerifySignWithRsAsn1(byte[] msg, string signature, byte[] id = null)
        {
            return VerifySignWithRsAsn1(msg, Helper.Decode(signature), id);
        }

        /// <summary>r 或 s 的固定长度</summary>
        private const int RS_LEN = 32;

        private static bool TryDecodeDerSignature(byte[] der, out BigInteger r, out BigInteger s)
        {
            r = BigInteger.Zero;
            s = BigInteger.Zero;
            try
            {
                var seq = new DerReader(der).ReadSequence();
                r = seq.ReadInteger();
                s = seq.ReadInteger();
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        /**
         * SM3withSM2 签名得到的 r、s 在 ASN.1 中是变长 INTEGER，
         * 这个方法把 ASN.1 格式转换成直接拼接的 r||s（各 32 字节）。
         * @param rsDer rs in asn1 format
         * @return sign result in plain byte array
         */
        public static byte[] RsAsn1ToPlainByteArray(byte[] rsDer)
        {
            var seq = new DerReader(rsDer).ReadSequence();
            var r = seq.ReadInteger();
            var s = seq.ReadInteger();

            var result = new byte[RS_LEN * 2];
            Buffer.BlockCopy(DerEncoder.ToFixedBigEndian(r, RS_LEN), 0, result, 0, RS_LEN);
            Buffer.BlockCopy(DerEncoder.ToFixedBigEndian(s, RS_LEN), 0, result, RS_LEN, RS_LEN);
            return result;
        }

        /**
         * 将直接拼接的 r||s（各 32 字节）转换成 ASN.1 DER 格式。
         * @param sign in plain byte array
         * @return rs result in asn1 format
         */
        public static byte[] RsPlainByteArrayToAsn1(byte[] sign)
        {
            if (sign == null || sign.Length != RS_LEN * 2)
            {
                throw new ArgumentException("err rs. ", nameof(sign));
            }
            var r = new BigInteger(sign.AsSpan(0, RS_LEN), isUnsigned: true, isBigEndian: true);
            var s = new BigInteger(sign.AsSpan(RS_LEN, RS_LEN), isUnsigned: true, isBigEndian: true);
            return DerEncoder.Sequence(DerEncoder.Integer(r), DerEncoder.Integer(s));
        }

        /// <summary>
        /// C1C2C3 排列转换为 C1C3C2
        /// </summary>
        /// <param name="c1c2c3">C1||C2||C3</param>
        /// <returns>C1||C3||C2</returns>
        public static byte[] C123ToC132(byte[] c1c2c3)
        {
            if (c1c2c3 == null || c1c2c3.Length < Sm2Curve.UncompressedPointBytes + Sm2Core.DigestBytes)
            {
                throw new ArgumentException("密文长度不足", nameof(c1c2c3));
            }
            // SM2P256V1 上 C1 固定为 65 字节（04||X||Y），C3 固定为 32 字节（SM3 输出）
            var c1Len = Sm2Curve.UncompressedPointBytes;
            var c3Len = Sm2Core.DigestBytes;
            var result = new byte[c1c2c3.Length];
            Array.Copy(c1c2c3, 0, result, 0, c1Len);                                                    // C1
            Array.Copy(c1c2c3, c1c2c3.Length - c3Len, result, c1Len, c3Len);                            // C3
            Array.Copy(c1c2c3, c1Len, result, c1Len + c3Len, c1c2c3.Length - c1Len - c3Len);            // C2
            return result;
        }

        /// <summary>
        /// C1C3C2 排列转换为 C1C2C3
        /// </summary>
        /// <param name="c1c3c2">C1||C3||C2</param>
        /// <returns>C1||C2||C3</returns>
        public static byte[] C132ToC123(byte[] c1c3c2)
        {
            if (c1c3c2 == null || c1c3c2.Length < Sm2Curve.UncompressedPointBytes + Sm2Core.DigestBytes)
            {
                throw new ArgumentException("密文长度不足", nameof(c1c3c2));
            }
            var c1Len = Sm2Curve.UncompressedPointBytes;
            var c3Len = Sm2Core.DigestBytes;
            var result = new byte[c1c3c2.Length];
            Array.Copy(c1c3c2, 0, result, 0, c1Len);                                                    // C1
            Array.Copy(c1c3c2, c1Len + c3Len, result, c1Len, c1c3c2.Length - c1Len - c3Len);            // C2
            Array.Copy(c1c3c2, c1Len, result, c1c3c2.Length - c3Len, c3Len);                            // C3
            return result;
        }
    }
}
