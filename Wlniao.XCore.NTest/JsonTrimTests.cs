using System.Text.Json;
using Wlniao;

namespace Wlniao.XCore.NTest;

/// <summary>
/// Wlniao.Json 的裁剪/AOT 相关契约。
/// 重点覆盖 Result&lt;T&gt; 值类型实例化的元数据可用性（历史缺陷：Data 属性标 WhenWritingNull
/// 导致 Result&lt;int&gt; 的元数据一物化就抛 IgnoreConditionOnValueTypeInvalid）。
/// </summary>
[TestFixture]
public class JsonTrimTests
{
    [Test]
    public void Result_Int_元数据可正常物化并往返()
    {
        var result = new Result<int> { Code = 200, Data = 42 };

        var json = Json.Serialize(result, JsonContext.Instance.ResultInt32);
        var back = Json.Deserialize(json, JsonContext.Instance.ResultInt32);

        Assert.That(back.Code, Is.EqualTo(200));
        Assert.That(back.Data, Is.EqualTo(42));
    }

    [Test]
    public void Result_Int_默认值被省略且往返后仍为默认()
    {
        var result = new Result<int> { Code = 200 };

        var json = Json.Serialize(result, JsonContext.Instance.ResultInt32);

        Assert.That(json, Does.Not.Contain("Data"));
        var back = Json.Deserialize(json, JsonContext.Instance.ResultInt32);
        Assert.That(back.Data, Is.EqualTo(0));
    }

    [Test]
    public void Result_Bool_元数据可正常物化()
    {
        var json = Json.Serialize(new Result<bool> { Code = 200, Data = true }, JsonContext.Instance.ResultBoolean);
        var back = Json.Deserialize(json, JsonContext.Instance.ResultBoolean);

        Assert.That(back.Data, Is.True);
    }

    [Test]
    public void Result_Object_空数据被省略()
    {
        var json = Json.Serialize(new Result<object> { Code = 500, Message = "boom" }, JsonContext.Instance.ResultObject);

        Assert.That(json, Does.Not.Contain("\"Data\""));
        Assert.That(json, Does.Contain("boom"));
    }

    [Test]
    public void IsCovered_已登记类型返回true()
    {
        Assert.That(Json.IsCovered(typeof(Dictionary<string, string>)), Is.True);
        Assert.That(Json.IsCovered(typeof(string)), Is.True);
    }

    [Test]
    public void IsCovered_未登记类型返回false()
    {
        Assert.That(Json.IsCovered(typeof(UncoveredForTest)), Is.False);
    }

    [Test]
    public void 诊断开关默认关闭()
    {
        Assert.That(Json.StrictTrimDiagnostics, Is.False);
    }

    [Test]
    public void 诊断开关关闭时未覆盖类型不抛异常()
    {
        var previous = Json.StrictTrimDiagnostics;
        try
        {
            Json.StrictTrimDiagnostics = false;
            Assert.DoesNotThrow(() => Json.Serialize(new UncoveredForTest { Name = "x" }));
        }
        finally
        {
            Json.StrictTrimDiagnostics = previous;
        }
    }

    private sealed class UncoveredForTest
    {
        public string? Name { get; set; }
    }
}
