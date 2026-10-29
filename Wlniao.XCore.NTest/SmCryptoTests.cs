using System;
using System.Linq;
using System.Text;
using Wlniao.Crypto;

namespace Wlniao.XCore.NTest
{
    /// <summary>
    /// SM2 / SM3 国密算法的完整单元测试。
    /// SM3 部分使用国标 GB/T 32905 官方测试向量校验实现正确性；
    /// SM2 部分覆盖加解密往返、字节数组重载、Base64 输出、异常路径与签名验签。
    /// </summary>
    public class SmCryptoTests
    {
        /// <summary>国标 SM3 向量：Hello("abc")</summary>
        private const string VectorAbc = "66c7f0f462eeedd9d1f2d46bdc10e4e24167c4875cf2f7a2297da02b8f4ba8e0";

        /// <summary>国标 SM3 向量：64 字节输入（"abcd" 重复 16 次）</summary>
        private const string VectorAbcd16 = "debe9ff92275b8a138604889c18e5a4d6fdb70e5387e5765293dcba39c0c5732";

        /// <summary>生成一对 SM2 密钥的 Hex 形式（公钥为 130 位未压缩格式）</summary>
        private static (string PublicKey, string PrivateKey) CreateKeyPair()
        {
            var tool = new KeyTool();
            tool.OutHex(out var pub, out var priv);
            return (pub, priv);
        }

        #region SM3 摘要

        [Test]
        public void SM3Encrypt_MatchesOfficialVector_Abc()
        {
            Assert.That(Wlniao.Encryptor.SM3Encrypt("abc"), Is.EqualTo(VectorAbc));
        }

        [Test]
        public void SM3Encrypt_MatchesOfficialVector_64ByteInput()
        {
            var input = string.Concat(Enumerable.Repeat("abcd", 16));
            Assert.That(input.Length, Is.EqualTo(64));
            Assert.That(Wlniao.Encryptor.SM3Encrypt(input), Is.EqualTo(VectorAbcd16));
        }

        [Test]
        public void SM3Encrypt_EmptyString_ReturnsEmptyString()
        {
            // 实现约定：空输入直接返回空串，而不是 SM3("") 的标准摘要值。
            // 此处锁定该约定，若日后改为返回标准摘要，测试会失败并提醒同步调用方。
            Assert.That(Wlniao.Encryptor.SM3Encrypt(""), Is.EqualTo(string.Empty));
        }

        [Test]
        public void SM3Encrypt_Null_ReturnsEmptyString()
        {
            Assert.That(Wlniao.Encryptor.SM3Encrypt(null!), Is.EqualTo(string.Empty));
        }

