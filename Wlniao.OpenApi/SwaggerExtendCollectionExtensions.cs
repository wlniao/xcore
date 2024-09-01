using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wlniao.Swagger
{
    /// <summary>
    /// Swagger扩展处理程序
    /// </summary>
    public static class SwaggerExtendCollectionExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <param name="groups"></param>
        /// <returns></returns>
        public static IServiceCollection AddSwaggerExtend(this IServiceCollection services, List<ApiGroupInfo> groups = null)
        {
            if (groups != null)
            {
                ApiGroupInfo.GroupInfos = groups;
            }
            if (!ApiGroupInfo.GroupInfos.Where(o => o.Name == "default").Any())
            {
                ApiGroupInfo.GroupInfos.Add(new ApiGroupInfo { Name = "default", Title = ApiGroupInfo.GroupInfos.Count == 0 ? "接口文档" : "未分组接口" });
            }
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(o =>
            {
                o.OperationFilter<Filter>();
                foreach (var group in ApiGroupInfo.GroupInfos)
                {
                    o.SwaggerDoc(group.Name, new OpenApiInfo
                    {
                        Title = string.IsNullOrEmpty(group.Title) ? group.Name : group.Title,
                        Version = string.IsNullOrEmpty(group.Version) ? RuntimeInfo.ProgramVersion : group.Version,
                        Extensions = new Dictionary<string, IOpenApiExtension> { }
                    });
                }
                o.DocInclusionPredicate((groupName, apiDescription) =>
                {
                    if (groupName == "default")
                    {
                        //没加分组属性的接口都为默认分组
                        return string.IsNullOrEmpty(apiDescription.GroupName) || !ApiGroupInfo.GroupInfos.Where(o => o.Name == apiDescription.GroupName).Any();
                    }
                    else
                    {
                        return apiDescription.GroupName == groupName;
                    }
                });
                o.AddServer(new OpenApiServer { Url = RuntimeInfo.WebHost });
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
}
