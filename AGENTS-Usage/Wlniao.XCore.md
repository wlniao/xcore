# AGENTS.md 使用模板 · Wlniao.XCore

> **怎么用**：把本文件复制到项目根目录并改名为 `AGENTS.md`（已有 AGENTS.md 时并入「依赖约定」章节）。
> 文中签名均已与 Wlniao.XCore 10.0.0 源码逐条核对——**照抄本文件，不要按 .NET 习惯推测 API**。

## 0. 包与兼容边界

```bash
dotnet add package Wlniao.XCore
```

| 项 | 值 |
|---|---|
| 目标框架 | `net8.0` / `net10.0` |
| AOT | `IsAotCompatible=true`、`IsTrimmable=true`（可按 NativeAOT 发布） |
| 第三方依赖 | 无。SM2/SM3/SM4/RSA 均为纯托管实现，不依赖 BouncyCastle |
| 命名空间 | 多数类型在 `Wlniao` 根命名空间，**不在** `Wlniao.XCore` |

> ⚠️ `Wlniao.XCore` 是**程序集名 = 类名**（`Wlniao.XCore` 类是运行环境入口），
> 业务代码里 `Wlniao.XCore.xxx` 解析到的是类而不是命名空间。引用运行环境用 `Wlniao.XCore.ListenPort` 这类写法。

## 1. 强制要求

缓存、时间、加密、HTTP 访问等场景优先使用下列类型；**缓存与日志只面向统一入口，不要关心具体实现**（后端由配置切换）。

| 场景 | 唯一选择 | 禁止做法 |
|---|---|---|
| IO / 路径 | `Wlniao.IO.PathTool`、`Wlniao.IO.FileEx` | `Directory.GetCurrentDirectory()` 自行拼路径 |
| 运行环境 | `Wlniao.XCore` | `Environment.GetEnvironmentVariable` 直读配置 |
| 配置读取 | `Wlniao.Config` | `ConfigurationManager` / 自读 json |
| 类型转换 | `Wlniao.Convert` | `int.Parse` 裸调（异常/区域差异） |
| 日志输出 | `Wlniao.Log.Loger` | `Console.WriteLine`（生产无落库、无分级） |
| 网络访问 | `Wlniao.Net.ApiClient`、`Wlniao.Net.HttpClientManager` | `new HttpClient()` |
| 时间处理 | `Wlniao.DateTools` | `DateTime.Now`（时区不受 `WLN_TIMEZONE` 控制） |
| 加密解密 | `Wlniao.Encryptor`、`Wlniao.Crypto` | 自拼密钥补位 / 引 BouncyCastle |
| 文字处理 | `Wlniao.Text.StringUtil`、`Wlniao.Text` 其它类 | 自写正则判手机号/邮箱 |
| 缓存 | `Wlniao.Caching.Cache` | 自建静态 `Dictionary` 缓存 |
| JSON | `Wlniao.Json` | `new JsonSerializerOptions()` 各自一套 |

## 2. 三个必须先知道的约定

**① `Convert` 与 `System.Convert` 重名**
同时 `using System;` 和 `using Wlniao;` 时，`Convert.ToInt(x)` 会报 CS0104（二义性）。
→ 显式写 `Wlniao.Convert.ToInt(x)`。`Json`、`Config`、`StringUtil` 无此问题。

**② `Config.GetConfigs` 缺项返回空字符串，不是 null**
未命中且未给默认值时返回 `""`。判空用 `string.IsNullOrEmpty`，`== null` 永远不成立。

**③ 配置读取顺序：环境变量（key 全大写） → `xcore.config` → 默认值**
即 `WLN_NODE` 的环境变量会屏蔽配置文件里的同名项。给默认值时用 `GetConfigs("key", "默认值")`。

## 3. NativeAOT 与 JSON（最容易踩坑）

`Json.Serialize/Deserialize` 的**反射重载**带 `[RequiresUnreferencedCode]` / `[RequiresDynamicCode]`，
在 NativeAOT / 裁剪下会退化成"静默输出 `{}`"，不报错但数据全丢。

AOT 场景按优先级三选一：

