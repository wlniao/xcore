using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using Wlniao;
using Wlniao.Crypto;
using Wlniao.Net;
using Wlniao.Text;

namespace Wlniao.XCore.NTest;

/// <summary>
/// 库级行为回归断言，覆盖：
///   SM3.DoFinal(Span)       —— 结果回写进调用方缓冲区
///   SM3 复制构造             —— 克隆实例产出与原实例一致的摘要
///   Pattern.Regex.Url       —— 带端口的网址可匹配
///   StringUtil.GetHtmlTitle —— 静态方法可提取标题
///   StringExtend.IsEmail    —— 与 StringUtil 判据一致，支持多级域名
///   SM4.Pkcs7Pad/Unpad      —— PKCS#7 填充往返
///   Json.JsonNodeToObject   —— 零反射 Json→object 图转换
///   CustomHttpClientHandler —— 处理器工厂产出挂好 ConnectCallback 的实例
/// </summary>
[TestFixture]
public class AuditFixesTests
{
    /// <summary>国标 SM3 向量：SM3("abc")</summary>
    private const string VectorAbc = "66c7f0f462eeedd9d1f2d46bdc10e4e24167c4875cf2f7a2297da02b8f4ba8e0";

    [Test]
    public void SM3_DoFinal_Span_WritesIntoCallerBuffer()
    {
        var sm3 = new SM3();
        var input = Encoding.UTF8.GetBytes("abc");
        sm3.BlockUpdate(input, 0, input.Length);

        Span<byte> buffer = stackalloc byte[32];
        var written = sm3.DoFinal(buffer);

        Assert.That(written, Is.EqualTo(32));
        Assert.That(System.Convert.ToHexString(buffer).ToLowerInvariant(), Is.EqualTo(VectorAbc),
            "DoFinal(Span) 的结果必须写入调用方缓冲区");
    }

    [Test]
    public void SM3_DoFinal_Span_MatchesDoFinalByteArray()
    {
        var input = Encoding.UTF8.GetBytes("中文abcd混合");

        var a = new SM3();
        a.BlockUpdate(input, 0, input.Length);
        var spanResult = System.Convert.ToHexString(a.DoFinal()).ToLowerInvariant();

        var b = new SM3();
        b.BlockUpdate(input, 0, input.Length);
        Span<byte> buffer = new byte[32];
        b.DoFinal(buffer);

        Assert.That(System.Convert.ToHexString(buffer).ToLowerInvariant(), Is.EqualTo(spanResult));
    }

    [Test]
    public void SM3_DoFinal_Span_TooSmallBuffer_Throws()
    {
        var sm3 = new SM3();
        Assert.Throws<ArgumentException>(() => sm3.DoFinal(new Span<byte>(new byte[16])));
    }

    [Test]
    public void SM3_CopyConstructor_ProducesSameDigest()
    {
        var input = Encoding.UTF8.GetBytes("abcdabcdabcdabcd");
        var original = new SM3();
        original.BlockUpdate(input, 0, input.Length);

        var clone = new SM3(original);
        var rest = Encoding.UTF8.GetBytes("tail");
        original.BlockUpdate(rest, 0, rest.Length);
        clone.BlockUpdate(rest, 0, rest.Length);

        Assert.That(System.Convert.ToHexString(clone.DoFinal()).ToLowerInvariant(),
            Is.EqualTo(System.Convert.ToHexString(original.DoFinal()).ToLowerInvariant()),
            "克隆实例的摘要必须与原实例一致");
    }

    [Test]
    public void Regex_Url_MatchesUrlWithPort()
    {
        Assert.That(Wlniao.Pattern.Regex.IsMatch("http://example.com:8080/index.html", Wlniao.Pattern.Regex.Url), Is.True);
        Assert.That(Wlniao.Pattern.Regex.IsMatch("https://wlniao.com", Wlniao.Pattern.Regex.Url), Is.True);
        Assert.That(Wlniao.Pattern.Regex.IsMatch("ftp://x.y.z/pub/readme.txt", Wlniao.Pattern.Regex.Url), Is.True);
        Assert.That(Wlniao.Pattern.Regex.IsMatch("not-a-url", Wlniao.Pattern.Regex.Url), Is.False);
    }

