# AGENTS.md 使用模板 · Wlniao.AspNetCore

> **怎么用**：把本文件复制到项目根目录并改名为 `AGENTS.md`（与 `Wlniao.XCore.md` 一起用，后者覆盖通用工具类型）。
> 本文签名已与 Wlniao.AspNetCore 10.0.0 源码逐条核对。

## 0. 包与兼容边界

```bash
dotnet add package Wlniao.AspNetCore   # 自动依赖 Wlniao.XCore
```

| 项 | 值 |
|---|---|
| 目标框架 | `net8.0` / `net10.0`（`FrameworkReference Microsoft.AspNetCore.App`） |
| AOT | `IsAotCompatible=true` |
| 本项目角色 | **只做宿主与 HTTP 编排**：监听、TLS、参数解析、会话、限流、错误页都已有实现，不要重写 |

## 1. 强制要求

| 场景 | 唯一选择 | 禁止做法 |
|---|---|---|
| 宿主启动 | `builder.WebHost.UseWlniao()` | 自写 `UseKestrel(...)`、`UseHttps(...)` |
| 服务注册 | `services.AddBusiness()` + 类上标 `[BusinessService]` | 逐个 `AddScoped` 手写注册 |
| 控制器基类 | `XCoreController`（页面/API）、`XApiController`（SM4+SM3 加密通讯）、`EngineController`（引擎流程） | 直接继承 `Controller` |
| 请求参数 | 基类 `GetRequest`/`PostRequest`/`HeaderRequest` 系列 | 直接读 `Request.Query`/`Form`/`Body` |
| 响应输出 | 成功 `Json`/`OutSuccess`；失败 `ErrorMsg`/`OutMessage`；调试 `DebugMessage` | 手拼 `ContentResult` |
| 异常兜底 | `app.UseErrorHandling(...)` | 每个 Action 里 try/catch 包一层 |
| 接口限流 | `app.UseRateLimit(...)` | 自写中间件计数 |
| 会话状态 | `EngineSession.Encode/Decode` | 自拼 JWT/Cookie 结构 |
| 停服维护 | `WebService.ServiceStop(node, code, message)` | 手动 `Environment.Exit` |

## 2. 快速上手

```csharp
// Program.cs
using Wlniao;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseWlniao(o =>
{
    o.Limits.MaxRequestBodySize = 50 * 1024 * 1024;   // 可选：在框架已配好的监听上叠加
});                                                    // 端口 / TLS 由框架接管，勿再 UseKestrel

builder.Services.AddControllers();
builder.Services.AddBusiness();          // 扫描 [BusinessService] 自动注册
builder.Services.AddEngineBusiness();    // 需要 Engine.IContext 时加这行

var app = builder.Build();

app.UseErrorHandling(o => o.StatusCode = 502);        // 全局异常兜底（建议第一个）
app.UseRateLimit(o =>                                 // 按客户端 IP 限流
{
    o.TimeWindow = 60;
    o.MaxRequests = 600;                              // 0 = 不限流
    o.WhitePath = ["/health", "/api/public*"];
});
app.UseRouting();
app.MapControllers();
app.Run();
```

```csharp
// Controllers/OrderController.cs
using Microsoft.AspNetCore.Mvc;
using Wlniao;
using Wlniao.Mvc;

public class OrderController : XCoreController
{
    private readonly IOrderService _svc;
    public OrderController(IOrderService svc) => _svc = svc;

    [Route("order/detail")]
    public IActionResult Detail()
    {
        var id = GetRequest("id");                    // GET/FORM 统一入口
        if (string.IsNullOrEmpty(id)) return ErrorMsg("缺少参数 id");
        OpenUseTime();                                // 响应头输出 X-UseTime
        return Json(_svc.Get(id));                    // 走 XCore.JsonSerializerOptions
    }
}
```

```csharp
// Services/OrderService.cs
using Wlniao;
using Wlniao.Caching;

[BusinessService(LifeTime = ServiceLifetime.Scoped)]
public class OrderService : IOrderService
{
    public string Get(string id) => Cache.Get("order:" + id);
}
```

## 3. 三个必须先知道的约定

**① 监听与 TLS 由 `UseWlniao` 决定**
端口取 `WLN_LISTEN_PORT`（默认 5000）；`xcore/server.crt` + `xcore/server.key`（可用 `WLN_TLS_CRT` / `WLN_TLS_KEY` 改路径）同时存在时自动加开 TLS 端口
`WLN_TLS_PORT`（默认同 HTTP 端口，不同则双端口并存，协议 Http1/2/3）。**不要自己再调 `UseKestrel` / `UseHttps`**，否则会与框架监听冲突。