```csharp
// ① 最推荐：自有 source-generated 上下文
[JsonSerializable(typeof(OrderDto))]
internal partial class MyJsonContext : JsonSerializerContext { }

var json = Json.Serialize(dto, MyJsonContext.Default.OrderDto);
var dto2 = Json.Deserialize<OrderDto>(json, MyJsonContext.Default.OrderDto);

// ② 复用库内置上下文（内置/常用类型已被覆盖，可先用它判断）
if (Json.IsCovered(typeof(OrderDto))) { /* 内置上下文已覆盖，反射重载在 AOT 下也安全 */ }

// ③ 排查期开关：把"静默丢数据"变成"启动即失败"（默认 false）
Json.StrictTrimDiagnostics = true;   // 未覆盖类型直接抛 NotSupportedException，并给出迁移指引
```

其它 AOT 硬性要求：

- **禁止匿名类型参与序列化**（`new { a = 1 }` 无元数据，AOT 下必丢）→ 用具名 DTO 或 `Dictionary<string, object>`；
- `Cache.Set<T>` / `Cache.Get<T>` 必须传 `JsonTypeInfo<T>`（见下），没有免元数据的泛型重载；
- 反射式 API（`Type.GetProperties` 遍历赋值等）在 AOT 下不可用，改用显式映射。

## 4. 快速上手

```csharp
using Wlniao;
using Wlniao.Caching;
using Wlniao.Log;
using Wlniao.Text;

// 配置：环境变量优先，写入默认值便于运维发现
var node = Config.GetConfigs("WLN_NODE", "xcore");

// 日志：按模块分主题，便于检索
Loger.Topic("order", $"create order node={node}");

// 缓存：统一入口，后端由 WLN_CACHE 决定（auto/redis/file/memory）
Cache.Set("cfg:" + node, "1", 3600);
var v = Cache.Get("cfg:" + node);

// 时间：统一走 DateTools，时区由 WLN_TIMEZONE 控制（默认东八区）
var now = DateTools.GetNow();
var todayStart = DateTools.GetDayStart(DateTools.GetUnix());

// 加密：SM4 对称 + SM3 摘要
var cipher = Encryptor.SM4EncryptECBToHex("hello", "1234567890123456");
var plain = Encryptor.SM4DecryptECBFromHex(cipher, "1234567890123456");
var digest = Encryptor.SM3Encrypt("hello");

// IO：相对程序根目录取绝对路径
var logPath = PathTool.Map("logs", "app.log");
FileEx.Append(logPath, "line\n");

// 统一返回（AOT 下用字典/具名类型，别用匿名类型）
var ok = Result<object>.OutSuccess(new Dictionary<string, object> { ["id"] = 1 });
```

## 5. API 手册

### Wlniao.Config 配置

```csharp
string GetConfigs(string key, string defaultValue = null)   // 环境变量 → xcore.config → 默认值；缺项返回 ""
string GetSetting(string key, string defaultValue = null)   // 同上，但缺项时把默认值回写配置文件
string GetEnvironment(string key, string defaultValue = "") // 只读环境变量（key 自动大写）
bool   SetConfigs(string key, string value = "")            // 写配置文件，value 为空表示删除
string GetEncrypt(string key, string secret, string def = null)  // 读 SM4 加密存储的配置项
bool   SetEncrypt(string key, string value, string secret)       // 加密后写入配置
void   Read(string path) / bool Write(Dictionary<string,string> dic, string path)  // 手动加载/落盘
```

### Wlniao.Convert 类型转换（失败返回默认值，不抛异常）

```csharp
int     ToInt(object obj)      // "12"、"12.5"、12.9f → 12（截断，非四舍五入）
int     ToInt(float|double|decimal number)
long    ToLong(object|string obj)
double  ToDouble(string str)
decimal ToDecimal(string str)  / ToDecimal(string str, decimal defaultValue)
float   ToFloat(string str, float defaultValue = 0)
bool    ToBool(object|string obj)   // "1"/"true"/"yes" 等为真
byte[]  ToBytes(Stream stream)
int[]   ToIntArray(string myids)    // "1,2,3" → [1,2,3]
Stream  ToStream(object obj)        // object/byte[] → Stream
```

### Wlniao.XCore 运行环境与全局开关

