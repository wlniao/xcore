# AGENTS.md

本项目引用 Wlniao 框架：`Wlniao.XCore`，Web 项目再加 `Wlniao.AspNetCore`。
本文按包版本 **10.0.1** 编写，只写「必须遵守什么」，不写 API 清单。

- 完整签名查 `Reference/Wlniao/Wlniao.XCore.md`、`Reference/Wlniao/Wlniao.AspNetCore.md`，**按需检索，不要整篇预读**。
- 本文与源码冲突时，以源码 / 包内 XML 注释为准，并顺手修正对应文档。
- 本文没覆盖的 API，读源码后再说，**不要按 .NET 习惯推测参数顺序**。

## 1. 每个任务的固定动作

1. 动手前读 `*.csproj`：确认 `TargetFramework`（`net8.0` / `net10.0`）以及是否开启 `PublishAot` / `IsAotCompatible`。AOT 会直接改变下面一半的写法。
2. 改完跑 `dotnet build`。涉及加密、时间、证书、缓存、JSON 的改动，再跑 `dotnet test`。
3. 新增或修改公共 API、改配置键名 → 同步更新 `Reference/Wlniao/*.md`（`dotnet build` 发现不了文档不同步）。
4. 只改被要求的范围。跨模块重构、升级包版本、改 `Directory.Build.props` 先问。

## 2. 命令

```bash
dotnet build
dotnet test
dotnet run --project <启动项目>
dotnet publish -c Release -r linux-x64 -p:PublishAot=true     # AOT 发布
dotnet list package --include-transitive                       # 核对实际引用的 Wlniao 版本
```

> 最后一条显示的 Wlniao 版本若不是 10.0.1，先停下确认差异（重点看 `ApiResult`、
> `XApiController` 报文形状、`AddBusiness` 的 AOT 标注），不要照本文硬套。

## 3. 边界

**可以改**：Controller / Service / DTO / 业务逻辑 / 配置读取代码。

**不要改（要改先单独确认）**：

- `Directory.Build.props` —— `obj`/`bin` 已重定向到临时目录，动它会破坏构建缓存；
- 包版本与 `TargetFramework`；
- `xcore.config` 中已有的键名（运维侧依赖）。

**不要引入**：`BouncyCastle`（SM2/SM3/SM4 已是纯托管实现）、`Newtonsoft.Json`、自建 `HttpClient` 静态单例。

## 4. 依赖约定：优先用框架类型

下列是**默认选择**。确有必要偏离时（例如已用 `int.TryParse` 明确处理失败分支），
在代码处写一行注释说明原因，不要静默改用 BCL 等价物。

| 场景 | 首选 | 避免 |
|---|---|---|
| IO / 路径 | `Wlniao.IO.PathTool`、`Wlniao.IO.FileEx` | `Directory.GetCurrentDirectory()` 自行拼路径 |
| 运行环境 | `Wlniao.XCore` | `Environment.GetEnvironmentVariable` 直读配置 |
| 配置读取 | `Wlniao.Config` | `ConfigurationManager` / 自读 json |
| 类型转换 | `Wlniao.Convert` | `int.Parse` 裸调（异常/区域差异） |
| 日志输出 | `Wlniao.Log.Loger` | `Console.WriteLine`（生产无落库、无分级） |
| 网络访问 | `Wlniao.Net.ApiClient`、`Wlniao.Net.HttpClientManager` | `new HttpClient()` |
| 时间处理 | `Wlniao.DateTools` | `DateTime.Now`（时区不受 `WLN_TIMEZONE` 控制） |
| 加密解密 | `Wlniao.Encryptor`、`Wlniao.Crypto` | 自拼密钥补位 / 引 BouncyCastle |
| 文字处理 | `Wlniao.Text.StringUtil` | 自写正则判手机号/邮箱 |
| 缓存 | `Wlniao.Caching.Cache` | 自建静态 `Dictionary` 缓存 |
| JSON | `Wlniao.Json` | `new JsonSerializerOptions()` 各自一套 |

## 5. 六个必须先知道的坑