**② 失败信息不一定返回 JSON**
`ErrorMsg` 与 `UseErrorHandling` 都按「`POST` 或带 `do=` 参数 → JSON；其余 → HTML 错误页」判断。
前端 AJAX 请求请带 `do=1` 或使用 POST，否则拿到的是 HTML。

**③ `XApiController` 的 `token` 必须在构造函数里赋值**
未赋值时 `Check` 直接失败返回（`data="200"`，message="通讯密钥未配置"）。
校验内容：`sign == SM3(timestamp + data + token)`，且要求 `timestamp + 3600 >= now`
（**只拦过期，不拦超前**，客户端时钟可超前任意秒数）。

```csharp
IActionResult? Check(Func<Dictionary<string, object>, IActionResult> func)
// 入参：timestamp(Unix 秒) + data(SM4EncryptECBToHex 的 JSON) + sign(SM3(timestamp+data+token))
// 失败时统一 Json(ApiResult<object>) 输出，注意失败码字段不统一：
//   未配 token → data="200"；缺参 → data="202"
//   过期 → code="203"；签名错 → code="205"；解密失败 → code="206"；异常 → code="401"
// 成功 → 把 data 解密成 Dictionary<string, object> 传给 func
```

## 4. API 手册

### 宿主启动 UseWlniao

```csharp
IWebHostBuilder UseWlniao(this IWebHostBuilder builder, Action<KestrelServerOptions>? options = null)
// 内部自动完成：
// 1) 日志过滤为 Error 及以上（隐藏框架噪音）
// 2) UseContentRoot(Directory.GetCurrentDirectory())
// 3) Kestrel 监听 XCore.ListenPort；证书存在时追加 TLS 监听（Http1AndHttp2AndHttp3）
// 4) 输出监听终结点日志（WebService.ListenLogs）
// 传统写法等价：Host.CreateDefaultBuilder(args).ConfigureWebHostDefaults(w => w.UseWlniao())
```

### Wlniao.WebService 宿主状态

```csharp
int  TlsPort                                  // TLS 端口（WLN_TLS_PORT，默认同 ListenPort）
void ListenLogs()                             // 输出监听终结点（UseWlniao 内已调用）
void ServiceStop(string node,
                 string code = "302",
                 string message = "服务器正在维护中 Server maintenance.")
// 以维护模式启动占位服务：所有请求返回固定 JSON，用于带病重启/灰度切换
```

### 服务注册 AddBusiness

```csharp
void AddBusiness(this IServiceCollection service, string? assemblyFile = null)
// 扫描程序集（默认取 AppDomain 已加载的全部程序集；可传 dll 文件名或完整路径）
// 中标注 [BusinessService] 的类，按「类实现的每个接口 → 实现类」逐一注册
// ⚠️ 只按接口注册：没实现任何接口的类不会被注册（也不会报错），务必让业务类实现接口
void AddEngineBusiness(this IServiceCollection service)   // IContext → Context
```

```csharp
ServiceLifetime LifeTime   // Transient（默认）/ Scoped / Singleton
new BusinessServiceAttribute(ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
// 用法：[BusinessService(LifeTime = ServiceLifetime.Singleton)]
```

### Wlniao.Mvc.XCoreController 控制器基类

