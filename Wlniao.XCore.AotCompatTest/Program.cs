using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Wlniao;
using Wlniao.Caching;

public class Dto
{
    public string? Name { get; set; }
    public int Age { get; set; }
}

public static class Program
{
    public static int Main()
    {
        // 1) 匿名类型序列化（XStorage / XApi 的实际用法，AOT 头号风险）
        var anon = Json.Serialize(new { to = "upyun", host = "h", bucket = "b", policy = "p", signature = "s" });
        Console.WriteLine("anon: " + anon);

        // 2) 具名 DTO 序列化 / 反序列化
        var dtoJson = Json.Serialize(new Dto { Name = "x", Age = 18 });
        var dto = Json.Deserialize<Dto>(dtoJson);
        Console.WriteLine("dto: " + (dto?.Name ?? "null"));

        // 3) 字典反序列化（DeserializeToDic，依赖 UnknownTypeHandling）
        var dic = Json.DeserializeToDic("{\"a\":\"1\",\"b\":2}");
        Console.WriteLine("dic count: " + (dic?.Count ?? -1));

        // 4) 加密器（SM3 / AES / SM2）
        Console.WriteLine("sm3: " + Encryptor.SM3Encrypt("hello"));
        Console.WriteLine("aes: " + Encryptor.AesEncrypt("secret", "1234567890123456", "1234567890123456"));

        // 5) 运行时信息
        XCore.Info();

        // 6) AOT 安全缓存读写（源生成 JsonTypeInfo<T>，无运行时反射）
        var ctx = CacheDemoContext.Default;
        Cache.Set("dto_key", new Dto { Name = "cached", Age = 1 }, ctx.Dto);
        var cached = Cache.Get<Dto>("dto_key", ctx.Dto);
        Console.WriteLine("cached dto: " + (cached?.Name ?? "null"));
        Cache.Del("dto_key");

        // 7) SM2 / SM3 国密算法冒烟测试：任一断言失败，进程以非 0 退出码体现
        var smFailures = SmCryptoSmoke.Run();

        // 8) 自定义 JsonConverter 的正确注册形态（explicit Converters.Add，AOT 安全）
        var converterFailures = ConverterRegistrationDemo.Run();

        // 9) 裁剪/AOT 诊断开关：把静默 {} 变成明确异常
        var diagFailures = JsonTrimDiagnosticsDemo.Run();

        return (smFailures + converterFailures + diagFailures) == 0 ? 0 : 1;
    }
}

/// <summary>
/// 演示：为需要缓存的类型声明源生成的 JsonSerializerContext（AOT 安全）。
/// 说明：若需保留与 XCore.JsonSerializerOptions 一致的中文非转义编码，
/// 应在此声明 [JsonSourceGenerationOptions(Encoder = JavaScriptEncoder.Create(UnicodeRanges.All))]。
/// </summary>
[JsonSourceGenerationOptions]
[JsonSerializable(typeof(Dto))]
internal partial class CacheDemoContext : JsonSerializerContext { }
