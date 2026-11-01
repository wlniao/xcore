extern alias xcore;

using System;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace Wlniao.XCore.NTest;

/// <summary>
/// TLS 证书放行开关 xcore::Wlniao.XCore.AcceptAnyServerCertificate 的回归断言，覆盖：
///   默认取值        —— 未显式配置时为 false（收紧，按标准链校验）
///   配置解析        —— 仅显式肯定值（1/true/yes/on）放行，空值与无法识别的取值一律收紧
///   放行态          —— 任何 SslPolicyErrors / null 证书链都通过
///   收紧态          —— 标准链校验通过才放行，错误且无链 / 链无附加证书时拒绝
///   出厂接线        —— SocketsHttpHandler / HttpClientManager 均挂上受开关控制的统一策略
/// </summary>
[TestFixture]
public class CertificatePolicySwitchTests
{
    /// <summary>测试过程中会改写全局开关，此处备份以便复原</summary>
    private bool _original;

    /// <summary>框架对外公开的证书策略委托（出厂指向 ServerCertificateCustomValidation）</summary>
    private Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool> _validation;

    [SetUp]
    public void SetUp()
    {
        _original = xcore::Wlniao.XCore.AcceptAnyServerCertificate;
        _validation = xcore::Wlniao.XCore.ServerCertificateCustomValidationCallback;
    }

    [TearDown]
    public void TearDown()
    {
        xcore::Wlniao.XCore.AcceptAnyServerCertificate = _original;
    }

    [Test]
    public void DefaultIsStrict()
    {
        // 默认值必须与安全基线一致：未配置时收紧，按标准链校验
        Assert.That(xcore::Wlniao.XCore.AcceptAnyServerCertificate, Is.False,
            "未配置就放行全部服务端证书等于默认关闭 TLS 校验，会把所有调用方暴露在中间人攻击风险下。");
    }

    /// <summary>仅显式肯定值才放行；空值与无法识别的取值必须收紧，不能让配置笔误变成全局放行</summary>
    [TestCase(null, false)]
    [TestCase("", false)]
    [TestCase("   ", false)]
    [TestCase("1", true)]
    [TestCase("true", true)]
    [TestCase("TRUE", true)]
    [TestCase(" True ", true)]
    [TestCase("yes", true)]
    [TestCase("on", true)]
    [TestCase("0", false)]
    [TestCase("false", false)]
    [TestCase("no", false)]
    [TestCase("off", false)]
    [TestCase("ture", false)]
    [TestCase("enable", false)]
    [TestCase("是", false)]
    public void ConfigValueParsing_OnlyExplicitAffirmativeEnables(string? configured, bool expected)
    {
        Assert.That(ParseAcceptAnyServerCertificate(configured), Is.EqualTo(expected),
            "配置项 WLN_ACCEPT_ANY_CERT 取值 <" + configured + "> 解析结果不符合预期。");
    }

    [Test]
    public void AcceptAll_PassesAnyCertificateError()
    {
        xcore::Wlniao.XCore.AcceptAnyServerCertificate = true;

        // 放行态下连 null 证书、null 链都不能拦住（历史上 HttpClientManager 就是这种语义）
        Assert.That(_validation(null!, null, null, SslPolicyErrors.RemoteCertificateChainErrors), Is.True);
        Assert.That(_validation(null!, null, null, SslPolicyErrors.RemoteCertificateNameMismatch), Is.True);
        Assert.That(_validation(null!, null, null, SslPolicyErrors.RemoteCertificateNotAvailable), Is.True);
        Assert.That(_validation(null!, null, null,
            SslPolicyErrors.RemoteCertificateChainErrors | SslPolicyErrors.RemoteCertificateNameMismatch), Is.True);
        Assert.That(_validation(null!, null, null, SslPolicyErrors.None), Is.True);
    }

    [Test]
    public void AcceptAll_PassesEmptyChain()
    {
        xcore::Wlniao.XCore.AcceptAnyServerCertificate = true;

        using var chain = new X509Chain();
        Assert.That(_validation(null!, null, chain, SslPolicyErrors.RemoteCertificateChainErrors), Is.True,
            "放行态不应再去看证书链里有没有受信任指纹。");
    }

    [Test]
    public void Strict_RejectsWhenErrorAndNoChain()
    {
        xcore::Wlniao.XCore.AcceptAnyServerCertificate = false;

        Assert.That(_validation(null!, null, null, SslPolicyErrors.RemoteCertificateNameMismatch), Is.False);
        Assert.That(_validation(null!, null, null, SslPolicyErrors.RemoteCertificateChainErrors), Is.False);
    }

