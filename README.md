# Wlniao.XCore
![GitHub License](https://img.shields.io/github/license/wlniao/xcore) 
![Package Version](https://img.shields.io/nuget/v/Wlniao.XCore) 
![Pull Count](https://img.shields.io/nuget/dt/Wlniao.XCore) 

Wlniao.XCore 是一个基于 .NET Core 的轻量级开发框架，主要包括配置管理、日志记录、加密解密、缓存管理、HTTP客户端、API结果封装等功能模块提供了从基础工具类到高级功能模块的完整解决方案，帮助开发者快速构建稳定、高效的 .NET 应用程序。

## 核心功能模块

### 1. 配置管理 (Config)
- 支持环境变量和配置文件双重配置
- 提供加密配置项支持
- 自动读取 xcore.config 或 xcore.dev.config 配置文件（其中dev.config 文件为开发环境配置文件）

### 2. 日志系统 (Log)
- 多种日志输出方式：console、file、loki
- 支持日志级别控制（debug, info, warn, error, fatal）
- 可自定义日志提供程序

### 3. 加密解密 (Encryptor)
- 支持多种摘要算法：MD5, SHA1, SHA256, SHA512, SM3
- 支持对称加密：AES, SM4 (ECB/CBC模式)
- 支持非对称加密：RSA, SM2
- 支持 HMAC 算法：HmacSHA1, HmacSHA256, HmacSM3

### 4. 缓存管理 (Caching)
- 多种缓存实现：memory、redis、file
- 自动选择最优缓存策略
- 支持对象序列化缓存

### 5. 网络客户端 (Net)
- 简化的 HTTP 请求方法
- 支持 HTTPS 和自定义证书验证
- 提供 API 客户端工具

### 6. API 结果封装 (ApiResult)
- 统一的 API 响应格式
- 支持业务状态码和消息
- 可扩展的数据结构

### 7. 扩展工具类 (Extend)
- 字符串、集合、枚举等常用类型扩展方法
- 数据转换和验证工具
- 分页处理工具

### 8. 时间处理工具 (DateTools)
- 提供Unix时间戳转换功能
- 支持时区处理和时间格式化
- 提供GMT/UTC时间转换
- 支持时间字符串解析和格式化输出

### 9. XServer 微服务 (XServer)
- 微服务间通信支持
- API 签名验证机制
- 服务注册与发现

## ASP.NET Core 集成

### Wlniao.AspNetCore
- ASP.NET Core MVC 扩展
- Kestrel 服务器配置
- HTTPS/TLS 支持
- 自定义中间件支持

### Wlniao.OpenApi
- Swagger API 文档集成
- API 分组管理
- 自定义参数注解
- Knife4UI 前端界面

### Wlniao.XCenter
- 模块化认证服务管理
- 认证授权服务
- 微服务协调

## 安装使用

### 通过 NuGet 安装

```bash
# 安装核心框架
dotnet add package Wlniao.XCore

# 安装 ASP.NET Core 扩展
dotnet add package Wlniao.AspNetCore

# 安装 OpenAPI 支持
dotnet add package Wlniao.OpenApi

# 安装 XCenter 模块化认证服务
dotnet add package Wlniao.XCenter
```

### 基本配置

在项目中创建 `xcore.config` 配置文件：

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

// HTTP API请求
var response = Net.ApiClient.Get("https://api.example.com/data");
```

## 高级功能

### 1. 微服务通信
框架内置了基于 SM4+SM3 的安全通信机制，支持微服务间的加密通信。

### 2. API 签名验证
提供完整的 API 签名验证机制，确保接口调用的安全性。

### 3. 多环境支持
支持开发、测试、生产等多环境配置，通过环境变量或配置文件切换。

### 4. 扩展性设计
框架采用模块化设计，支持自定义扩展和替换核心组件。

## 环境要求

- .NET 6.0 / .NET 7.0 / .NET 8.0
- Windows/Linux/macOS
- Redis (可选，用于分布式缓存)

## 许可证

本项目采用 Apache License 2.0 许可证，详情请见 [LICENSE](license.txt) 文件。

## 贡献

欢迎提交 Issue 和 Pull Request 来改进这个项目。

## 项目链接

- [NuGet 包](https://www.nuget.org/packages/Wlniao.XCore/)
- [GitHub 仓库](https://github.com/wlniao/xcore)