/*==============================================================================
    文件名称：Sm2Core.cs
    适用环境：CoreCLR (.NET 8 / .NET 10, NativeAOT)
    功能描述：SM2 数字签名、公钥加解密的纯托管实现（零外部依赖、零反射）
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
    实现依据（按公开国家标准独立实现，未复制任何第三方库源码）：
      GB/T 32918.2-2016  SM2 数字签名算法
      GB/T 32918.4-2016  SM2 公钥加密算法
      GB/T 32905-2016     SM3 密码杂凑算法（本项目 Crypto/SM3.cs 自有实现）

    关键约定（必须与改造前的 BouncyCastle 行为保持一致，见 NTest 的 SM2InteropTests）：
      · 默认用户标识 ID = "1234567812345678"（ASCII），即 GB/T 32918.2 附录 A.2 的默认值
      · 密文排列 C1(65) || C2(len) || C3(32)
      · 签名中间量为 r、s 各 32 字节，最终由调用方转 ASN.1 DER
===============================================================================*/

using System;
using System.Numerics;

namespace Wlniao.Crypto
{
    /// <summary>
    /// SM2 密文校验失败或格式非法时抛出。
    /// 名称与 BouncyCastle 的同名异常一致，便于从旧实现平滑迁移。
    /// </summary>
    public class InvalidCipherTextException : Exception
    {
        /// <summary>默认提示</summary>
        public InvalidCipherTextException()
            : base("SM2 密文校验失败：C3 与计算结果不一致，通常是私钥不匹配或密文被篡改。")
        {
        }

        /// <summary>指定提示</summary>
        public InvalidCipherTextException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// SM2 核心算法：签名、验签、加密、解密。
    /// 只做数学运算，不涉及编码格式；DER 与 C1C2C3/C1C3C2 排列由上层 <see cref="SM2"/> 处理。
    /// </summary>
    internal static class Sm2Core
    {
        /// <summary>摘要长度（SM3 输出 32 字节）</summary>
        public const int DigestBytes = 32;

        /// <summary>GB/T 32918.2 默认用户标识</summary>
        public static readonly byte[] DefaultUserId = System.Text.Encoding.ASCII.GetBytes("1234567812345678");

        /// <summary>对参数顺序敏感的分段 SM3 求值</summary>
        public static byte[] Sm3Hash(params byte[][] parts)
        {
            var digest = new SM3();
            foreach (var part in parts)
            {
                if (part != null && part.Length > 0)
                {
                    digest.BlockUpdate(part, 0, part.Length);
                }
            }
            return digest.DoFinal();
        }

        /// <summary>
        /// 计算 Z 值：SM3(ENTL || ID || a || b || Gx || Gy || Px || Py)
        /// ENTL 为 ID 的比特长度，2 字节大端。
        /// </summary>
        public static byte[] ComputeZ(Sm2Point publicKey, byte[] userId)
        {
            userId ??= DefaultUserId;
            var entl = userId.Length * 8;
            if (entl > 0xFFFF)
            {
                throw new ArgumentException("用户标识过长", nameof(userId));
            }
            var entlBytes = new byte[] { (byte)(entl >> 8), (byte)entl };
            return Sm3Hash(
                entlBytes,
                userId,
                DerEncoder.ToFixedBigEndian(Sm2Curve.A, Sm2Curve.FieldBytes),
                DerEncoder.ToFixedBigEndian(Sm2Curve.B, Sm2Curve.FieldBytes),
                DerEncoder.ToFixedBigEndian(Sm2Curve.Gx, Sm2Curve.FieldBytes),
                DerEncoder.ToFixedBigEndian(Sm2Curve.Gy, Sm2Curve.FieldBytes),
                DerEncoder.ToFixedBigEndian(publicKey.X, Sm2Curve.FieldBytes),
                DerEncoder.ToFixedBigEndian(publicKey.Y, Sm2Curve.FieldBytes));
        }

