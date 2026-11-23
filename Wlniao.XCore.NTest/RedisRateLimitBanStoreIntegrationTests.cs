using System.Text;
using Wlniao.Caching;
using Wlniao.Middleware;

namespace Wlniao.XCore.NTest;

/// <summary>
/// RedisRateLimitBanStore 集成测试。
/// 默认跳过；设置 WLN_TEST_REDIS=<连接串> 且 WLN_REDIS 与之相同时执行。
/// 示例：WLN_REDIS=192.168.31.251:6379 WLN_TEST_REDIS=192.168.31.251:6379 dotnet test --filter RedisRateLimitBanStoreIntegrationTests
/// </summary>
[TestFixture]
[Category("Integration")]
public class RedisRateLimitBanStoreIntegrationTests
{
    private string _prefix = "";
    private readonly List<string> _keys = new();

    [SetUp]
    public void SetUp()
    {
        var testConn = Environment.GetEnvironmentVariable("WLN_TEST_REDIS");
        if (string.IsNullOrEmpty(testConn))
        {
            Assert.Ignore("未设置 WLN_TEST_REDIS，跳过 Redis 集成测试");
        }
        Assert.That(Config.GetConfigs("WLN_REDIS"), Is.EqualTo(testConn),
            "WLN_TEST_REDIS 与 WLN_REDIS 必须一致，避免误连其他 Redis 实例");

        _prefix = $"wlniao:test:ratelimit:{Guid.NewGuid():N}:";
    }

    [TearDown]
    public async Task TearDown()
    {
        RateLimitControl.UseStore(null);
        if (string.IsNullOrEmpty(_prefix))
        {
            return;
        }
        try
        {
            var store = new RedisRateLimitBanStore(_prefix);
            foreach (var key in _keys)
            {
                await store.UnbanAsync(key).ConfigureAwait(false);
            }
        }
        catch
        {
            // 清理失败不影响断言结果；测试键带随机前缀且带 TTL，会自动过期
        }
    }

    [Test]
    public async Task RedisStore_SharedAcrossInstances_ReschedulePreservesReason()
    {
        const string key = "10.99.0.1";
        _keys.Add(key);
        var storeA = new RedisRateLimitBanStore(_prefix);
        var storeB = new RedisRateLimitBanStore(_prefix);
        Assert.That(storeA.IsAvailable && storeB.IsAvailable, Is.True, "WLN_REDIS 必须已配置");

        var unbanAt = DateTimeOffset.UtcNow.AddMinutes(5);
        Assert.That(await storeA.BanAsync(key, unbanAt, "integration"), Is.True);

        var fromB = await storeB.GetAsync(key);
        Assert.That(fromB, Is.Not.Null, "另一个存储实例应能看到封禁（跨实例共享）");
        Assert.That(fromB!.Reason, Is.EqualTo("integration"));
        Assert.That(fromB.UnbanAt.ToUnixTimeMilliseconds(), Is.EqualTo(unbanAt.ToUnixTimeMilliseconds()));

        var later = unbanAt.AddMinutes(10);
        Assert.That(await storeB.SetUnbanAtAsync(key, later), Is.True);
        var fromA = await storeA.GetAsync(key);
        Assert.That(fromA, Is.Not.Null);
        Assert.That(fromA!.UnbanAt.ToUnixTimeMilliseconds(), Is.EqualTo(later.ToUnixTimeMilliseconds()));
        Assert.That(fromA.Reason, Is.EqualTo("integration"), "改期必须保留原原因");
        Assert.That(fromA.CreatedAt, Is.EqualTo(fromB.CreatedAt), "改期必须保留原创建时间");

        Assert.That(await storeA.UnbanAsync(key), Is.True);
        Assert.That(await storeB.GetAsync(key), Is.Null, "解封后所有实例都应看不到封禁");
    }

    [Test]
    public void RedisClient_IntegerReplies_AreHandled()
    {
        var rawKey = _prefix + "raw";
        var client = Redis.Instance;
        Assert.That(client.Set(rawKey, Encoding.UTF8.GetBytes("v"), 60), Is.True);
        Assert.That(client.KeyExists(rawKey), Is.True, "EXISTS 的整数回复 :1 应为 true");
        Assert.That(client.KeyDelete(rawKey), Is.True, "DEL 的整数回复 :1 应为 true");
        Assert.That(client.KeyExists(rawKey), Is.False, "DEL 后 EXISTS 的整数回复 :0 应为 false");
    }

    [Test]
    public async Task RedisStore_TtlExpiresAutomatically()
    {
        const string key = "10.99.0.2";
        _keys.Add(key);
        var store = new RedisRateLimitBanStore(_prefix);
        await store.BanAsync(key, DateTimeOffset.UtcNow.AddSeconds(2), "ttl");
        Assert.That(await store.GetAsync(key), Is.Not.Null);

        await Task.Delay(3500);
        Assert.That(await store.GetAsync(key), Is.Null, "TTL 到期后应自动失效并清理");
    }

    [Test]
    public void RateLimitControl_WithRedisStore_WorksAcrossInstances()
    {
        const string key = "10.99.0.3";
        _keys.Add(key);

        RateLimitControl.UseStore(new RedisRateLimitBanStore(_prefix));
        Assert.That(RateLimitControl.Ban(key, TimeSpan.FromMinutes(3), "control"), Is.True);
        Assert.That(RateLimitControl.IsBanned(key), Is.True);

        // 模拟另一节点：换成新的存储实例读取同一 Redis
        RateLimitControl.UseStore(new RedisRateLimitBanStore(_prefix));
        var unbanAt = RateLimitControl.GetUnbanAt(key);
        Assert.That(unbanAt, Is.Not.Null, "另一实例应看到同一封禁");
        Assert.That(RateLimitControl.GetBan(key)?.Reason, Is.EqualTo("control"));

        var later = DateTimeOffset.UtcNow.AddMinutes(10);
        Assert.That(RateLimitControl.SetUnbanAt(key, later), Is.True);
        Assert.That(RateLimitControl.GetBan(key)?.UnbanAt.ToUnixTimeMilliseconds(), Is.EqualTo(later.ToUnixTimeMilliseconds()));

        Assert.That(RateLimitControl.Unban(key), Is.True);
        Assert.That(RateLimitControl.IsBanned(key), Is.False);
    }
}