**① `Convert` 与 `System.Convert` 重名**
同时 `using System;` 和 `using Wlniao;` 时，`Convert.ToInt(x)` 报 CS0104（二义性）。
→ 显式写 `Wlniao.Convert.ToInt(x)`。`Json`、`Config`、`StringUtil` 无此问题。

**② `Config.GetConfigs` 缺项返回空字符串，不是 null**
未命中且未给默认值时返回 `""`，判空用 `string.IsNullOrEmpty`，`== null` 永远不成立。
读取顺序：环境变量（key 全大写）→ `xcore.config` → 默认值，即 `WLN_NODE` 环境变量会屏蔽配置文件同名项。

**③ AOT / 裁剪下 JSON 必须带元数据**
`Json.Serialize/Deserialize` 的反射重载带 `[RequiresUnreferencedCode]`/`[RequiresDynamicCode]`，
AOT 下**静默输出 `{}`**，不报错但数据全丢。禁匿名类型参与序列化；`Cache.Get<T>`/`Set<T>` 必须传 `JsonTypeInfo<T>`。
排障时 `Json.StrictTrimDiagnostics = true` 可把静默丢数据变成 `NotSupportedException`。

**④ `Cache.Set(key, value, 0)` 是「立即过期」**
第三个参数是秒数，`0` 不是「永久」。想长期缓存给足够大的值（如 `365*86400`）。

**⑤ `DateTools` 的时间都是无时区语义**
`GetNow()` 返回 `Kind=Unspecified` 且已按 `WLN_TIMEZONE`（默认 8）偏移。
不要拿它和 `DateTime.UtcNow`/`DateTime.Now` 混算，也不要 `.ToUniversalTime()`。

**⑥ TLS 默认收紧**
`XCore.AcceptAnyServerCertificate` 默认 `false`（标准链校验），只有 `1/true/yes/on` 才放行。
自签证书请装 CA 到信任库或对内网域名定向放行，**不要整体打开**（中间人可伪造任意站点）。

## 6. 仅 Web 项目（引用 `Wlniao.AspNetCore` 时适用）

> 未引用 `Wlniao.AspNetCore` 的项目，**删除本节**。

**宿主**：只用 `builder.WebHost.UseWlniao()`，不要再调 `UseKestrel`/`UseHttps`——端口与 TLS 双端口逻辑由框架接管。

**服务注册**：类上标 `[BusinessService(LifeTime = ...)]` + `services.AddBusiness()`。
⚠️ `AddBusiness` **只按接口注册**：没实现任何接口的类会被静默跳过（不报错）。

**控制器**：`XCoreController`（页面/API）、`XApiController`（SM4+SM3 加密通讯）、`EngineController`（引擎流程），不要直接继承 `Controller`。

**失败输出判据（两者不同，别混）**：

| 入口 | 返回 JSON 的条件 | 否则 |
|---|---|---|
| `ErrorMsg(...)` / `OnActionExecuted` 拦截 | 请求 URL 带 `do` 查询参数 | HTML 错误页 |
| `app.UseErrorHandling(...)` | 请求方法为 `POST` **或** 带 `do` 参数 | HTML 错误页 |

即：POST 请求若不带 `do=`，`ErrorMsg` 返回的仍是 HTML。AJAX 失败分支请带 `do=1` 或用 `OutMessage`。

**`XApiController`**：

- `token` 必须在构造函数里赋值（16 字符 SM4 密钥），否则 `Check` 直接失败返回；
- 校验式 `sign == SM3(timestamp + data + token)`，`timestamp` 只拦过期不拦超前；
- **成功与失败报文形状不同**：成功走手拼字典且 `data` 为 SM4 密文、`code="0"`；失败走 `ApiResult<object>` 且 `data` 为明文状态码。客户端必须分开解析。

**AOT 注意**：`AddBusiness` 标了 `[RequiresUnreferencedCode]`；`Json(object)`/`OutSuccess(object)` 走反射序列化。
启用了 AOT 就改用显式注册 + 具名 DTO，不要指望这两条路径。

## 7. 反例 → 正例