        /// <summary>
        /// 签名。返回 (r, s)，各为 [1, n-1] 内的整数。
        /// </summary>
        public static (BigInteger R, BigInteger S) Sign(BigInteger privateKey, Sm2Point publicKey, byte[] message, byte[] userId)
        {
            if (privateKey.Sign <= 0 || privateKey >= Sm2Curve.N)
            {
                throw new ArgumentException("私钥必须落在 [1, n-1] 区间", nameof(privateKey));
            }

            var z = ComputeZ(publicKey, userId);
            var e = new BigInteger(Sm3Hash(z, message ?? Array.Empty<byte>()), isUnsigned: true, isBigEndian: true);
            var d = privateKey;
            var dPlusOneInverse = ModInverseN(Sm2Curve.ModN(1 + d));

            // GB/T 32918.2：r=0、r+k=n、s=0 都要重新取 k。命中概率极低，循环上限仅用于防止实现错误导致死循环。
            for (var attempt = 0; attempt < 128; attempt++)
            {
                var k = Sm2Curve.RandomScalar();
                var point = Sm2Curve.Multiply(k, Sm2Curve.G);
                var r = Sm2Curve.ModN(e + point.X);
                if (r.IsZero || (r + k) == Sm2Curve.N)
                {
                    continue;
                }
                var s = Sm2Curve.ModN(dPlusOneInverse * (k - r * d));
                if (s.IsZero)
                {
                    continue;
                }
                return (r, s);
            }
            throw new InvalidOperationException("SM2 签名失败：连续多次生成的随机数均不满足条件");
        }

        /// <summary>
        /// 验签。
        /// </summary>
        public static bool Verify(Sm2Point publicKey, byte[] message, BigInteger r, BigInteger s, byte[] userId)
        {
            if (r.Sign <= 0 || r >= Sm2Curve.N || s.Sign <= 0 || s >= Sm2Curve.N)
            {
                return false;
            }

            var z = ComputeZ(publicKey, userId);
            var e = new BigInteger(Sm3Hash(z, message ?? Array.Empty<byte>()), isUnsigned: true, isBigEndian: true);
            var t = Sm2Curve.ModN(r + s);
            if (t.IsZero)
            {
                return false;
            }

            var point = Sm2Curve.Add(Sm2Curve.Multiply(s, Sm2Curve.G), Sm2Curve.Multiply(t, publicKey));
            if (point.IsInfinity)
            {
                return false;
            }
            return Sm2Curve.ModN(e + point.X) == r;
        }

        /// <summary>
        /// 加密，输出 C1 || C2 || C3。
        /// </summary>
        public static byte[] Encrypt(Sm2Point publicKey, byte[] message)
        {
            message ??= Array.Empty<byte>();

            for (var attempt = 0; attempt < 128; attempt++)
            {
                var k = Sm2Curve.RandomScalar();
                var c1 = Sm2Curve.Multiply(k, Sm2Curve.G);
                var shared = Sm2Curve.Multiply(k, publicKey);
                if (shared.IsInfinity)
                {
                    continue;
                }

                var x2 = DerEncoder.ToFixedBigEndian(shared.X, Sm2Curve.FieldBytes);
                var y2 = DerEncoder.ToFixedBigEndian(shared.Y, Sm2Curve.FieldBytes);

                var t = Kdf(x2, y2, message.Length);
                if (t == null)
                {
                    // t 全零，按 GB/T 32918.4 必须重新选择 k
                    continue;
                }

                var c2 = new byte[message.Length];
                for (var i = 0; i < message.Length; i++)
                {
                    c2[i] = (byte)(message[i] ^ t[i]);
                }
                var c3 = Sm3Hash(x2, message, y2);
                var c1Bytes = Sm2Curve.Encode(c1);

                var output = new byte[c1Bytes.Length + c2.Length + DigestBytes];
                Buffer.BlockCopy(c1Bytes, 0, output, 0, c1Bytes.Length);
                Buffer.BlockCopy(c2, 0, output, c1Bytes.Length, c2.Length);
                Buffer.BlockCopy(c3, 0, output, c1Bytes.Length + c2.Length, DigestBytes);
                return output;
            }
            throw new InvalidOperationException("SM2 加密失败：连续多次生成的随机数均导致 KDF 输出全零");
        }

