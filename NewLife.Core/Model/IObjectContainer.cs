using System;
using System.ComponentModel;

namespace NewLife.Model
{
    /// <summary>轻量级对象容器，支持注入</summary>
    /// <remarks>
    /// 文档 https://www.yuque.com/smartstone/nx/object_container
    /// </remarks>
    public interface IObjectContainer
    {
        #region 注册
        /// <summary>注册类型和名称</summary>
        /// <param name="serviceType">接口类型</param>
        /// <param name="implementationType">实现类型</param>
        /// <param name="instance">实例</param>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        IObjectContainer Register(Type serviceType, Type implementationType, Object instance);

        /// <summary>添加</summary>
        /// <param name="item"></param>
        void Add(IObject item);

        /// <summary>尝试添加</summary>
        /// <param name="item"></param>
        Boolean TryAdd(IObject item);
        #endregion

        #region 解析
        /// <summary>解析类型的实例</summary>
        /// <param name="serviceType">接口类型</param>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        Object Resolve(Type serviceType);
        #endregion
    }

    /// <summary>生命周期</summary>
    public enum ObjectLifetime
    {
        /// <summary>单实例</summary>
        Singleton,

        ///// <summary>容器内单实例</summary>
        //Scoped,

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