```csharp
int    ListenPort          // HTTP 监听端口（WLN_LISTEN_PORT，默认 5000）
string ListenUrls          // 监听地址（WLN_LISTEN_URLS，默认 http://*:{ListenPort}）
string WebNode             // 节点名（WLN_NODE，默认 xcore）
string WebHost             // 对外访问地址（WLN_HOST，默认 http://127.0.0.1:{ListenPort}）
string XServerId           // 服务器 ID（XServerId）
string StartupRoot         // 程序根目录（WLN_STARTUP_ROOT，自动剥掉 /bin）
string StartupTime         // 启动时间（格式化字符串）
string Version             // XCore 版本号
string ProgramVersion      // 当前程序版本号
long   PublishTime         // 当前程序发布时间（Unix）
bool   IsDevTest           // WLN_DEVTEST == true
bool   IsUseHttps          // 可写，默认 true
string Webroxy             // 代理地址（Webroxy）
string SessionEncryptKey   // 会话密钥，未配置时按节点/主机/库名派生
JsonSerializerOptions JsonSerializerOptions   // 全局 JSON 选项（中文不转义）

bool   AcceptAnyServerCertificate            // 全局 TLS 放行开关，默认 false（收紧，见下）
Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool>
       ServerCertificateCustomValidationCallback   // 全库统一的证书校验委托

void   Init()    // 清空配置缓存并重置日志级别（改配置后想立即生效可调用）
void   Info()    // 打印启动横幅与运行信息
```

> ✅ **TLS 默认收紧**：`AcceptAnyServerCertificate` 默认 `false`，按标准链校验，
> 过期 / 域名不符 / 自签 / 链不完整都会被拒绝。升级后若依赖旧的全放行行为，需按下式显式开启。

```csharp
XCore.AcceptAnyServerCertificate = true;    // 代码方式
// 或配置 WLN_ACCEPT_ANY_CERT=true（环境变量优先，只解析一次，改配置需重启）
```

> 放行态（`true`）下：任何服务端证书都会被接受，仅首次命中时打一条 Warn；
> 收紧态（`false`，默认）下：标准链校验通过即放行，未通过时仅放行内置受信任根 CA / 二级 CA 指纹。
> 配置项解析规则为「只有显式肯定值才放行」：`1/true/yes/on`（不分大小写）放行，
> 空值与无法识别的取值一律收紧 —— 笔误不会误开成全局放行。
> 若业务自行替换了 `ServerCertificateCustomValidationCallback`，本开关不再生效。

### Wlniao.Log.Loger 日志

```csharp
void Topic(string topic, string message, LogLevel logLevel = LogLevel.Information, bool consoleLocal = true)
                                   // 带主题标签，Agent 按模块分主题记录的首选
void Debug(string message)         // 另有 Info / Warn / Error / Fatal(string message)
void Console(string message, ConsoleColor color = ConsoleColor.White)   // 直接写控制台（不进日志后端）
void SetLogger(ILogProvider provider)   // 自定义日志后端
LogLevel LogLevel { get; }         // 只读；级别由 WLN_LOG_LEVEL 决定，运行期改不了
bool   AllowDebug = true;          // Debug 总开关
string LogLocal { get; }           // 本地日志目录
```

### Wlniao.DateTools 时间（时区由 `WLN_TIMEZONE` 控制，默认 8）

```csharp
long     GetUnix()                                   // 当前 Unix 秒，另有 GetUnix(string) / GetUnix(DateTime)
DateTime GetNow()                                    // 当前时间（Kind=Unspecified，已按配置时区偏移）
DateTime Convert(long unixtime) / Convert(string)    // → 无时区本地时间
DateTime ConvertToUtc(long) / ConvertToUtc(string)   // → UTC
string   ConvertToGMT(long unixtime = 0)             // → RFC 1123 格式
string   Format(string format = "yyyy-MM-dd HH:mm:ss")
string   Format(DateTime time, string format = "yyyy-MM-dd HH:mm:ss")
string   FormatUnix(long unixtime, string format = "yyyy-MM-dd HH:mm:ss")   // 0/负值返回 ""
string   FormatUnixToUtc(long, string) / FormatUnixToLocal(long, string)
string   FormatUtc(string) / FormatLocal(string) / FormatDate(string format = "yyyy-MM-dd")
long     GetDayStart(long unixtime)   // 当日起点（按配置时区），另有 GetUtcDayStart
DayOfWeek GetDayOfWeek() / GetDayOfWeek(long)
int      TimeZone                     // 当前时区偏移小时数
```