| ❌ 常见写法 | ✅ 正确写法 | 后果 |
|---|---|---|
| `Cache.Get<T>("k")` | `Cache.Get("k", MyCtx.Default.T)` | 编译失败（无该重载） |
| `Json.Deserialize<Dto>(json)` 用在 AOT 发布 | `Json.Deserialize(json, MyCtx.Default.Dto)` | 静默返回 `{}`，数据全丢 |
| `Json.Serialize(new { Id = 1 })` | 具名 DTO / `Dictionary<string, object>` | AOT 下无元数据 |
| `var v = Config.GetConfigs("k"); if (v == null)` | `if (string.IsNullOrEmpty(v))` | 缺项返回 `""`，判空永不成立 |
| `Convert.ToInt(x)` + `using System;` | `Wlniao.Convert.ToInt(x)` | CS0104 二义性 |
| `DateTime.Now` | `DateTools.GetNow()` | 时区不受配置控制 |
| `new HttpClient()` | `ApiClient.Get(url)` / `HttpClientManager.SharedInstance` | 连接池/DNS/证书策略失效 |
| `Cache.Set("k", v, 0)` 想表示"永久" | 传足够大的秒数 | `0` 是"立即过期"，写完读不到 |
| 自拼 SM2 公钥字节 / 引 BouncyCastle | `KeyTool.GetPublicKeyFromPrivateKey(privateKey)` | 手拼易错，第三方库违反零依赖约定 |
| `FileEx.Zip(path)` | `System.IO.Compression.ZipFile` | 必抛 `NotImplementedException` |
| `new HttpClient(new CustomHttpClientHandler{...})` | `new CustomHttpClientHandler{...}.CreateHttpClient()` | 它已是工厂不是处理器，不编译 |
| （Web）业务类标了 `[BusinessService]` 却没实现接口 | 实现接口（如 `IOrderService`） | 静默不注册，运行期拿不到服务 |
| （Web）每个 Action 里 try/catch | `app.UseErrorHandling(...)` 一次兜底 | 重复代码、漏捕获 |
| （Web）POST 请求指望 `ErrorMsg` 返回 JSON | 失败分支带 `do=1`，或用 `OutMessage` | `ErrorMsg` 只看 `do` 参数 |

## 8. 提交前自检

- [ ] 是否新增了 `DateTime.Now`、`new HttpClient()`、`int.Parse`、`Console.WriteLine`？→ 换成框架类型
- [ ] 新 DTO 参与 JSON 序列化，是否登记了 `[JsonSerializable]`？
- [ ] 用了 `Cache.Get<T>`，是否传了 `JsonTypeInfo<T>`？
- [ ] 是否误把 `XCore.AcceptAnyServerCertificate` 设成了 `true`？
- [ ] 需要「SM2 私钥 → 公钥」时是否用 `KeyTool.GetPublicKeyFromPrivateKey`？
- [ ] 异常路径是否返回 `Result<T>.OutMessage(...)` / `ErrorMsg(...)` 而不是抛到框架外？
- [ ] 日志是否用 `Loger.Topic("模块名", ...)`，而不是拼字符串进 `Info`？
- [ ] Web 项目：`Program.cs` 里是否只有 `UseWlniao`，没有额外的 `UseKestrel`/`UseHttps`？
- [ ] Web 项目：新业务类是否标了 `[BusinessService]` **并且实现了接口**？
- [ ] 加密通讯接口是否属于 `XApiController` 且 `token` 已赋值？

## 9. 文档与源码

| 需要什么 | 去哪找 |
|---|---|
| 某个类的完整签名 | `Reference/Wlniao/Wlniao.XCore.md` §5 / `Reference/Wlniao/Wlniao.AspNetCore.md` §4 |
| 配置项 | `Reference/Wlniao/Wlniao.XCore.md` §7、`Reference/Wlniao/Wlniao.AspNetCore.md` §6 |
| 报文/错误码细节 | `Reference/Wlniao/Wlniao.AspNetCore.md` §3「`XApiController` 报文形状」 |
| 本文没写的 API | 源码或包内 XML 注释：<https://github.com/wlniao/xcore> |

文档不是源码的镜像：手册里省略了重载、扩展方法与内部类型。**看到「另有重载」这类表述时去读源码确认签名**，别猜。
