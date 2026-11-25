# xcore
Wlniao.XCenter for .NET CORE

## License
![GitHub License](https://img.shields.io/github/license/wlniao/xcore)  
![Package Version](https://img.shields.io/nuget/v/Wlniao.XCenter) 
![Pull Count](https://img.shields.io/nuget/dt/Wlniao.XCenter) 

## Add Package
```
dotnet add package Wlniao.XCenter
```

## API 变动（10.0.8）

`Wlniao.XServer.TokenApi.Request<T>` 签名已收紧，以适配 NativeAOT / 裁剪发布：

```csharp
// 10.0.8 起
static ApiResult<T> Request<T>(string apinode, string url, string token,
    string dataJson, JsonTypeInfo<T> dataInfo, string traceid = "")
```

- `data` 不再接受匿名类型或任意 `object`，调用方需先用 `Json.Serialize(data, jsonTypeInfo)` 生成请求体；
- `dataInfo` 传响应载荷 `T` 的源生成元数据，与 `Wlniao.XServer.XApi.Request` 用法一致；
- 未登记类型在 AOT 下会抛 `NotSupportedException`，不再静默返回空数据。

迁移示例：

```csharp
var requestJson = Json.Serialize(request, MyJsonContext.Default.RequestDto);
var result = TokenApi.Request<ResultDto>("node", url, token, requestJson, MyJsonContext.Default.ResultDto);
```

另外，引用 `Wlniao.XCenter.Context` 会启用 `Json.StrictTrimDiagnostics = true`：AOT / 裁剪下未登记类型快速失败，CoreCLR 下行为不变。详见根目录 `CHANGELOG.md` 10.0.8。