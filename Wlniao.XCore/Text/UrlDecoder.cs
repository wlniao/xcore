/*==============================================================================
    文件名称：UrlDecoder.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：Url编码解码工具
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

namespace Wlniao.Text
{
    /// <summary>
    /// Url编码解码工具
    /// </summary>
    internal class UrlDecoder
    {
        // Fields
        private int _bufferSize;
        private byte[] _byteBuffer;
        private char[] _charBuffer;
        private System.Text.Encoding _encoding;
        private int _numBytes;
        private int _numChars;

        // Methods
        internal UrlDecoder(int bufferSize, System.Text.Encoding encoding)
        {
            this._bufferSize = bufferSize;
            this._encoding = encoding;
            this._charBuffer = new char[bufferSize];
        }

        internal void AddByte(byte b)
        {
            this._byteBuffer ??= new byte[this._bufferSize];
            var index = this._numBytes;
            this._numBytes = index + 1;
            this._byteBuffer[index] = b;
        }

        internal void AddChar(char ch)
        {
            if (this._numBytes > 0)
            {
                this.FlushBytes();
            }
            var index = this._numChars;
            this._numChars = index + 1;
            this._charBuffer[index] = ch;
        }

        private void FlushBytes()
        {
            if (this._numBytes <= 0)
            {
                return;
            }
            this._numChars += this._encoding.GetChars(this._byteBuffer, 0, this._numBytes, this._charBuffer, this._numChars);
            this._numBytes = 0;
        }

        internal string GetString()
        {
            if (this._numBytes > 0)
            {
                this.FlushBytes();
            }
            return this._numChars > 0 ? new string(this._charBuffer, 0, this._numChars) : string.Empty;
        }
    }
}
