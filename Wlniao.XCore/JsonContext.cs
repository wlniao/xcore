/*==============================================================================
    文件名称：JsonContext.cs
    适用环境：CoreCLR (.NET 8 / .NET 10, NativeAOT)
    功能描述：Wlniao.XCore 内置类型与常用类型的源生成 JSON 元数据上下文（零反射、AOT 安全）
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

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wlniao
{
    /// <summary>
    /// Wlniao.XCore 内置类型与常用类型的源生成 JSON 序列化/反序列化元数据上下文。
    /// 用于零反射（AOT / 裁剪安全）场景：调用方通过 <c>JsonContext.Instance.Xxx</c>
    /// 取得 <c>JsonTypeInfo&lt;T&gt;</c> 后传给 System.Text.Json 的 Serialize/Deserialize 重载。
    /// 生成的属性名规则：<c>Dictionary&lt;string,string&gt;</c> → <c>DictionaryStringString</c>，
    /// <c>Result&lt;int&gt;</c> → <c>ResultInt32</c>，<c>object</c> → <c>Object</c>，以此类推。
    /// 运行时类型（object / 匿名类型）的序列化仍走 Wlniao.Json 的逃生舱重载。
    /// </summary>
    [JsonSourceGenerationOptions]
    [JsonSerializable(typeof(Dictionary<string, string>))]
    [JsonSerializable(typeof(Dictionary<string, object>))]
    [JsonSerializable(typeof(Dictionary<string, System.Text.Json.JsonElement>))]
    [JsonSerializable(typeof(List<Dictionary<string, string>>))]
    [JsonSerializable(typeof(List<Dictionary<string, object>>))]
    [JsonSerializable(typeof(List<Dictionary<string, System.Text.Json.JsonElement>>))]
    [JsonSerializable(typeof(List<object>))]
    [JsonSerializable(typeof(object))]
    [JsonSerializable(typeof(string))]
    [JsonSerializable(typeof(int))]
    [JsonSerializable(typeof(long))]
    [JsonSerializable(typeof(bool))]
    [JsonSerializable(typeof(System.DateTime))]
    [JsonSerializable(typeof(Result<object>))]
    [JsonSerializable(typeof(Result<int>))]
    [JsonSerializable(typeof(Result<long>))]
    [JsonSerializable(typeof(Result<bool>))]
    [JsonSerializable(typeof(Result<string>))]
    [JsonSerializable(typeof(Result<Dictionary<string, string>>))]
    [JsonSerializable(typeof(Result<Dictionary<string, object>>))]
    [JsonSerializable(typeof(Result<Dictionary<string, System.Text.Json.JsonElement>>))]
    [JsonSerializable(typeof(Result<List<Dictionary<string, object>>>))]
    [JsonSerializable(typeof(Result<List<Dictionary<string, string>>>))]
    [JsonSerializable(typeof(Result<List<Dictionary<string, System.Text.Json.JsonElement>>>))]
    public partial class JsonContext : JsonSerializerContext
    {
        /// <summary>
        /// 本上下文专用的序列化选项，行为对齐 XCore.JsonSerializerOptions：中文不被转义、object 反序列化为 JsonNode。
        /// 注意：JsonSerializerOptions 一旦被 JsonSerializerContext 封装即变为只读，因此这些设置必须在构造上下文之前完成；
        /// 早先用静态构造去改 Default.Options 的做法会抛 InvalidOperationException（SerializerOptionsReadOnly）。
        /// </summary>
        public static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
        {
            Encoder = XCore.JsonSerializerOptions.Encoder,
            UnknownTypeHandling = JsonUnknownTypeHandling.JsonNode
        };

        /// <summary>
        /// 使用 <see cref="SerializerOptions"/> 构造的上下文实例。业务代码应统一使用它替代 Default，
        /// 以在零反射（AOT/裁剪安全）的前提下获得与 XCore.JsonSerializerOptions 一致的编码行为。
        /// </summary>
        public static JsonContext Instance { get; } = new JsonContext(SerializerOptions);
    }
}
