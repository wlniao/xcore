using System;
using Org.BouncyCastle.Crypto;
namespace Wlniao.Crypto
{
    /// <summary>
    /// SM3杂凑算法
    /// </summary>
    public class SM3 : IDigest
    {
        /// <summary>
        /// 内部缓冲区的大小
        /// </summary>
        private const int ByteLength = 64;
        /// <summary>
        /// 初始值IV
        /// </summary>
        private static readonly int[] IV = new int[] {
            0x7380166f, 0x4914b2b9, 0x172442d7,
            unchecked((int)0xda8a0600), unchecked((int)0xa96f30bc), 0x163138aa,
            unchecked((int)0xe38dee4d), unchecked((int)0xb0fb0e4e)
        };
        /// <summary>
        /// 备份的字寄存器
        /// </summary>
        private readonly int[] v = new int[8];
        /// <summary>
        /// 使用中的字寄存器
        /// </summary>
        private readonly int[] v_ = new int[8];

        private static readonly int[] X0 = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        private readonly int[] X = new int[68];
        private int xOff;

        /// <summary>
        /// 0到15的Tj常量
        /// </summary>
        private readonly int TOne = 0x79cc4519;
        /// <summary>
        /// 16到63的Tj常量
        /// </summary>
        private readonly int TSecond = 0x7a879d8a;
        /// <summary>
        /// 消息摘要
        /// </summary>
        private readonly byte[] XBuf = new byte[4];
        /// <summary>
        /// 待更新的消息摘要的索引
        /// </summary>
        private int XBufOff;
        /// <summary>
        /// 待更新的消息摘要的大小
        /// </summary>
        private long ByteCount;
        /// <summary>
        /// SM3算法产生的哈希值大小
        /// </summary>
        private const int DigestLength = 32;

        /// <summary>
        /// 构造函数
        /// </summary>
        public SM3()
        {
            Reset();
        }

