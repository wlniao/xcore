# 变更记录

本文件记录 Wlniao.AspNetCore 的公开行为变更，重点标注**破坏性变更**。版本号与 `Wlniao.AspNetCore.csproj` 保持一致。

## 10.0.5

安全审计修复与行为收紧（XCore / AspNetCore / XCenter / Tasker 同步升至 10.0.5）。

### 新增

- **`ApiSignGuard`（Wlniao.XCore）**
  - 双向时间窗校验 `|now - timestamp| ≤ 3600`（同时拒绝过期与超前时间戳）。
  - 请求签名常量时间比较；同一 `sign` 在时间窗内进程内只接受一次（重放返回 `208`）。
  - 响应完整性签名 `sign = SM3(data + token)`；`XApi`/`TokenApi` 客户端强制校验（失败 `108`）。
  - 出站请求日志不再落盘 `sign`（防日志成为可重放凭据副本）。

- **`RateLimitOptions.CircuitBreakerFallbackMaxRequests`（可选）**
  - 熔断打开期间的本地兜底配额；≤0 时自动取 `max(1, MaxRequests/4)`。
  - 配置项 `WLN_RATE_CB_FALLBACK_MAX`。熔断打开不再等同于「按正常配额放行」，也不再完全跳过限流。

- **`Wlniao.Middleware.ClientIpResolver`**
  - 统一解析客户端 IP，与限流共用 `WLN_RATE_TRUSTED_PROXIES` 的 fail-closed 策略。
  - `XCoreController.ClientIP` 与 `Engine.Context.ClientIp` 改为走该解析器：未配置可信代理时不再无条件采信 `X-Forwarded-For`。

- **`StringUtil.HasSensitiveWords` / `HasSensitiveWordsZhcn`**
  - 布尔判断入口，与同类型 `Is*` 风格一致；需要命中明细时仍用 `CheckSensitiveWords*`（返回 `ApiResult`）。

### 破坏性变更

- **`XApiController` / `TokenApiController` 请求校验收紧**
  - 时间戳改为双向窗口（超前时间戳也会被拒，错误码文案改为「请求时间戳超出允许窗口」）。
  - 新增重放拦截：同一 `sign` 窗口内重复返回 `code=208`。
  - 签名比较改为常量时间；校验失败不再把内部 `ex.Message` 原样回给客户端。
  - 响应报文新增 `sign` 字段；旧版客户端会忽略该字段，**新版客户端对接旧服务端会因缺少 `sign` 返回 `108`**——两端需同步升级。

- **`XCore.SessionEncryptKey` 未配置时改为进程级随机密钥**
  - 不再从 `WebNode`/`WebHost`/`ConnType`/`ConnName` 等可公开信息推导（原推导可离线复算）。
  - **多实例部署必须显式配置同一 `SessionEncryptKey`**，否则各实例会话互不相通；未配置时重启进程会使既有会话失效。

- **`Engine.Context.InputDeserialize<T>`（SKey 非空）解密失败不再回退明文**
  - 原：解密得到空串时直接把原始 `Body` 当 JSON 解析（客户端发明文即可绕过加密）。
  - 现：解密失败或结果为空抛 `MessageException`（400），禁止明文回退。

- **限流空 clientKey 不再视为白名单**
  - `RemoteIpAddress` 为 null 时落到固定键 `unknown` 参与限流，而不是走白名单分支直接放行。

### 修复

- **`FileLoger.Topic` 按 `logLevel` 过滤文件写入**
  - 原文件写入无视级别，Debug 级敏感明文即使配置了 Information 仍会全部落盘；现文件与控制台一致按级别过滤。

- **`RedisClient.Set` 对 `expire ≤ 0` 的语义与 memory/file 对齐**
  - 原发裸 `SET`（永不过期）；现 `SET + EXPIRE 0`（立即过期），与 `Cache.Set(k,v,0)` 文档一致。

- **`Redis.Instance` 双检锁**、**`Lang` 读写锁**、**`Cache.UseRedis` 写锁**、**`FileCache` StreamWriter dispose**、**`XApi`/`ApiClient(Stream)` 的 HttpRequest/Response dispose**

- **`XStorage` 三处裸 `new HttpClient()` 改为 `HttpClientManager.SharedInstance`**

- **`SM4DecryptECBFromBase64` / `SM4DecryptCBCFromBase64` 固定按 Base64 解码**
  - 原经 `Helper.Decode` 自动判型，纯 hex 字符的 Base64 密文会被误按 Hex 解码。

- **`PathTool.Map` 相对路径归一化**
  - `GetFullPath` 后若逃逸出程序根目录则拒绝（返回根目录），防 `..` 穿越。

- **`Pattern.Regex.IsMatch(input, pattern)` 默认 2 秒匹配超时**（防 ReDoS）；带 timeout 的重载不变。

- **`StringUtil` HTML 截断三处 `string.Replace` 误当正则 → 改为 `Regex.Replace`**（原为 no-op）。

- **`SM2DecryptByPrivateKey` 密文前缀判断**：修正 `encText[0]/[1]` 逻辑错误与长度为 1 时的越界。

