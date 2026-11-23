using System.Net;
using Microsoft.AspNetCore.Http;
using Wlniao.Middleware;

namespace Wlniao.XCore.NTest;

/// <summary>
/// 限流中间件扩展能力回归：动态封禁、绝对解封时间、响应后检查器、404 扫描检测器。
/// </summary>
[TestFixture]
public class RateLimitExtensionTests
{
    [SetUp]
    public void SetUp()
    {
        // 每个用例使用独立的进程内存储，避免全局状态互相污染
        RateLimitControl.UseStore(null);
    }

    [TearDown]
    public void TearDown()
    {
        RateLimitControl.UseStore(null);
    }

    [Test]
    public void Ban_UsesAbsoluteUnbanTime_AndSetUnbanAtPreservesReason()
    {
        const string key = "10.10.0.1";
        var first = DateTimeOffset.UtcNow.AddMinutes(1);
        var second = first.AddMinutes(5);

        Assert.That(RateLimitControl.Ban(key, first, "manual"), Is.True);
        Assert.That(RateLimitControl.IsBanned(key), Is.True);
        Assert.That(RateLimitControl.GetUnbanAt(key), Is.EqualTo(first));

        Assert.That(RateLimitControl.SetUnbanAt(key, second), Is.True);
        var entry = RateLimitControl.GetBan(key);
        Assert.That(entry, Is.Not.Null);
        Assert.That(entry!.UnbanAt, Is.EqualTo(second));
        Assert.That(entry.Reason, Is.EqualTo("manual"), "改期必须保留原封禁原因");

        Assert.That(RateLimitControl.Unban(key), Is.True);
        Assert.That(RateLimitControl.IsBanned(key), Is.False);
    }

    [Test]
    public async Task Ban_WithPastUnbanTime_IsNoOp()
    {
        const string key = "10.10.0.2";
        Assert.That(RateLimitControl.Ban(key, TimeSpan.FromMinutes(1)), Is.True);
        Assert.That(await RateLimitControl.BanAsync(key, DateTimeOffset.UtcNow.AddSeconds(-1)), Is.True);
        Assert.That(RateLimitControl.IsBanned(key), Is.False, "解封时间早于当前时间应等价于解封");
    }

    [Test]
    public async Task NotFoundScanInspector_BansAfterThreshold()
    {
        const string key = "10.10.0.3";
        var inspector = new NotFoundScanInspector
        {
            Threshold = 2,
            Window = TimeSpan.FromMinutes(1),
            BanDuration = TimeSpan.FromMinutes(10)
        };

        var observation = new RateLimitObservation
        {
            ClientKey = key,
            StatusCode = 404,
            Path = "/missing",
            Method = "GET",
            HttpContext = new DefaultHttpContext()
        };

        await inspector.InspectAsync(observation);
        Assert.That(RateLimitControl.IsBanned(key), Is.False, "未达阈值不应封禁");

        await inspector.InspectAsync(observation);
        Assert.That(RateLimitControl.IsBanned(key), Is.True, "达到阈值应自动封禁");
        Assert.That(RateLimitControl.GetBan(key)?.Reason, Is.EqualTo("http-404-scan"));
    }

    [Test]
    public async Task NotFoundScanInspector_FilterCanExcludePaths()
    {
        const string key = "10.10.0.4";
        var inspector = new NotFoundScanInspector
        {
            Threshold = 1,
            Filter = obs => !obs.Path.Contains("/health", StringComparison.OrdinalIgnoreCase)
        };
        var observation = new RateLimitObservation
        {
            ClientKey = key,
            StatusCode = 404,
            Path = "/health/missing",
            Method = "GET",
            HttpContext = new DefaultHttpContext()
        };

        await inspector.InspectAsync(observation);
        Assert.That(RateLimitControl.IsBanned(key), Is.False, "过滤器排除的路径不应计数");
    }

    [Test]
    public async Task Middleware_BlockedByDynamicBan_Returns428WithoutCallingNext()
    {
        const string ip = "10.10.0.5";
        var nextCalled = false;
        var options = new RateLimitOptions { MaxRequests = 0 };
        var middleware = new RateLimitMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, options);

        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        context.Response.Body = new MemoryStream();

        Assert.That(RateLimitControl.Ban(ip, TimeSpan.FromMinutes(1), "manual"), Is.True);
        await middleware.InvokeAsync(context);

