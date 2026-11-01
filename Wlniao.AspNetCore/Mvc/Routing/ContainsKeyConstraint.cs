/*==============================================================================
    文件名称：ContainsKeyConstraint.cs
    适用环境：.NET 8.0 / .NET 10.0
    功能描述：Mvc特定路径匹配工具
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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
namespace Wlniao.Mvc.Routing
{
    /// <summary>
    /// Mvc特定路径匹配工具
    /// </summary>
    public class ContainsKeyConstraint : IRouteConstraint
    {
        /// <summary>
        /// 允许的取值集合（实例级，避免多个约束实例互相覆盖）
        /// </summary>
        private readonly string[] _keys;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="keys"></param>
        public ContainsKeyConstraint(params string[] keys)
        {
            // 复制传入数组，既不修改调用方数据，也不会被后续实例覆盖
            _keys = keys == null || keys.Length == 0
                ? []
                : [.. keys.Select(k => k?.ToLowerInvariant() ?? string.Empty)];
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="httpContext"></param>
        /// <param name="route"></param>
        /// <param name="routeKey"></param>
        /// <param name="values"></param>
        /// <param name="routeDirection"></param>
        /// <returns></returns>
        public bool Match(HttpContext? httpContext, IRouter? route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection)
        {
            if (_keys.Length == 0 || !values.TryGetValue(routeKey, out var value))
            {
                return false;
            }
            var stringValue = value as string;
            return !string.IsNullOrEmpty(stringValue) && _keys.Contains(stringValue.ToLowerInvariant());
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public class LocalhostConstraint : IRouteConstraint
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="httpContext"></param>
        /// <param name="route"></param>
        /// <param name="routeKey"></param>
        /// <param name="values"></param>
        /// <param name="routeDirection"></param>
        /// <returns></returns>
        public bool Match(HttpContext? httpContext, IRouter? route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection)
        {
            var value = values[routeKey].ToString();
            if (value == "swagger")
            {
                return false;
            }
            return true;
        }
    }
}