# Wlniao.AspNetCore · 限流与动态封禁

> 由 `Wlniao.AspNetCore.md` §4「UseRateLimit 限流」拆出，避免主手册过长。依据源码版本 **10.0.6**。
> 通用配置键见 `Wlniao.XCore.md` §7；宿主/控制器等见 `Wlniao.AspNetCore.md`。

## 1. 快速启用

```csharp
app.UseErrorHandling(o => o.StatusCode = 502);        // 建议第一个
app.UseRateLimit(o =>
{
    o.TimeWindow = 60;
    o.MaxRequests = 600;                              // 0 = 不启用
    o.WhitePath = ["/health", "/api/public*"];
});
app.UseRouting();
app.MapControllers();
```

- 超限返回 **429**，静态黑名单与动态封禁返回 **428 + `MessageTpl`**
- 判定顺序：`WhiteKeys` → 黑名单/动态封禁 → `WhitePath`/限流开关 → 限流计数。
  命中 `WhiteKeys` 的客户端不做任何限流计数，也**不会被动态封禁**；
  `WhitePath` 只豁免该路径的**限流计数**，不豁免封禁——被封禁的客户端即使命中 `WhitePath` 仍返回 428
- 中间件注册须在 `UseRouting` / `MapControllers` 之前，响应后检查器才能看到最终状态码

## 2. RateLimitOptions

```csharp
IApplicationBuilder UseRateLimit(this IApplicationBuilder app, Action<RateLimitOptions> configureOptions)

RateLimitAlgorithm Algorithm      // TokenBucket（默认）/ FixedWindow / SlidingWindow
int  TimeWindow                   // 秒，默认 180（WLN_RATE_TIME_SECONDS）
int  MaxRequests                  // 窗口内上限，0 = 不启用（WLN_RATE_MAX_REQUESTS）
int  BucketCapacity               // 令牌桶容量，0 时取 MaxRequests
bool IsolationPath                // 不同目录分开计数
string[] WhitePath                // 白名单目录，支持 /api* 前缀（WLN_RATE_WHITEPATH）
string[] WhiteKeys / BlackKeys    // 白名单 / 黑名单 IP（WLN_RATE_WHITEKEYS / WLN_RATE_BLACKKEYS）
string[] TrustedProxies           // 可信代理（CIDR，IPv4/IPv6）；未配置时保持历史行为：采信 X-Forwarded-For 的第一个有效 IP，
                                  // 仅非空配置才启用信任策略并自右向左取第一个不可信跳（WLN_RATE_TRUSTED_PROXIES，"*" 表示信任全部）
string   MessageTpl               // 拒绝响应模板，支持 {ClientKey}
bool EnableCircuitBreaker         // 默认 true
int  CircuitBreakerFailureThreshold / CircuitBreakerRecoveryTime   // 默认 5 次 / 30 秒
IRateLimitStore DistributedStore  // 分布式计数：Task<bool> TryAcquireAsync(string key, int limit, TimeSpan window) + bool IsAvailable
bool PreferDistributedStore       // 优先用分布式计数
IList<IRateLimitInspector> Inspectors  // 响应后检查器，构造时快照（见 §4）
```

**客户端 IP**：`WLN_RATE_TRUSTED_PROXIES` **未配置时默认采信 `X-Forwarded-For` 的第一个有效 IP**，这是为兼容历史项目刻意保留的行为，并非默认安全策略；无有效 XFF 时才用 `Connection.RemoteIpAddress`。
配置非空后才启用信任策略：例如 `WLN_RATE_TRUSTED_PROXIES=10.0.0.0/8` 或 `*`；配置存在但全为非法项时 fail-closed。`XCoreController.ClientIP` 与 `Engine.Context.ClientIp` 共用同一解析器。

## 3. 动态封禁与解封时间

现有 API 不变，以下都是新增类型。业务代码可直接封禁；`key` 必须与限流键一致（通常是客户端 IP）。

```csharp
RateLimitControl.Ban(ip, TimeSpan.FromHours(1), "manual");               // 相对时长
RateLimitControl.Ban(ip, DateTimeOffset.UtcNow.AddHours(2), "manual");   // 绝对解封时间（UTC）
RateLimitControl.SetUnbanAt(ip, DateTimeOffset.UtcNow.AddMinutes(30));   // 改期，保留原因
RateLimitControl.GetUnbanAt(ip);       // DateTimeOffset? 查询解封时间
RateLimitControl.GetBan(ip);           // BanEntry（Key / UnbanAt / Reason / CreatedAt）
RateLimitControl.IsBanned(ip);
RateLimitControl.Unban(ip);            // 立即解封
RateLimitControl.GetBans();            // 诊断快照
// 全部有对应 *Async 方法；RateLimitControl.UseStore(null) 恢复进程内默认实现
```