```csharp
// —— 请求参数（均为 protected）——
string GetRequest(string key, string defaultValue = "")     // GET/FORM；见下方安全标记说明
string GetRequestSecurity(string key, string defaultValue = "")   // 真实过滤掉敏感字符后返回
string GetRequestNoSecurity(string key, string defaultValue = "") // 原样返回，不做任何标记
string GetRequestDecode(string key, string defaultValue = "")     // 等同 GetRequest
int    GetRequestInt(string key)
string PostRequest(string key, string defaultValue = "")    // 表单 / JSON 体 / Query 合并
Task<string> PostRequestAsync(string key, string defaultValue = "")
int    PostRequestInt(string key)
string HeaderRequest(string key, string defaultValue = "")  // 请求头，不区分大小写
string GetPostString()                                      // POST 原始报文
string GetCookies(string key)                               // Cookie，不区分大小写

// —— 响应输出 ——
void OpenUseTime()                       // 启用耗时统计 → 响应头 X-UseTime
void DebugMessage(string message)        // → 响应头 X-Debug
ActionResult Json(object data)           // 走 XCore.JsonSerializerOptions，中文不转义
ActionResult Json(object? data, Encoding encoding)
ActionResult JsonStr(string jsonStr)     // 直接输出 JSON 字符串
ActionResult JsonStr(string jsonStr, Encoding encoding)
ActionResult ErrorMsg(string? message = null)   // do=/POST 返回 JSON，否则 HTML 错误页
ActionResult RedirectWait(string url, int seconds)   // 带倒计时跳转页

// —— 可覆盖的错误页模板 ——
protected string errorTitle / errorIcon / errorHtml
protected bool   RequestSecurity   // 默认 true，关掉则 GetRequest 不过滤
protected string method / errorMsg

// —— 请求信息 ——
string ClientIP      // 自动解析 x-forwarded-for
bool   IsHttps       // 兼容 x-forwarded-proto / x-client-scheme / referer
string UrlHost       // 优先 WLN_HOST 配置，带协议头
string UrlDomain
string UrlReferer
string UserAgent
string GetPlatform   // wxwork / weixin / alipay / dingtalk / wlniao / wlnapp / other
string TraceId       // 取 wln-trace-id，无则生成，并从响应头 X-TraceId 回传
```

> ⚠️ **`GetRequest` 是"检测并拦截"，不是"过滤后返回"**：内部会把含 `; / ( ) [ ] { } % * ! ' . < > "`
> 的参数值判定为不安全，置 `RequestSecurity = false`，然后原样返回该值。
> `OnActionExecuted` 发现 `RequestSecurity == false` 时读取配置 `NoSecurityMessage`：
> 配了就把**整个响应**替换成这条文案（HTML 或 JSON），请求等于直接失败；没配则正常返回。
> **注意点号也在名单里**——邮箱、IP、小数、带 `.html` 的路径都会命中。这类接口请改用
> `GetRequestNoSecurity`，或干脆不配置 `NoSecurityMessage`。

### Wlniao.Mvc.XApiController 加密通讯控制器（继承 XCoreController）

```csharp
protected string token = null;                       // 构造时赋值 SM4 通讯密钥（16 字符），一般取自 Config
protected ApiResult<object> result = new() { node = XCore.WebNode, message = "" };

IActionResult? Check(Func<Dictionary<string, object>, IActionResult> func)
// 校验 timestamp(Unix 秒) + data(SM4 密文) + sign(SM3(timestamp+data+token))，解密后把参数字典交给 func
// 失败输出 Json(result)：未配 token→data="200"、缺参→data="202"、过期→code="203"、
// 签名错→code="205"、解密失败→code="206"、异常→code="401"（字段不统一，客户端两个都要判）
IActionResult OutSuccess(object obj) / OutSuccess(object obj, bool tips) / OutSuccess(string str)
IActionResult OutMessage(string message, string code = null)
IActionResult OutDefault() / OutDefault(bool tips)   // 按 result 当前状态输出
```

```csharp
// 典型用法
public class ApiController : XApiController
{
    private readonly IOrderService _svc;
    public ApiController(IOrderService svc)
    {
        _svc = svc;
        token = Config.GetConfigs("ApiToken");   // 16 字符 SM4 密钥
    }

    [Route("api/order/query")]
    public IActionResult Query() => Check(p =>
    {
        var id = p.TryGetValue("id", out var v) ? v?.ToString() : null;
        return OutSuccess(_svc.Get(id));
    });
}
```

### Wlniao.Engine.EngineController 引擎控制器（构造注入 IContext）

```csharp
protected const bool MustAuthentication = true;
public EngineController(IContext ctx)

protected Task<IActionResult> Invoke(Func<IContext, Task<IActionResult>> func, bool mustAuthentication = false)
protected Task<IActionResult> Invoke(Func<IContext, Task<object>> func, bool mustAuthentication = false)
// 自动链路：InitAsync(解析请求) → AuthAsync(身份认证) → mustAuthentication 校验 Session → func → OutputSerialize
// 认证失败：返回 401 JSON，或执行 AuthFailedCallback
```

### Wlniao.Engine.IContext 请求上下文

