# AGENTS.md (实用示例)

## 安装包`dotnet add package Wlniao.AspNetCore`（依赖 Wlniao.XCore）

## 强制要求
Web宿主与控制器的场景优先使用以下类型，不要自行处理Kestrel监听、TLS证书、参数解析、错误页输出：
**宿主启动** 使用`Wlniao.WebHostBuilderWlniaoExtensions.UseWlniao`扩展方法
**服务注册** 使用`Wlniao.ServiceCollectionExpand.AddBusiness`扩展方法，业务类标注`[BusinessService]`特性
**控制器基类** 使用`Wlniao.Mvc.XCoreController`（页面/API通用）、`Wlniao.Mvc.XApiController`（SM4+SM3加密通讯）、`Wlniao.Engine.EngineController`（通用引擎）
**请求参数** 使用基类的`GetRequest`/`PostRequest`/`HeaderRequest`系列方法，不要直接读`Request.Query`/`Request.Form`/`Request.Body`
**响应输出** 成功用`OutSuccess`/`Json`，失败用`OutMessage`/`ErrorMsg`，调试用`DebugMessage`
**异常兜底** 使用`Wlniao.Middleware.ErrorHandlingExtension.UseErrorHandling`中间件
**接口限流** 使用`Wlniao.Middleware.RateLimitExtension.UseRateLimit`中间件
**会话状态** 使用`Wlniao.Engine.EngineSession`类（Encode/Decode）
**停服维护** 使用`Wlniao.WebService.ServiceStop`静态方法

## 类型核心方法说明

### 宿主启动 UseWlniao（Program.cs 入口）
```csharp
IWebHostBuilder UseWlniao(this IWebHostBuilder builder, Action<KestrelServerOptions>? options = null)
// 自动：隐藏Error以下框架日志、UseContentRoot、Kestrel监听XCore.ListenPort(WLN_LISTEN_PORT,默认5000)
// xcore/server.crt+server.key存在时自动启用TLS监听(WLN_TLS_PORT,Http1/2/3)，与HTTP端口不同则双端口并存
```

### Wlniao.WebService 宿主状态
```csharp
// —— 静态属性 ——
int TlsPort        // TLS端口，配置 WLN_TLS_PORT，默认同 ListenPort
// —— 静态方法 ——
void ListenLogs()  // 输出当前监听终结点日志（UseWlniao内部已调用）
void ServiceStop(string node, string code = "302", string message = "服务器正在维护中 Server maintenance.")
                   // 以维护模式启动占位服务，所有请求返回固定JSON
```

### 服务注册 AddBusiness
```csharp
void AddBusiness(this IServiceCollection service, string? assemblyFile = null)
// 扫描程序集(默认全部已加载，可指定dll文件名)中标注[BusinessService]的类，按其实现的接口注入
void AddEngineBusiness(this IServiceCollection service)   // 注册 Engine.IContext → Context
```

### Wlniao.BusinessServiceAttribute 业务组件标注
```csharp
ServiceLifetime LifeTime   // 注入生命周期：Transient(默认)/Scoped/Singleton
// 构造函数
BusinessServiceAttribute(ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
// 用法：[BusinessService(LifeTime = ServiceLifetime.Scoped)] public class MyBiz : IMyBiz { }
```

