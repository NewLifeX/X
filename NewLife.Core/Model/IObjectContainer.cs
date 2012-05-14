using System;
using System.Collections.Generic;

namespace NewLife.Model
{
    /// <summary>对象容器接口</summary>
    /// <remarks>
    /// 1，如果容器里面没有这个类型，则返回空；
    /// 2，如果容器里面包含这个类型，<see cref="ResolveInstance"/>返回单例；
    /// 3，如果容器里面包含这个类型，<see cref="Resolve"/>创建对象返回多实例；
    /// 4，如果有带参数构造函数，则从容器内获取各个参数的实例，最后创建对象返回。
    /// 
    /// 这里有一点跟大多数对象容器非常不同，其它对象容器会控制对象的生命周期，在对象不再使用时收回到容器里面。
    /// 这里的对象容器主要是为了用于解耦，所以只有最简单的功能实现。
    /// </remarks>
    public interface IObjectContainer
    {
        #region 注册
        /// <summary>注册类型和名称</summary>
        /// <param name="from">接口类型</param>
        /// <param name="to">实现类型</param>
        /// <param name="instance">实例</param>
        /// <param name="id">标识</param>
        /// <param name="priority">优先级</param>
        /// <returns></returns>
        IObjectContainer Register(Type from, Type to, Object instance, Object id = null, Int32 priority = 0);

        /// <summary>注册类型和名称</summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <typeparam name="TImplement">实现类型</typeparam>
        /// <param name="id">标识</param>
        /// <param name="priority">优先级</param>
        /// <returns></returns>
        IObjectContainer Register<TInterface, TImplement>(Object id = null, Int32 priority = 0);

        /// <summary>注册类型指定名称的实例</summary>
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

        /// <summary>遍历所有程序集的所有类型，自动注册实现了指定接口或基类的类型</summary>
        /// <param name="from">接口或基类</param>
        /// <param name="excludeTypes">要排除的类型，一般是内部默认实现</param>
        /// <returns></returns>
        IObjectContainer AutoRegister(Type from, params Type[] excludeTypes);
        #endregion

        #region 解析
        /// <summary>解析类型指定名称的实例</summary>
        /// <param name="from">接口类型</param>
        /// <param name="id">标识</param>
        /// <param name="extend">扩展。若为ture，id为null而找不到时，采用第一个注册项；id不为null而找不到时，采用null注册项</param>
        /// <returns></returns>
        Object Resolve(Type from, Object id = null, Boolean extend = false);

        /// <summary>解析类型指定名称的实例</summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <param name="id">标识</param>
        /// <param name="extend">扩展。若为ture，id为null而找不到时，采用第一个注册项；id不为null而找不到时，采用null注册项</param>
        /// <returns></returns>
        TInterface Resolve<TInterface>(Object id = null, Boolean extend = false);

        /// <summary>解析类型指定名称的实例</summary>
        /// <param name="from">接口类型</param>
        /// <param name="id">标识</param>
        /// <param name="extend">扩展。若为ture，id为null而找不到时，采用第一个注册项；id不为null而找不到时，采用null注册项</param>
        /// <returns></returns>
        Object ResolveInstance(Type from, Object id = null, Boolean extend = false);

        /// <summary>解析类型指定名称的实例</summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <param name="id">标识</param>
        /// <param name="extend">扩展。若为ture，id为null而找不到时，采用第一个注册项；id不为null而找不到时，采用null注册项</param>
        /// <returns></returns>
        TInterface ResolveInstance<TInterface>(Object id = null, Boolean extend = false);

        /// <summary>解析类型所有已注册的实例</summary>
        /// <param name="from">接口类型</param>
        /// <returns></returns>
        IEnumerable<Object> ResolveAll(Type from);

        /// <summary>解析类型所有已注册的实例</summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <returns></returns>
        IEnumerable<TInterface> ResolveAll<TInterface>();
        #endregion

        #region 解析类型
        /// <summary>解析接口指定名称的实现类型</summary>
        /// <param name="from">接口类型</param>
        /// <param name="id">标识</param>
        /// <param name="extend">扩展。若为ture，id为null而找不到时，采用第一个注册项；id不为null而找不到时，采用null注册项</param>
        /// <returns></returns>
        Type ResolveType(Type from, Object id = null, Boolean extend = false);

        /// <summary>解析接口指定名称的实现类型</summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <param name="id">标识</param>
        /// <param name="extend">扩展。若为ture，id为null而找不到时，采用第一个注册项；id不为null而找不到时，采用null注册项</param>
        /// <returns></returns>
        Type ResolveType<TInterface>(Object id = null, Boolean extend = false);

        /// <summary>解析接口所有已注册的实现类型</summary>
        /// <param name="from">接口类型</param>
        /// <returns></returns>
        IEnumerable<Type> ResolveAllTypes(Type from);

        /// <summary>解析接口所有已注册的对象映射</summary>
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

        ///// <summary>单一实例</summary>
        //Boolean Singleton { get; }
    }
}