    [Test]
    public void Strict_RejectsWhenChainHasNoExtraStore()
    {
        xcore::Wlniao.XCore.AcceptAnyServerCertificate = false;

        using var chain = new X509Chain();
        Assert.That(chain.ChainElements.Count, Is.EqualTo(0));
        Assert.That(_validation(null!, null, chain, SslPolicyErrors.RemoteCertificateChainErrors), Is.False,
            "证书链为空（拿不到可校验的 CA）时必须拒绝，不能因为遍历不到就放过。");
    }

    [Test]
    public void Strict_PassesWhenNoSslError()
    {
        xcore::Wlniao.XCore.AcceptAnyServerCertificate = false;

        // 无错误时直接放行，且不依赖 chain / certificate（自签证书链可能为空但校验已通过）
        Assert.That(_validation(null!, null, null, SslPolicyErrors.None), Is.True);
    }

    [Test]
    public void ToggleTakesEffectImmediately()
    {
        // 同一个错误码在两种取值下结果必须相反，证明开关是在校验时即时读取而非构造时固化
        xcore::Wlniao.XCore.AcceptAnyServerCertificate = true;
        var accepted = _validation(null!, null, null, SslPolicyErrors.RemoteCertificateNameMismatch);
        xcore::Wlniao.XCore.AcceptAnyServerCertificate = false;
        var rejected = _validation(null!, null, null, SslPolicyErrors.RemoteCertificateNameMismatch);

        Assert.That(accepted, Is.True);
        Assert.That(rejected, Is.False);
    }

    [Test]
    public void HttpClientFactoryWiresGlobalPolicy()
    {
        // CustomHttpClientHandler 出厂时必须挂上统一策略，否则会绕过全局开关走操作系统默认校验
        using var handler = new Net.CustomHttpClientHandler().CreateHandler();
        var callback = handler.SslOptions.RemoteCertificateValidationCallback;

        Assert.That(callback, Is.Not.Null, "未挂证书策略，TLS 行为将与其他 HttpClient 不一致。");

        xcore::Wlniao.XCore.AcceptAnyServerCertificate = true;
        var accepted = callback(null!, null, null, SslPolicyErrors.RemoteCertificateNameMismatch);
        xcore::Wlniao.XCore.AcceptAnyServerCertificate = false;
        var rejected = callback(null!, null, null, SslPolicyErrors.RemoteCertificateNameMismatch);

        Assert.That(accepted, Is.True, "挂上的策略必须受全局开关控制。");
        Assert.That(rejected, Is.False, "挂上的策略必须受全局开关控制。");
    }

    [Test]
    public void HttpClientManagerWiresGlobalPolicy()
    {
        // HttpClientManager 内部 new 的是 SocketsHttpHandler（密封类型，无公开属性可直接取值），
        // 这里从 HttpClient 的私有 _handler 字段反射取出，确认它挂的就是受全局开关控制的策略。
        using var manager = new Net.HttpClientManager();
        Assert.That(manager.HttpClient, Is.Not.Null);

        var handler = GetPrivateHandler(manager.HttpClient);
        if (handler == null)
        {
            Assert.Ignore("当前运行时下 HttpMessageInvoker 的处理器字段名不同，无法反射取值（非断言失败）。");
            return;
        }
        var callback = handler.SslOptions.RemoteCertificateValidationCallback;
        Assert.That(callback, Is.Not.Null, "HttpClientManager 必须挂统一证书策略，不能漏挂或走操作系统默认。");

        xcore::Wlniao.XCore.AcceptAnyServerCertificate = true;
        var accepted = callback(null!, null, null, SslPolicyErrors.RemoteCertificateChainErrors);
        xcore::Wlniao.XCore.AcceptAnyServerCertificate = false;
        var rejected = callback(null!, null, null, SslPolicyErrors.RemoteCertificateChainErrors);

        Assert.That(accepted, Is.True, "放行开关未作用到 HttpClientManager。");
        Assert.That(rejected, Is.False, "收紧开关未作用到 HttpClientManager。");
    }

    /// <summary>
    /// 反射调用框架的配置解析规则（internal 成员，测试工程走 extern alias 无法直接访问）
    /// </summary>
    private static bool ParseAcceptAnyServerCertificate(string? configured)
    {
        var method = typeof(xcore::Wlniao.XCore).GetMethod("ParseAcceptAnyServerCertificate",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.That(method, Is.Not.Null, "未找到 XCore.ParseAcceptAnyServerCertificate，测试需同步更新。");
        return (bool)method!.Invoke(null, new object?[] { configured })!;
    }

    /// <summary>从 HttpClient 反射取出底层 SocketsHttpHandler</summary>
    private static SocketsHttpHandler? GetPrivateHandler(HttpClient client)
    {
        var field = typeof(HttpMessageInvoker).GetField("_handler",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field?.GetValue(client) as SocketsHttpHandler;
    }
}
