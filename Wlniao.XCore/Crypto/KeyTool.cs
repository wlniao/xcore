/*==============================================================================
    文件名称：KeyTool.cs
    适用环境：CoreCLR (.NET 8 / .NET 10, NativeAOT)
    功能描述：SM2 密钥工具（密钥生成、Hex/Base64 编解码、PKCS#8 解析）
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
    实现依据：
      密钥生成        GB/T 32918.1-2016（d 取 [1,n-2]，Q = dG）
      PKCS#8 解析     RFC 5208 (PrivateKeyInfo) + RFC 5915 (ECPrivateKey)
      SPKI 解析       RFC 5480 (SubjectPublicKeyInfo)
    字节约定（与改造前的 BouncyCastle 保持一致，由 NTest 的 SM2InteropTests 校验）：
      公钥    04 || X(32) || Y(32)，共 65 字节
      私钥    d 的二进制补码大端表示，最高位为 1 时会带一个前导 0x00（即 33 字节）
===============================================================================*/

using System;
using System.Numerics;
using System.Text.RegularExpressions;

namespace Wlniao.Crypto
{
    /// <summary>
    /// 密钥类型
    /// </summary>
    public enum KeyType
    {
        /// <summary>
        /// 直接给出 Hex 或 Base64 编码的密钥
        /// </summary>
        Generate,
        /// <summary>
        /// Base64 编码的 PKCS#8 / SPKI 结构
        /// </summary>
        Pkcs8
    }

    /// <summary>
    /// 密钥工具
    /// </summary>
    public class KeyTool
    {
        private byte[] _pubkey;
        private byte[] _privkey;
        private Sm2Point? _publicPoint;
        private BigInteger? _privateValue;

        /// <summary>
        /// Hex格式公钥以04开头
        /// </summary>
        public byte[] PublicKey
        {
            get { return _pubkey; }
        }

        /// <summary>
        /// 私钥
        /// </summary>
        public byte[] PrivateKey
        {
            get { return _privkey; }
        }

        /// <summary>
        /// 公钥曲线点。
        /// 公钥字节为空但私钥存在时，由 Q = dG 现算，对应历史上"只给私钥签名"的调用形态。
        /// </summary>
        internal Sm2Point PublicPoint
        {
            get
            {
                if (_publicPoint == null)
                {
                    if (_pubkey != null && _pubkey.Length > 0)
                    {
                        _publicPoint = Sm2Curve.Decode(_pubkey);
                    }
                    else if (_privkey != null && _privkey.Length > 0)
                    {
                        _publicPoint = Sm2Curve.Multiply(PrivateValue, Sm2Curve.G);
                    }
                    else
                    {
                        throw new InvalidOperationException("既未提供公钥，也未提供可用于推导公钥的私钥");
                    }
                }
                return _publicPoint.Value;
            }
        }

        /// <summary>
        /// 私钥整数值（私钥为空时抛异常）
        /// </summary>
        internal BigInteger PrivateValue
        {
            get
            {
                if (_privateValue == null)
                {
                    if (_privkey == null)
                    {
                        throw new InvalidOperationException("未提供私钥");
                    }
                    _privateValue = new BigInteger(_privkey, isUnsigned: true, isBigEndian: true);
                }
                return _privateValue.Value;
            }
        }

        /// <summary>
        /// 生成一对新密钥
        /// </summary>
        public KeyTool()
        {
            GenerateKey(out _pubkey, out _privkey);
        }

        /// <summary>
        /// 用给定的公钥、私钥字节构造
        /// </summary>
        /// <param name="pubkey">公钥 04||X||Y</param>
        /// <param name="privkey">私钥，d 的大端表示</param>
        public KeyTool(byte[] pubkey, byte[] privkey)
        {
            _pubkey = pubkey;
            _privkey = privkey;
        }

