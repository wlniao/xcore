/*==============================================================================
    文件名称：PathTool.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：路径辅助工具
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
using System.Collections;
using System.Collections.Generic;
namespace Wlniao.IO
{
    /// <summary>
    /// 路径辅助工具
    /// </summary>
    public class PathTool
    {
        /// <summary>
        /// 自动根据系统追加一级目录
        /// </summary>
        /// <returns></returns>
        public static String JoinPath(String path1, String path2)
        {
            path1 = path1.Replace("\\", "/");
            path2 = path2.Replace("\\", "/");
            var temp = "";
            if (path1.EndsWith("/") || path2.StartsWith("/"))
            {
                temp = path1 + path2;
            }
            else
            {
                temp = path1 + "/" + path2;
            }
            if (Runtime.SysInfo.IsWindows)
            {
                return temp.Replace("//", "\\").Replace("/", "\\");
            }
            else
            {
                return temp.Replace("//", "/").Replace("\\", "/");
            }
        }
        /// <summary>
        /// 将相对路径转换为绝对路径
        /// </summary>
        /// <returns></returns>
        internal static String Map(String path)
        {
            if (strUtil.IsNullOrEmpty(path))
            {
                return XCore.StartupRoot;
            }
            else
            {
                if (path.IndexOf('~') >= 0)
                {
                    path = path.Replace("~", "");
                }
                if (path.IndexOf(':') < 0 && path.IndexOf('/') < 0)
                {
                    path = JoinPath(XCore.StartupRoot, path);
                }
                return path;
            }
        }
        /// <summary>
        /// 将相对路径转换为绝对路径
        /// </summary>
        /// <param name="relativePath">必须是相对路径</param>
        /// <returns>返回绝对路径</returns>
        public static String Map(params String[] relativePath)
        {
            var path = "";
            if (relativePath == null || relativePath.Length == 0)
            {
                //返回程序根目录的绝对路径
                return XCore.StartupRoot;
            }
            else if (relativePath.Length == 1)
            {
                path = relativePath[0];
            }
            else
            {
                path = relativePath[0];
                for (int i = 1; i < relativePath.Length; i++)
                {
                    path = JoinPath(path, relativePath[i]);
                }
            }
            return Map(path);
        }
    }
}