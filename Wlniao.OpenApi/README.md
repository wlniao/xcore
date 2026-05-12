# xcore
Wlniao.OpenApi for .NET CORE

### License
![GitHub License](https://img.shields.io/github/license/wlniao/xcore)  
![Package Version](https://img.shields.io/nuget/v/Wlniao.OpenApi) 
![Pull Count](https://img.shields.io/nuget/dt/Wlniao.OpenApi) 

### Add Package
```
dotnet add package Wlniao.OpenApi
```

### Usage
注册Swagger服务
```
builder.Services.AddSwaggerExtend(new System.Collections.Generic.List<ApiGroupInfo> {
    new ApiGroupInfo { Name = "Service", Title = "对外开放服务接口" },
    new ApiGroupInfo { Name = "Control", Title = "内部管理服务接口" }
});
```
配置前端终结点
```
app.UseKnife4UI(o =>
{
    o.RoutePrefix = "docs";
    ApiGroupInfo.GroupInfos.ForEach(group => { o.SwaggerEndpoint("../swagger" + group.ApiUrl, group.Title); });
});
```