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

namespace Wlniao.Swagger
{
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
    /// Swagger扩展处理程序
    /// </summary>
    public static class SwaggerExtendCollectionExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <param name="info"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static IServiceCollection AddSwaggerExtend(
            this IServiceCollection services, OpenApiInfo info = null, String name = "v1")
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(o =>
            {
                if (name == null)
                {
                    info = new OpenApiInfo
                    {
                        Title = XCore.WebNode,
                        Extensions = new Dictionary<string, IOpenApiExtension> { }
                    };
                }
                o.OperationFilter<Filter>();
                o.SwaggerDoc(name, info);
                o.AddServer(new OpenApiServer { Url = XCore.WebHost, Description = XCore.WebNode });
                if (System.IO.File.Exists(Path.Combine(AppContext.BaseDirectory, $"Wlniao.XCore.xml")))
                {
                    o.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"Wlniao.XCore.xml"), true);
                }
                if (System.IO.File.Exists(Path.Combine(AppContext.BaseDirectory, $"{System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name}.xml")))
                {
                    o.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name}.xml"), true);
                }
            });
            return services;
        }
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
                    In = ParameterLocation.Query,
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
        }

        /// <summary>
        /// 获取动态类型对象
        /// </summary>
        /// <returns></returns>
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