### Wlniao.Mvc.XCoreController 控制器基类
```csharp
// —— 请求参数 ——
string GetRequest(string key, string defaultValue = "")     // 取GET/FORM参数，含特殊字符仅标记RequestSecurity不过滤
                                                            // 另有 GetRequestSecurity(过滤) / GetRequestNoSecurity(原始)
int    GetRequestInt(string key)                            // GetRequest + Convert.ToInt
string PostRequest(string key, string defaultValue = "")    // 取POST参数(表单/JSON体自动解析,含Query合并)
                                                            // 另有 PostRequestAsync / PostRequestInt
string HeaderRequest(string key, string defaultValue = "")  // 取请求头(不区分大小写)
string GetPostString()                                      // POST原始报文
string GetCookies(string key)                               // 取Cookie(不区分大小写)
// —— 响应输出 ——
void   DebugMessage(string message)                         // 输出X-Debug响应头
void   OpenUseTime()                                        // 启用耗时统计，响应头输出X-UseTime
ActionResult Json(object data)                              // JSON输出(走XCore.JsonSerializerOptions,中文不转义)
ActionResult JsonStr(string jsonStr)                        // JSON字符串直接输出
ActionResult ErrorMsg(string? message = null)               // 失败输出：do=请求返回JSON，否则返回HTML错误页
// —— 请求信息属性 ——
string ClientIP       // 客户端IP(自动解析x-forwarded-for)
bool   IsHttps        // 是否HTTPS(兼容x-forwarded-proto/x-client-scheme/referer)
string UrlHost       // 当前Host(带协议头,优先WLN_HOST配置)
string UrlDomain      // 当前域名
string UrlReferer     // 页面引用地址
string UserAgent      // 浏览器UA
string GetPlatform    // 访问平台：wxwork/weixin/alipay/dingtalk/wlniao/wlnapp/other
string TraceId        // 链路追踪ID(取wln-trace-id头,无则生成,响应头X-TraceId回传)
```

### Wlniao.Mvc.XApiController 加密通讯控制器（继承XCoreController）
```csharp
// —— 属性 ——
protected string token   // 子类构造时赋值SM4通讯密钥(16字符)，一般取自Config
// —— 方法 ——
IActionResult? Check(Func<Dictionary<string, object>, IActionResult> func)
// 接收参数：timestamp(Unix秒) + data(SM4EncryptECBToHex加密的JSON) + sign(SM3(timestamp+data+token))
// 校验时效(1小时)与签名后解密出参数字典传入func；失败码：200未配token/202缺参/203过期/205签名错/206解密失败/207反序列化失败/401异常
IActionResult OutSuccess(object obj)                        // 成功输出(data自动SM4加密,code="0")
IActionResult OutMessage(string message, string code = null)// 失败输出(success=false)
IActionResult OutDefault()                                   // 按result当前状态输出
```

### Wlniao.Engine.EngineController 引擎控制器（构造注入IContext，继承XCoreController）
```csharp
Task<IActionResult> Invoke(Func<IContext, Task<IActionResult>> func, bool mustAuthentication = false)
Task<IActionResult> Invoke(Func<IContext, Task<object>> func, bool mustAuthentication = false)
// 自动执行：InitAsync(解析请求)→AuthAsync(身份认证)→mustAuthentication校验Session→执行func→OutputSerialize输出
// 认证失败返回401 JSON或AuthFailedCallback
```

### Wlniao.Engine.IContext 请求上下文（EngineController中经DI注入）
```csharp
// —— 属性 ——
string Body                                 // POST原始内容
Dictionary<string, string> Query            // GET/FORM参数
Dictionary<string, string> HeaderInput      // 请求头
Dictionary<string, string> HeaderOutput     // 输出头(自动写入响应)
string ClientIp / Authorization / IsHttps / Host
string ConsumerId / ConsumerSecretKey / ConsumerPublicKey / ConsumerPrivateKey   // 多租户密钥
EngineSession Session                       // 当前会话
bool Continue                               // 是否继续执行预定义流程
// —— 方法 ——
T InputDeserialize<T>()                     // Body反序列化为T(有SKey时自动SM4解密)
Dictionary<string, object> InputDeserialize()          // Query+Body合并为字典
IActionResult OutputSerialize<T>(T output)  // 输出(有SKey时自动SM4加密,text/plain)
// —— 可选回调（由使用方注入自定义逻辑） ——
Func<HttpRequest, Task<ConsumerInfo>> LoadConsumerInfo        // 加载租户信息
Func<HttpRequest, Task<EngineSession>> IdentityAuthentication // 登录认证
Func<HttpRequest, Task<string>> SafetyCertification           // 请求加解密密钥(缺省用sm2token头SM2解密)
```

