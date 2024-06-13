# xcore
Wlniao.AspNetCore for .NET CORE

## License
![GitHub License](https://img.shields.io/github/license/wlniao/xcore)  
![Package Version](https://img.shields.io/nuget/v/Wlniao.AspNetCore) 
![Pull Count](https://img.shields.io/nuget/dt/Wlniao.AspNetCore) 

## Add Package
```
dotnet add package Wlniao.AspNetCore
```

## ENV 配置选项
- `WLN_TLS_CRT` TLS服务证书（默认为程序根目录：server.crt）
- `WLN_TLS_KEY` TLS服务私钥（默认为程序根目录：server.key）
- `WLN_LISTEN_PORT` 服务监听端口（默认：5000），当`WLN_TLS_CRT`与`WLN_TLS_KEY`均存在时，将在本端口开启TLS监听

