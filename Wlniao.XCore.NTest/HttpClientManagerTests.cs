using System.Collections;
using System.Reflection;
using Wlniao.Net;

namespace NTest
{
    /// <summary>
    /// HttpClientManager 的生命周期回归断言，覆盖：
    ///   SharedInstance —— 并发首次访问只创建一个管理器（原实现用 ??=，并发下会各自创建并泄漏 Timer 与连接池）
    ///   刷新           —— 旧实例先进入退役队列，宽限期过后才真正释放（避免掐断在途请求）
    ///   Dispose        —— 当前实例与退役队列一并清理
    /// </summary>
    public class HttpClientManagerTests
    {
        private static readonly FieldInfo? RetiredField =
            typeof(HttpClientManager).GetField("_retired", BindingFlags.NonPublic | BindingFlags.Instance);

        private static IEnumerable RetiredQueue(HttpClientManager manager)
        {
            Assert.That(RetiredField, Is.Not.Null, "HttpClientManager 应保留 _retired 队列");
            var queue = RetiredField!.GetValue(manager) as IEnumerable;
            Assert.That(queue, Is.Not.Null);
            return queue;
        }

        /// <summary>
        /// 取管理器内部的同步对象：退役队列由定时器线程在锁内增删，
        /// 测试侧若不加同一把锁去遍历，就会撞上 "Collection was modified"。
        /// </summary>
        private static object SyncRoot(HttpClientManager manager)
        {
            var field = typeof(HttpClientManager).GetField("_lock", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, "HttpClientManager 应保留 _lock 同步对象");
            return field!.GetValue(manager)!;
        }

        private static int CountRetired(HttpClientManager manager)
        {
            lock (SyncRoot(manager))
            {
                var count = 0;
                foreach (var _ in RetiredQueue(manager))
                {
                    count++;
                }
                return count;
            }
        }

        /// <summary>
        /// 退役队列里是否还留着指定实例（队列元素是 (Client, DueUtc) 元组，Item1 为实例本身）。
        /// </summary>
        private static bool ContainsRetired(HttpClientManager manager, System.Net.Http.HttpClient client)
        {
            lock (SyncRoot(manager))
            {
                foreach (var item in RetiredQueue(manager))
                {
                    var clientField = item?.GetType().GetField("Item1");
                    if (clientField != null && ReferenceEquals(clientField.GetValue(item), client))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        private static bool WaitUntil(System.Func<bool> condition, int timeoutMilliseconds = 5000)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            while (watch.ElapsedMilliseconds < timeoutMilliseconds)
            {
                if (condition())
                {
                    return true;
                }
                System.Threading.Thread.Sleep(20);
            }
            return condition();
        }

        [Test]
        public void SharedInstance_IsSingleton_UnderConcurrentFirstAccess()
        {
            var results = new System.Net.Http.HttpClient[64];
            System.Threading.Tasks.Parallel.For(0, results.Length, i => results[i] = HttpClientManager.SharedInstance);

            Assert.That(results, Has.All.Not.Null);
            Assert.That(results[0], Is.SameAs(results[^1]), "并发首次访问必须拿到同一个共享实例");
        }

        [Test]
        public void Refresh_RetiresOldClient_InsteadOfDisposingImmediately()
        {
            var manager = new HttpClientManager(System.TimeSpan.FromMilliseconds(50), System.TimeSpan.FromSeconds(30));
            try
            {
                var client = manager.HttpClient;

                Assert.That(WaitUntil(() => ContainsRetired(manager, client)), Is.True,
                    "刷新后旧实例应进入退役队列等待宽限期，而不是当场释放");
            }
            finally
            {
                manager.Dispose();
            }
        }

        [Test]
        public void Refresh_DisposesRetiredClient_AfterGraceExpires()
        {
            var manager = new HttpClientManager(System.TimeSpan.FromMilliseconds(50), System.TimeSpan.FromSeconds(2));
            try
            {
                var client = manager.HttpClient;

                Assert.That(WaitUntil(() => ContainsRetired(manager, client)), Is.True, "刷新后旧实例应先进入退役队列");
                Assert.That(WaitUntil(() => !ContainsRetired(manager, client), 8000), Is.True,
                    "宽限期过后旧实例必须离开队列（即已释放），否则队列会随刷新次数无界增长");
            }
            finally
            {
                manager.Dispose();
            }
        }

        [Test]
        public void Dispose_ReleasesRetiredClients()
        {
            var manager = new HttpClientManager(System.TimeSpan.FromMilliseconds(50), System.TimeSpan.FromHours(1));
            try
            {
                _ = manager.HttpClient;
                Assert.That(WaitUntil(() => CountRetired(manager) > 0), Is.True);

                manager.Dispose();

                Assert.That(CountRetired(manager), Is.EqualTo(0), "Dispose 必须把退役队列里的实例一并清理");
            }
            finally
            {
                manager.Dispose();
            }
        }
    }
}
