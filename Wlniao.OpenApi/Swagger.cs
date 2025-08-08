using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Runtime.Serialization;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Wlniao.Swagger
{
    /// <summary>
    /// 
    /// </summary>
    public class NoSwaggerRouteConstraint : IRouteConstraint
    {
        /// <summary>
        /// Usage: builder.Services.AddRouting(o => { o.ConstraintMap.Add("NoSwagger", typeof(Wlniao.Swagger.NoSwaggerRouteConstraint)); });
        /// </summary>
        /// <param name="httpContext"></param>
        /// <param name="route"></param>
        /// <param name="routeKey"></param>
        /// <param name="values"></param>
        /// <param name="routeDirection"></param>
        /// <returns></returns>
        public bool Match(HttpContext httpContext, IRouter route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection)
        {
            var value = values[routeKey].ToString();
            return value != "swagger";
        }
    }

    /// <summary>
    /// API分组信息
    /// </summary>
    public class ApiGroupInfo
    {
        /// <summary>
        /// 当前注册的分组列表
        /// </summary>
        public static List<ApiGroupInfo> GroupInfos = new List<ApiGroupInfo>();
        /// <summary>
        /// 分组名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 接口文档标题
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// 接口文档版本
        /// </summary>
        public string Version { get; set; }
        /// <summary>
        /// 接口文档链接
        /// </summary>
        public string ApiUrl
        {
            get
            {
                return "/" + Name + "/swagger.json";
            }
        }
    }
    /// <summary>
    /// API分组信息特性
    /// </summary>
    public class ApiGroupAttribute : Attribute, IApiDescriptionGroupNameProvider
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">分组名称</param>
        public ApiGroupAttribute(String name)
        {
            GroupName = name;
        }
        /// <summary>
        /// 
        /// </summary>
        public string GroupName { get; set; }
    }
    /// <summary>
    /// 认证授权域名
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class XDomainAttribute : Attribute
    {
        /// <summary>
        /// 请求头中的字段名称
        /// </summary>
        public string HeaderName { get; set; } = "X-Domain";
    }
    /// <summary>
    /// 需要授权认证
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class AuthHeaderAttribute : Attribute
    {
        /// <summary>
        /// 请求头中的字段名称
        /// </summary>
        public string HeaderName { get; set; } = "Authorization";
    }
    /// <summary>
    /// 输出参数自定义属性
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class WlniaoApiResponseAttribute : Attribute
    {
        /// <summary>
        /// 参数名称
        /// </summary>
        public String Name { get; set; }
        /// <summary>
        /// 参数是否必须
        /// </summary>
        public bool Required { get; set; } = false;
        /// <summary>
        /// 参数说明
        /// </summary>
        public string Description { get; set; }
    }
    /// <summary>
    /// Body参数自定义属性
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class WlniaoBodyParameterAttribute : Attribute
    {
        /// <summary>
        /// 参数是否必须
        /// </summary>
        public bool Required { get; set; } = false;
        /// <summary>
        /// 参数说明
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// 参数名称
        /// </summary>
        public OpenApiReference Reference { get; set; }
        /// <summary>
        /// 参数类型
        /// </summary>
        public Type Type { get; set; } = typeof(string);
    }
    /// <summary>
    /// 请求参数自定义属性
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class WlniaoPostParameterAttribute : Attribute
    {
        /// <summary>
        /// 参数名称
        /// </summary>
        public String Name { get; set; }
        /// <summary>
        /// 参数是否必须
        /// </summary>
        public bool Required { get; set; } = false;
        /// <summary>
        /// 参数说明
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// 参数类型
        /// </summary>
        public Type Type { get; set; } = typeof(string);
        /// <summary>
        /// 参数所在位置
        /// </summary>
        public ParameterLocation In { get; set; } = ParameterLocation.Query;
    }
    /// <summary>
    /// 查询参数自定义属性
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class WlniaoQueryParameterAttribute : Attribute
    {
        /// <summary>
        /// 参数名称
        /// </summary>
        public String Name { get; set; }
        /// <summary>
        /// 参数是否必须
        /// </summary>
        public bool Required { get; set; } = false;
        /// <summary>
        /// 参数说明
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// 参数所在位置
        /// </summary>
        public ParameterLocation In { get; set; } = ParameterLocation.Query;
    }


    /// <summary>
    /// 自定义参数设置
    /// </summary>
    public class Filter : IOperationFilter
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="context"></param>
        /// <exception cref="NotImplementedException"></exception>
        [Obsolete(DiagnosticId = "SYSLIB0051")]
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Parameters == null)
            {
                operation.Parameters = new List<OpenApiParameter>();
            }
            foreach (XDomainAttribute item in context.ApiDescription.ActionDescriptor.EndpointMetadata.Where(o => o is XDomainAttribute))
            {
                operation.Parameters.Add(new OpenApiParameter
                {
                    In = ParameterLocation.Header,
                    Name = item.HeaderName,
                    Required = true,
                    Description = "自定义认证域名"
                });
            }
            foreach (AuthHeaderAttribute item in context.ApiDescription.ActionDescriptor.EndpointMetadata.Where(o => o is AuthHeaderAttribute))
            {
                operation.Parameters.Add(new OpenApiParameter
                {
                    In = ParameterLocation.Header,
                    Name = item.HeaderName,
                    Required = true,
                    Description = "API调用令牌"
                });
            }
            var kvs = new Dictionary<string, Type>();
            foreach (WlniaoPostParameterAttribute item in context.ApiDescription.ActionDescriptor.EndpointMetadata.Where(o => o is WlniaoPostParameterAttribute))
            {
                kvs.Add(item.Name, item.Type);
                //operation.Parameters.Add(new OpenApiParameter
                //{
                //    In = ParameterLocation.Query,
                //    Name = item.Name,
                //    Required = item.Required,
                //    Description = item.Description
                //});
            }
            if (kvs.Count > 0)
            {
                var type = DynamicClass(kvs);
                var test = Activator.CreateInstance(type);
                var schema = Microsoft.OpenApi.Extensions.OpenApiTypeMapper.MapTypeToOpenApiPrimitiveType(type);
                schema.Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = type.Name };
                operation.RequestBody = new OpenApiRequestBody
                {
                    Required = true,
                    Reference = schema.Reference,
                    Description = "",
                    Content = {
                        ["text/json"] = new OpenApiMediaType{ Schema = schema },
                        ["application/json"] = new OpenApiMediaType{ Schema = schema }
                    }
                };
            }
            foreach (WlniaoQueryParameterAttribute item in context.ApiDescription.ActionDescriptor.EndpointMetadata.Where(o => o is WlniaoQueryParameterAttribute))
            {
                operation.Parameters.Add(new OpenApiParameter
                {
                    In = item.In,
                    Name = item.Name,
                    Required = item.Required,
                    Description = item.Description
                });
            }
            if (operation.RequestBody == null)
            {
                foreach (WlniaoBodyParameterAttribute item in context.ApiDescription.ActionDescriptor.EndpointMetadata.Where(o => o is WlniaoBodyParameterAttribute))
                {
                    var schema = Microsoft.OpenApi.Extensions.OpenApiTypeMapper.MapTypeToOpenApiPrimitiveType(item.Type);
                    schema.Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = item.Type.Name };
                    operation.RequestBody = new OpenApiRequestBody
                    {
                        Required = item.Required,
                        Reference = item.Reference,
                        Description = item.Description,
                        Content = {
                            ["text/json"] = new OpenApiMediaType{ Schema = schema },
                            ["application/json"] = new OpenApiMediaType{ Schema = schema }
                        }
                    };
                }
            }
            var responses = context.ApiDescription.ActionDescriptor.EndpointMetadata.Where(o => o is WlniaoApiResponseAttribute).ToList();
            if (operation.Responses == null && responses.Count() > 0)
            {
                operation.Responses = new OpenApiResponses();
                foreach (WlniaoApiResponseAttribute item in context.ApiDescription.ActionDescriptor.EndpointMetadata.Where(o => o is WlniaoApiResponseAttribute))
                {
                    operation.Responses.Add(item.Name, new OpenApiResponse
                    {
                    });
                }
            }
        }

        /// <summary>
        /// 获取动态类型对象
        /// </summary>
        /// <returns></returns>
        [Obsolete(DiagnosticId = "SYSLIB0051")]
        public static Type DynamicClass(Dictionary<string, Type> keyValues)
        {
            //程序集名
            AssemblyName assemblyName = new AssemblyName("DynamicAssembly");
            //程序集
            AssemblyBuilder dyAssembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
            //模块
            ModuleBuilder dyModule = dyAssembly.DefineDynamicModule("DynamicModule");
            //类
            TypeBuilder dyClass = dyModule.DefineType("Objects", TypeAttributes.Public | TypeAttributes.Serializable | TypeAttributes.Class | TypeAttributes.AutoClass);
            foreach (var item in keyValues)
            {
                //字段
                var fb = dyClass.DefineField(item.Key, item.Value, FieldAttributes.Public);
                //get方法
                MethodBuilder mbNumberGetAccessor = dyClass.DefineMethod("get_" + item.Key, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, item.Value, Type.EmptyTypes);
                ILGenerator numberGetIL = mbNumberGetAccessor.GetILGenerator();
                numberGetIL.Emit(OpCodes.Ldarg_0);
                numberGetIL.Emit(OpCodes.Ldfld, fb);
                numberGetIL.Emit(OpCodes.Ret);
                //set方法
                MethodBuilder mbNumberSetAccessor = dyClass.DefineMethod("set_" + item.Key, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, null, new Type[] { item.Value });
                ILGenerator numberSetIL = mbNumberSetAccessor.GetILGenerator();
                numberSetIL.Emit(OpCodes.Ldarg_0);
                numberSetIL.Emit(OpCodes.Ldarg_1);
                numberSetIL.Emit(OpCodes.Stfld, fb);
                numberSetIL.Emit(OpCodes.Ret);
                //属性
                PropertyBuilder pbNumber = dyClass.DefineProperty(item.Key, PropertyAttributes.HasDefault, item.Value, null);
                //绑定get，set方法
                pbNumber.SetGetMethod(mbNumberGetAccessor);
                pbNumber.SetSetMethod(mbNumberSetAccessor);
            }
            //创建类型信息
            return dyClass.CreateTypeInfo();
        }
    }

}