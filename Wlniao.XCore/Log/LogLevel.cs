/*==============================================================================
    文件名称：LogLevel.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：日志的等级
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
using System;
namespace Wlniao.Log
{
    /// <summary>
    /// 日志的等级
    /// Fatal>>Error>>Warn>>Debug(SQL)>>Info>>None
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// 无等级
        /// </summary>
        None = 0,
        /// <summary>
        /// 调试信息日志（通常用于打印开发调试明文内容）
        /// </summary>
        Debug = 1,
        /// <summary>
        /// 普通信息日志（通常用于记录接口通讯原始日志）
        /// </summary>
        Information = 2,
        /// <summary>
        /// 警告信息日志
        /// </summary>
        Warning = 3,
        /// <summary>
        /// 错误信息日志
        /// </summary>
        Error = 4,
        /// <summary>
        /// 崩溃信息日志
        /// </summary>
        Critical = 5,
    }
}

