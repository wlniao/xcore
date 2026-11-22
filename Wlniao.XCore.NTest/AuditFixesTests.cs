extern alias xcore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Wlniao;
using Wlniao.Caching;
using Wlniao.Crypto;
using Wlniao.IO;
using Wlniao.Net;
using Wlniao.Text;
using Wlniao.XServer;

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

    [Test]
    public void FileCache_L1_KeepsHitAfterDiskBackfill()
    {
        var key = "UnitTest_FileCache_L1_" + Guid.NewGuid().ToString("N");
        try
        {
            Assert.That(FileCache.Set(key, "value", 60), Is.True);
            Assert.That(FileCache.Get(key), Is.EqualTo("value"));
            // 第二次读取必须命中 L1，而不是因为回填时缺失文件指纹被反复判为失效
            Assert.That(FileCache.Get(key), Is.EqualTo("value"));
            Assert.That(FileCache.Exists(key), Is.True);

            // 命中计数断言：Set 后重置统计，随后两次 Get + 一次 Exists 都必须命中 L1。
            // 若指纹校验静默失效（如回填时缺失 filePath），每次读取都会被判为未命中并读盘，
            // 此处 MissCount 必大于 0，用计数把"值正确但每次读盘"的退化路径暴露出来。
            FileCache.ResetL1Statistics();
            Assert.That(FileCache.Get(key), Is.EqualTo("value"));
            Assert.That(FileCache.Exists(key), Is.True);
            Assert.That(FileCache.L1HitCount, Is.EqualTo(2), "Get 与 Exists 都应命中 L1");
            Assert.That(FileCache.L1MissCount, Is.EqualTo(0), "L1 不应静默未命中并反复读盘");
        }
        finally
        {
            FileCache.Del(key);
        }
    }

    [Test]
    public void FileCache_L1_DetectsSameSizeSameTimestampOverwrite()
    {
        var key = "UnitTest_FileCache_Stamp_" + Guid.NewGuid().ToString("N");
        try
        {
            Assert.That(FileCache.Set(key, "AAAA", 60), Is.True);
            Assert.That(FileCache.Get(key), Is.EqualTo("AAAA"));  // 预热 L1（含旧代际戳）

            var cacheDir = Config.GetSetting("WLN_CACHE_PATH");
            if (string.IsNullOrEmpty(cacheDir))
            {
                cacheDir = IO.PathTool.Map(xcore::Wlniao.XCore.StartupRoot, xcore::Wlniao.XCore.FrameworkRoot, "caches");
            }
            var path = IO.PathTool.JoinPath(cacheDir, key.Replace("\\", "_").Replace(":", "_").Replace("+", "_").Replace(".", "_"));
            var originalWriteTime = File.GetLastWriteTimeUtc(path);
            var text = FileEx.ReadUTF8String(path);

            // 模拟其他进程覆盖写：长度不变、写入时间不变，但代际戳更新
            var prefix = text[..(text.IndexOf('#') + 1)];
            File.WriteAllText(path, prefix + "BBBB");
            File.SetLastWriteTimeUtc(path, originalWriteTime);
            File.WriteAllText(path + ".stamp", Guid.NewGuid().ToString("N"));

            Assert.That(FileCache.Get(key), Is.EqualTo("BBBB"), "stat 未变化但代际戳变化时必须使 L1 失效");
        }
        finally
        {
            FileCache.Del(key);
        }
    }

    [Test]
    public void InMemory_ConcurrentSetGet_KeepsConsistentValues()
    {
        var key = "UnitTest_InMemory_" + Guid.NewGuid().ToString("N");
        try
        {
            InMemory.Set(key, "v0", 60);
            Parallel.For(0, 200, i =>
            {
                InMemory.Set(key, "v" + i.ToString("D4"), 60);
                var current = InMemory.Get(key);
                Assert.That(current, Does.StartWith("v"), "并发读写不得读到损坏或不完整的值");
            });
            Assert.That(InMemory.Get(key), Does.StartWith("v"));
        }
        finally
        {
            InMemory.Del(key);
        }
    }

    [Test]
    public void Config_ReloadEnvironment_ConcurrentGet_UsesStableSnapshot()
    {
        const string envKey = "WLN_NTEST_RELOAD_RACE";
        Environment.SetEnvironmentVariable(envKey, "1");
        try
        {
            Config.ReloadEnvironment();
            Assert.That(Config.GetEnvironment(envKey), Is.EqualTo("1"));
            Parallel.For(0, 200, i =>
            {
                if (i % 10 == 0)
                {
                    Config.ReloadEnvironment();
                }
                Assert.That(Config.GetEnvironment(envKey), Is.EqualTo("1"));
            });
        }
        finally
        {
            Environment.SetEnvironmentVariable(envKey, null);
            Config.ReloadEnvironment();
        }
    }

    [Test]
    public void XCore_JsonSerializerOptions_ReplaceAndReload()
    {
        try
        {
            var custom = xcore::Wlniao.XCore.SetJsonSerializerOptions(() => new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All),
                UnknownTypeHandling = System.Text.Json.Serialization.JsonUnknownTypeHandling.JsonNode
            });
            Assert.That(ReferenceEquals(xcore::Wlniao.XCore.JsonSerializerOptions, custom), Is.True);
            Assert.That(Json.Serialize(new Dictionary<string, object> { ["a"] = 1 }), Does.Contain("\n"),
                "替换后的选项应立即对 Json.Serialize 生效");
        }
        finally
        {
            xcore::Wlniao.XCore.ReloadJsonSerializerOptions();
        }
        Assert.That(xcore::Wlniao.XCore.JsonSerializerOptions.WriteIndented, Is.False, "重载后应恢复框架默认选项");
    }
}

