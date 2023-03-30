/*==============================================================================
    文件名称：ServiceCollectionExpand.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：IServiceCollection扩展类
================================================================================

示例：
    services.AddBusiness("Eming.Biz.dll");

================================================================================
 
    Copyright 2014 XieChaoyi

    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at

               http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.

===============================================================================*/

using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Wlniao
{
    /// <summary>
    /// IServiceCollection扩展类
    /// </summary>
    public static class ServiceCollectionExpand
    {
        /// <summary>
        /// 注册有BusinessService属性的业务组件
        /// </summary>
        /// <param name="service"></param>
        /// <param name="assemblyFile">要注册的DLL名称</param>
        public static void AddBusiness(this IServiceCollection service, String assemblyFile = null)
        {
            Type[] types = null;
            if (string.IsNullOrEmpty(assemblyFile))
            {
                types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()).ToArray();
            }
            else
            {
                var asms = System.IO.Directory.GetFiles(Wlniao.XCore.StartupRoot, assemblyFile, System.IO.SearchOption.AllDirectories);
                if (asms.Length > 0)
                {
                    types = Assembly.LoadFrom(asms[0]).GetTypes();
                    log.Error("Find business component from: " + asms[0]);
                }
                else
                {
                    log.Error("Business component registration failed: the '" + assemblyFile + "' file was not found");
                }
            }
            if (types != null && types.Length > 0)
            {
                types = types.Where(t => t.GetCustomAttributes(typeof(BusinessServiceAttribute), false).Any()).ToArray();
                if (types.Length > 0)
                {
                    foreach (var impl in types)
                    {
                        var lifetime = impl.GetCustomAttribute<BusinessServiceAttribute>().LifeTime; //获取该类注入的生命周期

                        // 获取该类所继承的所有接口并循环注入
                        impl.GetInterfaces().ToList().ForEach(i =>
                        {
                            switch (lifetime)
                            {
                                case ServiceLifetime.Transient:
                                    service.AddTransient(i, impl);  //每次服务提供请求，ServiceProvider总会创建一个新的对象
                                    break;
                                case ServiceLifetime.Singleton:
                                    service.AddSingleton(i, impl);  //以“单例”的方式管理服务实例的生命周期
                                    break;
                                case ServiceLifetime.Scoped:
                                    service.AddScoped(i, impl);
                                    break;
                            }
                        });
                    }
                }
                else
                {
                    log.Error("Business component registration failed: Please check BusinessService attribute is registered");
                }
            }
        }
    }
}