### Wlniao.Encryptor 加解密

```csharp
// —— SM4 对称（密钥不足 16 字节补 '0'，超出截断）——
string SM4EncryptECBToHex(string plainText, string secretKey, bool isPadding = true)
string SM4DecryptECBFromHex(string encText, string secretKey, bool isPadding = true)
string SM4EncryptECBToBase64 / SM4DecryptECBFromBase64(string, string, bool isPadding = true)
string SM4EncryptCBCToHex / SM4DecryptCBCFromHex(string, string secretKey, string iv, bool isPadding = true)
string SM4EncryptCBCToBase64 / SM4DecryptCBCFromBase64(string, string secretKey, string iv, bool isPadding = true)

// —— SM2 非对称 ——
string SM2EncryptByPublicKey(string plainText, string publicKey, bool base64 = false)
string SM2DecryptByPrivateKey(string encText, string privateKey)
string Sm2SignWithPrivateKey(string text, string privateKey)
bool   Sm2VerifyWithPublicKey(string text, string sign, string publicKey)
// 亦提供 byte[] 重载，跨语言互通时优先用字节重载

// —— 摘要 / HMAC ——
string SM3Encrypt(string str)
string Sha1(string str) / Sha256(string str) / Sha512(string str)
string HmacSM3(string str, string key)
string HmacSHA1(string str, string key) / HmacSHA256Hex(string str, string key)
string Md5Encryptor16(string str) / Md5Encryptor32(string str) / Md5Encryptor32(string str, int time)

// —— RSA ——
string RsaEncryptWithPublic(string data, string publicKey, Encoding encoding = null)
string RsaDecryptWithPrivate(string data, string privateKey, Encoding encoding = null)
string RsaSignWithPrivate(string data, string privateKey, Encoding encoding = null)
bool   RsaVerifyWithPublicKey(string data, string sign, string publicKey, Encoding encoding = null)

// —— AES / Base64 / 短链混淆 ——
string AesEncrypt(string pToEncrypt, string sKey, string IV = "")
string AesDecrypt(string pToDecrypt, string sKey, string sIV = "")
string Base64Encrypt(string str) / Base64Decrypt(string str)
string VerbatimEncrypt(string txt, int crypt = 6338) / VerbatimDecrypt(string txt, int crypt = 6338)  // 可逆混淆，非安全加密
```

> 摘要类方法传**空串返回空串**；其余按 ASCII 编码计算——含中文的输入请自己先转 UTF-8 字节再算。

### Wlniao.Text.StringUtil 文本

```csharp
string CreateId()                   // 24 位：yyyyMMddHHmmssfff + 7 位数字
string CreateMinId()                // 15 位：yyMMddHHmmss + 3 位数字
string CreateLongId()               // 36 位：yyyyMMddHHmmssffffff + 0000 + 12 位数字，适合订单号
string CreateRndStr(int length)     // 纯数字随机串
string CreateRndStrE(int length)    // 数字+小写字母随机串

bool   IsIP(string ip) / IsIPv4 / IsIPv6 / IsEmail / IsMobile / IsIdentity / IsNumber
bool   IsNullOrEmpty(string str) / HasText(string str)
bool   IsChineseLetter(string) / IsAbcNumberAndChineseLetter(string) / IsColorValue(string)
bool   IsSafeSqlString(string) / string GetSafeSqlString(string) / GetSafeSqlString(string, bool tolower)
string UrlEncode(string) / UrlEncodeSymbol(string) / UrlDecode(string)     // 另有带 encoding 的重载
string UTF8ToHexString(string s) / HexStringToUTF8(string hex)
string CutString(object|string str, int length)     // 截断并补 "..."
string GetCamelCase(string) / GetTitleCase(string) / GetHtmlTitle(string)
string GetTypeName(Type) / GetTypeFullName(Type) / GetGenericTypeWithArgs(Type)   // 无反射泛型拼装
```

