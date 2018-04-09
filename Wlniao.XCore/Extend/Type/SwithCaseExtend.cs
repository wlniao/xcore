/*==============================================================================
    文件名称：SwithCaseExtend.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：switch/case组扩展
================================================================================

示例：
    string typeName = string.Empty;
        typeId.Switch((string s) => typeName = s)
            .Case(1, "男")
            .Case(2, "女")
            .Default("未知");

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
using System.Linq;
using System.Linq.Expressions;
namespace Wlniao
{
    /// <summary>
    /// switch/case组扩展
    /// </summary>
    public static class SwithCaseExtend
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TCase"></typeparam>
        /// <typeparam name="TOther"></typeparam>
        public class SwithCase<TCase, TOther>
        {
            /// <summary>
            /// 
            /// </summary>
            /// <param name="value"></param>
            /// <param name="action"></param>
            public SwithCase(TCase value, Action<TOther> action)
            {
                Value = value;
                Action = action;
            }
            /// <summary>
            /// 
            /// </summary>
            public TCase Value { get; private set; }
            /// <summary>
            /// 
            /// </summary>
            public Action<TOther> Action { get; private set; }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TCase"></typeparam>
        /// <typeparam name="TOther"></typeparam>
        /// <param name="t"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static SwithCase<TCase, TOther> Switch<TCase, TOther>(this TCase t, Action<TOther> action) where TCase : IEquatable<TCase>
        {
            return new SwithCase<TCase, TOther>(t, action);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TInput"></typeparam>
        /// <typeparam name="TCase"></typeparam>
        /// <typeparam name="TOther"></typeparam>
        /// <param name="t"></param>
        /// <param name="selector"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static SwithCase<TCase, TOther> Switch<TInput, TCase, TOther>(this TInput t, Func<TInput, TCase> selector, Action<TOther> action) where TCase : IEquatable<TCase>
        {
            return new SwithCase<TCase, TOther>(selector(t), action);
        }  
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TCase"></typeparam>
        /// <typeparam name="TOther"></typeparam>
        /// <param name="sc"></param>
        /// <param name="option"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static SwithCase<TCase, TOther> Case<TCase, TOther>(this SwithCase<TCase, TOther> sc, TCase option, TOther other) where TCase : IEquatable<TCase>
        {
            return Case(sc, option, other, true);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TCase"></typeparam>
        /// <typeparam name="TOther"></typeparam>
        /// <param name="sc"></param>
        /// <param name="option"></param>
        /// <param name="other"></param>
        /// <param name="bBreak"></param>
        /// <returns></returns>
        public static SwithCase<TCase, TOther> Case<TCase, TOther>(this SwithCase<TCase, TOther> sc, TCase option, TOther other, bool bBreak) where TCase : IEquatable<TCase>
        {
            return Case(sc, c => c.Equals(option), other, bBreak);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TCase"></typeparam>
        /// <typeparam name="TOther"></typeparam>
        /// <param name="sc"></param>
        /// <param name="predict"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static SwithCase<TCase, TOther> Case<TCase, TOther>(this SwithCase<TCase, TOther> sc, Predicate<TCase> predict, TOther other) where TCase : IEquatable<TCase>
        {
            return Case(sc, predict, other, true);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TCase"></typeparam>
        /// <typeparam name="TOther"></typeparam>
        /// <param name="sc"></param>
        /// <param name="predict"></param>
        /// <param name="other"></param>
        /// <param name="bBreak"></param>
        /// <returns></returns>
        public static SwithCase<TCase, TOther> Case<TCase, TOther>(this SwithCase<TCase, TOther> sc, Predicate<TCase> predict, TOther other, bool bBreak) where TCase : IEquatable<TCase>
        {
            if (sc == null) return null;
            if (predict(sc.Value))
            {
                sc.Action(other);
                return bBreak ? null : sc;
            }
            else return sc;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TCase"></typeparam>
        /// <typeparam name="TOther"></typeparam>
        /// <param name="sc"></param>
        /// <param name="other"></param>
        public static void Default<TCase, TOther>(this SwithCase<TCase, TOther> sc, TOther other)
        {
            if (sc == null) return;
            sc.Action(other);
        }
    }
}