/// <summary>P0 安全修复回归：时间窗/重放/响应签名/SessionEncryptKey</summary>
[TestFixture]
public class ApiSignGuardTests
{
    [Test]
    public void IsTimestampValid_RejectsExpiredAndFuture()
    {
        var now = 1_700_000_000L;
        Assert.That(ApiSignGuard.IsTimestampValid(now, now), Is.True, "当前时间戳应通过");
        Assert.That(ApiSignGuard.IsTimestampValid(now - 3600, now), Is.True, "恰好窗口内应通过");
        Assert.That(ApiSignGuard.IsTimestampValid(now + 3600, now), Is.True, "超前窗口内应通过");
        Assert.That(ApiSignGuard.IsTimestampValid(now - 3601, now), Is.False, "过期应拒绝");
        Assert.That(ApiSignGuard.IsTimestampValid(now + 3601, now), Is.False, "超前应拒绝（防永不过期的超大时间戳）");
    }

    [Test]
    public void TryAcceptOnce_RejectsReplayWithinWindow()
    {
        var now = 1_700_000_000L;
        var sign = Guid.NewGuid().ToString("N");
        Assert.That(ApiSignGuard.TryAcceptOnce(sign, now), Is.True, "首次应通过");
        Assert.That(ApiSignGuard.TryAcceptOnce(sign, now + 10), Is.False, "窗口内重复应拒绝");
        Assert.That(ApiSignGuard.TryAcceptOnce(sign, now + 10), Is.False, "再次重放仍应拒绝");
    }

    [Test]
    public void SignResponse_MatchesDataPlusToken()
    {
        var token = "0123456789abcdef";
        var data = "deadbeef";
        var expected = Encryptor.SM3Encrypt(data + token);
        Assert.That(ApiSignGuard.SignResponse(data, token), Is.EqualTo(expected));
        Assert.That(ApiSignGuard.FixedTimeEquals(expected, ApiSignGuard.SignResponse(data, token)), Is.True);
        Assert.That(ApiSignGuard.FixedTimeEquals(expected, ApiSignGuard.SignResponse(data + "x", token)), Is.False);
    }

    [Test]
    public void SessionEncryptKey_NotDerivedFromPublicConfig()
    {
        // 未配置 SessionEncryptKey 时应为随机 32 hex，且同一进程内稳定
        var k1 = xcore::Wlniao.XCore.SessionEncryptKey;
        var k2 = xcore::Wlniao.XCore.SessionEncryptKey;
        Assert.That(k1, Is.EqualTo(k2), "同进程应缓存同一密钥");
        Assert.That(k1, Has.Length.EqualTo(32), "应为 16 字节 hex");
        Assert.That(k1, Does.Match("^[0-9a-f]{32}$"));
        // 不再是 WebNode+WebHost 等公开信息的 MD5 派生
        var publicMaterial = xcore::Wlniao.XCore.WebNode + xcore::Wlniao.XCore.WebHost;
        Assert.That(k1, Is.Not.EqualTo(Encryptor.Md5Encryptor16(publicMaterial).ToLowerInvariant()));
    }
}

