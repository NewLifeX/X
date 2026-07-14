using System.Collections.Concurrent;

namespace NewLife.Model;

/// <summary>范围服务。该范围生命周期内，每个服务类型只有一个实例</summary>
/// <remarks>
/// 满足Singleton和Scoped的要求，暂时无法满足Transient的要求（仍然只有一份）。
/// </remarks>
public interface IServiceScope : IDisposable
{
    /// <summary>服务提供者</summary>
    IServiceProvider ServiceProvider { get; }
}

/// <summary>可注册服务的范围作用域</summary>
/// <remarks>
/// 扩展 IServiceScope，允许在作用域内动态注册服务实例。
/// 适用于请求级作用域中注入上下文对象（如 IHttpContext）等场景。
/// </remarks>
public interface IServiceRegistry
{
    /// <summary>尝试添加服务实例到当前作用域。已存在时返回 false 不覆盖</summary>
    /// <param name="serviceType">服务类型</param>
    /// <param name="instance">服务实例</param>
    /// <returns>是否添加成功</returns>
    Boolean TryAdd(Type serviceType, Object? instance);
}

class MyServiceScope : IServiceScope, IServiceProvider, IServiceRegistry
{
    public IServiceProvider? MyServiceProvider { get; set; }

    public IServiceProvider ServiceProvider => this;

    private readonly ConcurrentDictionary<Type, Object?> _cache = new();

    /// <summary>尝试添加服务实例到当前作用域。已存在时返回 false 不覆盖</summary>
    /// <param name="serviceType">服务类型</param>
    /// <param name="instance">服务实例</param>
    /// <returns>是否添加成功</returns>
    public Boolean TryAdd(Type serviceType, Object? instance) => _cache.TryAdd(serviceType, instance);

    public void Dispose()
    {
        // 销毁所有缓存
        //foreach (var item in _cache)
        //{
        //    if (item.Value is IDisposable dsp) dsp.Dispose();
        //}
        _cache.Clear();
    }

    public Object? GetService(Type serviceType)
    {
        while (true)
        {
            // 查缓存，如果没有再获取一个并缓存起来
            if (_cache.TryGetValue(serviceType, out var service)) return service;

            service = MyServiceProvider?.GetService(serviceType);

            if (_cache.TryAdd(serviceType, service)) return service;
        }
    }
}