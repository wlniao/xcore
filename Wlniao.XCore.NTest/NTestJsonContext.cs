using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

/// <summary>
/// NTest 使用的源生成 JSON 元数据上下文（AOT 安全）。
/// 声明 XApi.Request 调用所需的请求体类型与响应体基础类型。
/// 置于全局命名空间以避免与 Wlniao.XCore.XCore 类型产生命名空间冲突（CS0435）。
/// </summary>
[JsonSourceGenerationOptions]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(string))]
internal partial class NTestJsonContext : JsonSerializerContext
{
}
