# AGENTS.md (实用示例)

## 安装包`dotnet add package Wlniao.XCore`

## 强制要求
缓存、时间、加密、HTTP访问等场景优先使用以下命名空间或类型，缓存、日志输出不要考虑具体实现：
**IO操作** 使用`Wlniao.IO.PathTool`或`Wlniao.IO.FileEx`类
**运行环境** 使用`Wlniao.XCore`类获取运行环境信息
**配置读取** 使用`Wlniao.Config`类
**类型转换** 使用`Wlniao.Convert`类
**日志输出** 使用`Wlniao.Log.Loger`类
**网络访问** 使用`Wlniao.Net.ApiClient`或`Wlniao.Net.HttpClientManager`类
**时间处理** 使用`Wlniao.DateTools`类
**加密解密** 使用`Wlniao.Encryptor`类或`Wlniao.Crypto`命名空间下的类
**文字处理** 使用`Wlniao.Text.StringUtil`类或`Wlniao.Text`命名空间下的其它类
**缓存处理** 使用`Wlniao.Caching.Cache`类

## 类型核心方法说明

### Wlniao.Config 配置读取
```csharp
string GetConfigs(string key, string defaultValue = null)   // 读配置：环境变量优先，其次 xcore.config 文件
string GetSetting(string key, string defaultValue = null)   // 同上，缺失时将默认值写入配置文件
bool   SetConfigs(string key, string value = "")            // 写配置文件，value 为空表示删除
```

### Wlniao.Convert 类型转换（失败返回默认值，不抛异常）
```csharp
int    ToInt(object obj)          // "12"、"12.5"、12.9f → 12
long   ToLong(object obj)
double ToDouble(object obj)
Stream ToStream(object obj)       // object/byte[] → Stream
```

### Wlniao.XCore 运行环境
```csharp
int    ListenPort                 // HTTP 监听端口（配置 WLN_LISTEN_PORT，默认 5000）
string WebNode                    // 节点名（配置 WLN_NODE，默认 xcore）
string XServerId                  // 服务器 ID
string StartupRoot                // 程序根目录
JsonSerializerOptions JsonSerializerOptions  // 全局 JSON 选项（中文不转义），与 Wlniao.Json 配套
```

### Wlniao.Log.Loger 日志
```csharp
void Topic(string topic, string message, LogLevel logLevel = LogLevel.Information, bool consoleLocal = true)
                                  // 带主题标签输出，Agent 按模块分主题记录首选
void Error(string message)        // 还有 Debug / Warn / Info / Fatal(string message)
```

### Wlniao.DateTools 时间（时区由 WLN_TIMEZONE 控制，默认东八区）
```csharp
long     GetUnix()                            // 当前 Unix 时间戳（秒），另有 GetUnix(string) / GetUnix(DateTime)
DateTime GetNow()                             // 当前时间（无时区 Unspecified）
DateTime Convert(long unixtime)               // Unix → 无时区时间，另有 Convert(string)
string   Format(string format = "yyyy-MM-dd HH:mm:ss")        // 当前时间格式化
string   FormatUnix(long unixtime, string format = "yyyy-MM-dd HH:mm:ss") // Unix 时间戳格式化
long     GetDayStart(long unixtime)           // 当日 0 点时间戳
```

### Wlniao.Encryptor 加解密
```csharp
string SM4EncryptECBToHex(string plainText, string secretKey, bool isPadding = true)   // SM4 对称加密 → Hex
string SM4DecryptECBFromHex(string encText, string secretKey, bool isPadding = true)   // Hex → 明文
string SM2DecryptByPrivateKey(string encText, string privateKey) // SM2 非对称解密
string SM2EncryptByPublicKey(string plainText, string publicKey) // SM2 非对称加密
string SM3Encrypt(string str) // SM3 摘要
string Sha1(string str)
string Sha256(string str)
string Md5Encryptor16(string str)
```