### Wlniao.Text.Encodings 编码

```csharp
Encoding GetEncoding(string name)   // 未注册 CodePages 时自动注册后重试（GBK/GB2312 已内置封装）
```

### Wlniao.Caching.Cache 缓存（后端 `WLN_CACHE`：auto/redis/file/memory）

```csharp
bool   Set(string key, string value, int expireSeconds = 86400)          // 秒；传 0 表示立即过期，长期缓存请给大值
bool   Set<T>(string key, T obj, JsonTypeInfo<T> typeInfo, int expireSeconds = 86400)
string Get(string key)                 // 缺项返回 ""
string GetAllowNull(string key)        // 缺项返回 null
T      Get<T>(string key, JsonTypeInfo<T> typeInfo)      // ⚠️ 必须传元数据，无免参重载
bool   Del(string key) / Exists(string key)
void   UseRedis(string connstr = null)                   // 手动切换后端
void   UseRedis(string server, string username, string password)
bool   LocalSet(string key, string value)                // 进程内本地缓存（不受后端影响）
string LocalGet(string key, int expireSeconds = 86400)
string CachePath { get; set; }                           // file 后端目录
```

### Wlniao.IO.PathTool / FileEx 文件

```csharp
string PathTool.Map(params string[] relativePath)   // 相对程序根目录 → 绝对路径：PathTool.Map("logs","a.log")
string PathTool.JoinPath(string path1, string path2) // 跨平台拼接，不依赖根目录

string FileEx.Read(string absolutePath)                  // 自动识别 BOM/编码
string FileEx.Read(string absolutePath, Encoding enc)
string FileEx.ReadUTF8String(string absolutePath)
string[] FileEx.ReadAllLines(string absolutePath)        // 另有带 Encoding 重载
byte[] FileEx.ReadByte(string absolutePath)
Encoding FileEx.GetEncoding(string absolutePath)
void   FileEx.Write(string absolutePath, string fileContent)                    // 目录不存在会创建
void   FileEx.Write(string absolutePath, string fileContent, bool autoCreateDir)
void   FileEx.Write(string absolutePath, string fileContent, Encoding enc)
void   FileEx.WriteByte / WriteStream / Append(string, string) / Append(string, string, Encoding)
void   FileEx.Delete / Move / Copy(src, dest) / Copy(src, dest, bool overwrite)
bool   FileEx.Exists(string absolutePath)
// ⚠️ FileEx.Zip / UnZip 已标记 Obsolete 且必然抛 NotImplementedException —— 请改用 System.IO.Compression.ZipFile
```

### Wlniao.Net.ApiClient HTTP（同步 + 异步成对）

```csharp
string       Get(string url, string webroxy = null)
Task<string> GetAsync(string url, string webroxy = null)
string       Post(string url, string postData, string contentType = "application/json", string webroxy = null)
Task<string> PostAsync(string url, string postData, string contentType = "application/json", string webroxy = null)
string       Post(string url, Stream stream, string webroxy = null)
Task<string> PostAsync(string url, Stream stream, string webroxy = null)
// 返回响应正文；业务层自行判断是否成功。未传 webroxy 时取配置 Webroxy
```

### Wlniao.Net.HttpClientManager / CustomHttpClientHandler 连接级管控

```csharp
// —— HttpClientManager：统一 DNS + 证书策略 + 连接池 ——
HttpClient SharedInstance { get; }                        // 进程级共享实例
HttpClient HttpClient { get; }
new HttpClientManager(TimeSpan? dnsRefreshInterval = null, TimeSpan? retireGrace = null)
// dnsRefreshInterval 默认 12 分钟；retireGrace 默认 1 分钟 = 旧实例退役后保留多久再释放，
// 期间在途请求可继续跑完；传 TimeSpan.Zero 表示刷新后立即释放（会掐断在途请求）
void Dispose()

// —— CustomHttpClientHandler：处理器工厂（SocketsHttpHandler 密封，无法继承）——
var handler = new CustomHttpClientHandler
{
    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
    ConnectTimeout = TimeSpan.FromSeconds(10),
    OnConnectionEstablished = ip => Loger.Topic("http", $"connect {ip}")   // 拿到实际连接 IP
};
using var client = handler.CreateHttpClient();   // 或 handler.CreateHandler() 自行组装
```

