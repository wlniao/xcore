using System;
using System.Text;
using NUnit.Framework;
using Wlniao.Crypto;

namespace Wlniao.XCore.NTest
{
    /// <summary>
    /// KeyTool.GetPublicKeyFromPrivateKey（由 SM2 私钥推导公钥，Q = dG）的回归断言：
    ///   国标向量    —— GB/T 32918.5 附录 A 示例密钥对，锁定算法正确性
    ///   往返一致    —— GenerateKey 产出的私钥推导结果必须与产出的公钥一致
    ///   编码兼容    —— Hex / Base64 输入、带前导 0x00 的 33 字节私钥
    ///   黑盒闭环    —— 仅私钥的 SM2 实例签名，用推导公钥的实例验签必须通过
    ///   异常路径    —— 空私钥、全 0、超出曲线阶的私钥都给出明确错误
    /// </summary>
    [TestFixture]
    public class Sm2KeyDeriveTests
    {
        /// <summary>国标 GB/T 32918.5 附录 A 示例私钥 d</summary>
        private const string StdPrivateHex = "3945208F7B2144B13F36E38AC6D39F95889393692860B51A42FB81EF4DF7C5B8";

        /// <summary>国标 GB/T 32918.5 附录 A 示例公钥 04||X||Y</summary>
        private const string StdPublicHex =
            "04" +
            "09F9DF311E5421A150DD7D161E4BC5C672179FAD1833FC076BB08FF356F35020" +
            "CCEA490CE26775A52DC6EA718CC1AA600AED05FBF35E084A6632F6072DA9AD13";

        [Test]
        public void ByteOverload_MatchesOfficialVector()
        {
            var priv = System.Convert.FromHexString(StdPrivateHex);
            var derived = KeyTool.GetPublicKeyFromPrivateKey(priv);

            Assert.That(System.Convert.ToHexString(derived), Is.EqualTo(StdPublicHex),
                "推导结果必须与国标示例公钥一致。");
        }

        [Test]
        public void StringOverload_MatchesOfficialVector()
        {
            Assert.That(KeyTool.GetPublicKeyFromPrivateKey(StdPrivateHex),
                Is.EqualTo(StdPublicHex.ToLowerInvariant()));
        }

        [Test]
        public void DerivedKey_MatchesGeneratedKey()
        {
            var tool = new KeyTool();
            var derived = KeyTool.GetPublicKeyFromPrivateKey(tool.PrivateKey);

            Assert.That(derived, Is.EqualTo(tool.PublicKey),
                "GenerateKey 产出的私钥推导出的公钥必须就是产出的公钥本身。");
        }

        [Test]
        public void StringOverload_AcceptsBase64Input()
        {
            var priv = System.Convert.FromHexString(StdPrivateHex);
            var base64 = System.Convert.ToBase64String(priv);

            var derived = KeyTool.GetPublicKeyFromPrivateKey(base64);

            Assert.That(derived, Is.EqualTo(StdPublicHex.ToLowerInvariant()));
        }

        [Test]
        public void PaddedPrivateKey_SameAsTrimmed()
        {
            // GenerateKey 输出的私钥在最高位为 1 时会带前导 0x00（33 字节），
            // 这里手工构造一个带前导 0x00 的私钥，推导结果必须与截断版一致
            var priv32 = System.Convert.FromHexString(StdPrivateHex);
            var priv33 = new byte[priv32.Length + 1];
            Array.Copy(priv32, 0, priv33, 1, priv32.Length);

            Assert.That(System.Convert.ToHexString(KeyTool.GetPublicKeyFromPrivateKey(priv33)).ToLowerInvariant(),
                Is.EqualTo(StdPublicHex.ToLowerInvariant()));
        }

        [Test]
        public void SignWithPrivateKeyOnly_VerifiesWithDerivedPublicKey()
        {
            var tool = new KeyTool();
            var message = Encoding.UTF8.GetBytes("derive-and-verify roundtrip 国密");

            // 仅私钥构造的实例内部也是 Q = dG 现算公钥，签名结果必须能被推导公钥验签
            var signer = new SM2(null!, tool.PrivateKey);
            var signature = signer.Sign(message);

            var derivedPub = KeyTool.GetPublicKeyFromPrivateKey(tool.PrivateKey);
            var verifier = new SM2(derivedPub, null!);

            Assert.That(verifier.VerifySign(message, signature), Is.True,
                "用推导公钥无法验签，说明推导结果与私钥不匹配。");
        }

        [Test]
        public void NullOrEmpty_Throws()
        {
            Assert.That(() => KeyTool.GetPublicKeyFromPrivateKey((byte[])null!), Throws.ArgumentException);
            Assert.That(() => KeyTool.GetPublicKeyFromPrivateKey(Array.Empty<byte>()), Throws.ArgumentException);
            Assert.That(() => KeyTool.GetPublicKeyFromPrivateKey((string)null!), Throws.ArgumentException);
            Assert.That(() => KeyTool.GetPublicKeyFromPrivateKey("   "), Throws.ArgumentException);
        }

        [Test]
        public void ScalarOutOfRange_Throws()
        {
            // d = 0：推导得到无穷远点，无法编码为公钥
            Assert.That(() => KeyTool.GetPublicKeyFromPrivateKey(new byte[32]), Throws.ArgumentException);

            // d = n：超出曲线阶，不是合法私钥
            var n = System.Convert.FromHexString("FFFFFFFEFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF7203DF6B21C6052B53BBF40939D54123");
            var ex = Assert.Throws<ArgumentException>(
                () => KeyTool.GetPublicKeyFromPrivateKey(n))!;
            Assert.That(ex.Message, Does.Contain("[1, n-1]"));
        }
    }
}
