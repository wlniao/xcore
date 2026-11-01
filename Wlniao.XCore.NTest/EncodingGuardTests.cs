using System;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace NTest
{
    /// <summary>
    /// 源码编码守卫：断言全仓源码必须是合法 UTF-8，且不得含 U+FFFD 替换字符，
    /// 防止源码被写成 GBK 或产生乱码。
    /// </summary>
    public class EncodingGuardTests
    {
        /// <summary>
        /// 取本文件在编译期的绝对路径。
        /// </summary>
        /// <remarks>
        /// 不能用 <c>AppContext.BaseDirectory</c> / <c>CurrentDirectory</c> 反推仓库根：
        /// 项目把 bin/obj 重定向到了临时目录，运行时这两个路径都不在仓库内，向上遍历永远找不到
        /// 解决方案文件。编译期路径则恒定指向仓库里的测试源码，与输出目录无关。
        /// </remarks>
        private static string GetThisFilePath(
            [System.Runtime.CompilerServices.CallerFilePath] string path = "")
        {
            return path;
        }

        private static string? FindRepoRoot()
        {
            // 本文件位于 <repo>/Wlniao.XCore.NTest/EncodingGuardTests.cs，其上级目录即仓库根
            var thisFile = GetThisFilePath();
            var dir = string.IsNullOrEmpty(thisFile) ? null : new DirectoryInfo(Path.GetDirectoryName(thisFile)!);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "Wlniao.XCore.sln"))
                    || File.Exists(Path.Combine(dir.FullName, "Wlniao.XCore", "Json.cs")))
                {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }

            // 兜底：仍按运行目录向上找，覆盖源码目录被整体搬移的场景
            dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "Wlniao.XCore.sln")))
                {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }

            return null;
        }

        [Test]
        public void AllSourceFiles_AreValidUtf8WithoutReplacementChars()
        {
            var root = FindRepoRoot();
            if (root == null)
            {
                TestContext.Out.WriteLine("编码守卫扫描起点 AppContext.BaseDirectory = " + AppContext.BaseDirectory);
                TestContext.Out.WriteLine("编码守卫扫描起点 CurrentDirectory = " + Environment.CurrentDirectory);
                TestContext.Out.WriteLine("编译期源码路径 = " + GetThisFilePath());
                Assert.Fail("未定位到仓库根目录（未找到 Wlniao.XCore.sln 或 Wlniao.XCore/Json.cs），编码守卫无法执行。");
                return;
            }

            var bad = new StringBuilder();
            var checkedCount = 0;
            var extensions = new[] { ".cs", ".csproj", ".sln", ".md", ".json", ".config", ".resx" };

            foreach (var file in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
            {
                if (file.IndexOf(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal) >= 0
                    || file.IndexOf(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal) >= 0
                    || file.IndexOf(Path.DirectorySeparatorChar + ".git" + Path.DirectorySeparatorChar, StringComparison.Ordinal) >= 0)
                {
                    continue;
                }
                if (Array.IndexOf(extensions, Path.GetExtension(file).ToLowerInvariant()) < 0)
                {
                    continue;
                }

                checkedCount++;
                byte[] bytes;
                try
                {
                    bytes = File.ReadAllBytes(file);
                }
                catch (IOException)
                {
                    continue;
                }

                // 严格 UTF-8 解码（忽略 BOM）；任何非法字节序列都说明不是 UTF-8
                var offset = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? 3 : 0;
                string text;
                try
                {
                    text = new UTF8Encoding(false, true).GetString(bytes, offset, bytes.Length - offset);
                }
                catch (DecoderFallbackException)
                {
                    bad.AppendLine("非 UTF-8 编码：").AppendLine("    " + Relative(root, file));
                    continue;
                }

                if (text.IndexOf('\uFFFD') >= 0)
                {
                    bad.AppendLine("含 U+FFFD 替换字符（内容已损坏无法还原）：").AppendLine("    " + Relative(root, file));
                }
            }

            Assert.That(checkedCount > 0, Is.True, "未扫描到任何源文件，检查路径配置是否正确。");
            Assert.That(bad.Length == 0, Is.True,
                "发现编码异常文件（请转存为 UTF-8 并修复注释）：" + Environment.NewLine + bad);
            TestContext.Out.WriteLine("已校验 {0} 个源文件，编码全部正常。", checkedCount);
        }

        private static string Relative(string root, string file)
        {
            return file.StartsWith(root, StringComparison.OrdinalIgnoreCase)
                ? file.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar)
                : file;
        }
    }
}
