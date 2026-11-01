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

using Wlniao.Text;

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
        public static string JoinPath(string path1, string path2)
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
        /// <remarks>
        /// 注意：本重载与 public 的 <c>Map(params string[])</c> 语义并不完全一致
        /// （本重载会额外剥掉 '~' 前缀），刻意保留 internal 可见性，
        /// 避免 <c>Map("~/x")</c> 从 params 重载改判到本重载。
        /// </remarks>
        /// <param name="path">相对路径，支持以 ~ 开头的写法</param>
        /// <returns>返回绝对路径；入参为空时返回程序根目录</returns>
        internal static string Map(string path)
        {
            if (StringUtil.IsNullOrEmpty(path))
            {
                return XCore.StartupRoot;
            }
            path = TrimHomePrefix(path);
            if (path.Length == 0)
            {
                return XCore.StartupRoot;
            }
            if (path.IndexOf(':') < 0 && path[0] != '/')
            {
                path = JoinPath(XCore.StartupRoot, path);
            }
            return path;
        }
        /// <summary>
        /// 剥掉路径开头的 '~'（表示程序根目录）：'~/a' → 'a'，'~' → 空串。
        /// </summary>
        /// <remarks>
        /// 只处理开头：路径中间的 '~' 是普通字符（如 <c>/tmp/~backup/a.txt</c>），
        /// 对它做整体替换会悄悄改变目标路径。
        /// </remarks>
        /// <param name="path">待处理的路径</param>
        /// <returns>去掉 '~' 前缀后的路径</returns>
        private static string TrimHomePrefix(string path)
        {
            if (StringUtil.IsNullOrEmpty(path) || path[0] != '~')
            {
                return path;
            }
            return path.Length > 1 && (path[1] == '/' || path[1] == '\\')
                ? path.Substring(2)
                : path.Substring(1);
        }
        /// <summary>
        /// 将相对路径转换为绝对路径
        /// </summary>
        /// <param name="relativePath">必须是相对路径</param>
        /// <returns>返回绝对路径</returns>
        public static string Map(params string[] relativePath)
        {
            var path = "";
            if (relativePath == null || relativePath.Length == 0)
            {
                //返回程序根目录的绝对路径
                return XCore.StartupRoot;
            }
            else
            {
                // '~' 必须在拼接之前剥掉：先拼接会让 '~' 混进路径中间，后续再也无法识别为根目录
                path = TrimHomePrefix(relativePath[0] ?? "");
                if (path.Length == 0)
                {
                    path = XCore.StartupRoot;
                }
                else if (path.IndexOf(':') < 0 && path[0] != '/')
                {
                    path = JoinPath(XCore.StartupRoot, path);
                }
                for (var i = 1; i < relativePath.Length; i++)
                {
                    path = JoinPath(path, relativePath[i]);
                }
            }
            return Map(path);
        }
    }
}