> 工厂产出的处理器已挂框架统一证书策略，**不要在 HTTPS 场景再包一层 `SslStream`**——会造成双重加密。
> 该策略默认收紧（按标准链校验）；确需放行自签证书时再设置 `XCore.AcceptAnyServerCertificate = true`。

### Wlniao.Net.Dns.DnsTool DNS 查询（IDisposable）

```csharp
new DnsTool()                        // 系统 DNS
new DnsTool(string serverIpOrHost)   // 指定 DNS
new DnsTool(IPAddress[] serverIPs) / new DnsTool(IPEndPoint[] servers)

IPAddress  GetIPAddressDefault(string qname)                    // A，失败回退 AAAA
Task<IPAddress> GetIPAddressDefaultAsync(string qname)
IPAddress  GetIPAddress(string qname, DnsRecordType type = DnsRecordType.A)
string     GetCNAME(string qname)
string     GetTXT(string qname)                                 // 多段已自动拼接
string[]   GetTXTs(string qname)
Task<string>  GetTXTAsync(string qname, CancellationToken ct = default)
Task<string[]> GetTXTsAsync(string qname, CancellationToken ct = default)
const int  TimeoutMilliseconds = 15000
```

> 用 `using var dns = new DnsTool();`。长 TXT（DKIM 2048 位公钥等）超出 UDP 512 字节会被上游截断，
> 这类记录建议直接查权威服务器或改用 `GetTXTs` 校验段数。

### Wlniao.Json 序列化

```csharp
string Serialize<T>(T obj, JsonSerializerOptions options = null)          // 中文不转义
string Serialize<T>(T obj, JsonTypeInfo<T> jsonTypeInfo)                  // AOT 安全
T      Deserialize<T>(string json, JsonSerializerOptions options = null)
T      Deserialize<T>(string json, JsonTypeInfo<T> jsonTypeInfo)
List<T> DeserializeToList<T>(string json, JsonTypeInfo<List<T>> jsonTypeInfo)
Dictionary<string, object?> DeserializeToDic(string json)                 // ⚠️ 值是 object?，不是 string
object? JsonNodeToObject(JsonNode? node)                                  // JsonNode 图 → 普通对象
bool   IsCovered(Type type)          // 是否已被内置源生成上下文覆盖
bool   StrictTrimDiagnostics { get; set; }   // 裁剪下把静默丢数据变成异常
```

### `Wlniao.Result<T>` 统一返回包装（Code 为 200 表示成功）

```csharp
// —— 属性 ——
int    Code          // 默认 0
T      Data          // 序列化时 null 忽略
string Message       // 序列化时 null 忽略
bool   Success       // 只读：Code == 200（不序列化）
bool   HasError      // 只读：Code != 200（不序列化）
// —— 实例链式方法 ——
Result<T> SetData(T data)                              // Code=200 且赋值 Data
Result<T> SetMessage(string message)                   // 仅改 Message，不动 Code
Result<T> SetMessage(string message, int statusCode)   // 同时改 Code
Result<T> SetStatusCode(int statusCode) / SetSuccess()
// —— 静态快捷方法 ——
Result<T> OutSuccess(T data, int statusCode = 200)
Result<T> OutMessage(string message, int statusCode, T data = default)
```

### `Wlniao.Pager<T>` 数据分页

```csharp
bool success      // 默认 true
string message    // 未赋值时按 rows/total 自动生成
int total / size / page      // size 默认 10（<=0 视为 10），page 从 1 开始
int skip          // 只读：(page-1)*size
List<T> rows
Pager<T> PutIn(List<T> source, int page = 1, int size = 10)                  // 传全量，自动切页
Pager<T> Format(List<T> data, int total, int page, int size = 0)             // 已切好页，只补元数据
```

### Wlniao.Runtime.XCoreException 内部异常（带状态码）

```csharp
int StatusCode { get; }
new XCoreException(string message, int statusCode)
new XCoreException(string message, int statusCode, Exception innerException)
```

