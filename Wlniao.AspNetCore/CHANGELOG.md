# 变更记录

本文件记录 Wlniao.AspNetCore 的公开行为变更，重点标注**破坏性变更**。版本号与 `Wlniao.AspNetCore.csproj` 保持一致。

## 未发布

### 新增

- **`RateLimitOptions.TrustedProxies`（可选，默认空数组 = 行为不变）**
  - 用途：限定哪些直连对端有权提供 `X-Forwarded-For`。此前该头被无条件采信，客户端只需每次伪造一个不同的值即可获得无限限流键、直接绕过限流。
  - 配置项 `WLN_RATE_TRUSTED_PROXIES`（逗号分隔），支持单个 IP、CIDR 网段（IPv4 / IPv6）与通配符 `*`。
  - **默认空数组：无条件采信 `X-Forwarded-For` 的第一个有效 IP，与历史行为完全一致**，现有部署无需任何改动。
  - 配置后：直连对端不在白名单内时忽略该头，直接按对端地址限流；对端可信时**自右向左**回溯，取第一个不可信的跳作为客户端地址——防止可信代理「追加而不覆盖」时，攻击者预置的伪造最左项被采信。
  - 配置为 `*` 等价于默认行为（显式声明信任全部对端）。
  - 配置项存在、但没有任何一项能解析为合法 IP / CIDR 时 **fail-closed**（忽略该头并写告警日志），不退回「信任全部」，避免配置笔误反而放大风险。
  - 说明：本次仅覆盖 `RateLimitMiddleware`；`Context.ClientIp` 仍沿用无条件采信，若要统一需另行决策。

### 破坏性变更

- **`EngineController` 未认证响应的 HTTP 状态码统一为 401**
  - 影响：`Invoke(Func<IContext, Task<IActionResult>>, mustAuthentication: true)` 的未认证分支，HTTP 状态码由 `200` 变为 `401`；第二个重载（`Func<IContext, Task<object>>`）原本即为 `401`，本次统一。
  - 响应体格式不变：`{"code":401,"message":"unauthorized"}`，`code` 仍为 JSON **数字**。
  - 若客户端依赖「HTTP 200 + 业务码」判断未登录，需改为按 401 判断。

- **`EngineController` 异常响应消息在非开发环境泛化**
  - 影响：`catch` 分支原样回传 `e.Message`；现仅在 `XCore.IsDevTest` 为真时回传原始消息，否则统一返回「系统异常，稍后可尝试重新提交」。与 `ErrorHandling` 中间件行为一致，避免内部异常信息泄露。
  - **注意**：若有业务代码通过抛出异常向客户端传递提示文案，升级后将看不到该文案，需改为返回业务对象，或调用 `ErrorResult(code, message)` 显式指定文案。
  - 完整异常与堆栈仍写入错误日志。

### 修复

- **`DevProxy`**
  - WebSocket 分支原返回 `new Task<HttpResponseMessage>(null)`（从未启动的 Task），`await` 会永久挂起；现改为等待代理结束后返回。
  - `AcceptWebSocketAsync`、`ConnectAsync`、`SendAsync` 三处缺失的 `await` 已补齐；收发改为严格串行，修复缓冲区复用导致的数据错乱。
  - `HttpClient` 改为静态复用，避免每次请求新建实例造成套接字耗尽。
  - 出站请求显式回填 `Content-Length`，避免退化为 chunked 而不被部分下游接受。
  - 用 `NonDisposingStream` 包装请求体，修复 `HttpRequestMessage.Dispose` 连带释放 `HttpRequest.Body` 的问题。
- **`XCoreController`**
  - `TraceId` 由 `Guid.ToString().Replace('-', '\0')` 改为 `ToString("N")`，不再写入 NUL 控制字符（原实现会产生非法响应头）。
  - 新增 `ReadBodyAsync()`：`EnableBuffering` + 异步读取 + 回退流位置，修复 `GetPostString()` 在 Kestrel（`AllowSynchronousIO=false`）下必然抛异常的问题，并使请求体可被重复读取。
  - `PostRequest()` 的 `.Result` 改为 `GetAwaiter().GetResult()`，异常不再被 `AggregateException` 包装。
- **`EngineController`**
  - 错误响应改为 `JsonObject` 序列化，异常消息含引号、反斜杠、换行时不再破坏 JSON 报文。
  - 异常补充错误日志。
