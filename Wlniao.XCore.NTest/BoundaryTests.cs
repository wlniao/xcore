using System;
using System.Text;
using Wlniao.Crypto;
using Wlniao.IO;
using Wlniao.Text;

namespace NTest
{
    /// <summary>
    /// 边界与错误信息类回归断言，覆盖：
    ///   SM3.DoFinal(byte[], int) —— 输出缓冲区越界时立即报错，而不是写坏堆内存
    ///   Encodings.GetEncoding    —— 编码名不存在时保留真实原因，不误诊为「未注册提供器」
    ///   PathTool.Map             —— '~' 只作为前缀剥掉，路径中间的 '~' 保持原样
    /// </summary>
    public class BoundaryTests
    {
        [Test]
        public void SM3_DoFinal_Array_RejectsTooSmallBuffer()
        {
            var sm3 = new SM3();
            var input = Encoding.UTF8.GetBytes("abc");
            sm3.BlockUpdate(input, 0, input.Length);

            Assert.Throws<ArgumentException>(() => sm3.DoFinal(new byte[31], 0));
            Assert.Throws<ArgumentException>(() => sm3.DoFinal(new byte[32], 1), "起始偏移导致越界同样要拒绝");
            Assert.Throws<ArgumentNullException>(() => sm3.DoFinal(null!, 0));
        }

        [Test]
        public void SM3_DoFinal_Array_AllowsExactBuffer()
        {
            var sm3 = new SM3();
            var input = Encoding.UTF8.GetBytes("abc");
            sm3.BlockUpdate(input, 0, input.Length);

            var buffer = new byte[32];
            Assert.That(sm3.DoFinal(buffer, 0), Is.EqualTo(32));
            Assert.That(Convert.ToHexString(buffer).ToLowerInvariant(),
                Is.EqualTo("66c7f0f462eeedd9d1f2d46bdc10e4e24167c4875cf2f7a2297da02b8f4ba8e0"));
        }

        [Test]
        public void Encodings_UnknownName_KeepsOriginalReason()
        {
            var ex = Assert.Throws<ArgumentException>(() => Encodings.GetEncoding("NOT-A-REAL-ENCODING"));

            // 关键点：错误信息里要能看到「编码名不存在」这类真实原因，
            // 而不是只剩一句「请先注册提供器」把排查方向带偏
            Assert.That(ex!.Message, Does.Contain("NOT-A-REAL-ENCODING").IgnoreCase
                .Or.Contain("encoding").IgnoreCase);
            Assert.That(ex.InnerException, Is.Not.Null, "必须带上底层异常，便于定位");
        }

        [Test]
        public void Encodings_Gbk_StillResolvesAfterProviderRegistration()
        {
            // GBK 是 CodePages 提供器的典型场景，注册后重试的路径必须照常可用
            var gbk = Encodings.GetEncoding("GBK");
            var text = "中文编码测试";
            Assert.That(gbk.GetString(gbk.GetBytes(text)), Is.EqualTo(text));
        }

        [Test]
        public void PathTool_Map_ExpandsTildeAsRootPrefix()
        {
            var root = PathTool.Map();
            Assert.That(PathTool.Map("~"), Is.EqualTo(root));
            Assert.That(PathTool.Map("~/a/b.txt"), Is.EqualTo(PathTool.Map("a/b.txt")));
        }

        [Test]
        public void PathTool_Map_KeepsTildeInMiddleOfPath()
        {
            // '/tmp/~backup' 里的 '~' 是普通字符：整串替换会把它删掉，指向完全不同的目录
            var path = PathTool.Map("/tmp/~backup/a.txt");
            Assert.That(path, Does.Contain("~backup"), "路径中间的 '~' 不应被删除");
        }
    }
}
