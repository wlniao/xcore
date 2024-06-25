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
ע��Swagger����
```
builder.Services.AddSwaggerExtend(new System.Collections.Generic.List<ApiGroupInfo> {
    new ApiGroupInfo { Name = "Service", Title = "���⿪�ŷ���ӿ�" },
    new ApiGroupInfo { Name = "Control", Title = "�ڲ��������ӿ�" }
});
```
����ǰ���ս��
```
app.UseKnife4UI(o =>
{
    o.RoutePrefix = "docs";
    ApiGroupInfo.GroupInfos.ForEach(group => { o.SwaggerEndpoint("../swagger" + group.ApiUrl, group.Title); });
});
```