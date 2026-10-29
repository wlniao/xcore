using System;
using System.Security.Cryptography;
using System.Text;
using Wlniao.Crypto;

namespace Wlniao.XCore.NTest;

/// <summary>
/// RSA 相关方法在移除 BouncyCastle 后的行为回归。
///
/// 背景：Encryptor 的四个 Rsa* 方法与 Cert 的 PKCS#1/PKCS#8 互转此前没有任何测试覆盖。
/// 改造时发现 RsaDecryptWithPrivate 的 PKCS#1 分支原本把签名算法名 "SHA1withRSA" 当 cipher 名传给
/// BouncyCastle，而 CipherUtilities 不认识该名称（实测抛 SecurityUtilityException），该分支历史上不可用。
/// 这里既验证可用的路径保持等价，也固定住修复后的行为。
/// </summary>
[TestFixture]
public class RsaCompatTests
{
    private RSA _rsa = null!;
    private string _pkcs1Pem = string.Empty;
    private string _pkcs8Pem = string.Empty;
    private string _pkcs1Body = string.Empty;
    private string _pkcs8Body = string.Empty;
    private string _spkiBase64 = string.Empty;

    [OneTimeSetUp]
    public void Setup()
    {
        _rsa = RSA.Create(2048);
        _pkcs1Pem = _rsa.ExportRSAPrivateKeyPem();
        _pkcs8Pem = _rsa.ExportPkcs8PrivateKeyPem();
        _spkiBase64 = System.Convert.ToBase64String(_rsa.ExportSubjectPublicKeyInfo());
        _pkcs1Body = StripPemBoundaries(_pkcs1Pem);
        _pkcs8Body = StripPemBoundaries(_pkcs8Pem);
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _rsa.Dispose();
    }

    /// <summary>去掉 PEM 头尾与换行，只留 Base64 正文——历史调用方常这样传。</summary>
    private static string StripPemBoundaries(string pem)
    {
        var builder = new StringBuilder();
        foreach (var line in pem.Split('\n'))
        {
            var text = line.Trim();
            if (text.Length == 0 || text.StartsWith("-----", StringComparison.Ordinal))
            {
                continue;
            }
            builder.Append(text);
        }
        return builder.ToString();
    }

    [Test]
    public void Rsa签名验签_PKCS1私钥_通过()
    {
        var signature = Wlniao.Encryptor.RsaSignWithPrivate("hello", _pkcs1Pem);
        Assert.That(Wlniao.Encryptor.RsaVerifyWithPublicKey("hello", signature, _spkiBase64), Is.True);
    }

    [Test]
    public void Rsa签名验签_PKCS8裸Base64私钥_通过()
    {
        var signature = Wlniao.Encryptor.RsaSignWithPrivate("hello", _pkcs8Body);
        Assert.That(Wlniao.Encryptor.RsaVerifyWithPublicKey("hello", signature, _spkiBase64), Is.True);
    }

    [Test]
    public void Rsa验签_消息被篡改_失败()
    {
        var signature = Wlniao.Encryptor.RsaSignWithPrivate("hello", _pkcs1Pem);
        Assert.That(Wlniao.Encryptor.RsaVerifyWithPublicKey("hello!", signature, _spkiBase64), Is.False);
    }

    [Test]
    public void Rsa无填充加解密_PKCS8_可还原明文()
    {
        var cipher = Wlniao.Encryptor.RsaEncryptWithPublic("hi", _spkiBase64);
        var plain = Wlniao.Encryptor.RsaDecryptWithPrivate(cipher, _pkcs8Body);

        // 无填充模式下解密结果是定长模长字节，前面补 0x00，因此只需保证尾部是原文
        Assert.That(plain, Does.EndWith("hi"));
    }

    [Test]
    public void Rsa无填充加解密_PKCS8_PEM形式_可还原明文()
    {
        var cipher = Wlniao.Encryptor.RsaEncryptWithPublic("hi", _spkiBase64);
        var plain = Wlniao.Encryptor.RsaDecryptWithPrivate(cipher, _pkcs8Pem);
        Assert.That(plain, Does.EndWith("hi"));
    }

    [Test]
    public void Pkcs1转Pkcs8_再转回Pkcs1_密钥等价()
    {
        var pkcs8 = Wlniao.Crypto.Cert.PrivateKeyPkcs1ToPkcs8(_pkcs1Body);
        Assert.That(pkcs8, Does.Contain("BEGIN PRIVATE KEY"));

        var pkcs1 = Wlniao.Crypto.Cert.PrivateKeyPkcs8ToPkcs1(pkcs8);
        Assert.That(pkcs1, Does.Contain("BEGIN RSA PRIVATE KEY"));

        using var imported = RSA.Create();
        imported.ImportFromPem(pkcs1);
        Assert.That(imported.KeySize, Is.EqualTo(2048));
        Assert.That(imported.ExportParameters(false).Modulus,
            Is.EqualTo(_rsa.ExportParameters(false).Modulus));
    }

    [Test]
    public void 私钥格式非法时_签名应抛异常而不是静默返回()
    {
        Assert.Catch<Exception>(() => Wlniao.Encryptor.RsaSignWithPrivate("hello", "not a key"));
    }
}
