using System;
using System.Linq;
using System.Text;
using Wlniao.Crypto;

/// <summary>
/// SM2 / SM3 国密算法的 NativeAOT 冒烟测试。
/// 覆盖契约与 Wlniao.XCore.NTest 的 SmCryptoTests 保持一致，但以控制台断言形式运行，
/// 用于在真实原生 AOT 产物里验证国密算法没有被裁剪掉、行为与托管运行一致。
/// 全程零反射，不引入任何 Requires* 抑制。
/// </summary>
public static class SmCryptoSmoke
{
    /// <summary>国标 GB/T 32905 SM3 向量：输入 "abc"</summary>
    private const string VectorAbc = "66c7f0f462eeedd9d1f2d46bdc10e4e24167c4875cf2f7a2297da02b8f4ba8e0";

    /// <summary>国标 GB/T 32905 SM3 向量：输入 64 字节（"abcd" 重复 16 次）</summary>
    private const string VectorAbcd16 = "debe9ff92275b8a138604889c18e5a4d6fdb70e5387e5765293dcba39c0c5732";

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

    private static void CheckEqual(string name, string? actual, string? expected)
    {
        Check(name, string.Equals(actual, expected, StringComparison.Ordinal),
            "expected=" + expected + " actual=" + actual);
    }

    private static void CheckThrows(string name, Action action)
    {
        _total++;
        try
        {
            action();
            _failed++;
            Console.WriteLine("  [FAIL] " + name + " -> 未抛出异常");
        }
        catch (Exception e)
        {
            Console.WriteLine("  [PASS] " + name + " (" + e.GetType().Name + ")");
        }
    }

    private static (string PublicKey, string PrivateKey) CreateKeyPair()
    {
        var tool = new KeyTool();
        tool.OutHex(out var pub, out var priv);
        return (pub, priv);
    }

