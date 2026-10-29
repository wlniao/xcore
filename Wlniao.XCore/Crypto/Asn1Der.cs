/*==============================================================================
    文件名称：Asn1Der.cs
    适用环境：CoreCLR (.NET 8 / .NET 10, NativeAOT)
    功能描述：极简 ASN.1 DER 编解码，供 SM2 签名格式转换与 SM2 密钥容器解析使用
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
    实现依据（均按公开标准独立实现，未复制任何第三方库源码）：
      ITU-T X.690             ASN.1 编码规则，DER 的 tag/length/value 与 INTEGER 补码规则
      RFC 5915                Elliptic Curve Private Key Structure (ECPrivateKey)
      RFC 5208 / RFC 5958     PKCS#8 PrivateKeyInfo
      RFC 5480                SubjectPublicKeyInfo
===============================================================================*/

using System;
using System.Numerics;

namespace Wlniao.Crypto
{
    /// <summary>
    /// 顺序读取 DER 的 TLV（type-length-value）结构。
    /// 只实现 SM2 场景需要的类型，不做通用 ASN.1 支持：遇到未知结构直接抛异常，避免静默解析错误。
    /// </summary>
    internal sealed class DerReader
    {
        private const byte TagInteger = 0x02;
        private const byte TagBitString = 0x03;
        private const byte TagOctetString = 0x04;
        private const byte TagSequence = 0x30;

        private readonly byte[] _data;
        private readonly int _end;
        private int _pos;

        public DerReader(byte[] data) : this(data, 0, data == null ? 0 : data.Length)
        {
        }

        public DerReader(byte[] data, int offset, int length)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _pos = offset;
            _end = offset + length;
            if (_end > _data.Length)
            {
                throw new FormatException("DER 长度越界");
            }
        }

        /// <summary>是否还有未读数据</summary>
        public bool HasMore => _pos < _end;

        private void EnsureData(int count)
        {
            if (count < 0 || _pos + count > _end)
            {
                throw new FormatException("DER 数据提前结束");
            }
        }

        /// <summary>跳过当前元素（不解析内容）</summary>
        public void Skip()
        {
            EnsureData(1);
            _pos++;
            var len = ReadLength();
            EnsureData(len);
            _pos += len;
        }

        /// <summary>读取下一个 INTEGER，按非负大端整数解释</summary>
        public BigInteger ReadInteger()
        {
            return DerEncoder.ToBigInteger(ReadValue(TagInteger));
        }

        /// <summary>读取 OCTET STRING 的内容</summary>
        public byte[] ReadOctetString()
        {
            return ReadValue(TagOctetString);
        }

        /// <summary>读取 BIT STRING 并去掉未使用位数字节</summary>
        public byte[] ReadBitString()
        {
            var value = ReadValue(TagBitString);
            if (value.Length == 0)
            {
                throw new FormatException("BIT STRING 为空");
            }
            if (value[0] != 0)
            {
                // SM2 公钥是整字节对齐的，出现非零未使用位数说明数据异常
                throw new FormatException("不支持非字节对齐的 BIT STRING");
            }
            var result = new byte[value.Length - 1];
            Buffer.BlockCopy(value, 1, result, 0, result.Length);
            return result;
        }

        /// <summary>进入一个 SEQUENCE，返回只覆盖其内容的新读取器</summary>
        public DerReader ReadSequence()
        {
            EnsureData(1);
            var tag = _data[_pos];
            if (tag != TagSequence)
            {
                throw new FormatException($"期望 SEQUENCE (0x30)，实际 0x{tag:X2}");
            }
            _pos++;
            var len = ReadLength();
            EnsureData(len);
            var inner = new DerReader(_data, _pos, len);
            _pos += len;
            return inner;
        }

        /// <summary>读取指定 tag 的内容（不含 tag 与 length）</summary>
        public byte[] ReadValue(byte expectedTag)
        {
            EnsureData(1);
            var tag = _data[_pos];
            if (tag != expectedTag)
            {
                throw new FormatException($"期望 DER tag 0x{expectedTag:X2}，实际 0x{tag:X2}");
            }
            _pos++;
            var len = ReadLength();
            EnsureData(len);
            var value = new byte[len];
            Buffer.BlockCopy(_data, _pos, value, 0, len);
            _pos += len;
            return value;
        }

