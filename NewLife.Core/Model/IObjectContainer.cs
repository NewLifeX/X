using System;
using System.Collections.Generic;

namespace NewLife.Model
{
    /// <summary>对象容器接口</summary>
    public interface IObjectContainer
    {
        #region 父容器
        /// <summary>父容器</summary>
        IObjectContainer Parent { get; }

        /// <summary>
        /// 移除所有子容器
        /// </summary>
        /// <returns></returns>
        IObjectContainer RemoveAllChildContainers();

        /// <summary>
        /// 创建子容器
        /// </summary>
        /// <returns></returns>
        IObjectContainer CreateChildContainer();
        #endregion

        #region 注册
        /// <summary>
        /// 注册类型
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        IObjectContainer RegisterType(Type type);

        /// <summary>
        /// 注册类型和名称
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        IObjectContainer RegisterType(Type type, String name);

        /// <summary>
        /// 注册类型
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        IObjectContainer RegisterType<T>();

        /// <summary>
        /// 注册类型和名称
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        IObjectContainer RegisterType<T>(String name);

        /// <summary>
        /// 注册类型的实例
        /// </summary>
        /// <param name="type"></param>
        /// <param name="instance"></param>
        /// <returns></returns>
        IObjectContainer RegisterInstance(Type type, Object instance);

        /// <summary>
        /// 注册类型指定名称的实例
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <param name="instance"></param>
        /// <returns></returns>
        IObjectContainer RegisterInstance(Type type, String name, Object instance);

        /// <summary>
        /// 注册类型的实例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance"></param>
        /// <returns></returns>
        IObjectContainer RegisterInstance<T>(Object instance);

        /// <summary>
        /// 注册类型指定名称的实例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="instance"></param>
        /// <returns></returns>
        IObjectContainer RegisterInstance<T>(String name, Object instance);
        #endregion

        #region 解析
        /// <summary>
        /// 解析类型的实例
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        Object Resolve(Type type);

        /// <summary>
        /// 解析类型指定名称的实例
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        Object Resolve(Type type, String name);

        /// <summary>
        /// 解析类型的实例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T Resolve<T>();

        /// <summary>
        /// 解析类型指定名称的实例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        T Resolve<T>(String name);

        /// <summary>
        /// 解析类型所有已注册的实例
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        IEnumerable<Object> ResolveAll(Type type);

        /// <summary>
        /// 解析类型所有已注册的实例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        IEnumerable<T> ResolveAll<T>();
        #endregion
    }
}