        /// <summary>
        /// 用 Hex 或 Base64 字符串构造；type 为 Pkcs8 时按 PKCS#8 / SPKI 解析
        /// </summary>
        /// <param name="pubkey">公钥字符串</param>
        /// <param name="privkey">私钥字符串</param>
        /// <param name="type">密钥类型</param>
        public KeyTool(string pubkey, string privkey, KeyType type)
        {
            if (type == KeyType.Generate)
            {
                _pubkey = string.IsNullOrEmpty(pubkey) ? null : Decode(pubkey.ToLowerInvariant());
                _privkey = string.IsNullOrEmpty(privkey) ? null : Decode(privkey.ToLowerInvariant());
            }
            else if (type == KeyType.Pkcs8)
            {
                _pubkey = string.IsNullOrEmpty(pubkey) ? null : ParseSpki(Decode(pubkey));
                _privkey = string.IsNullOrEmpty(privkey) ? null : ParsePkcs8(Decode(privkey));
            }
        }

        /// <summary>
        /// 密钥对生成
        /// </summary>
        /// <param name="pubkey">输出：公钥 04||X||Y</param>
        /// <param name="privkey">输出：私钥</param>
        public void GenerateKey(out byte[] pubkey, out byte[] privkey)
        {
            var d = Sm2Curve.RandomScalar();
            var point = Sm2Curve.Multiply(d, Sm2Curve.G);
            pubkey = Sm2Curve.Encode(point);
            // 用带符号大端，最高位为 1 时补一个前导 0x00，与历史输出保持一致
            privkey = d.ToByteArray(isUnsigned: false, isBigEndian: true);
        }

        /// <summary>
        /// 输出Hex格式的密钥对
        /// </summary>
        /// <param name="pubkey">输出：公钥 Hex</param>
        /// <param name="privkey">输出：私钥 Hex</param>
        public void OutHex(out string pubkey, out string privkey)
        {
            pubkey = _pubkey == null ? null : System.Convert.ToHexString(_pubkey).ToLowerInvariant();
            privkey = _privkey == null ? null : System.Convert.ToHexString(_privkey).ToLowerInvariant();
        }

        /// <summary>
        /// 从 SPKI(SubjectPublicKeyInfo) 结构中取出公钥点
        /// </summary>
        /// <remarks>
        /// SubjectPublicKeyInfo ::= SEQUENCE { algorithm AlgorithmIdentifier, subjectPublicKey BIT STRING }
        /// SM2 场景下 subjectPublicKey 就是 04||X||Y。
        /// </remarks>
        public static byte[] ParseSpki(byte[] der)
        {
            if (der == null || der.Length == 0)
            {
                throw new ArgumentException("公钥数据为空", nameof(der));
            }
            var info = new DerReader(der).ReadSequence();
            info.Skip();                    // AlgorithmIdentifier，本场景无需校验
            var point = info.ReadBitString();
            if (point.Length != Sm2Curve.UncompressedPointBytes || point[0] != 0x04)
            {
                throw new FormatException("SPKI 中的公钥不是 65 字节未压缩点");
            }
            return point;
        }

        /// <summary>
        /// 从 PKCS#8(PrivateKeyInfo) 结构里取出私钥字节
        /// </summary>
        /// <remarks>
        /// PrivateKeyInfo ::= SEQUENCE { version INTEGER, privateKeyAlgorithm AlgorithmIdentifier,
        ///                               privateKey OCTET STRING, ... }
        /// 其中 privateKey 内容为 RFC 5915 的 ECPrivateKey：
        /// ECPrivateKey ::= SEQUENCE { version INTEGER, privateKey OCTET STRING, ... }
        /// </remarks>
        public static byte[] ParsePkcs8(byte[] der)
        {
            if (der == null || der.Length == 0)
            {
                throw new ArgumentException("私钥数据为空", nameof(der));
            }
            var info = new DerReader(der).ReadSequence();
            info.ReadInteger();             // version，通常为 0
            info.Skip();                    // AlgorithmIdentifier
            var inner = info.ReadOctetString();

            var ec = new DerReader(inner).ReadSequence();
            ec.ReadInteger();               // ECPrivateKey.version，通常为 1
            return ec.ReadOctetString();    // 私钥 d 的大端字节
        }

        private static byte[] Decode(string key)
        {
            if (Regex.IsMatch(key, "^[0-9a-f]+$", RegexOptions.IgnoreCase))
            {
                // 历史实现允许奇数长度的 Hex，这里补齐后再解析
                if (key.Length % 2 == 1)
                {
                    key = "0" + key;
                }
                return System.Convert.FromHexString(key);
            }
            return System.Convert.FromBase64String(key);
        }
    }
}
