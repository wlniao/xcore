/*==============================================================================
    文件名称：JsonParserException.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：
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

    internal class JsonParserException : Exception
    {

        private String msg;

        public JsonParserException()
        {
        }

        public JsonParserException(String msg)
        {
            this.msg = msg;
        }

        public JsonParserException(String msg, Exception inner)
            : base(msg, inner)
        {
        }

        public override String Message
        {
            get
            {
                return base.Message + Environment.NewLine + this.msg;
            }
        }


        public override String ToString()
        {
            return base.ToString() + Environment.NewLine + this.msg;
        }

    }

}
