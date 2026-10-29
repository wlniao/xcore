using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Asn1.GM;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.X509;
using Wlniao.Crypto;

namespace Wlniao.XCore.NTest;

/// <summary>
/// SM2 改造前后一致性对照测试。
///
/// 设计意图：测试项目显式引用 BouncyCastle 作为「参照实现」，Wlniao.XCore 侧无论内部换成什么实现，
/// 都必须与参照实现在两个方向上互操作：
///   XCore 产出的签名/密文  →  BouncyCastle 能验签/能解密
///   BouncyCastle 产出的签名/密文  →  XCore 能验签/能解密
///
/// 这样「改造前后运行结果是否一致」就不是一次性的人工比对，而是每次 dotnet test 都重新验证一次。
/// 签名与加密本身带随机数，无法逐字节比对，互操作性才是等价的正确性判据。
/// </summary>
[TestFixture]
public class SM2InteropTests
{
    private byte[] _pub = Array.Empty<byte>();
    private byte[] _priv = Array.Empty<byte>();
    private Org.BouncyCastle.Crypto.Parameters.ECPublicKeyParameters _bcPub = null!;
    private Org.BouncyCastle.Crypto.Parameters.ECPrivateKeyParameters _bcPriv = null!;
    private ECDomainParameters _domain = null!;

    private static readonly byte[] CustomId = Encoding.ASCII.GetBytes("wlniao@example.com");

    private static readonly byte[] PlainAscii = Encoding.UTF8.GetBytes("wlniao sm2 interop payload");
    private static readonly byte[] PlainChinese = Encoding.UTF8.GetBytes("国密互操作测试：中文与符号 ~!@#$%^&*()");
    private static readonly byte[] PlainLong = Encoding.UTF8.GetBytes(new string('A', 1024));

    [OneTimeSetUp]
    public void Setup()
    {
        var kt = new KeyTool();
        kt.OutHex(out var pubHex, out var privHex);
        _pub = HexToBytes(pubHex);
        _priv = HexToBytes(privHex);

        var curve = GMNamedCurves.GetByName("SM2P256V1");
        _domain = new ECDomainParameters(curve);
        _bcPub = new Org.BouncyCastle.Crypto.Parameters.ECPublicKeyParameters(curve.Curve.DecodePoint(_pub), _domain);
        _bcPriv = new Org.BouncyCastle.Crypto.Parameters.ECPrivateKeyParameters(
            new Org.BouncyCastle.Math.BigInteger(1, _priv), _domain);
    }

    /// <summary>KeyTool 输出的 hex 可能是奇数长度（私钥按最少字节表示），补齐后再转字节。</summary>
    private static byte[] HexToBytes(string hex)
    {
        if (hex.Length % 2 == 1)
        {
            hex = "0" + hex;
        }
        return System.Convert.FromHexString(hex);
    }

    /// <summary>用参照实现签名，返回 DER 编码。</summary>
    private byte[] BcSign(byte[] msg, byte[]? id)
    {
        var signer = new SM2Signer();
        var random = new ParametersWithRandom(_bcPriv);
        ICipherParameters cp = id == null ? random : new ParametersWithID(random, id);
        signer.Init(true, cp);
        signer.BlockUpdate(msg, 0, msg.Length);
        return signer.GenerateSignature();
    }

    private bool BcVerify(byte[] msg, byte[] derSignature, byte[]? id)
    {
        var verifier = new SM2Signer();
        ICipherParameters cp = id == null ? _bcPub : new ParametersWithID(_bcPub, id);
        verifier.Init(false, cp);
        verifier.BlockUpdate(msg, 0, msg.Length);
        return verifier.VerifySignature(derSignature);
    }

    // ── 签名：两个方向 ──────────────────────────────────────────────

    [Test]
    public void XCore签名_BC验签_通过()
    {
        var sm2 = new SM2(_pub, _priv);
        var der = sm2.Sign(PlainAscii);

        // 契约：SM2.Sign 返回 ASN.1 DER 编码（不是 r||s 拼接），与 BouncyCastle SM2Signer 一致。
        // 改造后必须保持这一点，否则调用方会拿到不同格式的签名。
        Assert.That(der[0], Is.EqualTo(0x30), "DER SEQUENCE 起始字节应为 0x30");
        Assert.That(BcVerify(PlainAscii, der, null), Is.True);
    }

    [Test]
    public void BC签名_XCore验签_通过()
    {
        var der = BcSign(PlainAscii, null);

        var sm2 = new SM2(_pub, _priv);
        Assert.That(sm2.VerifySign(PlainAscii, der), Is.True);
    }

    [Test]
    public void XCore签名_带自定义ID_BC验签_通过()
    {
        var sm2 = new SM2(_pub, _priv);
        var der = sm2.Sign(PlainChinese, CustomId);

        Assert.That(BcVerify(PlainChinese, der, CustomId), Is.True);
    }

    [Test]
    public void BC签名_带自定义ID_XCore验签_通过()
    {
        var der = BcSign(PlainChinese, CustomId);

        var sm2 = new SM2(_pub, _priv);
        Assert.That(sm2.VerifySign(PlainChinese, der, CustomId), Is.True);
    }

    [Test]
    public void 签名_ID不一致时验签失败()
    {
        var der = BcSign(PlainAscii, CustomId);

        var sm2 = new SM2(_pub, _priv);
        Assert.That(sm2.VerifySign(PlainAscii, der), Is.False,
            "用默认 ID 验一个用自定义 ID 签出来的签名，必须失败");
    }

