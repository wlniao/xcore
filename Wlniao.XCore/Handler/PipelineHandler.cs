/*-----------------------Copyright 2017 www.wlniao.com---------------------------
    文件名称：Wlniao\Handler\PipelineHandler.cs
    适用环境：NETCoreCLR 1.0/2.0
    最后修改：2017年12月11日 02:58:50
    功能描述：管道处理接口

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
    /// WlniaoHandler管道处理
    /// </summary>
    public abstract class PipelineHandler
    {
        /// <summary>
        /// 管道PipelineHandler实例
        /// </summary>
        protected PipelineHandler inner { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public PipelineHandler()
        {
            inner = null;
        }
        /// <summary>
        /// 内部传递的Handler实例
        /// </summary>
        /// <param name="handler"></param>
        public PipelineHandler(PipelineHandler handler)
        {
            inner = handler;
        }

        /// <summary>
        /// 执行前调用
        /// </summary>
        /// <param name="ctx"></param>
        public abstract void HandleBefore(IContext ctx);
        /// <summary>
        /// 执行后调用
        /// </summary>
        /// <param name="ctx"></param>
        public abstract void HandleAfter(IContext ctx);
    }
}