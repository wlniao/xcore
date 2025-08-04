/*==============================================================================
    �ļ����ƣ�JsonParser.cs
    ���û�����CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    ����������Json �����л�����
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
    /// Json �����л�����
    /// </summary>
    public class JsonParser
    {
        /// <summary>
        /// �����ַ��������ض���
        /// ���� json �Ĳ�ͬ�����ܷ�������(int)����������(bool)���ַ���(string)��һ�����(Dictionary&lt;string, object&gt;)������(List&lt;object&gt;)�Ȳ�ͬ����
        /// </summary>
        /// <param name="src"></param>
        /// <returns>���� json �Ĳ�ͬ�����ܷ�������(int)����������(bool)���ַ���(string)��һ�����(Dictionary&lt;string, object&gt;)������(List&lt;object&gt;)�Ȳ�ͬ����</returns>
        public static object Parse(string src)
        {
            if (string.IsNullOrEmpty(src)) return null;
            return new InitJsonParser(new CharSource(src)).getResult();
        }
    }
}