    /// <summary>
    /// 执行全部 SM2 / SM3 断言。
    /// </summary>
    /// <returns>失败数量，0 表示全部通过。</returns>
    public static int Run()
    {
        Console.WriteLine("--- SM2 / SM3 (NativeAOT) ---");

        #region SM3

        CheckEqual("SM3 abc 国标向量", Wlniao.Encryptor.SM3Encrypt("abc"), VectorAbc);

        var abcd16 = string.Concat(Enumerable.Repeat("abcd", 16));
        CheckEqual("SM3 abcd*16 国标向量", Wlniao.Encryptor.SM3Encrypt(abcd16), VectorAbcd16);

        CheckEqual("SM3 空串返回空串", Wlniao.Encryptor.SM3Encrypt(string.Empty), string.Empty);
        CheckEqual("SM3 null 返回空串", Wlniao.Encryptor.SM3Encrypt(null!), string.Empty);

        Check("SM3 中文输入确定性",
            Wlniao.Encryptor.SM3Encrypt("中文测试") == Wlniao.Encryptor.SM3Encrypt("中文测试"));

        var hello = Wlniao.Encryptor.SM3Encrypt("hello");
        Check("SM3 输出为 64 位小写 hex", hello.Length == 64 && IsLowerHex(hello), hello);

        Check("SM3 不同输入摘要不同",
            Wlniao.Encryptor.SM3Encrypt("Message A") != Wlniao.Encryptor.SM3Encrypt("Message B"));

        var large = new string('a', 1024 * 1024);
        Check("SM3 1MB 大输入确定性",
            Wlniao.Encryptor.SM3Encrypt(large) == Wlniao.Encryptor.SM3Encrypt(large));

        #endregion

        #region Hmac SM3

        Check("HmacSM3 确定性",
            Wlniao.Encryptor.HmacSM3("message", "key") == Wlniao.Encryptor.HmacSM3("message", "key"));
        Check("HmacSM3 随密钥变化",
            Wlniao.Encryptor.HmacSM3("message", "key1") != Wlniao.Encryptor.HmacSM3("message", "key2"));
        Check("HmacSM3 区别于裸 SM3",
            Wlniao.Encryptor.HmacSM3("message", "key") != Wlniao.Encryptor.SM3Encrypt("message"));

        #endregion

        #region SM2 加解密

        var key = CreateKeyPair();
        Check("SM2 公钥为 130 位未压缩 hex",
            key.PublicKey.Length == 130 && key.PublicKey.StartsWith("04", StringComparison.Ordinal));
        Check("SM2 私钥为偶数位 hex",
            key.PrivateKey.Length >= 64 && key.PrivateKey.Length <= 66 && key.PrivateKey.Length % 2 == 0);

        var rtEnc = Wlniao.Encryptor.SM2EncryptByPublicKey("hello", key.PublicKey);
        CheckEqual("SM2 ASCII 往返", Wlniao.Encryptor.SM2DecryptByPrivateKey(rtEnc, key.PrivateKey), "hello");

        var chinese = "中文测试 @#$% 123 Ĝ遥";
        var cnEnc = Wlniao.Encryptor.SM2EncryptByPublicKey(chinese, key.PublicKey);
        CheckEqual("SM2 中文与符号往返", Wlniao.Encryptor.SM2DecryptByPrivateKey(cnEnc, key.PrivateKey), chinese);

        var b64Enc = Wlniao.Encryptor.SM2EncryptByPublicKey("hello", key.PublicKey, true);
        CheckEqual("SM2 Base64 输出往返", Wlniao.Encryptor.SM2DecryptByPrivateKey(b64Enc, key.PrivateKey), "hello");

        CheckEqual("SM2 空明文加密返回空串",
            Wlniao.Encryptor.SM2EncryptByPublicKey(string.Empty, key.PublicKey), string.Empty);
        CheckEqual("SM2 空密文解密返回空串",
            Wlniao.Encryptor.SM2DecryptByPrivateKey(string.Empty, key.PrivateKey), string.Empty);

        var randEncA = Wlniao.Encryptor.SM2EncryptByPublicKey("hello", key.PublicKey);
        var randEncB = Wlniao.Encryptor.SM2EncryptByPublicKey("hello", key.PublicKey);
        Check("SM2 密文随机化（同明文两次不同）", randEncA != randEncB);
        Check("SM2 随机密文均可正确解密",
            Wlniao.Encryptor.SM2DecryptByPrivateKey(randEncA, key.PrivateKey) == "hello"
            && Wlniao.Encryptor.SM2DecryptByPrivateKey(randEncB, key.PrivateKey) == "hello");

        var largePayload = new string('x', 4096);
        CheckEqual("SM2 4KB 大载荷往返",
            Wlniao.Encryptor.SM2DecryptByPrivateKey(
                Wlniao.Encryptor.SM2EncryptByPublicKey(largePayload, key.PublicKey), key.PrivateKey),
            largePayload);

        var tool2 = new KeyTool();
        tool2.GenerateKey(out var pubBytes, out var privBytes);
        var plainBytes = Encoding.UTF8.GetBytes("hello bytes");
        var bytesEncHex = Wlniao.Encryptor.SM2EncryptByPublicKey(plainBytes, pubBytes);
        CheckEqual("SM2 byte[] 重载往返",
            Wlniao.Encryptor.SM2DecryptByPrivateKey(System.Convert.FromHexString(bytesEncHex), privBytes),
            "hello bytes");

        var attackerKey = CreateKeyPair();
        var victimEnc = Wlniao.Encryptor.SM2EncryptByPublicKey("secret", key.PublicKey);
        CheckThrows("SM2 错误私钥解密应抛异常",
            () => Wlniao.Encryptor.SM2DecryptByPrivateKey(victimEnc, attackerKey.PrivateKey));

        #endregion

        #region SM2 签名验签

        var signKey = CreateKeyPair();
        var sign = Wlniao.Encryptor.Sm2SignWithPrivateKey("hello", signKey.PrivateKey);
        Check("SM2 签名验签通过", Wlniao.Encryptor.Sm2VerifyWithPublicKey("hello", sign, signKey.PublicKey));
        Check("SM2 篡改文本验签失败",
            !Wlniao.Encryptor.Sm2VerifyWithPublicKey("hello2", sign, signKey.PublicKey));
        Check("SM2 错误公钥验签失败",
            !Wlniao.Encryptor.Sm2VerifyWithPublicKey("hello", sign, attackerKey.PublicKey));
        Check("SM2 签名随机化",
            Wlniao.Encryptor.Sm2SignWithPrivateKey("hello", signKey.PrivateKey) != sign);

        #endregion

        Console.WriteLine("  SM2/SM3 结果: " + (_total - _failed) + "/" + _total + " 通过");
        return _failed;
    }

    private static bool IsLowerHex(string value)
    {
        foreach (var c in value)
        {
            var ok = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f');
            if (!ok)
            {
                return false;
            }
        }
        return true;
    }
}
