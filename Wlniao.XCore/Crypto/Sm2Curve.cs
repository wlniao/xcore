/*==============================================================================
    文件名称：Sm2Curve.cs
    适用环境：CoreCLR (.NET 8 / .NET 10, NativeAOT)
    功能描述：SM2 推荐曲线 SM2P256V1 的参数与椭圆曲线点运算
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
    实现依据：GB/T 32918.1-2016《信息安全技术 SM2 椭圆曲线公钥密码算法 第1部分：总则》
              与 GB/T 32918.5-2017 附录给出的推荐曲线参数。
    曲线方程：y^2 = x^3 + a*x + b (mod p)，基点 G，阶 n。
    点运算采用仿射坐标，模逆元用扩展欧几里得算法求得。
    选择 System.Numerics.BigInteger 而非自研定长整数，是因为 256 位规模下性能足够
    （单次标量乘在毫秒级），而正确性更容易保证，且 BigInteger 在 NativeAOT 下完全可用。
===============================================================================*/

using System;
using System.Numerics;

namespace Wlniao.Crypto
{
    /// <summary>
    /// SM2P256V1 曲线上的一个点（仿射坐标）
    /// </summary>
    internal readonly struct Sm2Point
    {
        /// <summary>横坐标</summary>
        public readonly BigInteger X;
        /// <summary>纵坐标</summary>
        public readonly BigInteger Y;
        /// <summary>是否为无穷远点 O</summary>
        public readonly bool IsInfinity;

        /// <summary>构造普通点</summary>
        public Sm2Point(BigInteger x, BigInteger y)
        {
            X = x;
            Y = y;
            IsInfinity = false;
        }

        private Sm2Point(bool infinity)
        {
            X = BigInteger.Zero;
            Y = BigInteger.Zero;
            IsInfinity = infinity;
        }

        /// <summary>无穷远点，作为点加法的单位元</summary>
        public static readonly Sm2Point Infinity = new Sm2Point(true);

        /// <summary>两个点是否相同</summary>
        public bool SameAs(Sm2Point other)
        {
            if (IsInfinity || other.IsInfinity)
            {
                return IsInfinity == other.IsInfinity;
            }
            return X == other.X && Y == other.Y;
        }
    }

    /// <summary>
    /// SM2 推荐曲线参数与点运算。
    /// </summary>
    internal static class Sm2Curve
    {
        /// <summary>坐标字节长度（256 位 = 32 字节）</summary>
        public const int FieldBytes = 32;

        /// <summary>未压缩点编码长度：0x04 || X(32) || Y(32)</summary>
        public const int UncompressedPointBytes = 65;

        /// <summary>域参数 p</summary>
        public static readonly BigInteger P = Hex("FFFFFFFEFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF00000000FFFFFFFFFFFFFFFF");

        /// <summary>曲线参数 a</summary>
        public static readonly BigInteger A = Hex("FFFFFFFEFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF00000000FFFFFFFFFFFFFFFC");

        /// <summary>曲线参数 b</summary>
        /// <remarks>
        /// 28E9FA9E 9D9F5E34 4D5A9E4B CF6509A7 F39789F5 15AB8F92 DDBCBD41 4D940E93
        /// 该值可由基点在曲线上反推校验：b = (Gy^2 - Gx^3 - a*Gx) mod p
        /// </remarks>
        public static readonly BigInteger B = Hex("28E9FA9E9D9F5E344D5A9E4BCF6509A7F39789F515AB8F92DDBCBD414D940E93");

        /// <summary>基点 G 的阶 n</summary>
        public static readonly BigInteger N = Hex("FFFFFFFEFFFFFFFFFFFFFFFFFFFFFFFF7203DF6B21C6052B53BBF40939D54123");

        /// <summary>基点 G 的横坐标</summary>
        public static readonly BigInteger Gx = Hex("32C4AE2C1F1981195F9904466A39C9948FE30BBFF2660BE1715A4589334C74C7");

        /// <summary>基点 G 的纵坐标</summary>
        public static readonly BigInteger Gy = Hex("BC3736A2F4F6779C59BDCEE36B692153D0A9877CC62A474002DF32E52139F0A0");

        /// <summary>基点 G</summary>
        public static readonly Sm2Point G = new Sm2Point(Gx, Gy);

        private static BigInteger Hex(string hex)
        {
            // 注意：本命名空间内 Wlniao.Convert 会遮蔽 System.Convert，必须全限定
            return new BigInteger(System.Convert.FromHexString(hex), isUnsigned: true, isBigEndian: true);
        }

        /// <summary>对 P 取模，保证结果非负</summary>
        public static BigInteger ModP(BigInteger value)
        {
            var r = value % P;
            return r.Sign < 0 ? r + P : r;
        }

        /// <summary>对阶 n 取模，保证结果非负</summary>
        public static BigInteger ModN(BigInteger value)
        {
            var r = value % N;
            return r.Sign < 0 ? r + N : r;
        }

        /// <summary>
        /// 求模 P 的乘法逆元（扩展欧几里得算法）。
        /// 比 BigInteger.ModPow(a, P-2, P) 快两个数量级，标量乘里会调用数百次，差异明显。
        /// </summary>
        public static BigInteger ModInverseP(BigInteger value)
        {
            return ModInverse(ModP(value), P);
        }

