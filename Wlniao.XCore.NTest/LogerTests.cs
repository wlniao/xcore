using Wlniao;
using Wlniao.Log;

namespace NTest
{
    /// <summary>
    /// 文件日志写入的回归断言，覆盖：
    ///   正常路径 —— 按「日期/info.log」落盘，内容包含消息原文
    ///   失败路径 —— 目标文件被独占占用时异常不得抛给调用方，且不能残留句柄导致后续写入永久失败
    /// </summary>
    public class LogerTests
    {
        private static string NewTempDir()
        {
            return Path.Combine(Path.GetTempPath(), "xcore-log-" + Guid.NewGuid().ToString("N"));
        }

        [Test]
        public void Write_WritesDatedFileWithMessage()
        {
            var dir = NewTempDir();
            try
            {
                new FileLoger(LogLevel.Debug, dir).Info("hello-loger");

                var file = Path.Combine(dir, DateTools.FormatDate(), "info.log");
                Assert.That(File.Exists(file), Is.True, "日志应写入 日期/info.log");
                Assert.That(File.ReadAllText(file), Does.Contain("hello-loger"));
            }
            finally
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, true);
                }
            }
        }

        [Test]
        public void Write_SwallowsError_AndKeepsLoggingUsable()
        {
            var dir = NewTempDir();
            var dateDir = Path.Combine(dir, DateTools.FormatDate());
            Directory.CreateDirectory(dateDir);
            var file = Path.Combine(dateDir, "info.log");
            try
            {
                var loger = new FileLoger(LogLevel.Debug, dir);

                using (new FileStream(file, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                {
                    Assert.DoesNotThrow(() => loger.Info("locked"), "写盘失败只能吞掉，不能反向打断业务");
                }

                // 写失败后若句柄没有释放，这次追加会继续撞共享冲突，文件里就不会出现这一行
                Assert.DoesNotThrow(() => loger.Info("after-unlock"));
                Assert.That(File.ReadAllText(file), Does.Contain("after-unlock"));
            }
            finally
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, true);
                }
            }
        }
    }
}
