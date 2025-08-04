/*==============================================================================
    �ļ����ƣ�ValueJsonParser.cs
    ���û�����CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    ����������ֵ����ת��
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

===============================================================================*/
using System;
using System.Collections.Generic;
using System.Text;

namespace Wlniao.Serialization
{
    /// <summary>
    /// ֵ����ת��
    /// </summary>
    internal class ValueJsonParser : JsonParserBase
    {
        protected StringBuilder sb = new StringBuilder();
        private static readonly string separatorChars = ",\":[]{}";

        private object _result;

        public override object getResult()
        {
            return _result;
        }

        public ValueJsonParser()
        {
        }

        public ValueJsonParser(CharSource charSrc) : base(charSrc)
        {
        }
        protected override void parse()
        {
            paserOne();
            charSrc.back();
            string s = sb.ToString().Trim();
            if (s.Equals(""))
            {
                throw ex("value is empty");
            }
            _result = getStringValue(s);
        }
        private void paserOne()
        {
            char c = charSrc.getCurrent();
            if (c >= ' ' && separatorChars.IndexOf(c) < 0)
            {
                if (c == '\\')
                {
                    processEscape();
                }
                else
                {
                    sb.Append(c);
                }
                if (charSrc.isEnd())
                {
                    return;
                }
                charSrc.move();
                paserOne();
            }
        }

        protected void processEscape()
        {
            charSrc.move();
            char c = charSrc.getCurrent();
            if (c == 'b')
            {
                sb.Append('\b');
            }
            else if (c == 't')
            {
                sb.Append('\t');
            }
            else if (c == 'n')
            {
                sb.Append('\n');
            }
            else if (c == 'n')
            {
                sb.Append('\n');
            }
            else if (c == 'r')
            {
                sb.Append('\r');
            }
            else if (c == 'u')
            {
                // sb.Append(''); // TODO 
            }
            else if (c == '"' || c == '\'' || c == '/' || c == '\\')
            {
                sb.Append(c);
            }
            else
            {
                throw ex("not a valid escape character");
            }
        }

        private static object getStringValue(string s)
        {
            int i = 0;
            if (int.TryParse(s, out i))
            {
                return i;
            }
            decimal d = 0;
            if (decimal.TryParse(s, out d))
            {
                return d;
            }
            bool b = false;
            if (bool.TryParse(s, out b))
            {
                return b;
            }
            return s;
        }
    }
}