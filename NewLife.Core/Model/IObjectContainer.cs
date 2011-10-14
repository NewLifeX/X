using System;
using System.Collections.Generic;

namespace NewLife.Model
{
    /// <summary>对象容器接口</summary>
    public interface IObjectContainer
    {
        #region 父容器
        ///// <summary>父容器</summary>
        //IObjectContainer Parent { get; }

        ///// <summary>
        ///// 移除所有子容器
        ///// </summary>
        ///// <returns></returns>
        //IObjectContainer RemoveAllChildContainers();

        ///// <summary>
        ///// 创建子容器
        ///// </summary>
        ///// <returns></returns>
        //IObjectContainer CreateChildContainer();
        #endregion

        #region 注册
        /// <summary>
        /// 注册类型
        /// </summary>
        /// <param name="from">接口类型</param>
        /// <param name="to">实现类型</param>
        /// <returns></returns>
        IObjectContainer Register(Type from, Type to);

        /// <summary>
        /// 注册类型和名称
        /// </summary>
        /// <param name="from">接口类型</param>
        /// <param name="to">实现类型</param>
        /// <param name="name">名称</param>
        /// <returns></returns>
        IObjectContainer Register(Type from, Type to, String name);

        /// <summary>
        /// 注册类型
        /// </summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <typeparam name="TImplement">实现类型</typeparam>
        /// <returns></returns>
        IObjectContainer Register<TInterface, TImplement>();

        /// <summary>
        /// 注册类型和名称
        /// </summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <typeparam name="TImplement">实现类型</typeparam>
        /// <param name="name">名称</param>
        /// <returns></returns>
        IObjectContainer Register<TInterface, TImplement>(String name);

        /// <summary>
        /// 注册类型的实例
        /// </summary>
        /// <param name="from">接口类型</param>
        /// <param name="instance">实例</param>
        /// <returns></returns>
        IObjectContainer Register(Type from, Object instance);

        /// <summary>
        /// 注册类型指定名称的实例
        /// </summary>
        /// <param name="from">接口类型</param>
        /// <param name="name">名称</param>
        /// <param name="instance">实例</param>
        /// <returns></returns>
        IObjectContainer Register(Type from, String name, Object instance);

        /// <summary>
        /// 注册类型的实例
        /// </summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <param name="instance">实例</param>
        /// <returns></returns>
        IObjectContainer Register<TInterface>(Object instance);

        /// <summary>
        /// 注册类型指定名称的实例
        /// </summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <param name="name">名称</param>
        /// <param name="instance">实例</param>
        /// <returns></returns>
        IObjectContainer Register<TInterface>(String name, Object instance);
        #endregion

        #region 解析
        /// <summary>
        /// 解析类型的实例
        /// </summary>
        /// <param name="type">接口类型</param>
        /// <returns></returns>
        Object Resolve(Type type);

        /// <summary>
        /// 解析类型指定名称的实例
        /// </summary>
        /// <param name="type">接口类型</param>
        /// <param name="name">名称</param>
        /// <returns></returns>
        Object Resolve(Type type, String name);

        /// <summary>
        /// 解析类型的实例
        /// </summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <returns></returns>
        TInterface Resolve<TInterface>();

        /// <summary>
        /// 解析类型指定名称的实例
        /// </summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <param name="name">名称</param>
        /// <returns></returns>
        TInterface Resolve<TInterface>(String name);

        /// <summary>
        /// 解析类型所有已注册的实例
        /// </summary>
        /// <param name="type">接口类型</param>
        /// <returns></returns>
        IEnumerable<Object> ResolveAll(Type type);

        /// <summary>
        /// 解析类型所有已注册的实例
        /// </summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <returns></returns>
        IEnumerable<TInterface> ResolveAll<TInterface>();
        #endregion
    }
}