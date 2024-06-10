using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

namespace NewLife.Model;

/// <summary>轻量级对象容器，支持注入</summary>
/// <remarks>
/// 文档 https://newlifex.com/core/object_container
/// </remarks>
public class ObjectContainer : IObjectContainer
{
    #region 静态
    /// <summary>当前容器</summary>
    public static IObjectContainer Current { get; set; }

    /// <summary>当前容器提供者</summary>
    public static IServiceProvider Provider { get; set; }

    static ObjectContainer()
    {
        var ioc = new ObjectContainer();
        Current = ioc;
        Provider = ioc.BuildServiceProvider() ?? new ServiceProvider(ioc);
    }
    #endregion

    #region 属性
    private readonly IList<IObject> _list = new List<IObject>();
    /// <summary>服务集合</summary>
    public IList<IObject> Services => _list;

    /// <summary>注册项个数</summary>
    public Int32 Count => _list.Count;
    #endregion

    #region 方法
    /// <summary>添加，允许重复添加同一个服务</summary>
    /// <param name="item"></param>
    public void Add(IObject item)
    {
        lock (_list)
        {
            //for (var i = 0; i < _list.Count; i++)
            //{
            //    // 覆盖重复项
            //    if (_list[i].ServiceType == item.ServiceType)
            //    {
            //        _list[i] = item;
            //        return;
            //    }
            //}

            if (item.ImplementationType == null && item is ServiceDescriptor sd)
                sd.ImplementationType = sd.Instance?.GetType();

            _list.Add(item);
        }
    }

    /// <summary>尝试添加，不允许重复添加同一个服务</summary>
    /// <param name="item"></param>
    public Boolean TryAdd(IObject item)
    {
        if (_list.Any(e => e.ServiceType == item.ServiceType)) return false;
        lock (_list)
        {
            if (_list.Any(e => e.ServiceType == item.ServiceType)) return false;

            if (item.ImplementationType == null && item is ServiceDescriptor sd)
                sd.ImplementationType = sd.Instance?.GetType();

            _list.Add(item);

            return true;
        }
    }
    #endregion

    #region 注册
    /// <summary>注册</summary>
    /// <param name="serviceType">接口类型</param>
    /// <param name="implementationType">实现类型</param>
    /// <param name="instance">实例</param>
    /// <returns></returns>
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
    /// <summary>在指定容器中解析类型的实例</summary>
    /// <param name="serviceType">接口类型</param>
    /// <param name="serviceProvider">容器</param>
    /// <returns></returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public virtual Object? Resolve(Type serviceType, IServiceProvider? serviceProvider = null)
    {
        if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));

        // 优先查找最后一个，避免重复注册
        //var item = _list.FirstOrDefault(e => e.ServiceType == serviceType);
        var item = _list.LastOrDefault(e => e.ServiceType == serviceType);
        if (item == null) return null;

        return Resolve(item, serviceProvider);
    }

    /// <summary>在指定容器中解析类型的实例</summary>
    /// <param name="item"></param>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    public virtual Object Resolve(IObject item, IServiceProvider? serviceProvider)
    {
        var map = item as ServiceDescriptor;
        if (item.Lifetime == ObjectLifetime.Singleton && map?.Instance != null) return map.Instance;

        var type = item.ImplementationType ?? item.ServiceType;
        serviceProvider ??= new ServiceProvider(this);
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

    private static IDictionary<TypeCode, Object?>? _defs;
    internal static Object? CreateInstance(Type type, IServiceProvider provider, Func<IServiceProvider, Object>? factory, Boolean throwOnError)
    {
        if (factory != null) return factory(provider);

        // 初始化
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
    /// <summary>已重载。</summary>
    /// <returns></returns>
    public override String ToString() => $"{GetType().Name}[Count={Count}]";
    #endregion
}

/// <summary>对象映射</summary>
[DebuggerDisplay("Lifetime = {Lifetime}, ServiceType = {ServiceType}, ImplementationType = {ImplementationType}")]
public class ServiceDescriptor : IObject
{
    #region 属性
    /// <summary>服务类型</summary>
    public Type ServiceType { get; set; }

    /// <summary>实现类型</summary>
    public Type? ImplementationType { get; set; }

    /// <summary>生命周期</summary>
    public ObjectLifetime Lifetime { get; set; }

    /// <summary>实例</summary>
    public Object? Instance { get; set; }

    /// <summary>对象工厂</summary>
    public Func<IServiceProvider, Object>? Factory { get; set; }
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    /// <param name="serviceType"></param>
    public ServiceDescriptor(Type serviceType) => ServiceType = serviceType;

    /// <summary>实例化</summary>
    /// <param name="serviceType"></param>
    /// <param name="implementationType"></param>
    public ServiceDescriptor(Type serviceType, Type? implementationType)
    {
        ServiceType = serviceType;
        ImplementationType = implementationType;
    }

    /// <summary>实例化</summary>
    /// <param name="serviceType"></param>
    /// <param name="implementationType"></param>
    /// <param name="instance"></param>
    public ServiceDescriptor(Type serviceType, Type? implementationType, Object? instance)
    {
        ServiceType = serviceType;
        ImplementationType = implementationType;
        Instance = instance;

        Lifetime = instance == null ? ObjectLifetime.Transient : ObjectLifetime.Singleton;
    }
    #endregion

    #region 方法
    /// <summary>显示友好名称</summary>
    /// <returns></returns>
    public override String ToString() => $"[{ServiceType?.Name},{ImplementationType?.Name}]";
    #endregion
}

internal class ServiceProvider : IServiceProvider
{
    private readonly IObjectContainer _container;
    /// <summary>容器</summary>
    public IObjectContainer Container => _container;

    public ServiceProvider(IObjectContainer container) => _container = container;

    public Object? GetService(Type serviceType)
    {
        if (serviceType == typeof(IObjectContainer)) return _container;
        if (serviceType == typeof(ObjectContainer)) return _container;
        if (serviceType == typeof(IServiceProvider)) return this;

        if (_container is ObjectContainer ioc && !ioc.Services.Any(e => e.ServiceType == typeof(IServiceScopeFactory)))
        {
            //oc.AddSingleton<IServiceScopeFactory>(new MyServiceScopeFactory { ServiceProvider = this });
            ioc.TryAdd(new ServiceDescriptor(typeof(IServiceScopeFactory))
            {
                Instance = new MyServiceScopeFactory { ServiceProvider = this },
                Lifetime = ObjectLifetime.Singleton,
            });
        }

        return _container.Resolve(serviceType, this);
    }
}