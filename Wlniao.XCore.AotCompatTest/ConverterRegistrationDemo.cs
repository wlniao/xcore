using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Wlniao;

/// <summary>
/// 自定义 JsonConverter 的推荐注册形态演示：options.Converters.Add(new X())，
/// 而不是给类型/属性打 [JsonConverter(typeof(X))]。
///
/// 为什么显式注册更稳（三条实测结论）：
///   1. 裁剪可见性：new MoneyConverter() 是静态可见的引用，ILC 一定保留该类型及其构造器；
///      [JsonConverter(typeof(X))] 里的 typeof(X) 只是个类型 token，在走反射解析器
///      （DefaultJsonTypeInfoResolver）时还要靠 Activator.CreateInstance 实例化，
///      那属于 RequiresDynamicCode，NativeAOT 下不可用。
///   2. 作用域可控：attribute 是类型级、全局生效的；显式注册只作用于这一套 JsonSerializerOptions，
///      同一个 DateTime 在 A 上下文里可以是 "yyyy-MM-dd"，在 B 上下文里保持默认 ISO 格式。
///      本文件的第 4 组断言就是拿这个差异当证据。
///   3. 第三方类型可改：框架内置类型、NuGet 里的类型你加不了 attribute，但可以注册 converter。
///
/// 时序约束：Converters.Add 必须发生在 new InvoiceContext(options) 之前。
/// JsonSerializerOptions 一旦被 JsonSerializerContext 封装即变只读，之后 Add 会抛
/// InvalidOperationException(SerializerOptionsReadOnly)。
/// </summary>
public static class ConverterRegistrationDemo
{
    private static int _total;
    private static int _failed;

    private static void Check(string name, bool ok, string? detail = null)
    {
        _total++;
        if (ok)
        {
            Console.WriteLine("  [PASS] " + name);
        }
        else
        {
            _failed++;
            Console.WriteLine("  [FAIL] " + name + (detail == null ? string.Empty : " -> " + detail));
        }
    }

    public static int Run()
    {
        Console.WriteLine("自定义 JsonConverter 注册（explicit Converters.Add）");

        var invoice = new Invoice
        {
            Id = "INV-1",
            Total = new Money(12.34m, "CNY"),
            IssuedAt = new DateTime(2026, 10, 30)
        };

        // 1) 显式注册：converter 生效，金额与日期都被改写为自定义格式
        var json = JsonSerializer.Serialize(invoice, InvoiceContext.Instance.Invoice);
        Console.WriteLine("    json: " + json);
        Check("显式注册：金额格式化为 'CNY 12.34'", json.Contains("\"CNY 12.34\"", StringComparison.Ordinal), json);
        Check("显式注册：日期裁剪为 yyyy-MM-dd", json.Contains("2026-10-30", StringComparison.Ordinal)
            && !json.Contains("T00:00:00", StringComparison.Ordinal), json);

        // 2) 往返一致：converter 的 Read 也必须正确，否则只是"能写不能读"
        var back = JsonSerializer.Deserialize(json, InvoiceContext.Instance.Invoice);
        Check("显式注册：反序列化金额一致",
            back != null && back.Total.Amount == 12.34m && back.Total.Currency == "CNY",
            back == null ? "null" : back.Total.ToString());
        Check("显式注册：反序列化日期一致",
            back != null && back.IssuedAt == new DateTime(2026, 10, 30),
            back == null ? "null" : back.IssuedAt.ToString("O", CultureInfo.InvariantCulture));

        // 3) 对照：同一 DTO、不同上下文，未注册 converter 时回到默认行为。
        //    这证明输出差异确实来自 Converters.Add，而不是别的因素。
        var plain = JsonSerializer.Serialize(invoice, PlainInvoiceContext.Default.Invoice);
        Console.WriteLine("    plain: " + plain);
        Check("未注册的上下文输出默认 ISO 日期格式",
            plain.Contains("2026-10-30T00:00:00", StringComparison.Ordinal), plain);

        // 4) 对照：attribute 形态。在源生成路径下它同样能跑通（生成器会把 new MoneyConverter() 写进代码），
        //    但它做不到第 3 组那样的"同类型不同上下文、不同格式"，也无法作用于第三方类型。
        var attrJson = JsonSerializer.Serialize(
            new InvoiceByAttribute { Id = "INV-2", Total = new Money(12.34m, "CNY"), IssuedAt = new DateTime(2026, 10, 30) },
            InvoiceAttributeContext.Default.InvoiceByAttribute);
        Console.WriteLine("    attr: " + attrJson);
        Check("attribute 形态金额输出与显式注册一致",
            attrJson.Contains("\"CNY 12.34\"", StringComparison.Ordinal), attrJson);
        Check("attribute 无法作用到同期 DateTime（仍是默认格式）",
            attrJson.Contains("2026-10-30T00:00:00", StringComparison.Ordinal), attrJson);

        // 5) 时序约束：context 构造之后再 Add 必然抛，提示调用方把注册提前
        var threw = false;
        try
        {
            InvoiceContext.Instance.Options.Converters.Add(new MoneyConverter());
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }
        Check("context 构造后 Options 只读，追加 converter 会抛", threw);

        Console.WriteLine($"  converter 用例：{_total - _failed}/{_total} 通过");
        return _failed;
    }
}

