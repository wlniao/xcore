# AGENTS.md（xcore 仓库维护指引）

本文件面向**本仓库的维护者**（修改 XCore / AspNetCore / XCenter / Tasker 源码的人）。
`Reference/Wlniao/AGENTS.md` 是分发给**业务项目使用者**的模板，不要与本文互相替换或合并。

当前统一版本：**10.0.8**。目标框架：`net8.0` / `net10.0`。当前版本变更记录见 `CHANGELOG.md`。

## 1. 仓库结构

| 路径 | 作用 |
|---|---|
| `Wlniao.XCore/` | 核心库：配置、日志、加密、缓存、HTTP、JSON、XServer 通信；`IsAotCompatible=true` |
| `Wlniao.AspNetCore/` | 宿主扩展：MVC 控制器、中间件、限流；`IsAotCompatible=true` |
| `Wlniao.XCenter/` | 认证与 SaaS 上下文：`Context`、`XAppController`、EMI、`TokenApi`；开 Trim/AOT 分析器 |
| `Wlniao.Tasker/` | 后台任务与 MQTT；开 Trim/AOT 分析器 |
| `Wlniao.XCore.NTest/` | NUnit 测试；因命名空间冲突使用 `extern alias xcore` |
| `Wlniao.XCore.AotCompatTest/` | NativeAOT 冒烟工程；**未加入 `Wlniao.XCore.sln`**，需单独构建 |
| `Reference/Wlniao/` | 面向业务项目的 AI 接入材料：`AGENTS.md` 模板 + 三份参考手册 |
| `CHANGELOG.md` / `Todo.md` | 发布说明与未处理事项；改公共 API 或行为时同步更新 |

六个子目录各有一份 `Directory.Build.props`，把 `obj` / `bin` 重定向到临时目录。**不要改动这些文件，也不要删除 `.codebuddy/`（项目数据，不是缓存）**。

## 2. 构建与测试

版本升级后，下游项目通过 `PackageReference` 引用同版本 XCore / AspNetCore；本地新版本尚未发布到 NuGet 时，先按顺序生成本地包，再构建解决方案：

```bash
# 1) 先生成 XCore 包（输出到 /.nuget；Windows 为 %HOMEDRIVE%%HOMEPATH%\.nuget）
dotnet build Wlniao.XCore/Wlniao.XCore.csproj -v minimal

# 2) 按顺序构建下游项目，补充本地包源
dotnet build Wlniao.AspNetCore/Wlniao.AspNetCore.csproj -p:RestoreAdditionalProjectSources=/.nuget -v minimal
dotnet build Wlniao.XCenter/Wlniao.XCenter.csproj -p:RestoreAdditionalProjectSources=/.nuget -v minimal
dotnet build Wlniao.Tasker/Wlniao.Tasker.csproj -p:RestoreAdditionalProjectSources=/.nuget -v minimal

# 3) 全量构建与测试
dotnet build Wlniao.XCore.sln -p:RestoreAdditionalProjectSources=/.nuget -v minimal
dotnet test Wlniao.XCore.NTest/Wlniao.XCore.NTest.csproj --no-build -v minimal

# 4) AOT 冒烟工程（不在 sln 中）
dotnet build Wlniao.XCore.AotCompatTest/Wlniao.XCore.AotCompatTest.csproj -p:RestoreAdditionalProjectSources=/.nuget -v minimal
```

约束：

- `Wlniao.XCore.NTest` 只跑 `net10.0`；集成测试通过 `[Category("Integration")]` 与环境变量跳过（如 `WLN_TEST_REDIS`、`WLN_TEST_REDIS_CONNECT_HOST`），不要因为跳过而改断言。
- `Wlniao.XCore.AotCompatTest` 已移除全局 `NoWarn=IL2026;IL3050`；抑制只存在于故意演示反射风险的 `Program.cs` / `JsonTrimDiagnosticsDemo.cs`。**不要恢复全局抑制**。
- 改加密、时间、证书、缓存、JSON、协议签名后，必须跑 `dotnet test`。
- 提交前执行 `git diff --cached --check`，确保无行尾空格、无空格+制表符混用缩进。

## 3. AOT / 裁剪红线

1. **禁止匿名类型参与序列化**。请求体、响应体、日志载荷一律用 `Dictionary<string, object>` 或具名 DTO + `[JsonSerializable]` 上下文。匿名类型在 AOT 下会静默输出 `{}`。
2. `Wlniao.Json.Serialize/Deserialize` 的反射重载仅在 CoreCLR 兜底；AOT 路径必须传 `JsonTypeInfo<T>`。先查 `Wlniao.JsonContext.Instance` / `Wlniao.XCenter.JsonContext.Instance` 是否已登记，再自建上下文。**不要用生成的 `Default` 做输出序列化**（缺中文不转义 / `JsonNode` 选项）。
3. 出站 HTTP / 平台请求：
   - `Wlniao.XServer.XApi.Request<T>` 与 `TokenApi.Request<T>`（10.0.8 起）都接收预序列化 `dataJson` + `JsonTypeInfo<T>`。
   - `Context.AppData<T>` / `ApiData<T>` 的请求参数用字典；响应优先用源生成元数据反序列化。
