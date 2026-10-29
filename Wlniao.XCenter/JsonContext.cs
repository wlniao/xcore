using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Wlniao.XCenter
{
    /// <summary>
    /// Wlniao.XCenter 自有业务类型的源生成 JSON 元数据上下文（零反射、AOT / 裁剪安全）。
    /// 与 XCore 侧 Wlniao.JsonContext 分工：本上下文负责 XCenter 的业务类型，
    /// 已在 Wlniao.JsonContext 注册的公共类型（string / Dictionary&lt;string,string&gt; / Dictionary&lt;string,object&gt;）直接复用，不重复登记。
    /// </summary>
    [JsonSourceGenerationOptions]
    [JsonSerializable(typeof(Context))]
    [JsonSerializable(typeof(EmiContext))]
    [JsonSerializable(typeof(Models.Enum))]
    [JsonSerializable(typeof(List<Models.Enum>))]
    public partial class JsonContext : JsonSerializerContext
    {
    }
}
