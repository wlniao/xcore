/*==============================================================================
    �ļ����ƣ�LogLevel.cs
    ���û�����CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    ������������־�ĵȼ�
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

namespace Wlniao.Log
{
    /// <summary>
    /// ��־�ĵȼ�
    /// Fatal>>Error>>Warn>>Debug(SQL)>>Info>>None
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// �޵ȼ�
        /// </summary>
        None = 0,
        /// <summary>
        /// ������Ϣ��־��ͨ�����ڴ�ӡ���������������ݣ�
        /// </summary>
        Debug = 1,
        /// <summary>
        /// ��ͨ��Ϣ��־��ͨ�����ڼ�¼�ӿ�ͨѶԭʼ��־��
        /// </summary>
        Information = 2,
        /// <summary>
        /// ������Ϣ��־
        /// </summary>
        Warning = 3,
        /// <summary>
        /// ������Ϣ��־
        /// </summary>
        Error = 4,
        /// <summary>
        /// ������Ϣ��־
        /// </summary>
        Critical = 5,
    }
}

