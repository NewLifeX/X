using System.ComponentModel;

namespace NewLife.Model;

/// <summary>轻量级对象容器，支持注入</summary>
/// <remarks>
/// 文档 https://newlifex.com/core/object_container
/// </remarks>
public interface IObjectContainer
{
    #region 属性
    /// <summary>服务注册集合</summary>
    IList<IObject> Services { get; }
    #endregion

    #region 注册
    /// <summary>注册类型和名称</summary>
    /// <param name="serviceType">接口类型</param>
    /// <param name="implementationType">实现类型</param>
    /// <param name="instance">实例对象</param>
    /// <returns>当前容器实例，支持链式调用</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    IObjectContainer Register(Type serviceType, Type? implementationType, Object? instance);

    /// <summary>添加服务注册，允许重复添加同一个服务</summary>
    /// <param name="item">服务映射对象</param>
    void Add(IObject item);

    /// <summary>尝试添加服务注册，不允许重复添加同一个服务</summary>
    /// <param name="item">服务映射对象</param>
    /// <returns>是否成功添加，已存在时返回false</returns>
    Boolean TryAdd(IObject item);
    #endregion

    #region 解析
    /// <summary>解析类型的实例</summary>
    /// <param name="serviceType">接口类型</param>
    /// <returns>服务实例，未注册时返回null</returns>
    Object? GetService(Type serviceType);

    /// <summary>在指定容器中解析类型的实例</summary>
    /// <param name="serviceType">接口类型</param>
    /// <param name="serviceProvider">服务提供者容器</param>
    /// <returns>服务实例，未注册时返回null</returns>
    [Obsolete("=>GetService")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    Object? Resolve(Type serviceType, IServiceProvider? serviceProvider = null);
    #endregion
}

/// <summary>生命周期</summary>
public enum ObjectLifetime
{
    /// <summary>单实例。整个应用程序生命周期内只有一个实例</summary>
    Singleton,

    /// <summary>容器内单实例。同一作用域内共享实例</summary>
    Scoped,

    /// <summary>每次一个实例。每次请求都创建新实例</summary>
    Transient
}

/// <summary>对象映射接口</summary>
public interface IObject
{
    /// <summary>服务类型。接口或抽象类类型</summary>
    Type ServiceType { get; }

    /// <summary>实现类型。具体实现类类型</summary>
    Type? ImplementationType { get; }

    /// <summary>生命周期。控制实例的创建和销毁策略</summary>
    ObjectLifetime Lifetime { get; }
}