- **`Context`**
  - 请求体读取改为 `EnableBuffering` + 异步 + `finally` 回退位置，替换「读完为空再读同一流」的无效兜底。
  - **初始化幂等控制重做**：原以 `HeaderInput.Count > 0` 判断是否已初始化（请求无头时会重复解析全部内容）；改为私有状态位 `_initState`，并用 `Interlocked.CompareExchange` 抢占置位——本方法在多个 `await` 之后写状态，单纯「读布尔 + 赋值」无法保证并发下只执行一次（实测并发 32 次调用，旧写法会多次执行租户加载回调）。
  - 初始化失败时复位为「未初始化」并**同时恢复 `Continue = true`**。失败分支都会先把 `Continue` 置 `false` 再抛异常，若只复位状态位，重试会在 `if (!Continue)` 处静默早退，返回一个 `ConsumerId`/`SKey` 为空却被视为「已初始化」的 Context——这比直接报错更危险。
  - 流程拆分为 `InitAsync`（幂等控制 + 失败复位）+ `InitCoreAsync`（实际解析），避免幂等逻辑与业务解析混在一个方法里。
  - **两处 `if (!Continue)` 早退路径补上状态位复位**。状态位在方法入口已被抢占置 `1`，早退时若不复位，会留下一个 `ConsumerId`/`SKey` 为空却被视为「已初始化」的实例，后续调用永远跳过初始化。现统一为 `Interlocked.Exchange(ref _initState, 0)`，并固化不变量「状态非 0 ⇔ 完整初始化」。注意：`Continue` 在库内只有三处赋值且均紧接 `throw`（已由 `catch` 统一恢复），因此该早退仅在调用方或业务回调主动中断时触发。
  - 行为提示：初始化失败后在同一实例上重试会**完整重跑**一遍，`LoadConsumerInfo` / `SafetyCertification` 等业务回调会被再次调用；`HeaderInput`、`Query` 使用 `TryAdd`，不会重复写入。
  - `IsHttps`、`ClientIp` 增加 `Request` 为空保护。
- **`RateLimitMiddleware`**
  - 修复 `options` 传 `null` 时构造抛 `NullReferenceException`。
  - 三个只增不删的 `ConcurrentDictionary` 改为带容量与过期的 `MemoryCache`，消除内存泄漏。
  - **修复下游异常导致整条管道被执行两次**：原 `catch` 内再次调用 `_next`，下游抛异常时会重复执行。现限流判断阶段只产出「放行 / 拒绝」结论（布尔标志），`_next` 在方法内**唯一一次**调用且位于判断 `try` 之外，所有旁路路径（熔断打开、客户端白名单、黑色名单、`MaxRequests<=0`、路径白名单、常规路径）共用这一出口。
  - 下游异常不再被限流中间件吞掉，而是计入熔断失败后原样向上抛出（由 `ErrorHandling` 兜底响应）。
  - 限流状态读取改为取缓存中的唯一实例，收敛并发下 `GetOrCreate` 非原子导致的计数偏差（仍非严格原子，极端并发下限流略松，注释已说明）。
  - **缓存容量压力下的行为变更**：`MemoryCache` 超过 `SizeLimit` 时不会抛异常，而是压缩并淘汰条目（实测 `SizeLimit=100` 连续写入 5000 条，最终存活约 95 条），被淘汰的计数条目会从 0 重来，导致限流静默失效。现条目数达到容量 90% 即切换到容量固定的粗粒度分桶计数（128 桶，每桶每窗口按 `MaxRequests` 配额），存储异常与「写入即被淘汰」同样走该兜底，不再 fail-open。
    - 代价：降级期间同一桶内的客户端共享配额，可能出现误伤；正常规模下不会触发。站点活跃客户端接近 9000 时应调大 `CacheSizeLimit`。
  - 熔断器语义与历史行为保持一致（同时观察限流自身与下游处理），已在 `remarks` 中写明。
  - **修复黑名单判定顺位**：`BlackKeys` 语义为「封禁该标识」，此前因判定条件合并短路（`MaxRequests<=0 || IsWhitePath(...)` 前置），被拉黑的客户端在**限流未启用**或**命中路径白名单**时不再返回 428，封禁被绕过。现判定顺位固定为：客户端白名单 → **黑名单（428）** → 熔断打开（放行）→ 未启用限流 / 路径白名单（放行）→ 限流（429）。
    - 与历史版本的唯一差异：熔断打开时黑名单**仍然生效**（旧代码会一并放行）。封禁不应依赖限流模块健康状态。
    - 同一标识同时出现在黑白名单时，仍以白名单为准（与历史行为一致）。
  - 响应 `Content-Type` 由非标准的 `text/json` 改为 `application/json`。
- **`ContainsKeyConstraint`**：`_keys` 由 `static` 改为实例 `readonly` 并复制入参，修复多实例互相覆盖。
- **`ErrorHandling`**：`X-Wlniao-Debug` 响应头仅在 `XCore.IsDevTest` 为真时输出，并截断至 200 字符、去除换行。

### 文档

- 修正 `WebService.cs` 文件头（文件名、适用环境）与 `Attributes.cs` 的过时注释；其余文件「适用环境」统一为 `.NET 8.0 / .NET 10.0`。