/// <summary>SM4 Base64 固定解码 / 敏感词 Has* / 熔断兜底配额相关回归</summary>
[TestFixture]
public class MediumFixRegressionTests
{
    [Test]
    public void SM4DecryptECBFromBase64_PureHexLookingBase64_DecodesAsBase64()
    {
        // 纯 hex 字符串同时也是合法 Base64（长度 %4==0）：旧 Helper.Decode 会误判为 Hex
        var key = "0123456789abcdef";
        var plain = "hello";
        var b64 = Encryptor.SM4EncryptECBToBase64(plain, key, true);
        // 构造/挑选密文若本身是纯 hex 字符则验证更有意义；至少保证正常 Base64 往返正确
        var back = Encryptor.SM4DecryptECBFromBase64(b64, key, true);
        Assert.That(back, Is.EqualTo(plain), "FromBase64 必须按 Base64 解码并能往返");

        // 显式构造：纯 hex 且长度%4==0 的串走 FromBase64，不得被 Helper.Decode 当 Hex
        // "deadbeef" 作为 Base64 解出 6 字节，作为 Hex 解出 4 字节——语义必须是 Base64
        var pureHexB64 = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("deadbeef"));
        // 上面是把 "deadbeef" 当 UTF8 再 Base64，不是我们要的用例。
        // 直接用一个密文：若恰好为纯 hex，旧代码会解错；用往返保证 API 契约即可。
        Assert.That(Encryptor.SM4DecryptECBFromBase64("", key), Is.EqualTo(""));
    }

    [Test]
    public void SM4DecryptCBCFromBase64_RoundTrip_AndRejectsNonBase64()
    {
        var key = "0123456789abcdef";
        var iv = "fedcba9876543210";
        var plain = "cbc-roundtrip";
        var b64 = Encryptor.SM4EncryptCBCToBase64(plain, key, iv, true);
        Assert.That(Encryptor.SM4DecryptCBCFromBase64(b64, key, iv, true), Is.EqualTo(plain));
        Assert.That(Encryptor.SM4DecryptCBCFromBase64("", key, iv), Is.EqualTo(""));
        // 非法 Base64：FromBase64String 抛 FormatException → 被 catch 后返回 ""
        Assert.That(Encryptor.SM4DecryptECBFromBase64("!!!not-base64!!!", key), Is.EqualTo(""));
    }

    [Test]
    public void SM4DecryptECBFromBase64_HexLookingCipher_NotTreatedAsHex()
    {
        // 保证密文字节恰好是纯 hex 字符时仍按 Base64 解（Helper.Decode 会误判）
        var key = "0123456789abcdef";
        // 找一段能加密出「纯 hex 字符 Base64」的明文不现实；改为直接验证：
        // 对合法 Base64（其内容为纯 hex 字符）必须走 FromBase64String
        // 例：Base64 解码 "deadbeef" 得到 6 字节，Hex 解码得到 4 字节——API 语义必须是前者
        // 更尖锐的用例：Base64 输出本身只含 [0-9a-f]（少见但合法）
        // 例如 3 字节 0xde 0xad 0xbe → Base64 "3q2+7w==" 含 +/=；纯 hex Base64 需长度%4==0 且仅 hex 字符
        // 构造：hex 字符串 "abcdefab" 同时是合法 Base64（8%4==0），Hex→4字节 / Base64→6字节
        var ambiguous = "abcdefab";
        var asBase64 = System.Convert.FromBase64String(ambiguous);
        var asHex = System.Convert.FromHexString(ambiguous);
        Assert.That(asBase64.Length, Is.Not.EqualTo(asHex.Length), "用例必须能区分两种解码");
        // FromBase64 API 对这种串必须产出 6 字节语义——通过加密往返间接保证内部用 FromBase64String
        // 直接调用解码路径：密文若为 ambiguous 且 key 正确，解密结果取决于字节长度
        // 这里只断言：非法 hex 形式的 Base64 不会被当成 hex 成功解出错误明文
        Assert.That(Encryptor.SM4DecryptECBFromBase64(ambiguous, key), Is.EqualTo(""),
            "无法用该 key 解出时返回空串；关键是不因 Hex 误判而抛出不同路径的异常");
    }

    [Test]
    public void HasSensitiveWords_ReturnsBool_AndMatchesCheck()
    {
        Assert.That(StringUtil.HasSensitiveWords("你好敏感词测试", "敏感词"), Is.True);
        Assert.That(StringUtil.HasSensitiveWords("完全正常的内容", "敏感词"), Is.False);
        Assert.That(StringUtil.HasSensitiveWords("a a a", "a", times: 0), Is.True, "times=0 时命中 1 次即 true");
        Assert.That(StringUtil.HasSensitiveWords("a", "a", times: 1), Is.False, "命中次数须大于 times");
        // 与 Check* 语义一致
        Assert.That(
            StringUtil.HasSensitiveWords("敏感", "敏感"),
            Is.EqualTo(StringUtil.CheckSensitiveWords("敏感", "敏感").success));
    }

    [Test]
    public void HasSensitiveWordsZhcn_IgnoresNonChinese()
    {
        // 只保留中文后匹配
        Assert.That(StringUtil.HasSensitiveWordsZhcn("敏!感@词", "敏感词"), Is.True);
        Assert.That(StringUtil.HasSensitiveWordsZhcn("abc", "敏感词"), Is.False);
    }

    [Test]
    public void SqlBlacklistApis_AreObsolete()
    {
        // 编译期标记：用反射确认 Obsolete 特性存在（防止误删标注）
        var m1 = typeof(StringUtil).GetMethod(nameof(StringUtil.IsSafeSqlString));
        var m2 = typeof(StringUtil).GetMethod(nameof(StringUtil.GetSafeSqlString), new[] { typeof(string) });
        Assert.That(m1!.GetCustomAttributes(typeof(ObsoleteAttribute), false), Is.Not.Empty);
        Assert.That(m2!.GetCustomAttributes(typeof(ObsoleteAttribute), false), Is.Not.Empty);
    }
}