        /// <summary>读取 DER 的长度字段（支持短形式与最长 4 字节的长形式）</summary>
        public int ReadLength()
        {
            EnsureData(1);
            var first = _data[_pos++];
            if ((first & 0x80) == 0)
            {
                return first;
            }
            var count = first & 0x7F;
            if (count == 0 || count > 4)
            {
                // 0x80 是 BER 的不定长写法，DER 不允许；超过 4 字节在本场景不可能出现
                throw new FormatException("不支持的 DER 长度编码");
            }
            EnsureData(count);
            var len = 0;
            for (var i = 0; i < count; i++)
            {
                len = (len << 8) | _data[_pos++];
            }
            if (len < 0)
            {
                throw new FormatException("DER 长度溢出");
            }
            return len;
        }
    }

    /// <summary>
    /// DER 编码器。只覆盖 SM2 需要的 INTEGER / SEQUENCE / BIT STRING / OCTET STRING。
    /// </summary>
    internal static class DerEncoder
    {
        private const byte TagInteger = 0x02;
        private const byte TagBitString = 0x03;
        private const byte TagOctetString = 0x04;
        private const byte TagSequence = 0x30;

        /// <summary>把非负大端字节序列解释为整数</summary>
        public static BigInteger ToBigInteger(byte[] value)
        {
            if (value == null || value.Length == 0)
            {
                return BigInteger.Zero;
            }
            return new BigInteger(value, isUnsigned: true, isBigEndian: true);
        }

        /// <summary>把非负整数转成大端字节（不含符号位填充）</summary>
        public static byte[] ToBigEndian(BigInteger value)
        {
            if (value.Sign < 0)
            {
                throw new ArgumentException("仅支持非负整数", nameof(value));
            }
            if (value.IsZero)
            {
                return new byte[] { 0 };
            }
            return value.ToByteArray(isUnsigned: true, isBigEndian: true);
        }

        /// <summary>把非负整数编码为 32 字节定长大端（不足左补 0，超出报错）</summary>
        public static byte[] ToFixedBigEndian(BigInteger value, int length)
        {
            var raw = ToBigEndian(value);
            if (raw.Length > length)
            {
                throw new ArgumentException($"整数超出 {length} 字节", nameof(value));
            }
            var result = new byte[length];
            Buffer.BlockCopy(raw, 0, result, length - raw.Length, raw.Length);
            return result;
        }

        /// <summary>编码长度字段</summary>
        public static byte[] EncodeLength(int length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }
            if (length < 0x80)
            {
                return new[] { (byte)length };
            }
            if (length <= 0xFF)
            {
                return new byte[] { 0x81, (byte)length };
            }
            if (length <= 0xFFFF)
            {
                return new byte[] { 0x82, (byte)(length >> 8), (byte)length };
            }
            return new byte[] { 0x83, (byte)(length >> 16), (byte)(length >> 8), (byte)length };
        }

        /// <summary>组装一个 TLV</summary>
        public static byte[] Tlv(byte tag, byte[] value)
        {
            var lenBytes = EncodeLength(value.Length);
            var result = new byte[1 + lenBytes.Length + value.Length];
            result[0] = tag;
            Buffer.BlockCopy(lenBytes, 0, result, 1, lenBytes.Length);
            Buffer.BlockCopy(value, 0, result, 1 + lenBytes.Length, value.Length);
            return result;
        }

        /// <summary>编码 SEQUENCE</summary>
        public static byte[] Sequence(params byte[][] items)
        {
            var total = 0;
            foreach (var item in items)
            {
                total += item.Length;
            }
            var body = new byte[total];
            var offset = 0;
            foreach (var item in items)
            {
                Buffer.BlockCopy(item, 0, body, offset, item.Length);
                offset += item.Length;
            }
            return Tlv(TagSequence, body);
        }

        /// <summary>
        /// 把非负整数编码为 DER INTEGER。
        /// DER 要求整数为最短的二进制补码表示：去掉多余前导 0，若最高位为 1 则补一个 0x00 保持正数语义。
        /// </summary>
        public static byte[] Integer(BigInteger value)
        {
            if (value.Sign < 0)
            {
                throw new ArgumentException("仅支持非负整数", nameof(value));
            }
            var raw = ToBigEndian(value);
            var start = 0;
            while (start < raw.Length - 1 && raw[start] == 0)
            {
                start++;
            }
            var trimmed = new byte[raw.Length - start];
            Buffer.BlockCopy(raw, start, trimmed, 0, trimmed.Length);

            if ((trimmed[0] & 0x80) != 0)
            {
                var padded = new byte[trimmed.Length + 1];
                Buffer.BlockCopy(trimmed, 0, padded, 1, trimmed.Length);
                trimmed = padded;
            }
            return Tlv(TagInteger, trimmed);
        }

        /// <summary>编码 OCTET STRING</summary>
        public static byte[] OctetString(byte[] value)
        {
            return Tlv(TagOctetString, value);
        }

        /// <summary>编码 BIT STRING（仅支持字节对齐内容）</summary>
        public static byte[] BitString(byte[] value)
        {
            var body = new byte[value.Length + 1];
            body[0] = 0x00; // 未使用位数
            Buffer.BlockCopy(value, 0, body, 1, value.Length);
            return Tlv(TagBitString, body);
        }
    }
}
