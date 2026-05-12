/*==============================================================================
    文件名称：ContainsKeyConstraint.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
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
        private static string[] _keys = null;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="keys"></param>
        public ContainsKeyConstraint(params string[] keys)
        {
            _keys = keys;
            for(var i = 0; i < _keys.Length; i++)
            {
                _keys[i] = _keys[i].ToLower();
            }
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
            if (_keys != null && _keys.Length > 0 && values.ContainsKey(routeKey))
            {
                var stringValue = values[routeKey] as string;
                if (!string.IsNullOrEmpty(stringValue))
                {
                    return _keys.Contains<string>(stringValue.ToLower());
                }
            }
            return false;
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