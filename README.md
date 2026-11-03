# Wlniao.XCore
![GitHub License](https://img.shields.io/github/license/wlniao/xcore)
![Package Version](https://img.shields.io/nuget/v/Wlniao.XCore)
![Pull Count](https://img.shields.io/nuget/dt/Wlniao.XCore)

项目主页：<https://github.com/wlniao/xcore>　·　国内源码仓库：<https://gitee.com/wlniao/xcore>

Wlniao.XCore 是一个基于 .NET Core 的轻量级开发框架，覆盖配置管理、日志、加密解密、缓存、HTTP 客户端、API 结果封装等基础能力，并提供 ASP.NET Core 宿主集成与微服务通信支持，帮助开发者快速构建稳定、高效的 .NET 应用程序。

## 环境要求

- .NET 8.0 / .NET 10.0（支持 NativeAOT 发布）
- Windows / Linux / macOS
- Redis（可选，用于分布式缓存）

## 安装

```bash
# 核心框架（必装）
dotnet add package Wlniao.XCore

# ASP.NET Core 宿主扩展（Web 项目）
dotnet add package Wlniao.AspNetCore

# 模块化认证服务（按需）
dotnet add package Wlniao.XCenter
```

## 快速开始

### 基本配置

在项目中创建 `xcore.config` 配置文件（开发环境可另建 `xcore.dev.config`，会自动优先读取）：

```ini
# 日志配置
WLN_LOG_TYPE=console
WLN_LOG_LEVEL=info

# 缓存配置
WLN_CACHE=memory

# 监听端口
WLN_LISTEN_PORT=5000

# 节点配置
WLN_NODE=api
```

### 简单示例

```csharp
using Wlniao;

// 配置读取
var port = Config.GetConfigs("WLN_LISTEN_PORT", "5000");

// 日志记录
Log.Loger.Info("Application started");

// 时间处理
var now = DateTools.GetNow();
var unixTime = DateTools.GetUnix();
var formattedTime = DateTools.Format(now, "yyyy-MM-dd HH:mm:ss");
var utcTime = DateTools.ConvertToUtc(unixTime);

// 加密操作
var encrypted = Encryptor.Md5Encryptor32("hello world");

// 缓存操作
Cache.Set("key", "value", 3600);
var value = Cache.Get("key");

// HTTP 请求
var response = Net.ApiClient.Get("https://api.example.com/data");
```

## 功能模块

| 模块 | 说明 |
|---|---|
| Config | 环境变量与配置文件双重配置，支持加密配置项 |
| Log | 多种输出方式（console / file / loki），日志级别控制，可自定义提供程序 |
| Encryptor | 摘要：MD5 / SHA1 / SHA256 / SHA512 / SM3；对称：AES / SM4（ECB / CBC）；非对称：RSA / SM2；HMAC：HmacSHA1 / HmacSHA256 / HmacSM3 |
| Caching | memory / redis / file 多实现，自动选择最优缓存策略 |
| Net | HTTP 客户端（含 HTTPS 与统一证书策略）、DNS 查询（A / CNAME / TXT / PTR / MX / SOA）、ApiClient 工具 |
| ApiResult | 统一 API 响应格式，业务状态码与消息封装 |
| Extend | 字符串 / 集合 / 枚举扩展方法，数据转换校验，分页工具 |
| DateTools | Unix 时间戳转换、时区处理、GMT/UTC 转换与格式化 |
| XServer | 微服务间通信、API 签名验证、服务注册与发现 |

**ASP.NET Core 集成**（`Wlniao.AspNetCore`）：MVC 扩展、Kestrel 服务器配置、HTTPS/TLS 支持、统一异常处理与限流中间件。
**模块化认证**（`Wlniao.XCenter`）：认证授权服务、微服务协调。

## 高级特性

- **微服务安全通信**：内置基于 SM4+SM3 的加密通信机制
- **API 签名验证**：完整的接口签名校验机制
- **多环境支持**：通过环境变量或配置文件切换开发 / 测试 / 生产环境
- **NativeAOT**：全库兼容 NativeAOT 发布，反射兜底 API 均有显式标注

## AI 编码助手接入（AGENTS-Usage）

[`AGENTS-Usage/`](AGENTS-Usage/) 目录提供面向 AI 编码助手（Codex / Cursor / Claude Code / WorkBuddy 等）的接入材料，按包拆分、分层使用：

| 文件 | 落位 | 作用 |
|---|---|---|
| [`AGENTS.md`](AGENTS-Usage/AGENTS.md) | `AGENTS.md`（项目根目录） | **执行约定**：命令、边界、必须遵守的规则、自检清单 |
| [`Wlniao.XCore.md`](AGENTS-Usage/Wlniao.XCore.md) | `Reference/Wlniao/Wlniao.XCore.md` | **参考手册**：`Wlniao.XCore` 的签名、配置项、反例 |
| [`Wlniao.AspNetCore.md`](AGENTS-Usage/Wlniao.AspNetCore.md) | `Reference/Wlniao/Wlniao.AspNetCore.md` | **参考手册**：`Wlniao.AspNetCore` 的宿主、控制器、报文、限流 |

在**业务项目根目录**执行（`sh` / `bash` / Git Bash / WSL），目录会自动创建。**境内推荐从 Gitee 下载**（直连快、无需代理）：

```bash
BASE=https://gitee.com/wlniao/xcore/raw/master/AGENTS-Usage; mkdir -p Reference/Wlniao; curl -fsSL "$BASE/AGENTS.md" -o AGENTS.md; curl -fsSL "$BASE/Wlniao.XCore.md" -o Reference/Wlniao/Wlniao.XCore.md; curl -fsSL "$BASE/Wlniao.AspNetCore.md" -o Reference/Wlniao/Wlniao.AspNetCore.md
```

境外或可直连 GitHub 的环境，把 `BASE` 换为 `https://raw.githubusercontent.com/wlniao/xcore/master/AGENTS-Usage` 即可。Windows PowerShell 中 `curl` 是 `Invoke-WebRequest` 的别名，请改用 `curl.exe`。

未引用 `Wlniao.AspNetCore` 的项目，执行后删掉 `Reference/Wlniao/Wlniao.AspNetCore.md` 即可（`AGENTS.md` 已把 Web 相关约定集中在 §6，删掉该节即可）。

> ⚠️ 三个文件要按上表的**落位**分别放置——`AGENTS.md` 用 `Reference/Wlniao/Wlniao.XCore.md` 这样的相对路径指路，
> 路径或文件名对不上等于断链；也**不要**把两份手册拼接成一个文件（章节编号与同名小节会冲突）。

### 升级需留意的行为差异

以下四条会影响已有代码的运行结果，升级时请逐条确认：

| 项 | 变化 | 影响 |
|---|---|---|
| `CustomHttpClientHandler` | 由「继承 `HttpClientHandler` 的处理器」改为「处理器工厂」 | `new HttpClient(new CustomHttpClientHandler{...})` 不再编译；改用 `new CustomHttpClientHandler{...}.CreateHttpClient()` |
| `XCore.AcceptAnyServerCertificate` | 默认 `false`（按标准链校验），仅 `1/true/yes/on` 才放行 | 依赖全局放行访问自签站点的服务需显式开启，否则握手失败 |
| `"x@y.z".IsEmail()` | 判据统一委托 `StringUtil.IsEmail`：多级域名由 `false` 变 `true`，单字符 TLD 由 `true` 变 `false` | 以扩展方法做邮箱校验的表单，结论可能与升级前不同 |
| `StringUtil.GetHtmlTitle` | 由实例方法改为静态方法 | 调用方式改为 `StringUtil.GetHtmlTitle(html)` |

### 维护约定（文档贡献者）

- 本文档组按 `Wlniao.XCore` / `Wlniao.AspNetCore` **10.0.1** 编写；改公共 API 时同步更新对应文件（`dotnet build` 不会发现文档不同步）。
- 新增 API 补三处：手册的签名段落、反例对照（如有坑）、`AGENTS.md` 的自检清单（如属高频误用）。
- AOT 标注必须与源码一致：源码标了 `[RequiresUnreferencedCode]` / `[RequiresDynamicCode]` 的 API，手册里必须写明。
- 手册只写「当前行为」，不写历史；泛型类型名用反引号包裹，不要写 `&lt;T&gt;` 实体；签名要能直接编译，不要省略参数名。

## 许可证

本项目采用 Apache License 2.0 许可证，详情请见 [LICENSE](license.txt) 文件。

## 贡献

欢迎提交 Issue 和 Pull Request 来改进这个项目。

## 项目链接

- [NuGet 包](https://www.nuget.org/packages/Wlniao.XCore/)
- [GitHub 仓库](https://github.com/wlniao/xcore)
- [Gitee 仓库](https://gitee.com/wlniao/xcore)