        /// <summary>
        /// 复制构造函数
        /// </summary>
        /// <param name="t"></param>
        public SM3(SM3 t)
        {
            XBuf = new byte[t.XBuf.Length];
            Array.Copy(t.XBuf, 0, XBuf, 0, t.XBuf.Length);

            XBufOff = t.XBufOff;
            ByteCount = t.ByteCount;
        }
        /// <summary>
        /// 用字节块更新消息摘要
        /// </summary>
        /// <param name="input"></param>
        /// <param name="inOff"></param>
        /// <param name="length"></param>
        public void BlockUpdate(byte[] input, int inOff, int length)
        {
            //更新当前消息摘要
            while ((XBufOff != 0) && (length > 0))
            {
                Update(input[inOff]);
                inOff++;
                length--;
            }

            //处理完整的消息摘要
            while (length > XBuf.Length)
            {
                ProcessWord(input, inOff);

                inOff += XBuf.Length;
                length -= XBuf.Length;
                ByteCount += XBuf.Length;
            }

            //填充剩余的消息摘要
            while (length > 0)
            {
                Update(input[inOff]);

                inOff++;
                length--;
            }
        }
        /// <summary>
        /// 用字节块更新消息摘要
        /// </summary>
        /// <param name="input"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void BlockUpdate(ReadOnlySpan<byte> input)
        {
            var buffer = input.ToArray();
            BlockUpdate(buffer, 0, buffer.Length);
        }
        /// <summary>
        /// 用一个字节更新消息摘要。
        /// </summary>
        /// <param name="input"></param>
        public void Update(byte input)
        {
            XBuf[XBufOff++] = input;

            if (XBufOff == XBuf.Length)
            {
                ProcessWord(XBuf, 0);
                XBufOff = 0;
            }

            ByteCount++;
        }
        /// <summary>
        /// 产生最终的摘要值
        /// </summary>
        public void Finish()
        {
            long bitLength = (ByteCount << 3);

            //添加字节
            Update(unchecked((byte)128));

            while (XBufOff != 0) Update(unchecked((byte)0));
            ProcessLength(bitLength);
            ProcessBlock();
        }
        /// <summary>
        /// 重启
        /// </summary>
        public virtual void Reset()
        {
            ByteCount = 0;
            XBufOff = 0;
            Array.Clear(XBuf, 0, XBuf.Length);
            Array.Copy(IV, 0, v, 0, IV.Length);
            xOff = 0;
            Array.Copy(X0, 0, X, 0, X0.Length);
        }
        /// <summary>
        /// 摘要应用其压缩功能的内部缓冲区的大小
        /// </summary>
        /// <returns></returns>
        public int GetByteLength()
        {
            return ByteLength;
        }
        /// <summary>
        /// 处理消息摘要
        /// ABCDEFGH 串联
        /// </summary>
        /// <param name="input"></param>
        /// <param name="inOff"></param>
        internal void ProcessWord(byte[] input, int inOff)
        {
            int n = input[inOff] << 24;
            n |= (input[++inOff] & 0xff) << 16;
            n |= (input[++inOff] & 0xff) << 8;
            n |= (input[++inOff] & 0xff);
            X[xOff] = n;

            if (++xOff == 16)
            {
                ProcessBlock();
            }
        }
        internal void ProcessLength(long bitLength)
        {
            if (xOff > 14)
            {
                ProcessBlock();
            }

            X[14] = (int)(Helper.URShift(bitLength, 32));
            X[15] = (int)(bitLength & unchecked((int)0xffffffff));
        }
        /// <summary>
        /// 迭代压缩
        /// </summary>
        internal void ProcessBlock()
        {
            int j;

            int[] ww = X;
            //64位比特串
            int[] ww_ = new int[64];

            #region 块消息扩展
            //消息扩展16 TO 67
            for (j = 16; j < 68; j++)
            {
                ww[j] = P1(ww[j - 16] ^ ww[j - 9] ^ (Rotate(ww[j - 3], 15))) ^ (Rotate(ww[j - 13], 7)) ^ ww[j - 6];
            }
            //消息扩展0 TO 63
            for (j = 0; j < 64; j++)
            {
                ww_[j] = ww[j] ^ ww[j + 4];
            }
            #endregion

            #region 压缩函数
            int[] vv = v;
            int[] vv_ = v_;//A,B,C,D,E,F,G,H为字寄存器

            Array.Copy(vv, 0, vv_, 0, IV.Length);
            //中间变量SS1,SS2,TT1,TT2
            int SS1, SS2, TT1, TT2;
            int aaa;
            //将消息分组B(i)划分为16个字
            for (j = 0; j < 16; j++)
            {
                aaa = Rotate(vv_[0], 12);
                SS1 = aaa + vv_[4] + Rotate(TOne, j);
                SS1 = Rotate(SS1, 7);
                SS2 = SS1 ^ aaa;

                TT1 = FFOne(vv_[0], vv_[1], vv_[2]) + vv_[3] + SS2 + ww_[j];
                TT2 = GGOne(vv_[4], vv_[5], vv_[6]) + vv_[7] + SS1 + ww[j];

                #region 更新各个寄存器
                vv_[3] = vv_[2];
                vv_[2] = Rotate(vv_[1], 9);
                vv_[1] = vv_[0];
                vv_[0] = TT1;
                vv_[7] = vv_[6];
                vv_[6] = Rotate(vv_[5], 19);
                vv_[5] = vv_[4];
                vv_[4] = P0(TT2);
                #endregion
            }

            for (j = 16; j < 64; j++)
            {
                aaa = Rotate(vv_[0], 12);
                SS1 = aaa + vv_[4] + Rotate(TSecond, j);
                SS1 = Rotate(SS1, 7);
                SS2 = SS1 ^ aaa;

                TT1 = FFSecond(vv_[0], vv_[1], vv_[2]) + vv_[3] + SS2 + ww_[j];
                TT2 = GGSecond(vv_[4], vv_[5], vv_[6]) + vv_[7] + SS1 + ww[j];

                #region 更新各个寄存器
                vv_[3] = vv_[2];
                vv_[2] = Rotate(vv_[1], 9);
                vv_[1] = vv_[0];
                vv_[0] = TT1;
                vv_[7] = vv_[6];
                vv_[6] = Rotate(vv_[5], 19);
                vv_[5] = vv_[4];
                vv_[4] = P0(TT2);
                #endregion
            }
            #endregion

            //256比特的杂凑值y =vv_(j+1) ABCDEFGH
            for (j = 0; j < 8; j++)
            {
                vv[j] ^= vv_[j];
            }

            // Reset
            xOff = 0;
            Array.Copy(X0, 0, X, 0, X0.Length);
        }
        /// <summary>
        /// 算法名称
        /// </summary>
        public string AlgorithmName
        {
            get
            {
                return "SM3";
            }
        }
        /// <summary>
        /// 消息摘要生成的摘要的大小
        /// </summary>
        /// <returns></returns>
        public int GetDigestSize()
        {
            return DigestLength;  //SM3算法产生的哈希值大小
        }
        /// <summary>
        /// 写入到大端
        /// </summary>
        /// <param name="n"></param>
        /// <param name="bs"></param>
        /// <param name="off"></param>
        private void IntToBigEndian(int n, byte[] bs, int off)
        {
            bs[off] = (byte)(Helper.URShift(n, 24));
            bs[++off] = (byte)(Helper.URShift(n, 16));
            bs[++off] = (byte)(Helper.URShift(n, 8));
            bs[++off] = (byte)(n);
        }
        /// <summary>
        /// 关闭摘要，产生最终的摘要值。doFinal调用使摘要复位。
        /// </summary>
        /// <param name="output"></param>
        /// <param name="outOff"></param>
        /// <returns></returns>
        public int DoFinal(byte[] output, int outOff)
        {
            Finish();
            for (int i = 0; i < 8; i++)
            {
                IntToBigEndian(v[i], output, outOff + i * 4);
            }
            Reset();
            return DigestLength;
        }
        /// <summary>
        /// 关闭摘要，产生最终的摘要值。doFinal调用使摘要复位。
        /// </summary>
        /// <param name="output"></param>
        /// <returns></returns>
        public int DoFinal(Span<byte> output)
        {
            Finish();
            var buffer = new byte[output.Length];
            for (int i = 0; i < 8; i++)
            {
                IntToBigEndian(v[i], buffer, i * 4);
            }
            Reset();
            output = new Span<byte>(buffer);
            return DigestLength;
        }
        /// <summary>
        /// 关闭摘要，产生最终的摘要值。doFinal调用使摘要复位。
        /// </summary>
        /// <returns></returns>
        public byte[] DoFinal()
        {
            var buffer = new byte[DigestLength];//SM3算法产生的哈希值大小
            DoFinal(buffer, 0);
            return buffer;
        }

