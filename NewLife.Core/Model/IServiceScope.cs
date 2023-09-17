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

class MyServiceScope : IServiceScope, IServiceProvider
{
    public IServiceProvider? MyServiceProvider { get; set; }

    public IServiceProvider ServiceProvider => this;

    private readonly ConcurrentDictionary<Type, Object?> _cache = new();

    public void Dispose()
    {
        // 销毁所有缓存
        foreach (var item in _cache)
        {
            if (item.Value is IDisposable dsp) dsp.Dispose();
        }
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