        [Test]
        public void SM3Encrypt_IsDeterministic_ForChineseInput()
        {
            var first = Wlniao.Encryptor.SM3Encrypt("中文测试");
            var second = Wlniao.Encryptor.SM3Encrypt("中文测试");
            Assert.That(first, Is.Not.Null.And.Not.Empty);
            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void SM3Encrypt_Returns64LowercaseHexChars()
        {
            var digest = Wlniao.Encryptor.SM3Encrypt("hello");
            Assert.That(digest.Length, Is.EqualTo(64));
            Assert.That(digest, Does.Match("^[0-9a-f]{64}$"));
        }

        [Test]
        public void SM3Encrypt_DifferentInputs_ProduceDifferentDigests()
        {
            var a = Wlniao.Encryptor.SM3Encrypt("Message A");
            var b = Wlniao.Encryptor.SM3Encrypt("Message B");
            Assert.That(a, Is.Not.EqualTo(b));
        }

        [Test]
        public void SM3Encrypt_LargeInput_IsDeterministicAnd64Hex()
        {
            var large = new string('a', 1024 * 1024);
            var first = Wlniao.Encryptor.SM3Encrypt(large);
            var second = Wlniao.Encryptor.SM3Encrypt(large);
            Assert.That(first, Is.EqualTo(second));
            Assert.That(first.Length, Is.EqualTo(64));
        }

        [Test]
        public void SM3Encrypt_Utf8BytesUsed_ChineseNotGarbled()
        {
            // 中文与英文摘要长度一致，且互不相同，间接确认按 UTF-8 编码处理而非丢失字符。
            var cn = Wlniao.Encryptor.SM3Encrypt("中文");
            var en = Wlniao.Encryptor.SM3Encrypt("en");
            Assert.That(cn.Length, Is.EqualTo(en.Length));
            Assert.That(cn, Is.Not.EqualTo(en));
        }

        #endregion

        #region Hmac SM3

        [Test]
        public void HmacSM3_IsDeterministic()
        {
            var first = Wlniao.Encryptor.HmacSM3("message", "key");
            var second = Wlniao.Encryptor.HmacSM3("message", "key");
            Assert.That(first, Is.Not.Null.And.Not.Empty);
            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void HmacSM3_VariesWithKey()
        {
            var k1 = Wlniao.Encryptor.HmacSM3("message", "key1");
            var k2 = Wlniao.Encryptor.HmacSM3("message", "key2");
            Assert.That(k1, Is.Not.EqualTo(k2));
        }

        [Test]
        public void HmacSM3_DiffersFromPlainSM3()
        {
            var plain = Wlniao.Encryptor.SM3Encrypt("message");
            var hmac = Wlniao.Encryptor.HmacSM3("message", "key");
            Assert.That(hmac, Is.Not.EqualTo(plain));
            Assert.That(hmac.Length, Is.EqualTo(64));
        }

        #endregion

        #region SM2 密钥

        [Test]
        public void SM2_GeneratedPublicKey_IsUncompressed130Hex()
        {
            var (pub, _) = CreateKeyPair();
            Assert.That(pub.Length, Is.EqualTo(130));
            Assert.That(pub, Does.Match("^04[0-9a-f]{128}$"));
        }

        [Test]
        public void SM2_GeneratedPrivateKey_IsEvenLengthHex()
        {
            // SM2 私钥为 32 字节，BytesToHexString 时可能因最高位符号位多带一个 0x00，
            // 实际长度在 64 ~ 66 之间波动。
            var (_, priv) = CreateKeyPair();
            Assert.That(priv.Length, Is.GreaterThanOrEqualTo(64));
            Assert.That(priv.Length, Is.LessThanOrEqualTo(66));
            Assert.That(priv.Length % 2, Is.EqualTo(0));
            Assert.That(priv, Does.Match("^[0-9a-f]+$"));
        }

        [Test]
        public void SM2_GeneratedKeyPairs_AreUnique()
        {
            var first = CreateKeyPair();
            var second = CreateKeyPair();
            Assert.That(first.PublicKey, Is.Not.EqualTo(second.PublicKey));
            Assert.That(first.PrivateKey, Is.Not.EqualTo(second.PrivateKey));
        }

        #endregion

        #region SM2 加解密

        [Test]
        public void SM2_RoundTrip_Ascii()
        {
            var (pub, priv) = CreateKeyPair();
            var enc = Wlniao.Encryptor.SM2EncryptByPublicKey("hello", pub);
            Assert.That(Wlniao.Encryptor.SM2DecryptByPrivateKey(enc, priv), Is.EqualTo("hello"));
        }

        [Test]
        public void SM2_RoundTrip_ChineseAndSymbols()
        {
            var plain = "中文测试 @#$% 123 Ĝ遥";
            var (pub, priv) = CreateKeyPair();
            var enc = Wlniao.Encryptor.SM2EncryptByPublicKey(plain, pub);
            Assert.That(Wlniao.Encryptor.SM2DecryptByPrivateKey(enc, priv), Is.EqualTo(plain));
        }

        [Test]
        public void SM2_RoundTrip_Base64Output()
        {
            var (pub, priv) = CreateKeyPair();
            var enc = Wlniao.Encryptor.SM2EncryptByPublicKey("hello", pub, true);
            Assert.That(Wlniao.Encryptor.SM2DecryptByPrivateKey(enc, priv), Is.EqualTo("hello"));
        }

        [Test]
        public void SM2_RoundTrip_ByteArrayOverload()
        {
            var tool = new KeyTool();
            tool.GenerateKey(out var pubBytes, out var privBytes);
            var plainBytes = Encoding.UTF8.GetBytes("hello bytes");

            var encHex = Wlniao.Encryptor.SM2EncryptByPublicKey(plainBytes, pubBytes);
            var backBytes = Wlniao.Encryptor.SM2DecryptByPrivateKey(System.Convert.FromHexString(encHex), privBytes);
            Assert.That(backBytes, Is.EqualTo("hello bytes"));
        }

        [Test]
        public void SM2_RoundTrip_LargePayload()
        {
            var large = new string('x', 4096);
            var (pub, priv) = CreateKeyPair();
            Assert.That(Wlniao.Encryptor.SM2DecryptByPrivateKey(
                Wlniao.Encryptor.SM2EncryptByPublicKey(large, pub), priv), Is.EqualTo(large));
        }

        [Test]
        public void SM2_Encrypt_EmptyPlaintext_ReturnsEmptyString()
        {
            var (pub, _) = CreateKeyPair();
            Assert.That(Wlniao.Encryptor.SM2EncryptByPublicKey("", pub), Is.EqualTo(string.Empty));
        }

        [Test]
        public void SM2_Decrypt_EmptyCiphertext_ReturnsEmptyString()
        {
            var (_, priv) = CreateKeyPair();
            Assert.That(Wlniao.Encryptor.SM2DecryptByPrivateKey("", priv), Is.EqualTo(string.Empty));
        }

        [Test]
        public void SM2_Encrypt_UsesRandomCiphertext()
        {
            var (pub, priv) = CreateKeyPair();
            var first = Wlniao.Encryptor.SM2EncryptByPublicKey("hello", pub);
            var second = Wlniao.Encryptor.SM2EncryptByPublicKey("hello", pub);

            Assert.That(first, Is.Not.EqualTo(second), "SM2 应使用随机数 k，相同明文两次密文不同");
            Assert.That(Wlniao.Encryptor.SM2DecryptByPrivateKey(first, priv), Is.EqualTo("hello"));
            Assert.That(Wlniao.Encryptor.SM2DecryptByPrivateKey(second, priv), Is.EqualTo("hello"));
        }

        [Test]
        public void SM2_Decrypt_WithWrongPrivateKey_Throws()
        {
            var attackerKey = CreateKeyPair();
            var victimKey = CreateKeyPair();
            var enc = Wlniao.Encryptor.SM2EncryptByPublicKey("secret", victimKey.PublicKey);

            Assert.Catch<Exception>(() => Wlniao.Encryptor.SM2DecryptByPrivateKey(enc, attackerKey.PrivateKey));
        }

        #endregion

        #region SM2 签名验签

        [Test]
        public void SM2_SignAndVerify_ValidSignature()
        {
            var (pub, priv) = CreateKeyPair();
            var sign = Wlniao.Encryptor.Sm2SignWithPrivateKey("hello", priv);
            Assert.That(Wlniao.Encryptor.Sm2VerifyWithPublicKey("hello", sign, pub), Is.True);
        }

        [Test]
        public void SM2_Verify_TamperedMessage_Fails()
        {
            var (pub, priv) = CreateKeyPair();
            var sign = Wlniao.Encryptor.Sm2SignWithPrivateKey("hello", priv);
            Assert.That(Wlniao.Encryptor.Sm2VerifyWithPublicKey("hello2", sign, pub), Is.False);
        }

        [Test]
        public void SM2_Verify_WrongPublicKey_Fails()
        {
            var other = CreateKeyPair();
            var (_, priv) = CreateKeyPair();
            var sign = Wlniao.Encryptor.Sm2SignWithPrivateKey("hello", priv);
            Assert.That(Wlniao.Encryptor.Sm2VerifyWithPublicKey("hello", sign, other.PublicKey), Is.False);
        }

        [Test]
        public void SM2_Sign_IsRandomized()
        {
            var (_, priv) = CreateKeyPair();
            var first = Wlniao.Encryptor.Sm2SignWithPrivateKey("hello", priv);
            var second = Wlniao.Encryptor.Sm2SignWithPrivateKey("hello", priv);
            Assert.That(first, Is.Not.EqualTo(second), "SM2 签名含随机数，同一私钥两次签名应不同");
        }

        #endregion
    }
}