    [Test]
    public void StringUtil_GetHtmlTitle_IsStaticAndExtractsTitle()
    {
        Assert.That(StringUtil.GetHtmlTitle("<html><head><title>登录</title></head></html>"), Is.EqualTo("登录"));
        Assert.That(StringUtil.GetHtmlTitle("<html/>"), Is.EqualTo("(unknown)"));
    }

    [Test]
    public void StringExtend_IsEmail_AcceptsMultiLevelDomain()
    {
        Assert.That("user@sub.domain.com".IsEmail(), Is.True);
        Assert.That(StringUtil.IsEmail("user@sub.domain.com"), Is.True, "两套判据必须一致");
        Assert.That("not-an-email".IsEmail(), Is.False);
    }

    [Test]
    public void StringExtend_IsNullOrEmpty_KeepsBclSemantics()
    {
        Assert.That("   ".IsNullOrEmpty(), Is.False, "扩展方法对齐 BCL string.IsNullOrEmpty");
        Assert.That(StringUtil.IsNullOrEmpty("   "), Is.True, "StringUtil 版本会把纯空格视为空，两者刻意保持差异");
    }

    [Test]
    public void SM4_Pkcs7Pad_And_Unpad_RoundTrip()
    {
        foreach (var length in new[] { 0, 1, 15, 16, 17, 32 })
        {
            var data = Enumerable.Range(0, length).Select(i => (byte)i).ToArray();
            var padded = SM4.Pkcs7Pad(data);
            // 加密方向补齐后长度必为 16 的整数倍，空输入同样补满一个整分组
            Assert.That(padded.Length % 16, Is.EqualTo(0), $"长度 {length} 填充后应补齐到分组整数倍");
            Assert.That(padded.Length, Is.GreaterThanOrEqualTo(16), $"长度 {length} 填充后至少一个分组");
            Assert.That(SM4.Pkcs7Unpad(padded), Is.EqualTo(data), $"长度 {length} 往返失败");
        }
    }

    [Test]
    public void SM4_Pkcs7Unpad_RejectsNonBlockAlignedInput()
    {
        // 未按分组对齐的数据不可能是 PKCS#7 密文，必须在解填充阶段就拒绝，而不是靠末字节碰运气
        Assert.Throws<ArgumentException>(() => SM4.Pkcs7Unpad(new byte[17]));
        Assert.Throws<ArgumentException>(() => SM4.Pkcs7Unpad(Array.Empty<byte>()));
    }

    [Test]
    public void Json_JsonNodeToObject_ConvertsNestedGraph()
    {
        var json = "{\"a\":1,\"b\":{\"c\":\"x\"},\"d\":[1,2,3]}";
        var graph = Json.JsonNodeToObject(System.Text.Json.Nodes.JsonNode.Parse(json));

        Assert.That(graph, Is.TypeOf<System.Collections.Generic.Dictionary<string, object>>());
        var dic = (System.Collections.Generic.Dictionary<string, object>?)graph;
        Assert.That(dic["a"], Is.EqualTo(1L));
        Assert.That(dic["b"], Is.TypeOf<System.Collections.Generic.Dictionary<string, object>>());
        Assert.That(dic["d"], Is.TypeOf<System.Collections.Generic.List<object>>());
    }

    [Test]
    public void CustomHttpClientHandler_CreatesHandlerWithConnectCallback()
    {
        string? actualIp = null;
        var handler = new CustomHttpClientHandler { OnConnectionEstablished = ip => actualIp = ip };

        using var socks = handler.CreateHandler();
        Assert.That(socks, Is.Not.Null);
        Assert.That(socks, Is.TypeOf<SocketsHttpHandler>());

        using var client = handler.CreateHttpClient();
        Assert.That(client, Is.Not.Null);
        Assert.That(actualIp, Is.Null, "尚未发起请求时不应触发回调");
    }
}
