using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

namespace NewLife.Model;

/// <summary>轻量级对象容器，支持依赖注入</summary>
/// <remarks>
/// 文档 https://newlifex.com/core/object_container
/// 
/// 提供简单的 IoC 容器功能，支持单例、瞬态和作用域生命周期。
/// </remarks>
public class ObjectContainer : IObjectContainer
{
    #region 静态
    /// <summary>当前容器。全局默认容器实例</summary>
    public static IObjectContainer Current { get; set; }

    /// <summary>当前容器提供者。全局默认服务提供者</summary>
    public static IServiceProvider Provider { get; set; }

    static ObjectContainer()
    {
        var ioc = new ObjectContainer();
        Current = ioc;
        Provider = ioc.BuildServiceProvider();
    }

    /// <summary>设置内部服务提供者。用于在 UseXxx 阶段更新为真正的 IServiceProvider，替换临时提供者</summary>
    /// <param name="innerServiceProvider">真正的服务提供者，通常是 app.ApplicationServices</param>
    public static void SetInnerProvider(IServiceProvider innerServiceProvider)
    {
        if (Provider is ServiceProvider sp)
            sp.InnerServiceProvider = innerServiceProvider;
    }

    /// <summary>设置内部服务提供者工厂。用于在 AddXxx 阶段延迟绑定，允许在需要时创建临时提供者</summary>
    /// <param name="innerServiceProviderFactory">服务提供者工厂，延迟获取 IServiceProvider</param>
    public static void SetInnerProvider(Func<IServiceProvider> innerServiceProviderFactory)
    {
        if (Provider is ServiceProvider sp)
            sp.InnerServiceProviderFactory = innerServiceProviderFactory;
    }
    #endregion

    #region 属性
    /// <summary>服务集合。已注册的服务描述符列表</summary>
    public IList<IObject> Services => _list;

    /// <summary>注册项个数</summary>
    public Int32 Count => _list.Count;

    private readonly IList<IObject> _list = [];
    private static Dictionary<TypeCode, Object?>? _defs;
    #endregion

    #region 注册
    /// <summary>添加服务，允许重复添加同一个服务类型</summary>
    /// <param name="item">服务描述符</param>
    public void Add(IObject item)
    {
        lock (_list)
        {
            if (item.ImplementationType == null && item is ServiceDescriptor sd)
                sd.ImplementationType = sd.Instance?.GetType();

            _list.Add(item);
        }
    }

    /// <summary>尝试添加服务，不允许重复添加同一个服务类型</summary>
    /// <param name="item">服务描述符</param>
    /// <returns>是否添加成功</returns>
    public Boolean TryAdd(IObject item)
    {
        // 对象集合仅在应用启动早期用到几十次，后续不再使用，不需要优化性能。lock之间的判断，可能抛出集合修改异常
        lock (_list)
        {
            if (_list.Any(e => e.ServiceType == item.ServiceType)) return false;

            if (item.ImplementationType == null && item is ServiceDescriptor sd)
                sd.ImplementationType = sd.Instance?.GetType();

            _list.Add(item);

            return true;
        }
    }