/// <summary>
/// 金额值类型。本身没有任何 JSON 特性，序列化格式完全由注册的 converter 决定。
/// </summary>
public readonly struct Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = string.IsNullOrEmpty(currency) ? "CNY" : currency;
    }

    public override string ToString()
    {
        return Currency + " " + Amount.ToString("0.00", CultureInfo.InvariantCulture);
    }
}

/// <summary>
/// 把 Money 写成单个字符串 "CNY 12.34"，避免金额以浮点对象在前后端之间传递时出现精度与格式漂移。
/// 本类型通过 new MoneyConverter() 被静态引用，裁剪器一定能看到，不存在被裁掉的风险。
/// </summary>
public sealed class MoneyConverter : JsonConverter<Money>
{
    public override Money Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return default;
        }
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Money 期望字符串，实际为 " + reader.TokenType);
        }
        var raw = reader.GetString();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return default;
        }
        var idx = raw!.IndexOf(' ');
        if (idx <= 0)
        {
            throw new JsonException("Money 格式应为 'CNY 12.34'，实际为 '" + raw + "'");
        }
        var currency = raw.Substring(0, idx);
        var amount = decimal.Parse(raw.AsSpan(idx + 1), NumberStyles.Number, CultureInfo.InvariantCulture);
        return new Money(amount, currency);
    }

    public override void Write(Utf8JsonWriter writer, Money value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

/// <summary>
/// 只在本上下文内生效的日期格式 converter。attribute 做不到这种"按上下文覆写，不影响别处"。
/// </summary>
public sealed class IsoDateConverter : JsonConverter<DateTime>
{
    private const string Format = "yyyy-MM-dd";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return default;
        }
        var raw = reader.GetString();
        return DateTime.ParseExact(raw!, Format, CultureInfo.InvariantCulture, DateTimeStyles.None);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(Format, CultureInfo.InvariantCulture));
    }
}

public class Invoice
{
    public string? Id { get; set; }
    public Money Total { get; set; }
    public DateTime IssuedAt { get; set; }
}

public class InvoiceByAttribute
{
    public string? Id { get; set; }

    /// <summary>对照项：converter 挂在属性上，只影响该成员。</summary>
    [JsonConverter(typeof(MoneyConverter))]
    public Money Total { get; set; }

    public DateTime IssuedAt { get; set; }
}

/// <summary>
/// 推荐形态。注意静态初始化顺序：SerializerOptions 必须声明在 Instance 之前，
/// 且两个 converter 都要在 new InvoiceContext(...) 之前注册完毕。
/// </summary>
[JsonSerializable(typeof(Invoice))]
public sealed partial class InvoiceContext : JsonSerializerContext
{
    public static readonly JsonSerializerOptions SerializerOptions = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            Encoder = XCore.JsonSerializerOptions.Encoder
        };
        options.Converters.Add(new MoneyConverter());
        options.Converters.Add(new IsoDateConverter());
        return options;
    }

    public static InvoiceContext Instance { get; } = new InvoiceContext(SerializerOptions);
}

/// <summary>对照用：不注册任何 converter，输出默认格式。</summary>
[JsonSerializable(typeof(Invoice))]
public sealed partial class PlainInvoiceContext : JsonSerializerContext
{
}

/// <summary>对照用：靠 [JsonConverter] 特性驱动。</summary>
[JsonSerializable(typeof(InvoiceByAttribute))]
public sealed partial class InvoiceAttributeContext : JsonSerializerContext
{
}