### Wlniao.Text.StringUtil 文本
```csharp
string CreateMinId()                // 15位短唯一ID（时间+随机）
string CreateLongId()               // 36位长唯一ID（时间戳+随机），适合订单号/消息 ID
string CreateRndStrE(int length)    // 指定长度的纯字母随机串
bool   IsIP(string ip)              // IP 合法性（另有 IsNumber / IsIPv4 / IsIPv6 / IsEmail / IsMobile / IsIdentity）
string UrlDecode(string str)        // URL 解码（编码用 UrlEncode）
string UTF8ToHexString(string s)    // UTF8 字符串 ↔ Hex（反向 HexStringToUTF8）
```

### Wlniao.Caching.Cache 缓存（后端 auto/redis/file/memory 由配置 WLN_CACHE 决定）
```csharp
bool Set(string key, string value, int expireSeconds = 86400)   // 默认缓存 1 天，传 0 永久
T    Get<T>(string key)             // 泛型读取（自动 JSON 反序列化），Get(string) 返回字符串
```

### Wlniao.IO.PathTool / FileEx 文件
```csharp
string Map(params string[] relativePath)   // 相对程序根目录拼接为绝对路径：PathTool.Map("logs","a.log")
string JoinPath(params string[] relativePath)   // 跨平台安全拼接路径：PathTool.JoinPath("a", "b", "c.txt") → a/b/c.txt
string ReadAll(string path)   // 读取文件内容，支持 UTF-8 编码，返回空字符串
```

### Wlniao.Net.ApiClient HTTP（默认带代理支持）
```csharp
string Get(string url, string webroxy = null)
string Post(string url, string postData, string contentType = "application/json", string webroxy = null)
string Post(string url, Stream stream, string webroxy = null)   // 流式上传
```

### Wlniao.Json 序列化
```csharp
string Json.Serialize<T>(T obj)                          // 序列化（中文不转义），支持附加 KV 与自定义 options
T    Json.Deserialize<T>(string json)
Dictionary<string, string> Json.DeserializeToDic(string json)   // JSON → 字符串字典
```

### Wlniao.Result<T> 统一返回包装（Code==200 表示成功）
```csharp
// —— 属性 ——
int    Code          // 状态码，默认 0；Success 判定条件为 Code==200
T      Data          // 返回数据，JSON 序列化时 null 忽略
string Message       // 返回消息，JSON 序列化时 null 忽略
bool   Success       // 只读：Code == 200（序列化时 Always 忽略）
bool   HasError      // 只读：Code != 200（完全 JsonIgnore）
// —— 实例链式方法 ——
Result<T> SetData(T data)                 // Code=200 并赋值 Data
Result<T> SetMessage(string message)      // 仅赋值 Message
Result<T> SetMessage(string message, int statusCode)   // 同时赋值 Code 与 Message
Result<T> SetStatusCode(int statusCode)   // 仅赋值 Code
Result<T> SetSuccess()                    // Code=200
// —— 静态快捷方法 ——
Result<T> OutSuccess(T data, int statusCode = 200)
Result<T> OutMessage(string message, int statusCode, T data = default)
```

### Wlniao.Pager<T> 数据分页
```csharp
// —— 属性 ——
bool      success   // 是否成功，默认 true
string    message   // 自动根据 rows 生成描述，可手动覆盖
int       total     // 结果总数，未赋值时取数据源 Count
int       size      // 分页大小，默认 10，<=0 视为 10
int       page      // 当前页码（从 1 开始），<=0 不生效
int       skip      // 只读：(page-1)*size，JsonIgnore
List<T>   rows      // 当前页数据（自动分页或手动赋值）
// —— 静态方法 ——
Pager<T> PutIn(List<T> source, int page = 1, int size = 10)   // 传入完整数据源，自动按 page/size 分页
Pager<T> Format(List<T> data, int total, int page, int size = 0)   // 仅格式化（已分页好的 data + total 元数据）
```

### Wlniao.Runtime.XCoreException 内部异常（带状态码）
```csharp
int StatusCode   // 只读，异常对应的状态码
// 构造函数
XCoreException(string message, int statusCode)
XCoreException(string message, int statusCode, Exception innerException)
```