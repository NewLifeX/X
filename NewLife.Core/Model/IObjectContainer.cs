using System;
using System.Collections.Generic;

namespace NewLife.Model
{
    //TODO 考虑把标识对象的String name改为Object id，这样子方便使用任何类型作为标识，比如很多时候使用的就是枚举。

    /// <summary>对象容器接口</summary>
    /// <remarks>
    /// 1，如果容器里面没有这个类型，则返回空；
    /// 2，如果容器里面包含这个类型，并且指向的实例不为空，则返回，单例；
    /// 3，如果容器里面包含这个类型，并且指向的实例为空，则创建对象返回，多实例；
    /// 4，如果有带参数构造函数，则从容器内获取各个参数的实例，最后创建对象返回。
    /// </remarks>
    public interface IObjectContainer
    {
        #region 注册
        /// <summary>
        /// 注册类型和名称
        /// </summary>
        /// <param name="from">接口类型</param>
        /// <param name="to">实现类型</param>
        /// <param name="instance">实例</param>
        /// <param name="id">标识</param>
        /// <param name="priority">优先级</param>
        /// <returns></returns>
        IObjectContainer Register(Type from, Type to, Object instance, Object id = null, Int32 priority = 0);

        /// <summary>
        /// 注册类型和名称
        /// </summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <typeparam name="TImplement">实现类型</typeparam>
        /// <param name="id">标识</param>
        /// <param name="priority">优先级</param>
        /// <returns></returns>
        IObjectContainer Register<TInterface, TImplement>(Object id = null, Int32 priority = 0);

        /// <summary>
        /// 注册类型指定名称的实例
        /// </summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <param name="instance">实例</param>
        /// <param name="id">标识</param>
        /// <param name="priority">优先级</param>
        /// <returns></returns>
        IObjectContainer Register<TInterface>(Object instance, Object id = null, Int32 priority = 0);

        /// <summary>注册前事件</summary>
        event EventHandler<EventArgs<Type, IObjectMap>> OnRegistering;

        /// <summary>注册后事件</summary>
        event EventHandler<EventArgs<Type, IObjectMap>> OnRegistered;
        #endregion

        #region 解析
        /// <summary>
        /// 解析类型指定名称的实例
        /// </summary>
        /// <param name="from">接口类型</param>
        /// <param name="id">标识</param>
        /// <param name="extend">扩展。若为ture，id为null而找不到时，采用第一个注册项；id不为null而找不到时，采用null注册项</param>
        /// <returns></returns>
        Object Resolve(Type from, Object id = null, Boolean extend = false);

        /// <summary>
        /// 解析类型指定名称的实例
        /// </summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <param name="id">标识</param>
        /// <param name="extend">扩展。若为ture，id为null而找不到时，采用第一个注册项；id不为null而找不到时，采用null注册项</param>
        /// <returns></returns>
        TInterface Resolve<TInterface>(Object id = null, Boolean extend = false);

        /// <summary>
        /// 解析类型所有已注册的实例
        /// </summary>
        /// <param name="from">接口类型</param>
        /// <returns></returns>
        IEnumerable<Object> ResolveAll(Type from);

        /// <summary>
        /// 解析类型所有已注册的实例
        /// </summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <returns></returns>
        IEnumerable<TInterface> ResolveAll<TInterface>();
        #endregion

        #region 解析类型
        /// <summary>
        /// 解析接口指定名称的实现类型
        /// </summary>
        /// <param name="from">接口类型</param>
        /// <param name="id">标识</param>
        /// <param name="extend">扩展。若为ture，id为null而找不到时，采用第一个注册项；id不为null而找不到时，采用null注册项</param>
        /// <returns></returns>
        Type ResolveType(Type from, Object id = null, Boolean extend = false);

        /// <summary>
        /// 解析接口指定名称的实现类型
        /// </summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <param name="id">标识</param>
        /// <param name="extend">扩展。若为ture，id为null而找不到时，采用第一个注册项；id不为null而找不到时，采用null注册项</param>
        /// <returns></returns>
        Type ResolveType<TInterface>(Object id = null, Boolean extend = false);

        /// <summary>
        /// 解析接口所有已注册的实现类型
        /// </summary>
        /// <param name="from">接口类型</param>
        /// <returns></returns>
        IEnumerable<Type> ResolveAllTypes(Type from);

        /// <summary>
        /// 解析接口所有已注册的对象映射
        /// </summary>
        /// <param name="from">接口类型</param>
        /// <returns></returns>
        IEnumerable<IObjectMap> ResolveAllMaps(Type from);
        #endregion
    }

    /// <summary>对象映射接口</summary>
    public interface IObjectMap
    {
        /// <summary>名称</summary>
        Object Identity { get; }

        /// <summary>实现类型</summary>
        Type ImplementType { get; }

        /// <summary>对象实例</summary>
        Object Instance { get; }

        /// <summary>单一实例</summary>
        Boolean Singleton { get; }
    }
}