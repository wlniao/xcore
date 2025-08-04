/*==============================================================================
    �ļ����ƣ�InitJsonParser.cs
    ���û�����CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    ����������Int���͵�ת��
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
    /// Int���͵�ת��
    /// </summary>
    internal class InitJsonParser : JsonParserBase
    {
        private object _result;
        public InitJsonParser(CharSource charSrc) : base(charSrc)
        {
        }
        protected override void parse()
        {
            _result = moveAndGetParser().getResult();
        }
        public override object getResult()
        {
            return _result;
        }
    }
}