## 6. 反例 → 正例

| ❌ 常见写法 | ✅ 正确写法 | 后果 |
|---|---|---|
| `Cache.Get<T>("k")` | `Cache.Get("k", MyCtx.Default.T)` | 编译失败（无该重载） |
| `Json.Deserialize<Dto>(json)` 用在 AOT 发布 | `Json.Deserialize(json, MyCtx.Default.Dto)` | 静默返回 `{}`，数据全丢 |
| `Json.Serialize(new { Id = 1 })` | 具名 DTO / `Dictionary<string, object>` | AOT 下无元数据 |
| `DateTime.Now` | `DateTools.GetNow()` | 时区不受 `WLN_TIMEZONE` 控制 |
| `int.Parse(s)` | `Convert.ToInt(s)` | 脏数据直接抛异常 |
| `new HttpClient()` | `ApiClient.Get(url)` / `HttpClientManager.SharedInstance` | 连接池/DNS 失效、证书策略不一致 |
| `var v = Config.GetConfigs("k"); if (v == null)` | `if (string.IsNullOrEmpty(v))` | 缺项返回 `""`，判空永不成立 |
| `Convert.ToInt(x)` + `using System;` | `Wlniao.Convert.ToInt(x)` | CS0104 二义性 |
| `app.UseHttps()` 自行配证书 | `UseWlniao()`（见 AspNetCore 模板） | 绕过 `WLN_TLS_PORT` 双端口逻辑 |
| 为省事直接打开 TLS 全放行 | 装自签 CA 到信任库，或仅对内网域名定向放行 | 全放行中间人可伪造任意站点 |
| `Cache.Set("k", v, 0)` 想表示"永久" | 传足够大的秒数（如 `365*86400`） | `0` 是"立即过期"，写完就读不到 |
| `FileEx.Zip(path)` | `System.IO.Compression.ZipFile` | 必抛 NotImplementedException |

## 7. 配置项速查

| 键 | 默认 | 说明 |
|---|---|---|
| `WLN_LISTEN_PORT` | `5000` | HTTP 监听端口 |
| `WLN_LISTEN_URLS` | `http://*:{端口}` | 监听地址 |
| `WLN_HOST` | `http://127.0.0.1:{端口}` | 对外访问地址 |
| `WLN_NODE` | `xcore` | 节点名 |
| `WLN_STARTUP_ROOT` | 当前目录 | 程序根目录（`/bin` 之后被截断） |
| `WLN_TIMEZONE` | `8` | 时区偏移小时 |
| `WLN_CACHE` | `auto` | `auto`/`redis`/`file`/`memory` |
| `WLN_REDIS_HOST` / `WLN_REDIS_PORT` / `WLN_REDIS_DB` / `WLN_REDIS` | — | Redis 连接 |
| `WLN_LOG_TYPE` / `WLN_LOG_LEVEL` / `WLN_LOG_PATH` / `WLN_LOG_LOCAL` / `WLN_LOG_INTERVAL` / `WLN_LOG_SERVER` | — | 日志后端与级别 |
| `WLN_ACCEPT_ANY_CERT` | `false` | 全局 TLS 放行开关（`1/true/yes/on` 才放行），见 §5 |
| `Webroxy` | — | 出网代理 |
| `XServerId` | — | 服务器标识 |
| `SessionEncryptKey` | 自动派生 | 会话加密密钥 |

## 8. 提交前自检

- [ ] 新增了 `DateTime.Now`、`new HttpClient()`、`int.Parse`、`Console.WriteLine`？→ 换成框架类型
- [ ] 新 DTO 参与 JSON 序列化，是否在 `JsonSerializerContext` 里登记了 `[JsonSerializable]`？
- [ ] 用了 `Cache.Get<T>`，是否传了 `JsonTypeInfo<T>`？
- [ ] 对外 HTTPS 调用的服务，是否误把 `XCore.AcceptAnyServerCertificate` 设成了 `true`？
- [ ] 异常路径是否返回 `Result<T>.OutMessage(...)` 而不是抛到框架外？
- [ ] 日志是否用 `Loger.Topic("模块名", ...)`，而不是拼字符串进 `Info`？