    [Test]
    public void 签名_消息被篡改时验签失败()
    {
        var sm2 = new SM2(_pub, _priv);
        var der = sm2.Sign(PlainAscii);
        var tampered = (byte[])PlainAscii.Clone();
        tampered[0] ^= 0xFF;

        Assert.That(sm2.VerifySign(tampered, der), Is.False);
    }

    // ── 加解密：C1C2C3（默认）两个方向 ─────────────────────────────

    [TestCaseSource(nameof(PlainCases))]
    public void XCore加密_BC解密_一致(byte[] plain)
    {
        var sm2 = new SM2(_pub, _priv);
        var cipher = sm2.Encrypt(plain);
        Assert.That(cipher.Length, Is.EqualTo(plain.Length + 97), "C1(65)+C3(32)+C2(len)");

        var engine = new SM2Engine();
        engine.Init(false, _bcPriv);
        Assert.That(engine.ProcessBlock(cipher, 0, cipher.Length), Is.EqualTo(plain));
    }

    [TestCaseSource(nameof(PlainCases))]
    public void BC加密_XCore解密_一致(byte[] plain)
    {
        var engine = new SM2Engine();
        engine.Init(true, new ParametersWithRandom(_bcPub));
        var cipher = engine.ProcessBlock(plain, 0, plain.Length);

        var sm2 = new SM2(_pub, _priv);
        Assert.That(sm2.Decrypt(cipher), Is.EqualTo(plain));
    }

    // ── 加解密：C1C3C2 两个方向 ───────────────────────────────────

    [Test]
    public void XCore加密_C1C3C2_BC解密_一致()
    {
        var sm2 = new SM2(_pub, _priv, SM2Mode.C1C3C2);
        var cipher = sm2.Encrypt(PlainAscii);

        var engine = new SM2Engine(SM2Engine.Mode.C1C3C2);
        engine.Init(false, _bcPriv);
        Assert.That(engine.ProcessBlock(cipher, 0, cipher.Length), Is.EqualTo(PlainAscii));
    }

    [Test]
    public void BC加密_C1C3C2_XCore解密_一致()
    {
        var engine = new SM2Engine(SM2Engine.Mode.C1C3C2);
        engine.Init(true, new ParametersWithRandom(_bcPub));
        var cipher = engine.ProcessBlock(PlainAscii, 0, PlainAscii.Length);

        var sm2 = new SM2(_pub, _priv, SM2Mode.C1C3C2);
        Assert.That(sm2.Decrypt(cipher), Is.EqualTo(PlainAscii));
    }

    [Test]
    public void C1C2C3与C1C3C2排列互转_可逆()
    {
        var sm2 = new SM2(_pub, _priv);
        var c123 = sm2.Encrypt(PlainAscii);
        var c132 = SM2.C123ToC132(c123);

        Assert.That(SM2.C132ToC123(c132), Is.EqualTo(c123));
        Assert.That(c132, Is.Not.EqualTo(c123));

        var sm2c132 = new SM2(_pub, _priv, SM2Mode.C1C3C2);
        Assert.That(sm2c132.Decrypt(c132), Is.EqualTo(PlainAscii));
    }

    // ── ASN.1 与密钥格式 ──────────────────────────────────────────

    [TestCase(0x00)]
    [TestCase(0xFF)]
    [TestCase(0x7F)]
    [TestCase(0x80)]
    public void RS的ASN1转换_往返一致_含最高位边界(byte highBit)
    {
        var rs = new byte[64];
        rs[0] = highBit;
        rs[32] = highBit;
        RandomNumberGenerator.Fill(rs.AsSpan(1, 31));
        RandomNumberGenerator.Fill(rs.AsSpan(33, 31));

        var der = SM2.RsPlainByteArrayToAsn1(rs);
        Assert.That(SM2.RsAsn1ToPlainByteArray(der), Is.EqualTo(rs));
    }

    [Test]
    public void 密钥格式_与BC约定一致()
    {
        Assert.That(_pub.Length, Is.EqualTo(65), "公钥应为未压缩点 04||X||Y");
        Assert.That(_pub[0], Is.EqualTo(0x04));

        var d = new Org.BouncyCastle.Math.BigInteger(1, _priv);
        Assert.That(d.ToByteArrayUnsigned().Length, Is.LessThanOrEqualTo(32), "私钥有效字节数不超过 32");
        Assert.That(_priv.Length, Is.LessThanOrEqualTo(33), "允许 BigInteger 补一个前导 0x00");
        Assert.That(d.SignValue, Is.GreaterThan(0));
        Assert.That(d.CompareTo(_domain.N.Subtract(Org.BouncyCastle.Math.BigInteger.One)), Is.LessThanOrEqualTo(0));

        // 公钥必须在 SM2P256V1 曲线上
        Assert.DoesNotThrow(() => _domain.Curve.DecodePoint(_pub));
    }

    [Test]
    public void Pkcs8密钥解析_结果与BC一致()
    {
        var privDer = PrivateKeyInfoFactory.CreatePrivateKeyInfo(_bcPriv).GetEncoded();
        var pubDer = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(_bcPub).GetEncoded();

        var sm2 = new SM2(System.Convert.ToBase64String(pubDer), System.Convert.ToBase64String(privDer), KeyType.Pkcs8);
        var der = sm2.Sign(PlainAscii);

        Assert.That(BcVerify(PlainAscii, der, null), Is.True,
            "从 PKCS#8 解析出的密钥应能签出被参照实现认可的签名");
    }

    public static IEnumerable<TestCaseData> PlainCases()
    {
        yield return new TestCaseData(PlainAscii).SetArgDisplayNames("ascii");
        yield return new TestCaseData(PlainChinese).SetArgDisplayNames("chinese");
        yield return new TestCaseData(PlainLong).SetArgDisplayNames("long-1024");
    }
}
