/*==============================================================================
    文件名称：CacheData.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：缓存数据模型
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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Wlniao.Caching
{
    /// <summary>
    /// 
    /// </summary>
    public class CacheData
    {
        /// <summary>
        /// 
        /// </summary>
        public DateTime Expire { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public String Value { get; set; }
    }
}
