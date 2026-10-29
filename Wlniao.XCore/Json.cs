/*==============================================================================
    文件名称：Json.cs
    适用环境：CoreCLR 5.0,.NET Framework 6.0/8.0/10.0
    功能描述：Json序列化、反序列化工具
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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Wlniao
{
    /// <summary>
    /// Json序列化、反序列化工具
    /// </summary>
    public class Json
    {
        private const string AotMessage = "任意类型的 JSON 序列化/反序列化需要运行时反射或动态代码生成，在 NativeAOT/裁剪环境下可能失效。请在 AOT 场景使用接受 JsonTypeInfo<T> 的重载，或基于 source-generated JsonSerializerContext 调用。";

        /// <summary>
        /// 裁剪/AOT 场景下的降级选项（含显式 TypeInfoResolver），按需延迟创建。
        /// </summary>
        private static volatile JsonSerializerOptions? _degradeOptions;

        /// <summary>
        /// 裁剪 / AOT 严格诊断开关，默认 false（不改变任何现有行为）。
        /// </summary>
        /// <remarks>
        /// 背景：裁剪应用里 System.Text.Json 的反射解析器只能"尽力而为"。当目标类型未登记在源生成上下文、
        /// 且其成员已被裁剪时，序列化不会报错，而是静默输出 {} —— 数据被整段丢掉，却看不出任何异常，
        /// 线上极难定位。
        /// 开启本开关后，在反射被禁用的环境（NativeAOT、或 PublishTrimmed 且
        /// JsonSerializerIsReflectionDisabledByDefault=true）中调用反射式重载时，
        /// 会先判断目标类型是否已被源生成上下文覆盖，未覆盖则立即抛 <see cref="NotSupportedException"/>，
        /// 消息里带类型名与迁移指引，把"无声数据丢失"变成"启动即失败"。
        /// 反射可用的普通 CoreCLR 环境不受影响，保持原语义。
        /// </remarks>
        public static bool StrictTrimDiagnostics { get; set; }

        /// <summary>
        /// 判断类型是否已被源生成上下文 <see cref="JsonContext"/> 覆盖。
        /// 被覆盖即代表该类型的序列化/反序列化在裁剪与 AOT 下是完整且安全的。
        /// </summary>
        /// <param name="type">待判断的类型</param>
        /// <returns>已覆盖返回 true；未覆盖返回 false（此时反射兜底可能丢字段）</returns>
        public static bool IsCovered(Type type)
        {
            return type != null && JsonContext.Instance.GetTypeInfo(type) != null;
        }

        /// <summary>
        /// 诊断拦截：反射被禁用时，未覆盖类型直接失败，避免静默退化成 {}。
        /// 开关关闭（默认）或反射可用时为零开销直通。
        /// </summary>
        /// <param name="type">本次序列化/反序列化的目标类型</param>
        /// <param name="member">调用的重载名，用于拼装迁移指引</param>
        private static void EnsureCovered(Type type, string member)
        {
            if (!StrictTrimDiagnostics || JsonSerializer.IsReflectionEnabledByDefault)
            {
                return;
            }
            if (JsonContext.Instance.GetTypeInfo(type) != null)
            {
                return;
            }
            throw new NotSupportedException(
                $"裁剪/AOT 环境下无法安全序列化类型 '{type.FullName ?? type.Name}'（调用 Json.{member}）。"
                + "该类型未被源生成上下文 Wlniao.JsonContext 覆盖，反射兜底会把对象静默序列化成 {} 造成数据丢失。"
                + $"迁移方式：1) 改用 Json.{member}(..., JsonTypeInfo<T>) 重载，传入自有 JsonSerializerContext 生成的元数据；"
                + $"2) 或在 Wlniao.JsonContext 上补 [JsonSerializable(typeof({type.Name}))]；"
                + "3) 匿名类型改为 Dictionary<string, object> 或具名 DTO。"
                + "若确认可接受降级行为，可将 Json.StrictTrimDiagnostics 置回 false。");
        }

        /// <summary>
        /// 为反射式（逃生舱）重载补齐 TypeInfoResolver，避免裁剪/AOT 应用里硬抛 InvalidOperationException。
        /// </summary>
        /// <remarks>
        /// 裁剪应用默认 JsonSerializerIsReflectionDisabledByDefault=true，此时 System.Text.Json 要求
        /// 显式指定 TypeInfoResolver，否则序列/反序列化会在运行时抛
        /// “Reflection-based serialization has been disabled for this application.”。
        /// 降级策略：优先使用源生成上下文 Wlniao.JsonContext（覆盖内置/常用类型，AOT 安全），
        /// 未覆盖的类型回退到反射解析器 DefaultJsonTypeInfoResolver（best-effort：被裁剪的成员可能丢失）。
        /// 常规 CoreCLR 应用（反射可用）下原样返回，保持原有语义且零额外开销。
        /// 注意：缓存的降级选项是 XCore.JsonSerializerOptions 的一次性快照，
        /// 若运行期再修改该静态字段，快照不会同步（裁剪场景下极少见）。
        /// </remarks>
        [RequiresUnreferencedCode(AotMessage)]
        [RequiresDynamicCode(AotMessage)]
        private static JsonSerializerOptions DegradeOptions(JsonSerializerOptions options)
        {
            // 反射仍可用，或调用方已自行指定 resolver：无需降级
            if (JsonSerializer.IsReflectionEnabledByDefault || options.TypeInfoResolver is not null)
            {
                return options;
            }
            if (ReferenceEquals(options, XCore.JsonSerializerOptions))
            {
                // 默认选项走缓存，避免每次调用都克隆 JsonSerializerOptions
                return _degradeOptions ??= CloneWithResolver(options);
            }
            return CloneWithResolver(options);
        }

        /// <summary>
        /// 克隆选项并挂载「源生成优先 + 反射兜底」的混合 TypeInfoResolver。
        /// </summary>
        [RequiresUnreferencedCode(AotMessage)]
        [RequiresDynamicCode(AotMessage)]
        private static JsonSerializerOptions CloneWithResolver(JsonSerializerOptions source)
        {
            IJsonTypeInfoResolver resolver = JsonTypeInfoResolver.Combine(
                Wlniao.JsonContext.Instance,      // 命中即用源生成元数据，AOT 安全
                new DefaultJsonTypeInfoResolver() // 未命中回退到反射，best-effort
            );
            return new JsonSerializerOptions(source) { TypeInfoResolver = resolver };
        }

        /// <summary>
        /// 将 json 字符串反序列化为对象（反射式，非 AOT 安全）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="jsonString">json 字符串</param>
        /// <param name="options">序列化选项</param>
        /// <returns></returns>
        [RequiresUnreferencedCode(AotMessage)]
        [RequiresDynamicCode(AotMessage)]
        public static T Deserialize<T>(string jsonString, JsonSerializerOptions options = null)
        {
            EnsureCovered(typeof(T), nameof(Deserialize));
            return System.Text.Json.JsonSerializer.Deserialize<T>(jsonString, DegradeOptions(options ?? XCore.JsonSerializerOptions));
        }

        /// <summary>
        /// 将 json 字符串反序列化为对象（AOT 安全，需提供 source-generated 元数据）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="jsonString">json 字符串</param>
        /// <param name="jsonTypeInfo">由 JsonSerializerContext 生成的类型元数据</param>
        /// <returns></returns>
        public static T Deserialize<T>(string jsonString, JsonTypeInfo<T> jsonTypeInfo)
        {
            return System.Text.Json.JsonSerializer.Deserialize(jsonString, jsonTypeInfo);
        }

        /// <summary>
        /// 将 json 字符串反序列化为字典对象（AOT 安全，基于 JsonNode 解析，不依赖反射）
        /// </summary>
        /// <param name="jsonString"></param>
        /// <param name="options">保留参数以兼容旧签名，实际解析使用 JsonNode</param>
        /// <returns></returns>
        public static Dictionary<string, object?> DeserializeToDic(string jsonString, JsonSerializerOptions options = null)
        {
            var dict = new Dictionary<string, object?>();
            if (string.IsNullOrEmpty(jsonString))
            {
                return dict;
            }
            var node = JsonNode.Parse(jsonString);
            if (node is JsonObject obj)
            {
                foreach (var kvp in obj)
                {
                    dict[kvp.Key] = JsonNodeToObject(kvp.Value);
                }
            }
            return dict;
        }

        /// <summary>
        /// 将 json 字符串反序列化为对象列表（AOT 安全，需提供 source-generated 元数据）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="jsonString">json 字符串</param>
        /// <param name="jsonTypeInfo">由 JsonSerializerContext 生成的 List&lt;T&gt; 类型元数据</param>
        /// <returns></returns>
        public static List<T> DeserializeToList<T>(string jsonString, JsonTypeInfo<List<T>> jsonTypeInfo)
        {
            return System.Text.Json.JsonSerializer.Deserialize(jsonString, jsonTypeInfo);
        }

        /// <summary>
        /// 将对象序列化为json字符串,支持子对象的序列化（反射式，非 AOT 安全）
        /// </summary>
        /// <param name="obj">序列化的对象</param>
        /// <param name="options">序列化选项</param>
        /// <returns></returns>
        [RequiresUnreferencedCode(AotMessage)]
        [RequiresDynamicCode(AotMessage)]
        public static string Serialize<T>(T obj, JsonSerializerOptions options = null)
        {
            EnsureCovered(typeof(T), nameof(Serialize));
            return JsonSerializer.Serialize<T>(obj, DegradeOptions(options ?? XCore.JsonSerializerOptions));
        }

        /// <summary>
        /// 将对象序列化为json字符串（AOT 安全，需提供 source-generated 元数据）
        /// </summary>
        /// <param name="obj">序列化的对象</param>
        /// <param name="jsonTypeInfo">由 JsonSerializerContext 生成的类型元数据</param>
        /// <returns></returns>
        public static string Serialize<T>(T obj, JsonTypeInfo<T> jsonTypeInfo)
        {
            return JsonSerializer.Serialize(obj, jsonTypeInfo);
        }

        /// <summary>
        /// 将 JsonNode 递归转换为 object 图（AOT 安全，无任何反射）
        /// </summary>
        private static object? JsonNodeToObject(JsonNode? node)
        {
            if (node is null)
            {
                return null;
            }
            if (node is JsonValue val)
            {
                if (val.TryGetValue<string>(out var s)) return s;
                if (val.TryGetValue<long>(out var l)) return l;
                if (val.TryGetValue<double>(out var d)) return d;
                if (val.TryGetValue<bool>(out var b)) return b;
                return val.ToString();
            }
            if (node is JsonObject o)
            {
                var d = new Dictionary<string, object?>();
                foreach (var kvp in o)
                {
                    d[kvp.Key] = JsonNodeToObject(kvp.Value);
                }
                return d;
            }
            if (node is JsonArray arr)
            {
                var list = new List<object?>();
                foreach (var item in arr)
                {
                    list.Add(JsonNodeToObject(item));
                }
                return list;
            }
            return null;
        }

    }
}