        /// <summary>
        /// 基于SM3的Hmac算法
        /// </summary>
        /// <param name="dataBytes"></param>
        /// <param name="keyBytes"></param>
        /// <returns></returns>
        public byte[] Hmac(byte[] dataBytes, byte[] keyBytes)
        {
            //1.填充0至key,或者hashkey,使其长度为sm3分组长度
            /** ByteLength SM3分组长度,64个字节,512位*/
            byte[] sm3_key;
            byte[] structured_key = new byte[ByteLength];
            byte[] IPAD = new byte[ByteLength];
            byte[] OPAD = new byte[ByteLength];
            if (keyBytes.Length > ByteLength)
            {
                BlockUpdate(keyBytes, 0, keyBytes.Length);
                var keyHash = DoFinal();
                Array.Copy(keyHash, 0, structured_key, 0, keyHash.Length);
            }
            else
            {
                Array.Copy(keyBytes, 0, structured_key, 0, keyBytes.Length);
            }
            //2.让处理之后的key 与ipad (分组长度的0x36)做异或运算
            for (int i = 0; i < ByteLength; i++)
            {
                IPAD[i] = 0x36;
                OPAD[i] = 0x5c;
            }
            byte[] ipadkey = ByteArray.Xor(structured_key, IPAD);
            //3.将2的结果与text拼接
            byte[] t3 = new byte[ByteLength + dataBytes.Length];
            Array.Copy(ipadkey, 0, t3, 0, ipadkey.Length);
            Array.Copy(dataBytes, 0, t3, ipadkey.Length, dataBytes.Length);
            //4.将3的结果sm3 哈希
            BlockUpdate(t3, 0, t3.Length);
            var t4 = DoFinal();
            //5.让处理之后的key 与opad(分组长度的0x5c)做异或运算
            byte[] opadkey = ByteArray.Xor(structured_key, OPAD);
            //6.4的结果拼接在5之后
            byte[] t6 = new byte[ByteLength + t4.Length];
            Array.Copy(opadkey, 0, t6, 0, opadkey.Length);
            Array.Copy(t4, 0, t6, opadkey.Length, t4.Length);
            //7.对6做hash
            BlockUpdate(t6, 0, t6.Length);
            return DoFinal();
        }

        /// <summary>
        /// x循环左移n比特运算
        /// </summary>
        /// <param name="x"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        private static int Rotate(int x, int n)
        {
            return (x << n) | (Helper.URShift(x, (32 - n)));
        }

        #region 置换函数
        /// <summary>
        /// 置换函数P0
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        private static int P0(int x)
        {
            return (x) ^ Rotate(x, 9) ^ Rotate(x, 17);
        }
        /// <summary>
        /// 置换函数P1
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        private static int P1(int x)
        {
            return (x) ^ Rotate(x, 15) ^ Rotate(x, 23);
        }
        #endregion

        #region 布尔函数
        /// <summary>
        /// 0到15的布尔函数FF (X⊕^Y⊕Z)
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <param name="Z"></param>
        /// <returns></returns>
        private static int FFOne(int X, int Y, int Z)
        {
            return (X ^ Y ^ Z);
        }
        /// <summary>
        /// 16到63的布尔函数FF
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <param name="Z"></param>
        /// <returns></returns>
        private static int FFSecond(int X, int Y, int Z)
        {
            return ((X & Y) | (X & Z) | (Y & Z));
        }

        /// <summary>
        /// 0到15的布尔函数GG
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <param name="Z"></param>
        /// <returns></returns>
        private static int GGOne(int X, int Y, int Z)
        {
            return (X ^ Y ^ Z);
        }
        /// <summary>
        /// 16到63的布尔函数GG
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <param name="Z"></param>
        /// <returns></returns>
        private static int GGSecond(int X, int Y, int Z)
        {
            return ((X & Y) | (~X & Z));
        }
        #endregion



    }
}
