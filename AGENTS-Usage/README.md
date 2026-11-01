# AGENTS-Usage

面向 AI 编码助手的 `AGENTS.md` 使用模板，按包拆分。**模板内容已与源码逐条核对**，可直接复制到业务项目根目录使用。

| 文件 | 适用场景 | 覆盖内容 |
|---|---|---|
| [`Wlniao.XCore.md`](Wlniao.XCore.md) | 任何引用 `Wlniao.XCore` 的项目（控制台、服务、网关） | 强制约定、NativeAOT/JSON 写法、配置/时间/缓存/加密/HTTP/DNS/IO/JSON/Result/Pager、反例对照、配置项、自检清单 |
| [`Wlniao.AspNetCore.md`](Wlniao.AspNetCore.md) | 引用 `Wlniao.AspNetCore` 的 Web 项目 | 宿主启动、服务注册、控制器基类、加密通讯、Engine 引擎、中间件、反例对照、配置项、自检清单 |

## 使用方式

```bash
# 只引 XCore
cp AGENTS-Usage/Wlniao.XCore.md ../your-project/AGENTS.md

# 引 AspNetCore（两个都拷，合并成一个 AGENTS.md）
cat AGENTS-Usage/Wlniao.XCore.md AGENTS-Usage/Wlniao.AspNetCore.md > ../your-project/AGENTS.md
```

## 需要业务方留意的行为差异

以下五条会影响已有代码的运行结果，升级时请逐条确认（不属于「历史叙述」，是仍在生效的当下语义）：

| 项 | 变化 | 影响 |
|---|---|---|
| `CustomHttpClientHandler` | 由「继承 `HttpClientHandler` 的处理器」改为「处理器工厂」 | `new HttpClient(new CustomHttpClientHandler{...})` 不再编译；改用 `new CustomHttpClientHandler{...}.CreateHttpClient()` |
| `XCore.AcceptAnyServerCertificate` | 默认 `false`（按标准链校验），仅 `1/true/yes/on` 才放行 | 依赖全局放行访问自签站点的服务需显式开启，否则握手失败 |
| `"x@y.z".IsEmail()` | 判据统一委托 `StringUtil.IsEmail`：多级域名 `user@sub.domain.com` 由 `false` 变 `true`，单字符 TLD 由 `true` 变 `false` | 以扩展方法做邮箱校验的表单，结论可能与升级前不同 |
| `StringUtil.GetHtmlTitle` | 由实例方法改为静态方法 | 原来的实例方法定义在抽象类上，任何代码都无法调用，因此只影响「照抄签名」的代码；调用方式改为 `StringUtil.GetHtmlTitle(html)` |
| `Wlniao.OpenApi` 依赖 | 锁定 `Swashbuckle.AspNetCore.SwaggerGen 6.5.0` + `Microsoft.OpenApi 1.6.14` | 其源码使用 `Microsoft.OpenApi.Models`，该命名空间在 2.x 起被移除；若同一应用引用了 Swashbuckle 10.x，会因主版本冲突需要显式降级 |

## 维护约定

- 模板里的签名必须与源码一致；改公共 API 时同步更新对应文件（`dotnet build` 不会发现文档不同步）。
- 泛型类型名一律用反引号包裹（如 `` `Wlniao.Result<T>` ``），**不要写 `&lt;T&gt;` 实体**——实体在部分渲染器里会原样显示。
- 只写「当前行为」，不写「历史缺陷/曾经怎样」——变更过程留在提交信息里。
- 新增 API 时补三处：API 手册、反例对照（如有坑）、自检清单（如属高频误用）。
