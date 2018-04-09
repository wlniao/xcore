/*==============================================================================
    文件名称：NotSerializeAttribute.cs
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
    /// <summary>
    /// 
    /// </summary>
    internal class CharSource
    {
        public char[] charList;
        private int index = -1;
        private String _strSrc;
        public String strSrc { get { return _strSrc; } set { _strSrc = value; } }
        public CharSource(String src)
        {
            //this.strSrc = src;
            this.strSrc = clearComment(src);
            this.charList = this.strSrc.ToCharArray();
        }
        /// <summary>
        /// 清除备注（以“//”开始的行）
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        private String clearComment(String src)
        {
            if (src == null)
            {
                return null;
            }
            String[] arr = src.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            StringBuilder sb = new StringBuilder();
            foreach (String line in arr)
            {
                if (line.Trim().StartsWith("//"))
                {
                    continue;
                }
                sb.Append(line);
                sb.AppendLine();
            }
            return sb.ToString();
        }
        public char getCurrent()
        {
            return charList[index];
        }
        public void move()
        {
            if (index >= charList.Length - 1)
            {
                return;
            }
            index++;
        }
        public void back()
        {
            index--;
        }
        public void moveToText()
        {
            index++;
            char c = this.getCurrent();
            if (c == 0 || c > ' ')
            {
                return;
            }
            moveToText();
        }
        public Boolean isEnd()
        {
            return (index >= charList.Length - 1);
        }
        public int getIndex()
        {
            return this.index;
        }
    }
}
