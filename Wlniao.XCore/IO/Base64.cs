/*==============================================================================
    文件名称：Base64.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：Base64编码类
================================================================================
 
    Copyright 2015 XieChaoyi

    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at

               http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.

===============================================================================*/

using System.Text;

namespace Wlniao.IO
{
    /// <summary>
    /// Base64编码类
    /// 将byte[]类型转换成Base64编码的string类型。
    /// </summary>
    public class Base64Encoder
    {
        byte[] source;
        int length, length2;
        int blockCount;
        int paddingCount;
        /// <summary>
        /// 
        /// </summary>
        public static Base64Encoder Encoder = new Base64Encoder();
        /// <summary>
        /// 
        /// </summary>
        public Base64Encoder()
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        private void init(byte[] input)
        {
            source = input;
            length = input.Length;
            if ((length % 3) == 0)
            {
                paddingCount = 0;
                blockCount = length / 3;
            }
            else
            {
                paddingCount = 3 - (length % 3);
                blockCount = (length + paddingCount) / 3;
            }
            length2 = length + paddingCount;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public string GetEncoded(byte[] input)
        {
            //初始化
            init(input);

            byte[] source2;
            source2 = new byte[length2];

            for (var x = 0; x < length2; x++)
            {
                if (x < length)
                {
                    source2[x] = source[x];
                }
                else
                {
                    source2[x] = 0;
                }
            }

            byte b1, b2, b3;
            byte temp, temp1, temp2, temp3, temp4;
            var buffer = new byte[blockCount * 4];
            var result = new char[blockCount * 4];
            for (var x = 0; x < blockCount; x++)
            {
                b1 = source2[x * 3];
                b2 = source2[x * 3 + 1];
                b3 = source2[x * 3 + 2];

                temp1 = (byte)((b1 & 252) >> 2);

                temp = (byte)((b1 & 3) << 4);
                temp2 = (byte)((b2 & 240) >> 4);
                temp2 += temp;

                temp = (byte)((b2 & 15) << 2);
                temp3 = (byte)((b3 & 192) >> 6);
                temp3 += temp;

                temp4 = (byte)(b3 & 63);

                buffer[x * 4] = temp1;
                buffer[x * 4 + 1] = temp2;
                buffer[x * 4 + 2] = temp3;
                buffer[x * 4 + 3] = temp4;

            }

            for (var x = 0; x < blockCount * 4; x++)
            {
                result[x] = sixbit2char(buffer[x]);
            }


            switch (paddingCount)
            {
                case 0: break;
                case 1: result[blockCount * 4 - 1] = '='; break;
                case 2: result[blockCount * 4 - 1] = '=';
                    result[blockCount * 4 - 2] = '=';
                    break;
                default: break;
            }
            return new string(result);
        }
        private char sixbit2char(byte b)
        {
            var lookupTable = new char[64]{
                  'A','B','C','D','E','F','G','H','I','J','K','L','M',
                 'N','O','P','Q','R','S','T','U','V','W','X','Y','Z',
                 'a','b','c','d','e','f','g','h','i','j','k','l','m',
                 'n','o','p','q','r','s','t','u','v','w','x','y','z',
                 '0','1','2','3','4','5','6','7','8','9','+','/'};

            if ((b >= 0) && (b <= 63))
            {
                return lookupTable[(int)b];
            }
            else
            {

                return ' ';
            }
        }
    }

    /// <summary>
    /// Base64解码类
    /// 将Base64编码的string类型转换成byte[]类型
    /// </summary>
    public class Base64Decoder
    {
        char[] source;
        int length, length2, length3;
        int blockCount;
        int paddingCount;
        /// <summary>
        /// 
        /// </summary>
        public static Base64Decoder Decoder = new Base64Decoder();
        /// <summary>
        /// 
        /// </summary>
        public Base64Decoder()
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        private void init(char[] input)
        {
            var temp = 0;
            source = input;
            length = input.Length;

            for (var x = 0; x < 2; x++)
            {
                if (input[length - x - 1] == '=')
                    temp++;
            }
            paddingCount = temp;

            blockCount = length / 4;
            length2 = blockCount * 3;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="strInput"></param>
        /// <returns></returns>
        public byte[] GetDecoded(string strInput)
        {
            //初始化
            init(strInput.ToCharArray());

            var buffer = new byte[length];
            var buffer2 = new byte[length2];

            for (var x = 0; x < length; x++)
            {
                buffer[x] = char2sixbit(source[x]);
            }

            byte b, b1, b2, b3;
            byte temp1, temp2, temp3, temp4;

            for (var x = 0; x < blockCount; x++)
            {
                temp1 = buffer[x * 4];
                temp2 = buffer[x * 4 + 1];
                temp3 = buffer[x * 4 + 2];
                temp4 = buffer[x * 4 + 3];

                b = (byte)(temp1 << 2);
                b1 = (byte)((temp2 & 48) >> 4);
                b1 += b;

                b = (byte)((temp2 & 15) << 4);
                b2 = (byte)((temp3 & 60) >> 2);
                b2 += b;

                b = (byte)((temp3 & 3) << 6);
                b3 = temp4;
                b3 += b;

                buffer2[x * 3] = b1;
                buffer2[x * 3 + 1] = b2;
                buffer2[x * 3 + 2] = b3;
            }

            length3 = length2 - paddingCount;
            var result = new byte[length3];

            for (var x = 0; x < length3; x++)
            {
                result[x] = buffer2[x];
            }

            return result;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private byte char2sixbit(char c)
        {
            var lookupTable = new char[64]{  
                 'A','B','C','D','E','F','G','H','I','J','K','L','M','N',
                 'O','P','Q','R','S','T','U','V','W','X','Y', 'Z',
                 'a','b','c','d','e','f','g','h','i','j','k','l','m','n',
                 'o','p','q','r','s','t','u','v','w','x','y','z',
                 '0','1','2','3','4','5','6','7','8','9','+','/'};
            if (c == '=')
                return 0;
            else
            {
                for (var x = 0; x < 64; x++)
                {
                    if (lookupTable[x] == c)
                        return (byte)x;
                }

                return 0;
            }

        }
    }
    /// <summary>
    /// 
    /// </summary>
    public class Base64Default
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string Encoder(string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
           return System.Convert.ToBase64String(bytes);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string EncoderByte(byte[] bytes)
        {
            return System.Convert.ToBase64String(bytes);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string Decoder(string str)
        {
            var outputb = System.Convert.FromBase64String(str);
            return Encoding.UTF8.GetString(outputb, 0, outputb.Length);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static byte[] DecoderByte(string str)
        {
            return System.Convert.FromBase64String(str);
        }
    }
}
