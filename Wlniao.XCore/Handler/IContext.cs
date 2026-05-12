/*-----------------------Copyright 2017 www.wlniao.com---------------------------
    文件名称：Wlniao\Handler\IContext.cs
    适用环境：NETCoreCLR 1.0/2.0
    最后修改：2017年12月12日 10:58:50
    功能描述：Wlniao统一约束接口

    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at

               http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.
------------------------------------------------------------------------------*/
namespace Wlniao.Handler
{
    /// <summary>
    /// WlniaoHandler统一上下文接口
    /// </summary>
    public interface IContext
    {
        /// <summary>
        /// 请求方式 GET/POST
        /// </summary>
        public string Method { get; set; }
        /// <summary>
        /// 接口路径
        /// </summary>
        public string ApiPath { get; set; }
        /// <summary>
        /// 请求内容
        /// </summary>
        public object RequestBody { get; set; }
        /// <summary>
        /// 输出内容
        /// </summary>
        public object ResponseBody { get; set; }
    }
}