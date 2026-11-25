# 未处理事项清单

以下内容在本轮「高 → 中」修复中**有意未做**，按类别整理：

---

## 一、协议 / 密码学（高影响，需两端同步）

| 项 | 来源 | 说明 |
|---|---|---|
| **全线 SM4-ECB → CBC/GCM** | 安全 M2 | 相同明文→相同密文、可块级重排；改协议要动 `XApi`/`TokenApi`/`XApiController`/`EngineSession`/`XSession` 并做存量兼容 |
| **`FitKey16` 截断+`'0'` 补位** | 安全 M12 | 超 16 字符只用前 16（熵减半）；改动会使**存量密文全部无法解密** |
| **AES 全零 IV / 密钥补 `\0` / ASCII 取字节** | 安全 M10 | 已文档化的弱默认；需新增带随机 IV + KDF 的替代 API 并引导迁移 |
| **协议签名改为 HMAC-SM3** | 安全 L11 | 现为 `SM3(ts‖data‖token)` 裸拼接，无分隔符有边界歧义；框架已有 `HmacSM3` 未用；**需对端同步升级** |
| **EmiContext `MD5(无分隔拼接+apptoken)` 签名** | 安全 L5 | 参数边界歧义 + MD5 作 MAC |
| **DNS 事务 ID 用 CSPRNG** | 安全 L7 | 现为 `Interlocked.Increment`，离路径 UDP 欺骗只剩源端口熵 |
| **`IsSafeSqlString` 字符黑名单** | 安全 L8 | 公开但仓库内无调用方；宜 `[Obsolete]` 引导参数化查询 |
| **上传 ticket 放 URL query + 确定性密文** | 安全 L10 | 可重放、进对端日志/Referer |

---

## 二、安全（中低，未动）

| 项 | 来源 | 说明 |
|---|---|---|
| **证书内置指纹放行不区分 `sslErrors` 类型** | 安全 M4 | `XCore.cs:312-344` 硬编码指纹；改策略可能影响内网自签部署，需产品决策 |
| **证书指纹无法轮换** | 安全 M4 | 同上 |
| **熔断期间「更严格本地兜底配额」** | 安全 M7 | 本轮只做到「熔断打开仍走正常限流」，未做「半开前更严配额」 |
| **`SM4DecryptECBFromBase64` 自动判型误判** | 安全 L3 | 纯 hex 字符串的 Base64 会被当 Hex 解码 |
| **`StringUtil` 敏感词/防注入类 API 设计** | 架构 M6/L8 | 返回风格混用；黑名单式「防注入」本身不可靠 |

---

## 三、架构 / 可维护性

| 项 | 来源 | 说明 |
|---|---|---|
| **超大文件拆分** | 架构 M1 | `StringUtil` 2317 行、`IdentifyEncoding` 2245、`XStorage` 1559、`DnsTool` 1128 |
| **XServer 出站请求管线 5 处复制** | 架构 M2 | 计时+SM4+日志同构块；头名已漂移（`X-UseTime` vs `X-UseTime`） |
| **`Config` 三套读 API 语义纠缠** | 架构 H4 | `GetConfigs` 返回 `""`、`GetSetting` 会回写默认值、默认参数不统一 |
| **两个 `IsNullOrEmpty` 同名不同义** | 架构 M7 | `StringUtil`（空白算空）vs `StringExtend`（BCL） |
| **`Runtime.Console` 自认将删** | 架构 M9 | 注释写明下个大版本重命名/移除，仍公开 |
| **`Directory.Build.props` 6 份拷贝** | 架构 L4 | `AotCompatTest` 还缺 `BaseOutputPath` 段；**AGENTS 约定动 props 先问** |
| **命名风格 / 拼写错误** | 架构 L1 | `ConverToNotNull`、`getPropertyTypeName`、`newWorker` 等 |
| **`FileEx.Zip/UnZip` 必抛** | 架构 M9 | 已 `[Obsolete]`+文档反例；彻底移除需 major 版本 |
| **TODO 1 条 + 大段注释代码** | 架构 L7 | `StringUtil.cs:1096`、`Pinyin.cs` 旧算法等 |

---

## 四、并发 / 资源（残留）

| 项 | 来源 | 说明 |
|---|---|---|
| **同步阻塞异步 `.Result`/`.Wait()`（约 15 处）** | 并发 H6 | XCenter/Context、XCoreController、LokiLoger、Mqtt 等；需逐点 async 化 |
| **`Mqtt.Start` 无限循环无 CancellationToken** | 并发 H15 | 无法优雅停机；故障多数次静默 |
| **`Http.ConnectionSlots` 进程级 static** | 并发 H16 | 多实例共享 100 并发槽；无 `Stop()` |
| **`RedisClient.GetSocket` 可能超 `MaxConnections`** | 并发 L27 | 检查与创建非原子，短暂可超 30 |
| **`DateTools.TimeZone` / `FileCache._path` / XCenter 静态字段懒初始化** | 并发 H12-14 | 低风险竞态，未改 `Lazy<>` |
| **`XCore.Lock` 仍 public** | 并发 L21 | 注释说不该用，未标 `[Obsolete]` |
| **`InMemory` 过期清理抽样** | 并发 L23 | 纯写不读的键可能滞留 |
| **缓存层 `catch` 后返回 `""` 无法区分未命中与故障** | 并发 H7 | 需 `TryGet` 或区分错误语义（API 变更） |

---

## 五、工程 / 工具链

| 项 | 来源 | 说明 |
|---|---|---|
| **打开 `EnableNETAnalyzers`** | 架构 L3 | 所有产品工程当前 `False`；开打可能大量新警告，**AGENTS 要求先问** |
| **`TreatWarningsAsErrors`** | 架构 L3 | 未设置 |
| **`Nullable=annotations` → `enable`** | 架构 L3 | 只标注不强制；测试工程已是 `enable` |
| **AOT 分析器标准不一** | 架构 L3 | XCenter/Tasker 开 `EnableAotAnalyzer`，XCore/AspNetCore 不开 |
| **覆盖率从 30.8% 提上去** | 测试 | 缓存/XServer/XCenter/RateLimit/Tasker 几乎无测试 |
| **`Wlniao.XCore.AotCompatTest` 未进 sln** | 工程 | 独立冒烟工程，不随 `dotnet test` 跑 |
| **coverlet 已引用但无基线/门槛** | 测试 | 无 `CollectCoverage` 默认配置、无 diff-cover |

---

## 六、文档 / 流程

- README「全库反射兜底均有标注」——本轮补齐了 `Reflection` 主路径，**其它文件未再逐个核对**
- 升级行为差异表（README）已补：响应 `sign`、双向时间窗、FileLoger 级别过滤、熔断仍限流、`SessionSafeKey` 改名与派生方式、10.0.8 的 `TokenApi.Request` 签名与 AOT 严格诊断
- CHANGELOG 已补 10.0.5 安全修复与 10.0.8 AOT / API 收紧条目（根目录 `CHANGELOG.md`）

---

## 建议下一步（若继续）

1. **P0 延伸**：协议签名 HMAC 化 + 响应/请求统一（需版本协调）  
2. **独立 PR**：超大文件拆分 + XServer 管线去重（纯重构，易 review）  
3. **工程 PR**：开分析器 + 覆盖率门槛（先在分支上看警告量）  
4. **并发 PR**：`.Result` 清理 + Mqtt 停机  
5. **版本**：破坏性变更已写进 CHANGELOG / 升级表，随 10.0.8 发布