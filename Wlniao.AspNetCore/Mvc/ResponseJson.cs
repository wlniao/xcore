/*==============================================================================

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
using System.Text.Json;

namespace Wlniao.Mvc
{
    /// <summary>
    /// HTTP 响应负载序列化入口：按运行时类型优先命中 <see cref="Wlniao.JsonContext"/> 源生成元数据，
    /// 反射可用时兜底反射，反射禁用的 AOT/裁剪环境对未登记类型立即抛 NotSupportedException
    /// （不再静默输出 {}）。XCoreController / XApiController / Engine 输出与 XCenter 令牌接口共用。
    /// </summary>
    public static class ResponseJson
    {
        /// <summary>
        /// 序列化响应负载。
        /// </summary>
        /// <param name="obj">负载对象；null 输出字面量 null</param>
        /// <param name="options">自定义选项；与 <see cref="Wlniao.XCore.JsonSerializerOptions"/> 不同时走反射路径以保持覆盖语义</param>
        public static string Serialize(object? obj, JsonSerializerOptions? options = null)
        {
            if (obj is null)
            {
                return "null";
            }
            // Context.JsonSerializerOptions 允许业务方覆盖；与默认选项不同时走反射路径以保持选项语义
            if (options != null && !ReferenceEquals(options, Wlniao.XCore.JsonSerializerOptions))
            {
                return SerializeWithCustomOptions(obj, obj.GetType(), options);
            }
            // 反射可用：源生成命中优先（更快、选项一致），嵌套 object 成员解析失败时回退反射，
            // 避免「已登记根类型 + 未登记匿名/DTO」在 CoreCLR 下被源生成解析器直接抛错。
            var typeInfo = Wlniao.JsonContext.Instance.GetTypeInfo(obj.GetType());
            if (typeInfo != null)
            {
                try
                {
                    return JsonSerializer.Serialize(obj, typeInfo);
                }
                catch (NotSupportedException) when (JsonSerializer.IsReflectionEnabledByDefault)
                {
                    // 嵌套未登记类型：落到下方反射路径（与旧 Wlniao.Json.Serialize 语义一致）
                }
            }
            // 反射禁用（AOT/裁剪）：源生成未覆盖立即失败，避免静默输出 {}
            if (!JsonSerializer.IsReflectionEnabledByDefault)
            {
                throw new NotSupportedException(
                    $"裁剪/AOT 环境下无法安全序列化类型 '{obj.GetType().FullName}'（HTTP 响应负载）。"
                    + "该类型未被 Wlniao.JsonContext 覆盖；请改用具名 DTO + [JsonSerializable] 上下文后传 JsonTypeInfo。");
            }
            return SerializeWithReflection(obj);
        }

        [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026", Justification = "仅在 JsonSerializer.IsReflectionEnabledByDefault 为 true 的分支调用；反射被禁用的 AOT 环境在调用前已抛出 NotSupportedException")]
        [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("ReflectionAnalysis", "IL3050", Justification = "仅在 JsonSerializer.IsReflectionEnabledByDefault 为 true 的分支调用；反射被禁用的 AOT 环境在调用前已抛出 NotSupportedException")]
        private static string SerializeWithReflection(object obj)
        {
            return JsonSerializer.Serialize(obj, Wlniao.XCore.JsonSerializerOptions);
        }

        [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026", Justification = "自定义 JsonSerializerOptions 需要按运行时类型反射序列化，以保持业务方可覆盖输出格式")]
        [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("ReflectionAnalysis", "IL3050", Justification = "自定义 JsonSerializerOptions 需要按运行时类型反射序列化，以保持业务方可覆盖输出格式")]
        private static string SerializeWithCustomOptions(object obj, Type type, JsonSerializerOptions options)
        {
            return JsonSerializer.Serialize(obj, type, options);
        }
    }
}