- **`RateLimitMiddleware` 熔断打开期间仍强制限流**（配额见 `CircuitBreakerFallbackMaxRequests`）；不再获得「诱发 5 次下游错误即无限流窗口」。

- **AOT 标注补齐**：`Reflection.GetPropertyValue`/`GetAttribute(MemberInfo,…)`/`GetAttributes(MemberInfo,…)` 补 `[RequiresUnreferencedCode]`/`[RequiresDynamicCode]`。

- **约定收敛**：产品代码 `DateTime.Now` 基本清零（计时改 `UtcNow`、业务时间改 `DateTools`）；裸 `int.Parse` 改 `Wlniao.Convert.ToInt`；关键路径空 `catch` 补 Debug 日志；`throw new Exception` 改具体类型。

- **`Config.Read` 磁盘 IO 移出锁外**，避免持锁读文件阻塞所有 `GetConfigs`。

### 其它

- `IsSafeSqlString` / `GetSafeSqlString` 标 `[Obsolete]`（字符黑名单不可作安全边界，请用参数化查询）。
- 无填充 RSA / SHA1 签名相关 API 标 `[Obsolete]`。
- 删除整文件被注释的死代码 `Pattern/FileExtentName.cs`；`DateTools.cs` 由 CR-only 单行规范为 CRLF 多行。
- 同步 AGENTS-Usage 文档至 10.0.5；新增回归测试（时间窗/重放/响应签名/SessionEncryptKey/Base64/HasSensitiveWords 等）。

### 历史未发布条目（并入本版）

- **`RateLimitOptions.TrustedProxies`（可选）**
  - 用途：限定哪些直连对端有权提供 `X-Forwarded-For`。此前该头被无条件采信，客户端只需每次伪造一个不同的值即可获得无限限流键、直接绕过限流。
  - 配置项 `WLN_RATE_TRUSTED_PROXIES`（逗号分隔），支持单个 IP、CIDR 网段（IPv4 / IPv6）与通配符 `*`。
  - 配置后：直连对端不在白名单内时忽略该头，直接按对端地址限流；对端可信时**自右向左**回溯，取第一个不可信的跳作为客户端地址——防止可信代理「追加而不覆盖」时，攻击者预置的伪造最左项被采信。
  - 配置项存在、但没有任何一项能解析为合法 IP / CIDR 时 **fail-closed**（忽略该头并写告警日志），不退回「信任全部」，避免配置笔误反而放大风险。
  - 说明：`RateLimitMiddleware`、`XCoreController.ClientIP`、`Engine.Context.ClientIp` 已统一走 `ClientIpResolver`。

### 破坏性变更

- **`RateLimitMiddleware` 默认不再采信 `X-Forwarded-For`（fail-closed）**
  - 影响：`TrustedProxies` **未配置（空数组）时**，一律忽略 `X-Forwarded-For`，客户端地址取直连对端（`Connection.RemoteIpAddress`）。历史行为是无条件采信该头的第一个有效 IP。
  - 仅当显式配置 `"*"` 时才信任全部代理（等价于旧默认）。配置具体 IP / CIDR 时语义不变：仅白名单内的直连对端可提供该头。
  - **需要恢复旧行为的部署**：设置 `WLN_RATE_TRUSTED_PROXIES=*`，或列出全部可信代理。
  - 动机：默认部署下伪造 `X-Forwarded-For` 可无限切换限流键绕过限流；默认改为不采信后，限流键回落到直连地址。
  - 与 AGENTS-Usage 手册、`RateLimitOptions.TrustedProxies` XML 注释已对齐（未配置 = 不采信）。

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
  - **修复黑名单判定顺位**：`BlackKeys` 语义为「封禁该标识」，此前因判定条件合并短路（`MaxRequests<=0 || IsWhitePath(...)` 前置），被拉黑的客户端在**限流未启用**或**命中路径白名单**时不再返回 428，封禁被绕过。现判定顺位固定为：客户端白名单 → **黑名单（428）** → 未启用限流 / 路径白名单（放行）→ 限流（熔断打开时用更严兜底配额，否则正常配额，超限 429）。
    - 与历史版本的唯一差异：熔断打开时黑名单**仍然生效**（旧代码会一并放行）。封禁不应依赖限流模块健康状态。
    - 同一标识同时出现在黑白名单时，仍以白名单为准（与历史行为一致）。
  - 响应 `Content-Type` 由非标准的 `text/json` 改为 `application/json`。
- **`ContainsKeyConstraint`**：`_keys` 由 `static` 改为实例 `readonly` 并复制入参，修复多实例互相覆盖。
- **`ErrorHandling`**：`X-Wlniao-Debug` 响应头仅在 `XCore.IsDevTest` 为真时输出，并截断至 200 字符、去除换行。

### 文档

- 修正 `WebService.cs` 文件头（文件名、适用环境）与 `Attributes.cs` 的过时注释；其余文件「适用环境」统一为 `.NET 8.0 / .NET 10.0`。