    /// <summary>注册服务</summary>
    /// <param name="serviceType">服务类型</param>
    /// <param name="implementationType">实现类型</param>
    /// <param name="instance">服务实例</param>
    /// <returns>当前容器</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public virtual IObjectContainer Register(Type serviceType, Type? implementationType, Object? instance)
    {
        if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));

        var item = new ServiceDescriptor(serviceType, implementationType, instance)
        {
            Lifetime = instance == null ? ObjectLifetime.Transient : ObjectLifetime.Singleton,
        };
        Add(item);

        return this;
    }
    #endregion

    #region 解析
    /// <summary>获取服务实例</summary>
    /// <param name="serviceType">服务类型</param>
    /// <returns>服务实例，未找到时返回null</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public virtual Object? GetService(Type serviceType)
    {
        if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));

        // 优先查找最后一个，避免重复注册
        var item = _list.LastOrDefault(e => e.ServiceType == serviceType);
        if (item == null) return null;

        return Resolve(item, null);
    }

    /// <summary>解析服务实例</summary>
    /// <param name="serviceType">服务类型</param>
    /// <param name="serviceProvider">服务提供者</param>
    /// <returns>服务实例，未找到时返回null</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public virtual Object? Resolve(Type serviceType, IServiceProvider? serviceProvider = null)
    {
        if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));

        // 优先查找最后一个，避免重复注册
        var item = _list.LastOrDefault(e => e.ServiceType == serviceType);
        if (item == null) return null;

        return Resolve(item, serviceProvider);
    }

    /// <summary>解析服务实例</summary>
    /// <param name="item">服务描述符</param>
    /// <param name="serviceProvider">服务提供者</param>
    /// <returns>服务实例</returns>
    public virtual Object Resolve(IObject item, IServiceProvider? serviceProvider)
    {
        var map = item as ServiceDescriptor;
        if (item.Lifetime == ObjectLifetime.Singleton && map?.Instance != null) return map.Instance;

        var type = item.ImplementationType ?? item.ServiceType;
        serviceProvider ??= new ServiceProvider(this, null);
        switch (item.Lifetime)
        {
            case ObjectLifetime.Singleton:
                if (map != null)
                {
                    map.Instance ??= CreateInstance(type, serviceProvider, map.Factory, true)!;

                    return map.Instance;
                }
                return CreateInstance(type, serviceProvider, null, true)!;

            case ObjectLifetime.Scoped:
            case ObjectLifetime.Transient:
            default:
                return CreateInstance(type, serviceProvider, map?.Factory, true)!;
        }
    }

    /// <summary>创建类型实例</summary>
    /// <param name="type">目标类型</param>
    /// <param name="provider">服务提供者</param>
    /// <param name="factory">工厂方法</param>
    /// <param name="throwOnError">失败时是否抛出异常</param>
    /// <returns>类型实例</returns>
    internal static Object? CreateInstance(Type type, IServiceProvider provider, Func<IServiceProvider, Object>? factory, Boolean throwOnError)
    {
        if (factory != null) return factory(provider);

        // 初始化默认值字典
        if (_defs == null)
        {
            var dic = new Dictionary<TypeCode, Object?>
            {
                { TypeCode.Empty, null },
                { TypeCode.DBNull, null},
                { TypeCode.Boolean, false },
                { TypeCode.Char, (Char)0 },
                { TypeCode.SByte, (SByte)0 },
                { TypeCode.Byte, (Byte)0 },
                { TypeCode.Int16, (Int16)0 },
                { TypeCode.UInt16, (UInt16)0 },
                { TypeCode.Int32, (Int32)0 },
                { TypeCode.UInt32, (UInt32)0 },
                { TypeCode.Int64, (Int64)0 },
                { TypeCode.UInt64, (UInt64)0 },
                { TypeCode.Single, (Single)0 },
                { TypeCode.Double, (Double)0 },
                { TypeCode.Decimal, (Decimal)0 },
                { TypeCode.DateTime, DateTime.MinValue },
                { TypeCode.String, null }
            };

            _defs = dic;
        }

        ParameterInfo? errorParameter = null;
        if (!type.IsAbstract)
        {
            // 选择构造函数，优先选择参数最多的可匹配构造函数
            var constructors = type.GetConstructors();
            foreach (var constructorInfo in constructors.OrderByDescending(e => e.GetParameters().Length))
            {
                if (constructorInfo.IsStatic) continue;

                ParameterInfo? errorParameter2 = null;
                var ps = constructorInfo.GetParameters();
                var pv = new Object?[ps.Length];
                for (var i = 0; i != ps.Length; i++)
                {
                    if (pv[i] != null) continue;

                    var ptype = ps[i].ParameterType;
                    if (_defs.TryGetValue(Type.GetTypeCode(ptype), out var obj))
                        pv[i] = obj;
                    else
                    {
                        var service = provider.GetService(ps[i].ParameterType);
                        if (service == null)
                        {
                            errorParameter2 = ps[i];
                            break;
                        }
                        else
                        {
                            pv[i] = service;
                        }
                    }
                }

                if (errorParameter2 == null) return constructorInfo.Invoke(pv);
                errorParameter = errorParameter2;
            }
        }

        if (throwOnError)
            throw new InvalidOperationException($"No suitable constructor was found for '{type}'. Please confirm that all required parameters for the type constructor are registered. Unable to parse parameter '{errorParameter}'");

        return null;
    }
    #endregion

    #region 辅助
    /// <summary>已重载。显示容器信息</summary>
    /// <returns>容器描述</returns>
    public override String ToString() => $"{GetType().Name}[Count={Count}]";
    #endregion
}

