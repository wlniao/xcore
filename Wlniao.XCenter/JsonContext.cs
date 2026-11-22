using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace Wlniao.XCenter
{
    /// <summary>
    /// Wlniao.XCenter 自有业务类型的源生成 JSON 元数据上下文（零反射、AOT / 裁剪安全）。
    /// 与 XCore 侧 Wlniao.JsonContext 分工：本上下文负责 XCenter 的业务类型，
    /// 已在 Wlniao.JsonContext 注册的公共类型（string / Dictionary&lt;string,string&gt; / Dictionary&lt;string,object&gt;）直接复用，不重复登记。
    /// 序列化输出统一用 <see cref="Instance"/>（中文不转义）；<see cref="Default"/> 仅用于反序列化等不关心编码器的场景。
    /// </summary>
    [JsonSourceGenerationOptions]
    [JsonSerializable(typeof(Context))]
    [JsonSerializable(typeof(EmiContext))]
    [JsonSerializable(typeof(Models.Enum))]
    [JsonSerializable(typeof(List<Models.Enum>))]
    [JsonSerializable(typeof(Models.Account))]
    [JsonSerializable(typeof(ApiResult<string>))]
    public partial class JsonContext : JsonSerializerContext
    {
        /// <summary>
        /// 本上下文专用序列化选项：中文不转义，与 XCore.JsonSerializerOptions 编码行为一致。
        /// JsonSerializerOptions 一旦被上下文封装即只读，必须在构造上下文之前完成设置。
        /// </summary>
        public static readonly JsonSerializerOptions SerializerOptions = new()
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        };

        /// <summary>
        /// 带中文编码器的上下文实例；响应输出等序列化场景统一走它。
        /// </summary>
        public static JsonContext Instance { get; } = new JsonContext(SerializerOptions);
    }
}