- 解封时间早于当前时间等价于解封（幂等）
- 默认存储是进程内字典，重启即失效；多实例需接 `RedisRateLimitBanStore`（§5）
- 存储异常只写日志并放行，不影响主链路

## 4. 定制拦截：响应后检查器

```csharp
public interface IRateLimitInspector
{
    ValueTask InspectAsync(RateLimitObservation observation, CancellationToken ct = default);
}
// RateLimitObservation: ClientKey / StatusCode / Path / Method / HttpContext
// ClientKey 由中间件给出，检查器不要自行读 X-Forwarded-For，避免与封禁键不一致
```

```csharp
app.UseRateLimit(o =>
{
    o.Inspectors.Add(new NotFoundScanInspector
    {
        Threshold = 20,                       // 窗口内 404 次数
        Window = TimeSpan.FromMinutes(1),
        BanDuration = TimeSpan.FromHours(2),  // 或设置 BanUntil 指定绝对解封时间
        StatusCode = 404,
        Filter = obs => !obs.Path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
    });
});
```

内置 `NotFoundScanInspector` 在滑动窗口内累计指定状态码（默认 404），达到阈值自动封禁；`BanUntil` 设置后优先于 `BanDuration`。
⚠️ `BanUntil` 必须传**未来**的 UTC 时刻：传入已过去的时刻时 `BanAsync` 等价于解封（幂等），封禁会立刻失效，不会保留记录。
窗口表每分钟惰性清理过期条目；`MaxTrackedClients`（默认 10000）限制跟踪的客户端数，超出时淘汰最旧窗口，`TrackedClientCount` 可用于诊断。

检查器在 `_next` 返回后执行，此时响应状态码已确定；异常只写日志，不影响请求与熔断；已被限流/封禁的请求不会触发检查器。
自定义规则实现 `IRateLimitInspector` 并调用 `RateLimitControl.BanAsync` 即可。

## 5. 分布式封禁（Redis）

```csharp
// 复用现有 WLN_REDIS / WLN_REDIS_HOST+PORT+DB+USER+PASS 配置，不新增配置项
RateLimitControl.UseStore(new RedisRateLimitBanStore());          // 默认前缀 wlniao:ratelimit:ban:
RateLimitControl.UseStore(new RedisRateLimitBanStore("my:ban:")); // 自定义前缀
```

- 每条封禁是一个带 TTL 的 Redis String：`createdMs|unbanAtMs|escapedReason`；TTL 按秒向上取整，最多晚 1 秒解封
- `SetUnbanAtAsync` 保留原 `Reason` / `CreatedAt`；不同进程用同一前缀即可共享封禁状态
- 未配置 Redis 时存储不可用，`RateLimitControl` 自动降级放行
- `GetBans()` 只列本进程写入过的键（上限 1000）；完整审计请直接查 Redis
- ⚠️ **性能代价**：接入 Redis store 后，每个未命中 `WhiteKeys` 的请求都会多做一次同步 Redis GET（动态封禁检查先于 `WhitePath`/限流开关，即使 `MaxRequests = 0`、熔断打开或路径在 `WhitePath` 也照查）。当前 `RedisClient` 是同步实现，.NET 线程池开销与 Redis 延迟/可用性直接相关。高 QPS 场景请评估 Redis 连接池容量与拓扑（同机房/本地缓存兜底），或仅对需要防封禁的入口启用限流中间件

## 6. 配置项速查

| 键 | 默认 | 说明 |
|---|---|---|
| `WLN_RATE_TIME_SECONDS` | `180` | 限流窗口（秒） |
| `WLN_RATE_MAX_REQUESTS` | `0` | 窗口内上限，0 不启用 |
| `WLN_RATE_WHITEPATH` | — | 白名单目录（逗号分隔，支持 `/api*`） |
| `WLN_RATE_WHITEKEYS` / `WLN_RATE_BLACKKEYS` | — | IP 白 / 黑名单 |
| `WLN_RATE_TRUSTED_PROXIES` | — | 可信代理列表；**未配置时采信 `X-Forwarded-For` 第一个有效 IP（历史兼容）**，非空才启用信任策略，`*` 信任全部 |
| `WLN_RATE_CB_FALLBACK_MAX` | 0 | 熔断打开期间每窗口每键兜底配额；≤0 时自动取 `max(1, MaxRequests/4)` |

Redis 连接配置（`WLN_REDIS` 等）见 `Wlniao.XCore.md` §7。

## 7. 自检

- [ ] 新增外部接口是否评估过限流与白名单？
- [ ] 反向代理部署是否配置了 `WLN_RATE_TRUSTED_PROXIES`？（未配置会采信任意 XFF，仅出于是历史兼容）
- [ ] 是否需要 `NotFoundScanInspector` 或其他滥用行为检查器？
- [ ] 多实例部署是否接入 `RedisRateLimitBanStore`？
