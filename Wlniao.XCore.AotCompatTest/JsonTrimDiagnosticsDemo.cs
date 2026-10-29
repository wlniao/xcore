using System;
using System.Collections.Generic;
using System.Text.Json;
using Wlniao;

/// <summary>
/// Json.StrictTrimDiagnostics 诊断开关的行为验证。
///
/// 开关默认关闭，关闭时与原行为完全一致；开启后，仅在反射被禁用的环境
/// （NativeAOT / PublishTrimmed 且 JsonSerializerIsReflectionDisabledByDefault=true）中，
/// 对未被 Wlniao.JsonContext 覆盖的类型立即抛 NotSupportedException，
/// 而不是让 DefaultJsonTypeInfoResolver 把对象静默序列化成 {}。
///
/// 在普通 CoreCLR（反射可用）下开关不介入，相关断言标记为 SKIP —— 这是设计使然，
/// 不是测试缺失。要在这台机器上验证拦截效果，请运行 NativeAOT 产物。
/// </summary>
public static class JsonTrimDiagnosticsDemo
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
        Console.WriteLine("Json.StrictTrimDiagnostics 诊断开关");

        // 1) 覆盖判定本身与运行环境无关，可在托管下直接验证。
        //    注意：GetTypeInfo 会强制物化元数据，所以这几行同时是 Result<T> 值类型实例化的回归检查
        //    （历史缺陷：Data 标 WhenWritingNull，Result<int>/<long>/<bool> 一物化就抛）。
        Check("已覆盖类型：Dictionary<string,string>", Json.IsCovered(typeof(Dictionary<string, string>)));
        Check("已覆盖类型：Result<int>", Json.IsCovered(typeof(Result<int>)));
        Check("已覆盖类型：Result<long>", Json.IsCovered(typeof(Result<long>)));
        Check("已覆盖类型：Result<bool>", Json.IsCovered(typeof(Result<bool>)));
        Check("已覆盖类型：Result<object>", Json.IsCovered(typeof(Result<object>)));
        Check("未覆盖类型：UncoveredPayload", !Json.IsCovered(typeof(UncoveredPayload)));

        // 2) 开关默认值
        Check("开关默认关闭", !Json.StrictTrimDiagnostics);

        // 3) 框架异常消息保持可读：UseSystemResourceKeys 已从 csproj 移除。
        //    若有人把它加回来，这里会退化成 "Format_InvalidString" 这类裸键名，
        //    而正是这种裸键让上一轮的 EdbNeeded 异常难以排查。
        string? formatError = null;
        try
        {
            _ = int.Parse("abc");
        }
        catch (FormatException e)
        {
            formatError = e.Message;
        }
        Console.WriteLine("    FormatException: " + formatError);
        Check("框架异常消息为可读文本而非资源键",
            formatError != null
            && !formatError.Contains("Format_InvalidString", StringComparison.Ordinal)
            && formatError.IndexOf(' ') > 0,
            formatError);

        var reflectionDisabled = !JsonSerializer.IsReflectionEnabledByDefault;
        if (!reflectionDisabled)
        {
            Console.WriteLine("  [SKIP] 当前为反射可用的托管环境，诊断开关按设计不介入");
            Console.WriteLine($"  诊断用例：{_total - _failed}/{_total} 通过（拦截断言已跳过）");
            return _failed;
        }

        var previous = Json.StrictTrimDiagnostics;
        try
        {
            Json.StrictTrimDiagnostics = true;

            // 3) 未覆盖类型：必须立刻失败，而不是输出 {}
            string? message = null;
            try
            {
                Json.Serialize(new UncoveredPayload { Name = "x", Amount = 1 });
            }
            catch (NotSupportedException e)
            {
                message = e.Message;
            }
            Check("未覆盖类型被拦截并抛 NotSupportedException", message != null,
                message == null ? "未抛出，说明发生了静默降级" : message);
            Check("异常消息包含类型名", message != null && message.Contains("UncoveredPayload", StringComparison.Ordinal));
            Check("异常消息包含迁移指引", message != null && message.Contains("JsonTypeInfo<T>", StringComparison.Ordinal));

            // 4) 反序列化侧同样拦截
            string? readMessage = null;
            try
            {
                Json.Deserialize<UncoveredPayload>("{\"Name\":\"x\"}");
            }
            catch (NotSupportedException e)
            {
                readMessage = e.Message;
            }
            Check("反序列化侧同样拦截", readMessage != null);

            // 5) 已覆盖类型不受影响：开关开启也必须正常通过
            var okJson = Json.Serialize(new Dictionary<string, string> { { "a", "1" } });
            Check("已覆盖类型在开关开启时仍正常", okJson.Contains("\"a\"", StringComparison.Ordinal), okJson);

            // 6) 关闭后恢复静默降级（行为与改造前一致）
            Json.StrictTrimDiagnostics = false;
            var silent = false;
            try
            {
                Json.Serialize(new UncoveredPayload { Name = "x", Amount = 1 });
                silent = true;
            }
            catch (NotSupportedException)
            {
                silent = false;
            }
            Check("关闭开关后恢复不抛异常", silent);
        }
        finally
        {
            Json.StrictTrimDiagnostics = previous;
        }

        Console.WriteLine($"  诊断用例：{_total - _failed}/{_total} 通过");
        return _failed;
    }
}

/// <summary>
/// 故意不登记到 Wlniao.JsonContext，用来验证诊断拦截。真实业务里它应当有自己的源生成上下文。
/// </summary>
public class UncoveredPayload
{
    public string? Name { get; set; }
    public int Amount { get; set; }
}