        Assert.That(nextCalled, Is.False, "动态封禁命中后不得调用下游");
        Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status428PreconditionRequired));
    }

    [Test]
    public async Task Middleware_DynamicBan_BeatsWhitePath()
    {
        const string ip = "10.10.0.55";
        var nextCalled = false;
        var options = new RateLimitOptions
        {
            MaxRequests = 600,
            WhitePath = ["/health"]
        };
        var middleware = new RateLimitMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, options);

        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        context.Request.Path = "/health";
        context.Response.Body = new MemoryStream();

        Assert.That(RateLimitControl.Ban(ip, TimeSpan.FromMinutes(1), "manual"), Is.True);
        await middleware.InvokeAsync(context);

        Assert.That(nextCalled, Is.False, "WhitePath 只豁免限流计数，不豁免动态封禁");
        Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status428PreconditionRequired));
    }

    [Test]
    public async Task Middleware_UnconfiguredTrustedProxies_AcceptsXffForCompatibility()
    {
        var observedKey = "";
        var options = new RateLimitOptions { MaxRequests = 0, TrustedProxies = [] };
        options.Inspectors.Add(new CallbackInspector(obs => observedKey = obs.ClientKey));
        var middleware = new RateLimitMiddleware(_ => Task.CompletedTask, options);

        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");
        context.Request.Headers["x-forwarded-for"] = "203.0.113.9, 10.0.0.1";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);
        Assert.That(observedKey, Is.EqualTo("203.0.113.9"),
            "未配置 TrustedProxies 时应保持历史行为，采信 XFF 第一个有效 IP");
    }

    [Test]
    public async Task Middleware_ConfiguredTrustedProxies_RejectsXffFromUntrustedPeer()
    {
        var observedKey = "";
        var options = new RateLimitOptions { MaxRequests = 0, TrustedProxies = ["10.0.0.0/8"] };
        options.Inspectors.Add(new CallbackInspector(obs => observedKey = obs.ClientKey));
        var middleware = new RateLimitMiddleware(_ => Task.CompletedTask, options);

        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.1");
        context.Request.Headers["x-forwarded-for"] = "203.0.113.9, 192.168.1.1";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);
        Assert.That(observedKey, Is.EqualTo("192.168.1.1"), "配置信任策略后，不可信直连对端的 XFF 必须忽略");
    }

    [Test]
    public async Task Middleware_ConfiguredTrustedProxies_UsesRightmostUntrustedHop()
    {
        var observedKey = "";
        var options = new RateLimitOptions { MaxRequests = 0, TrustedProxies = ["10.0.0.0/8"] };
        options.Inspectors.Add(new CallbackInspector(obs => observedKey = obs.ClientKey));
        var middleware = new RateLimitMiddleware(_ => Task.CompletedTask, options);

        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.5");
        context.Request.Headers["x-forwarded-for"] = "203.0.113.9, 10.0.0.5";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);
        Assert.That(observedKey, Is.EqualTo("203.0.113.9"), "可信代理下的 XFF 应自右向左取第一个不可信跳");
    }

    [Test]
    public async Task Middleware_RunsInspectorsAfterDownstream()
    {
        const string ip = "10.10.0.6";
        var nextCalled = false;
        var observed = false;
        var options = new RateLimitOptions { MaxRequests = 0 };
        options.Inspectors.Add(new CallbackInspector(obs =>
        {
            observed = obs.ClientKey == ip && obs.StatusCode == 404;
        }));
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        context.Response.Body = new MemoryStream();
        var middleware = new RateLimitMiddleware(_ =>
        {
            nextCalled = true;
            context.Response.StatusCode = 404;
            return Task.CompletedTask;
        }, options);


        await middleware.InvokeAsync(context);

        Assert.That(nextCalled, Is.True);
        Assert.That(observed, Is.True, "下游执行完毕后应把状态码交给检查器");
        Assert.That(context.Response.StatusCode, Is.EqualTo(404));
    }

    [Test]
    public async Task NotFoundScanInspector_PrunesExpiredWindowsAndCapsTracking()
    {
        var inspector = new NotFoundScanInspector
        {
            Threshold = 100,                       // 不触发封禁，只验证窗口清理
            Window = TimeSpan.FromSeconds(1),
            MaxTrackedClients = 2
        };

        async Task Observe(string key)
        {
            await inspector.InspectAsync(new RateLimitObservation
            {
                ClientKey = key,
                StatusCode = 404,
                Path = "/missing",
                Method = "GET",
                HttpContext = new DefaultHttpContext()
            });
        }

        await Observe("window-a");
        await Observe("window-b");
        await Observe("window-c");
        Assert.That(inspector.TrackedClientCount, Is.LessThanOrEqualTo(inspector.MaxTrackedClients),
            "容量超限时必须淘汰最旧窗口，不能无界增长");

        await Task.Delay(1200);
        await Observe("window-d");
        Assert.That(inspector.TrackedClientCount, Is.LessThanOrEqualTo(inspector.MaxTrackedClients),
            "过期窗口应被惰性回收");
    }

    [Test]
    public async Task NotFoundScanInspector_ConcurrentAdds_KeepHardCap()
    {
        var inspector = new NotFoundScanInspector
        {
            Threshold = 100,
            Window = TimeSpan.FromMinutes(1),
            MaxTrackedClients = 8
        };
        var tasks = new List<Task>();
        for (var i = 0; i < 200; i++)
        {
            tasks.Add(inspector.InspectAsync(new RateLimitObservation
            {
                ClientKey = "burst-" + i,
                StatusCode = 404,
                Path = "/missing",
                Method = "GET",
                HttpContext = new DefaultHttpContext()
            }).AsTask());
        }

        await Task.WhenAll(tasks);
        Assert.That(inspector.TrackedClientCount, Is.LessThanOrEqualTo(inspector.MaxTrackedClients),
            "并发新增也必须满足容量硬上限");
    }

    private sealed class CallbackInspector(Action<RateLimitObservation> callback) : IRateLimitInspector
    {
        public ValueTask InspectAsync(RateLimitObservation observation, CancellationToken cancellationToken = default)
        {
            callback(observation);
            return ValueTask.CompletedTask;
        }
    }
}