4. 响应输出：`Wlniao.Mvc.ResponseJson` 反射禁用时要求 `options.TypeInfoResolver` 命中，否则抛 `NotSupportedException`；`XAppController.OutputSerialize<T>` / `OutputMessage<T>` 经 `SerializeOutput` 先查 XCenter 上下文再回退 XCore 上下文，未登记类型快速失败。
5. 引用 `XCenter.Context` 会启用 `Json.StrictTrimDiagnostics = true`：CoreCLR 行为不变，AOT / 裁剪下未覆盖类型直接抛异常。这是有意的 fail-fast，不要为「让测试通过」关掉。
6. 修改 `JsonContext` 登记类型时，`Wlniao.XCore/JsonContext.cs` 与 `Wlniao.XCenter/JsonContext.cs` 都要检查；`ApiResult<T>` 的载荷类型也要登记。

## 4. 安全与协议边界

- SM2 / SM3 / SM4 已是纯托管实现；生产项目**不要引入 BouncyCastle**（仅 `Wlniao.XCore.NTest` 保留它作互操作参照）、不要引入 `Newtonsoft.Json`、不要自建 `HttpClient` 静态单例。
- `XApiController` / `TokenApiController` 的 `token` 必须在构造函数赋值；签名 `SM3(timestamp + data + token)`、响应签名 `SM3(data + token)`、双向时间窗与重放拦截属于协议，**改动必须两端同步并写破坏性说明**。
- `XCore.AcceptAnyServerCertificate` 默认 `false`；只有 `1/true/yes/on` 放行。不要整体打开。
- 日志不要落盘 `sign`、令牌明文或私钥；需要模块日志用 `Loger.Topic("模块名", ...)`。
- `SessionSafeKey` 默认由 `WebNode` + 进程路径 + DB / Redis 密码派生；多实例部署要显式配置相同值。旧配置键 `SessionEncryptKey` 已失效。

## 5. 改动边界（先确认再动）

- 包版本与四个 csproj 的 `<Version>`、互相之间的 `PackageReference` 版本。
- `TargetFramework`、`Directory.Build.props`（6 份）、`.csproj` 的 AOT / 分析器开关。
- 协议字段、错误码、签名公式、已有配置键名（运维依赖）。
- 跨模块重构、删除公开 API、改变 `Result<T>` / `ApiResult<T>` 语义。

## 6. 版本与文档同步清单

改公共 API、配置键或行为时，以下文件必须一起更新（`dotnet build` 不会发现文档不同步）：

- `CHANGELOG.md`：新版本段落，破坏性变更写迁移示例。
- 根目录 `README.md`：升级差异表 + 文档版本基线。
- 对应包 README（如 `Wlniao.XCenter/README.md`）：API 变动与迁移方式。
- `Reference/Wlniao/AGENTS.md`：反例对照 / 自检清单（高频误用时）。
- `Reference/Wlniao/Wlniao.XCore.md` / `Wlniao.AspNetCore.md` / `Wlniao.AspNetCore.RateLimit.md`：顶部版本基线与签名段落。
- `Todo.md`：尚未处理的事项同步标记为完成。
- 回归测试：覆盖新增行为与破坏性变更。

版本号升级时，四个包版本及内部 `PackageReference` 必须一致；不要只改其中一处。

## 7. 常用陷阱

| 场景 | 正确做法 | 错误后果 |
|---|---|---|
| `Convert.ToInt(x)` 且 `using System;` | `Wlniao.Convert.ToInt(x)` | CS0104 二义性 |
| `Config.GetConfigs("k")` 缺项 | `string.IsNullOrEmpty(v)` 判空 | 返回 `""`，`== null` 永不成立 |
| `Cache.Set("k", v, 0)` | 传足够大的秒数 | `0` 是立即过期，不是永久 |
| `DateTools.GetNow()` | 不与 `DateTime.Now/UtcNow` 混算 | 时区语义不一致 |
| `Json(new { ... })` | 字典或具名 DTO | AOT 下输出 `{}` |
| `TokenApi.Request<T>(..., new { ... }, ...)` | `dataJson` + `JsonTypeInfo<T>` | 10.0.8 起签名已移除 |

## 8. 提交规范

仅在用户明确要求时执行 `git commit`。提交信息沿用仓库风格：

```
vX.Y.Z 简短主题
1.变更点一
2.变更点二
...
```

不要自行 push、改 Git 配置或执行破坏性 Git 命令。

## 9. 文档索引

| 需要什么 | 去哪找 |
|---|---|
| 核心库签名与配置 | `Reference/Wlniao/Wlniao.XCore.md` |
| 宿主、控制器、报文 | `Reference/Wlniao/Wlniao.AspNetCore.md` |
| 限流与动态封禁 | `Reference/Wlniao/Wlniao.AspNetCore.RateLimit.md` |
| 发布与破坏性变更 | `CHANGELOG.md` |
| 未处理事项 | `Todo.md` |
| 本文没写的实现细节 | 源码与包内 XML 注释 |

文档只写当前行为；看到「另有重载」时读源码确认签名，不要按 .NET 习惯推测参数顺序。