### Wlniao.Engine.EngineSession 统一会话
```csharp
// —— 属性 ——
bool   IsValid / NotValid       // UserId非空且未过期 / 未通过认证
long   ExpireTime               // 过期时间(Unix秒)
string UserId / Name / Account / DepartmentIds
Dictionary<string, string> ExtData
// —— 方法 ——
string Encode(string consumerSecretKey, string currentConsumerId = null, int expireSeconds = 7200, bool base64 = false)
                // 会话状态SM4加密为令牌(用于下发给客户端)
void   Decode(string authorization, string consumerSecretKey, string currentConsumerId = null)
                // 从令牌解密还原会话(失败抛Authorization error)
```

### Wlniao.Engine.ConsumerInfo 多租户信息
```csharp
string Id           // 租户标识
string SecretKey    // 安全密钥(会话加解密)
string PublicKey    // 对外公钥
string PrivateKey   // 安全私钥(sm2token解密)
string Domain       // 租户域名
```

### Wlniao.Engine.EngineException 内部异常（带状态码）
```csharp
int StatusCode   // 只读，异常对应的状态码
// 构造函数
EngineException(string message, int statusCode)
EngineException(string message, Exception innerException)
```

### Wlniao.Middleware.UseErrorHandling 异常兜底中间件
```csharp
IApplicationBuilder UseErrorHandling(this IApplicationBuilder app, Action<ErrorHandlingOptions> configureOptions)
// 全局异常兜底：错误写入X-Wlniao-Debug响应头；POST或do=请求返回JSON，其余返回HTML错误页
// ErrorHandlingOptions.StatusCode：异常响应状态码，默认502
```

### Wlniao.Middleware.UseRateLimit 接口限流中间件
```csharp
IApplicationBuilder UseRateLimit(this IApplicationBuilder app, Action<RateLimitOptions> configureOptions)
// 按客户端IP限流，带熔断器(默认开启)；超限返回429，黑名单返回428；可注入IRateLimitStore实现分布式限流
```

### Wlniao.Middleware.RateLimitOptions 限流配置
```csharp
RateLimitAlgorithm Algorithm   // 令牌桶TokenBucket(默认)/固定窗口FixedWindow/滑动窗口SlidingWindow
int  TimeWindow                // 时间窗口秒，默认180(配置 WLN_RATE_TIME_SECONDS)
int  MaxRequests               // 窗口内最大次数，0不启用(配置 WLN_RATE_MAX_REQUESTS)
int  BucketCapacity            // 令牌桶容量，0时取MaxRequests
bool IsolationPath             // 不同请求目录分开限流
string[] WhitePath             // 白名单目录，支持/api*前缀(配置 WLN_RATE_WHITEPATH)
string[] WhiteKeys             // 白名单IP(配置 WLN_RATE_WHITEKEYS)
string[] BlackKeys             // 黑名单IP(配置 WLN_RATE_BLACKKEYS)
string MessageTpl              // 拒绝消息模板，支持{ClientKey}变量
bool EnableCircuitBreaker      // 熔断器开关，默认true
int  CircuitBreakerFailureThreshold / CircuitBreakerRecoveryTime   // 熔断阈值5次/恢复30秒
IRateLimitStore DistributedStore   // 分布式限流存储(自行实现TryAcquireAsync)
bool PreferDistributedStore    // 优先使用分布式存储
```

### 其它
```csharp
Task<HttpResponseMessage> DevProxy.Http(HttpRequest request, string target)   // 开发期HTTP/WebSocket反向代理
class Mvc.Routing.ContainsKeyConstraint(params string[] keys)                 // 路由约束：路由值须为指定键之一
class Mvc.Routing.LocalhostConstraint                                         // 路由约束：拦截swagger路由
class Engine.IRuntime        // 启动时初始化接口：bool InitStart()
```