/// <summary>服务描述符</summary>
/// <remarks>
/// 描述服务的类型、实现、生命周期等信息。
/// </remarks>
[DebuggerDisplay("Lifetime = {Lifetime}, ServiceType = {ServiceType}, ImplementationType = {ImplementationType}")]
public class ServiceDescriptor : IObject
{
    #region 属性
    /// <summary>服务类型。通常是接口或抽象类</summary>
    public Type ServiceType { get; set; }

    /// <summary>实现类型。具体的实现类</summary>
    public Type? ImplementationType { get; set; }

    /// <summary>生命周期。单例、瞬态或作用域</summary>
    public ObjectLifetime Lifetime { get; set; }

    /// <summary>服务实例。仅单例模式有效</summary>
    public Object? Instance { get; set; }

    /// <summary>对象工厂。用于创建服务实例的委托</summary>
    public Func<IServiceProvider, Object>? Factory { get; set; }
    #endregion

    #region 构造
    /// <summary>实例化服务描述符</summary>
    /// <param name="serviceType">服务类型</param>
    public ServiceDescriptor(Type serviceType) => ServiceType = serviceType;

    /// <summary>实例化服务描述符</summary>
    /// <param name="serviceType">服务类型</param>
    /// <param name="implementationType">实现类型</param>
    public ServiceDescriptor(Type serviceType, Type? implementationType)
    {
        ServiceType = serviceType;
        ImplementationType = implementationType;
    }

    /// <summary>实例化服务描述符</summary>
    /// <param name="serviceType">服务类型</param>
    /// <param name="implementationType">实现类型</param>
    /// <param name="instance">服务实例</param>
    public ServiceDescriptor(Type serviceType, Type? implementationType, Object? instance)
    {
        ServiceType = serviceType;
        ImplementationType = implementationType;
        Instance = instance;

        Lifetime = instance == null ? ObjectLifetime.Transient : ObjectLifetime.Singleton;
    }
    #endregion

    #region 辅助
    /// <summary>显示友好名称</summary>
    /// <returns>服务描述</returns>
    public override String ToString() => $"[{ServiceType?.Name},{ImplementationType?.Name}]";
    #endregion
}

/// <summary>服务提供者</summary>
/// <remarks>
/// 包装对象容器，实现 <see cref="IServiceProvider"/> 接口。
/// </remarks>
internal class ServiceProvider(IObjectContainer container, IServiceProvider? innerServiceProvider) : IServiceProvider
{
    #region 属性
    /// <summary>容器</summary>
    public IObjectContainer Container => _container;

    /// <summary>内部服务提供者。用于链式查找</summary>
    public IServiceProvider? InnerServiceProvider { get; set; } = innerServiceProvider;

    /// <summary>内部服务提供者工厂。延迟获取真实的 IServiceProvider</summary>
    public Func<IServiceProvider>? InnerServiceProviderFactory { get; set; }

    private readonly IObjectContainer _container = container;
    private readonly Object _lock = new();
    #endregion

    #region 方法
    /// <summary>获取服务实例</summary>
    /// <param name="serviceType">服务类型</param>
    /// <returns>服务实例</returns>
    public Object? GetService(Type serviceType)
    {
        if (serviceType == typeof(IObjectContainer)) return _container;
        if (serviceType == typeof(ObjectContainer)) return _container;
        if (serviceType == typeof(IServiceProvider)) return this;

        var ioc = _container as ObjectContainer;
        if (ioc != null && !ioc.Services.Any(e => e.ServiceType == typeof(IServiceScopeFactory)))
        {
            ioc.TryAdd(new ServiceDescriptor(typeof(IServiceScopeFactory))
            {
                Instance = new MyServiceScopeFactory { ServiceProvider = this },
                Lifetime = ObjectLifetime.Singleton,
            });
        }

        var service = ioc?.Resolve(serviceType, this);
        if (service != null) return service;

        // 使用工厂延迟解析，调用后直接赋值给 InnerServiceProvider
        if (InnerServiceProviderFactory != null)
        {
            lock (_lock)
            {
                if (InnerServiceProvider == null && InnerServiceProviderFactory != null)
                    InnerServiceProvider = InnerServiceProviderFactory();
            }
        }

        return InnerServiceProvider?.GetService(serviceType);
    }

    #endregion
}