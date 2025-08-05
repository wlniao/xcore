using System;
using Microsoft.Extensions.DependencyInjection;

namespace Wlniao;

/// <summary>
/// 表示可通过AddBusiness方式动态注入的业务类
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class BusinessServiceAttribute : Attribute
{
    /// <summary>
    /// 注入的业务类的生命周期
    /// </summary>
    public ServiceLifetime LifeTime { get; set; }
    /// <summary>
    /// 表示可通过AddBusiness方式动态注入的业务类
    /// </summary>
    /// <param name="serviceLifetime"></param>
    public BusinessServiceAttribute(ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        LifeTime = serviceLifetime;
    }
}