        /// <summary>
        /// 解密。输入必须是 C1 || C2 || C3 顺序。
        /// </summary>
        public static byte[] Decrypt(BigInteger privateKey, byte[] cipher)
        {
            var c1Length = Sm2Curve.UncompressedPointBytes;
            if (cipher == null || cipher.Length < c1Length + DigestBytes)
            {
                throw new InvalidCipherTextException("SM2 密文长度不足，至少需要 C1(65)+C3(32) 共 97 字节");
            }
            if (privateKey.Sign <= 0 || privateKey >= Sm2Curve.N)
            {
                throw new ArgumentException("私钥必须落在 [1, n-1] 区间", nameof(privateKey));
            }

            var c1Bytes = new byte[c1Length];
            Buffer.BlockCopy(cipher, 0, c1Bytes, 0, c1Length);
            var c1 = Sm2Curve.Decode(c1Bytes);

            var c2Length = cipher.Length - c1Length - DigestBytes;
            var c2 = new byte[c2Length];
            Buffer.BlockCopy(cipher, c1Length, c2, 0, c2Length);

            var c3 = new byte[DigestBytes];
            Buffer.BlockCopy(cipher, c1Length + c2Length, c3, 0, DigestBytes);

            var shared = Sm2Curve.Multiply(privateKey, c1);
            if (shared.IsInfinity)
            {
                throw new InvalidCipherTextException("SM2 解密失败：C1 与私钥计算得到无穷远点");
            }

            var x2 = DerEncoder.ToFixedBigEndian(shared.X, Sm2Curve.FieldBytes);
            var y2 = DerEncoder.ToFixedBigEndian(shared.Y, Sm2Curve.FieldBytes);

            var t = Kdf(x2, y2, c2Length);
            if (t == null)
            {
                throw new InvalidCipherTextException("SM2 解密失败：KDF 输出全零");
            }

            var message = new byte[c2Length];
            for (var i = 0; i < c2Length; i++)
            {
                message[i] = (byte)(c2[i] ^ t[i]);
            }

            var u = Sm3Hash(x2, message, y2);
            if (!System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(u, c3))
            {
                throw new InvalidCipherTextException();
            }
            return message;
        }

        /// <summary>
        /// 密钥派生函数 KDF(Z, klen)：按 GB/T 32918.4，逐块计算 SM3(Z || ct)。
        /// 返回值全零时返回 null，调用方需要重新选择随机数。
        /// </summary>
        private static byte[]? Kdf(byte[] x2, byte[] y2, int keyLength)
        {
            if (keyLength == 0)
            {
                // 空明文场景：C2 为空，KDF 无需派生，也不能因"全零"判断而误判失败
                return Array.Empty<byte>();
            }

            var z = new byte[x2.Length + y2.Length];
            Buffer.BlockCopy(x2, 0, z, 0, x2.Length);
            Buffer.BlockCopy(y2, 0, z, x2.Length, y2.Length);

            var output = new byte[keyLength];
            var offset = 0;
            var counter = 1;
            var counterBytes = new byte[4];
            var allZero = true;

            while (offset < keyLength)
            {
                counterBytes[0] = (byte)(counter >> 24);
                counterBytes[1] = (byte)(counter >> 16);
                counterBytes[2] = (byte)(counter >> 8);
                counterBytes[3] = (byte)counter;

                var block = Sm3Hash(z, counterBytes);
                var take = Math.Min(DigestBytes, keyLength - offset);
                Buffer.BlockCopy(block, 0, output, offset, take);

                for (var i = 0; i < take; i++)
                {
                    if (output[offset + i] != 0)
                    {
                        allZero = false;
                    }
                }

                offset += take;
                counter++;
            }

            return allZero ? null : output;
        }

        /// <summary>对阶 n 求模逆元（扩展欧几里得）</summary>
        private static BigInteger ModInverseN(BigInteger value)
        {
            value = Sm2Curve.ModN(value);
            if (value.IsZero)
            {
                throw new ArgumentException("0 不存在模逆元");
            }

            BigInteger oldR = Sm2Curve.N, r = value;
            BigInteger oldT = BigInteger.Zero, t = BigInteger.One;

            while (!r.IsZero)
            {
                var quotient = oldR / r;

                var tmpR = oldR - quotient * r;
                oldR = r;
                r = tmpR;

                var tmpT = oldT - quotient * t;
                oldT = t;
                t = tmpT;
            }

            if (!oldR.IsOne)
            {
                throw new ArgumentException("模逆元不存在");
            }
            var result = oldT % Sm2Curve.N;
            return result.Sign < 0 ? result + Sm2Curve.N : result;
        }
    }
}