        private static BigInteger ModInverse(BigInteger value, BigInteger modulus)
        {
            if (value.IsZero)
            {
                throw new ArgumentException("0 不存在模逆元");
            }
            BigInteger oldR = modulus, r = value;
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
            var result = oldT % modulus;
            return result.Sign < 0 ? result + modulus : result;
        }

        /// <summary>点加：P + Q</summary>
        public static Sm2Point Add(Sm2Point p, Sm2Point q)
        {
            if (p.IsInfinity)
            {
                return q;
            }
            if (q.IsInfinity)
            {
                return p;
            }

            if (p.X == q.X)
            {
                // 纵坐标互为逆元（含 y=0 的自逆情况），结果为无穷远点
                if (ModP(p.Y + q.Y).IsZero)
                {
                    return Sm2Point.Infinity;
                }
                // 同一个点，走倍点公式
                return Twice(p);
            }

            var lambda = ModP(ModP(q.Y - p.Y) * ModInverseP(q.X - p.X));
            var x3 = ModP(lambda * lambda - p.X - q.X);
            var y3 = ModP(lambda * (p.X - x3) - p.Y);
            return new Sm2Point(x3, y3);
        }

        /// <summary>倍点：2P</summary>
        public static Sm2Point Twice(Sm2Point p)
        {
            if (p.IsInfinity || p.Y.IsZero)
            {
                return Sm2Point.Infinity;
            }
            var lambda = ModP((3 * p.X * p.X + A) * ModInverseP(2 * p.Y));
            var x3 = ModP(lambda * lambda - 2 * p.X);
            var y3 = ModP(lambda * (p.X - x3) - p.Y);
            return new Sm2Point(x3, y3);
        }

        /// <summary>标量乘：k * P（二进制展开，从低位到高位）</summary>
        public static Sm2Point Multiply(BigInteger k, Sm2Point point)
        {
            if (k.Sign < 0)
            {
                throw new ArgumentException("标量不能为负", nameof(k));
            }
            if (k.IsZero || point.IsInfinity)
            {
                return Sm2Point.Infinity;
            }

            var result = Sm2Point.Infinity;
            var addend = point;
            while (k.Sign > 0)
            {
                if (!k.IsEven)
                {
                    result = Add(result, addend);
                }
                addend = Twice(addend);
                k >>= 1;
            }
            return result;
        }

        /// <summary>点是否在曲线上，且坐标在合法范围内</summary>
        public static bool IsOnCurve(Sm2Point p)
        {
            if (p.IsInfinity)
            {
                return false;
            }
            if (p.X.Sign < 0 || p.X >= P || p.Y.Sign < 0 || p.Y >= P)
            {
                return false;
            }
            var left = ModP(p.Y * p.Y);
            var right = ModP(p.X * p.X * p.X + A * p.X + B);
            return left == right;
        }

        /// <summary>编码为未压缩点 0x04 || X || Y（65 字节），与 BouncyCastle 的 GetEncoded(false) 一致</summary>
        public static byte[] Encode(Sm2Point p)
        {
            if (p.IsInfinity)
            {
                throw new ArgumentException("无穷远点无法编码");
            }
            var result = new byte[UncompressedPointBytes];
            result[0] = 0x04;
            var x = DerEncoder.ToFixedBigEndian(p.X, FieldBytes);
            var y = DerEncoder.ToFixedBigEndian(p.Y, FieldBytes);
            Buffer.BlockCopy(x, 0, result, 1, FieldBytes);
            Buffer.BlockCopy(y, 0, result, 1 + FieldBytes, FieldBytes);
            return result;
        }

        /// <summary>解码未压缩点，并校验其位于 SM2P256V1 曲线上</summary>
        public static Sm2Point Decode(byte[] data)
        {
            if (data == null || data.Length != UncompressedPointBytes || data[0] != 0x04)
            {
                throw new FormatException("公钥必须是 65 字节的未压缩点编码 04||X||Y");
            }
            var x = new BigInteger(data.AsSpan(1, FieldBytes), isUnsigned: true, isBigEndian: true);
            var y = new BigInteger(data.AsSpan(1 + FieldBytes, FieldBytes), isUnsigned: true, isBigEndian: true);
            var point = new Sm2Point(x, y);
            if (!IsOnCurve(point))
            {
                throw new FormatException("公钥不在 SM2P256V1 曲线上");
            }
            return point;
        }

        /// <summary>
        /// 生成 [1, n-1] 区间内的随机标量。
        /// 采用拒绝采样而非取模，避免取模引入的分布偏斜。
        /// </summary>
        public static BigInteger RandomScalar()
        {
            Span<byte> buffer = stackalloc byte[FieldBytes];
            while (true)
            {
                System.Security.Cryptography.RandomNumberGenerator.Fill(buffer);
                var k = new BigInteger(buffer, isUnsigned: true, isBigEndian: true);
                if (k.Sign > 0 && k < N)
                {
                    return k;
                }
            }
        }
    }
}