```csharp
// —— 属性 ——
string Body                                   // POST 原始内容
Dictionary<string, string> Query              // GET/FORM 参数
Dictionary<string, string> HeaderInput        // 请求头
Dictionary<string, string> HeaderOutput       // 输出头（自动写入响应）
object Output                                 // 输出对象（OutputSerialize 时使用）
bool   Continue                               // 是否继续执行预定义流程
bool   IsHttps
string Host / ClientIp / Authorization
string ConsumerId / ConsumerSecretKey / ConsumerPublicKey / ConsumerPrivateKey   // 多租户密钥
EngineSession Session                         // 当前会话

// —— 方法 ——
Task InitAsync(HttpRequest httpRequest)
Task AuthAsync()
T InputDeserialize<T>(JsonTypeInfo<T> typeInfo) where T : new()   // ⚠️ 需元数据（AOT 安全）
Dictionary<string, object> InputDeserialize()                     // Query + Body 合并为字典
IActionResult OutputSerialize<T>(T output)                        // 有 SKey 时自动 SM4 加密，text/plain

// —— 使用方注入的回调 ——
Func<HttpRequest, Task<ConsumerInfo>> LoadConsumerInfo          // 加载租户信息
Func<HttpRequest, Task<EngineSession>> IdentityAuthentication   // 登录认证
Func<HttpRequest, Task<string>> SafetyCertification             // 请求加解密密钥（缺省用 sm2token 头 SM2 解密）
Func<IActionResult>? AuthFailedCallback                         // 自定义认证失败响应
Func<string, IActionResult>? OutputSerializeCallback            // 自定义输出序列化
```

### Wlniao.Engine.EngineSession 统一会话

```csharp
bool IsValid / NotValid          // UserId 非空且未过期 / 反之
long ExpireTime                  // Unix 秒
string UserId / Name / Account / DepartmentIds
Dictionary<string, string> ExtData

string Encode(string consumerSecretKey, string currentConsumerId = null,
              int expireSeconds = 7200, bool base64 = false)     // 会话 → 令牌
void   Decode(string authorization, string consumerSecretKey,
              string currentConsumerId = null)                    // 令牌 → 会话（失败抛 Authorization error）
```

### Wlniao.Engine.ConsumerInfo 多租户信息

```csharp
string Id / SecretKey / PublicKey / PrivateKey / Domain
```

### Wlniao.Engine.EngineException 内部异常（带状态码）

```csharp
int StatusCode { get; }
new EngineException(string message, int statusCode)
new EngineException(string message, Exception innerException)
```

### Wlniao.Middleware.UseErrorHandling 异常兜底

```csharp
IApplicationBuilder UseErrorHandling(this IApplicationBuilder app, Action<ErrorHandlingOptions> configureOptions)
// ErrorHandlingOptions.StatusCode = 502（默认）
// 异常信息写入响应头 X-Wlniao-Debug；POST 或 do= 请求返回 JSON，其余返回 HTML 错误页
```

### Wlniao.Middleware.UseRateLimit 限流

```csharp
IApplicationBuilder UseRateLimit(this IApplicationBuilder app, Action<RateLimitOptions> configureOptions)
// 按客户端 IP 限流 + 熔断；超限 429，黑名单 428
```

```csharp
RateLimitAlgorithm Algorithm      // TokenBucket（默认）/ FixedWindow / SlidingWindow
int  TimeWindow                   // 秒，默认 180（WLN_RATE_TIME_SECONDS）
int  MaxRequests                  // 窗口内上限，0 = 不启用（WLN_RATE_MAX_REQUESTS）
int  BucketCapacity               // 令牌桶容量，0 时取 MaxRequests
bool IsolationPath                // 不同目录分开计数
string[] WhitePath                // 白名单目录，支持 /api* 前缀（WLN_RATE_WHITEPATH）
string[] WhiteKeys / BlackKeys    // 白名单 / 黑名单 IP（WLN_RATE_WHITEKEYS / WLN_RATE_BLACKKEYS）
string   MessageTpl               // 拒绝响应模板，支持 {ClientKey}
bool EnableCircuitBreaker         // 默认 true
int  CircuitBreakerFailureThreshold / CircuitBreakerRecoveryTime   // 默认 5 次 / 30 秒
IRateLimitStore DistributedStore  // 分布式存储接口：Task<bool> TryAcquireAsync(string key, int limit, TimeSpan window) + bool IsAvailable
bool PreferDistributedStore       // 优先用分布式存储
```

### 其它

