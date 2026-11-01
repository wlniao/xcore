using System.Diagnostics;
using Wlniao.Pattern;

namespace NTest
{
    /// <summary>
    /// 正则表达式安全测试：守住「模式不得产生指数级回溯」这条底线。
    ///
    /// <see cref="Wlniao.Pattern.Regex.Url"/> 是公开常量，调用方会直接拿它去校验外部输入。
    /// 只要模式里出现外层量词套内层可空量词（如 <c>([-.\w]*[0-9a-zA-Z])*</c>），
    /// 一段几十字符的畸形 URL 就能让一次 IsMatch 跑到几十秒 —— 单线程被挂死即为拒绝服务。
    /// </summary>
    public class RegexSafetyTests
    {
        /// <summary>
        /// 构造「看起来像 URL 前缀、尾部却不可能匹配」的病态输入。
        /// </summary>
        private static string PathologicalUrl(int hostLength)
        {
            return "http://" + new string('a', hostLength) + "!";
        }

        [Test]
        public void Url_RejectsPathologicalInput_WithinLinearTime()
        {
            // 指数级回溯下 30 字符需要数十秒；线性匹配下同一输入耗时为微秒级，
            // 2 秒阈值留出三个数量级余量，避免 CI 抖动误报
            var input = PathologicalUrl(30);

            var watch = Stopwatch.StartNew();
            var matched = Regex.IsMatch(input, Regex.Url);
            watch.Stop();

            Assert.That(matched, Is.False);
            Assert.That(watch.ElapsedMilliseconds, Is.LessThan(2000),
                $"Url 正则疑似出现指数级回溯：{input.Length} 字符耗时 {watch.ElapsedMilliseconds}ms");
        }

        [Test]
        public void Url_RejectsLongPathologicalInput_WithinLinearTime()
        {
            // 长度翻到 5000 也是线性增长；一旦回退到嵌套量词写法，这里会直接挂住
            var input = PathologicalUrl(5000);

            var watch = Stopwatch.StartNew();
            var matched = Regex.IsMatch(input, Regex.Url);
            watch.Stop();

            Assert.That(matched, Is.False);
            Assert.That(watch.ElapsedMilliseconds, Is.LessThan(2000),
                $"Url 正则耗时随输入长度异常增长：{input.Length} 字符耗时 {watch.ElapsedMilliseconds}ms");
        }

        [Test]
        public void Url_AcceptsQueryStringWithAmpersandAndEquals()
        {
            // 原模式路径字符类里没有 '='，且 '&' 被写成了 HTML 实体 &amp;，
            // 结果是把 & 展开成「& a m p ;」五个字符，正常查询串反而匹配不上
            Assert.That(Regex.IsMatch("https://a.com/list?page=1&size=20", Regex.Url), Is.True);
            Assert.That(Regex.IsMatch("https://a.com/path/to/file.html#anchor", Regex.Url), Is.True);
        }

        [Test]
        public void Url_AcceptsPortRange_AndRejectsOverlongPort()
        {
            Assert.That(Regex.IsMatch("http://example.com:1/", Regex.Url), Is.True);
            Assert.That(Regex.IsMatch("http://example.com:65535/", Regex.Url), Is.True);
            Assert.That(Regex.IsMatch("http://example.com:123456/", Regex.Url), Is.False, "端口超过 5 位应判为非法");
            Assert.That(Regex.IsMatch("http://example.com:/", Regex.Url), Is.False, "空端口应判为非法");
        }

        [Test]
        public void Url_RejectsBackToBackDots()
        {
            // 逐标签匹配要求每段以字母数字开头结尾，连续点号属于畸形主机名
            Assert.That(Regex.IsMatch("http://a..b.com/", Regex.Url), Is.False);
            Assert.That(Regex.IsMatch("http://sub.domain.example.com/", Regex.Url), Is.True);
        }

        [Test]
        public void IsMatch_WithTimeout_ThrowsOnExpensivePattern()
        {
            // 公开的超时重载：即便调用方传入自己写的非安全模式，也能在超时后中断而不是挂死
            Assert.Throws<System.Text.RegularExpressions.RegexMatchTimeoutException>(() =>
                Regex.IsMatch(new string('a', 32) + "!", @"^(a+)+$", System.TimeSpan.FromMilliseconds(1)));
        }
    }
}
