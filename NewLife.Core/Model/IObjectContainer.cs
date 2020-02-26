using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace NewLife.Model
{
    /// <summary>对象容器，仅依赖查找，不支持注入</summary>
    public interface IObjectContainer : IList<IObject>
    {
        #region 注册
        /// <summary>注册类型和名称</summary>
        /// <param name="serviceType">接口类型</param>
        /// <param name="implementationType">实现类型</param>
        /// <param name="instance">实例</param>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        IObjectContainer Register(Type serviceType, Type implementationType, Object instance);

        /// <summary>注册类型和名称</summary>
        /// <param name="from">接口类型</param>
        /// <param name="to">实现类型</param>
        /// <param name="instance">实例</param>
        /// <param name="id">标识</param>
        /// <param name="priority">优先级</param>
        /// <returns></returns>
        [Obsolete]
        [EditorBrowsable(EditorBrowsableState.Never)]
        IObjectContainer Register(Type from, Type to, Object instance, Object id, Int32 priority = 0);
        #endregion

        #region 解析
        /// <summary>解析类型的实例</summary>
        /// <param name="serviceType">接口类型</param>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        Object Resolve(Type serviceType);

        /// <summary>解析类型指定名称的实例</summary>
        /// <param name="from">接口类型</param>
        /// <param name="id">标识</param>
        /// <returns></returns>
        [Obsolete]
        [EditorBrowsable(EditorBrowsableState.Never)]
        Object Resolve(Type from, Object id);

        /// <summary>解析类型指定名称的实例</summary>
        /// <param name="from">接口类型</param>
        /// <param name="id">标识</param>
        /// <returns></returns>
        [Obsolete]
        [EditorBrowsable(EditorBrowsableState.Never)]
        Object ResolveInstance(Type from, Object id = null);
        #endregion
    }

    /// <summary>生命周期</summary>
    public enum ObjectLifetime
    {
        /// <summary>单实例</summary>
        Singleton,

        /// <summary>容器内单实例</summary>
        Scoped,

        /// <summary>每次一个实例</summary>
        Transient
    }

    /// <summary>对象映射接口</summary>
    public interface IObject
    {
        /// <summary>服务类型</summary>
        Type ServiceType { get; }

        /// <summary>实现类型</summary>
        Type ImplementationType { get; }

        /// <summary>生命周期</summary>
        ObjectLifetime Lifttime { get; }
    }
}