```csharp
Task<HttpResponseMessage> DevProxy.Http(HttpRequest request, string target)   // 开发期 HTTP/WebSocket 反向代理
class Mvc.Routing.ContainsKeyConstraint(params string[] keys)                // 路由约束：路由值须为指定键之一
class Mvc.Routing.LocalhostConstraint                                        // 路由约束：仅本机可访问（拦截 swagger 用）
interface Engine.IRuntime { bool InitStart(); }                              // 启动初始化钩子
```

## 5. 反例 → 正例

| ❌ 常见写法 | ✅ 正确写法 | 后果 |
|---|---|---|
| `builder.WebHost.UseKestrel(o => o.ListenAnyIP(8080))` | `builder.WebHost.UseWlniao()` + `WLN_LISTEN_PORT` | 与框架监听/TLS 双端口逻辑冲突 |
| `builder.WebHost.UseHttps("a.pfx", "pwd")` | 放 `xcore/server.crt` + `server.key` | 绕过 `WLN_TLS_PORT`、Http3 协商 |
| 手写 `services.AddScoped<IOrderService, OrderService>()` | 类上标 `[BusinessService]` + `AddBusiness()` | 漏注册、生命周期不一致 |
| 业务类标了 `[BusinessService]` 但没实现接口 | 实现接口（如 `IOrderService`） | **静默不注册**，运行期拿不到服务 |
| `Request.Query["id"]` | `GetRequest("id")` | 绕过过滤/编码处理，GET+POST 不统一 |
| `return Content(json, "application/json")` | `return Json(obj)` | 中文转义、null 处理不一致 |
| `throw new Exception("...")` 交给框架 | `return ErrorMsg("...")` / `OutMessage(...)` | 前端拿到 HTML 或 502 |
| 每个 Action 里 try/catch | `app.UseErrorHandling(...)` 一次兜底 | 重复代码、漏捕获 |
| `XApiController` 不赋 `token` | 构造函数里 `token = Config.GetConfigs("ApiToken")` | `Check` 直接失败返回，业务函数不会执行 |
| `ctx.InputDeserialize<T>()` | `ctx.InputDeserialize(MyCtx.Default.T)` | 编译失败（无该重载） |
| 反射式读取 `IContext.Output` | `OutputSerializeCallback` 统一接管 | AOT 下失效 |
| 登录态塞进 `HttpContext.Items` | `EngineSession.Encode/Decode` | 无法跨节点/跨进程 |

## 6. 配置项速查（本包新增）

| 键 | 默认 | 说明 |
|---|---|---|
| `WLN_TLS_PORT` | 同 `WLN_LISTEN_PORT` | TLS 监听端口 |
| `WLN_TLS_CRT` | `xcore/server.crt` | 服务端证书路径 |
| `WLN_TLS_KEY` | `xcore/server.key` | 服务端私钥路径 |
| `WLN_RATE_TIME_SECONDS` | `180` | 限流窗口（秒） |
| `WLN_RATE_MAX_REQUESTS` | `0` | 窗口内上限，0 不启用 |
| `WLN_RATE_WHITEPATH` | — | 白名单目录（逗号分隔，支持 `/api*`） |
| `WLN_RATE_WHITEKEYS` / `WLN_RATE_BLACKKEYS` | — | IP 白 / 黑名单 |
| `NoSecurityMessage` | — | 参数被判定不安全时的拦截文案（配置后会把整个响应替换掉，见 §4 `GetRequest` 说明） |

> 其余配置（端口、时区、缓存、日志、TLS 放行开关 `WLN_ACCEPT_ANY_CERT`）见 `Wlniao.XCore.md`。

## 7. 提交前自检

- [ ] `Program.cs` 里是否只有 `UseWlniao`，没有额外的 `UseKestrel` / `UseHttps`？
- [ ] 新业务类是否标了 `[BusinessService]` 并实现了接口？
- [ ] 新 Action 是否继承框架控制器基类，参数是否走 `GetRequest`/`PostRequest` 系列？
- [ ] 失败路径是否用 `ErrorMsg` / `OutMessage` 返回，而不是抛异常？
- [ ] 新增外部可访问接口是否评估过限流（`UseRateLimit` 白名单是否需要调整）？
- [ ] 加密通讯接口是否属于 `XApiController` 且 `token` 已赋值？
- [ ] 对外 HTTP 调用是否确认过 `XCore.AcceptAnyServerCertificate` 仍为默认的 `false`（默认收紧，不要为了绕开自签证书问题整体打开）？
