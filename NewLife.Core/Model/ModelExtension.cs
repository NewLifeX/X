namespace NewLife.Model;

/// <summary>模型扩展</summary>
public static class ModelExtension
{
    /// <summary>获取指定类型的服务对象</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="provider"></param>
    /// <returns></returns>
    public static T GetService<T>(this IServiceProvider provider)
    {
        if (provider == null) return default;

        //// 服务类是否当前类的基类
        //if (provider.GetType().As<T>()) return (T)provider;

        return (T)provider.GetService(typeof(T));
    }

    /// <summary>获取必要的服务，不存在时抛出异常</summary>
    /// <param name="provider"></param>
    /// <param name="serviceType"></param>
    /// <returns></returns>
    public static Object GetRequiredService(this IServiceProvider provider, Type serviceType)
    {
        if (provider == null) throw new ArgumentNullException(nameof(provider));
        return serviceType == null
            ? throw new ArgumentNullException(nameof(serviceType))
            : provider.GetService(serviceType) ?? throw new InvalidOperationException($"未注册类型{serviceType.FullName}");
    }

    /// <summary>获取必要的服务，不存在时抛出异常</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="provider"></param>
    /// <returns></returns>
    public static T GetRequiredService<T>(this IServiceProvider provider) => provider == null ? throw new ArgumentNullException(nameof(provider)) : (T)provider.GetRequiredService(typeof(T));

    /// <summary>获取一批服务</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="provider"></param>
    /// <returns></returns>
    public static IEnumerable<T> GetServices<T>(this IServiceProvider provider) => provider.GetServices(typeof(T)).Cast<T>();

    /// <summary>获取一批服务</summary>
    /// <param name="provider"></param>
    /// <param name="serviceType"></param>
    /// <returns></returns>
    public static IEnumerable<Object> GetServices(this IServiceProvider provider, Type serviceType)
    {
        //var sp = provider as ServiceProvider;
        //if (sp == null && provider is MyServiceScope scope) sp = scope.MyServiceProvider as ServiceProvider;
        //var sp = provider.GetService<ServiceProvider>();
        //if (sp != null && sp.Container is ObjectContainer ioc)
        var ioc = GetService<ObjectContainer>(provider);
        if (ioc != null)
        {
            //var list = new List<Object>();
            //foreach (var item in ioc.Services)
            //{
            //    if (item.ServiceType == serviceType) list.Add(ioc.Resolve(item, provider));
            //}
            for (var i = ioc.Services.Count - 1; i >= 0; i--)
            {
                var item = ioc.Services[i];
                if (item.ServiceType == serviceType) yield return ioc.Resolve(item, provider);
            }
            //return list;
        }
        else
        {
            var serviceType2 = typeof(IEnumerable<>)!.MakeGenericType(serviceType);
            var enums = (IEnumerable<Object>)provider.GetRequiredService(serviceType2);
            foreach (var item in enums)
            {
                yield return item;
            }
        }
    }

    /// <summary>创建范围作用域，该作用域内提供者解析一份数据</summary>
    /// <param name="provider"></param>
    /// <returns></returns>
    public static IServiceScope CreateScope(this IServiceProvider provider) => provider.GetService<IServiceScopeFactory>()?.CreateScope();
}