using System.ComponentModel;
using NewLife.Model;

namespace System
{
    /// <summary>对象容器助手。扩展方法专用</summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public static class ObjectContainerHelper
    {
        /// <summary>注册类型和名称</summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <typeparam name="TImplement">实现类型</typeparam>
        /// <param name="container">对象容器</param>
        /// <param name="id">标识</param>
        /// <param name="priority">优先级</param>
        /// <returns></returns>
        public static IObjectContainer Register<TInterface, TImplement>(this IObjectContainer container, Object id = null, Int32 priority = 0)
        {
            return container.Register(typeof(TInterface), typeof(TImplement), null, id, priority);
        }

        /// <summary>注册类型指定名称的实例</summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <param name="container">对象容器</param>
        /// <param name="instance">实例</param>
        /// <param name="id">标识</param>
        /// <param name="priority">优先级</param>
        /// <returns></returns>
        public static IObjectContainer Register<TInterface>(this IObjectContainer container, Object instance, Object id = null, Int32 priority = 0)
        {
            return container.Register(typeof(TInterface), null, instance, id, priority);
        }

        /// <summary>解析类型指定名称的实例</summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <param name="container">对象容器</param>
        /// <param name="id">标识</param>
        /// <returns></returns>
        public static TInterface Resolve<TInterface>(this IObjectContainer container, Object id = null)
        {
            return (TInterface)container.Resolve(typeof(TInterface), id);
        }

        /// <summary>解析类型指定名称的实例</summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <param name="container">对象容器</param>
        /// <param name="id">标识</param>
        /// <returns></returns>
        public static TInterface ResolveInstance<TInterface>(this IObjectContainer container, Object id = null)
        {
            return (TInterface)container.ResolveInstance(typeof(TInterface), id);
        }

        /// <summary>遍历所有程序集的所有类型，自动注册实现了指定接口或基类的类型。如果没有注册任何实现，则默认注册第一个排除类型</summary>
        /// <remarks>自动注册一般用于单实例功能扩展型接口</remarks>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <typeparam name="TImplement">要排除的类型，一般是内部默认实现</typeparam>
        /// <param name="container">对象容器</param>
        /// <returns></returns>
        public static IObjectContainer AutoRegister<TInterface, TImplement>(this IObjectContainer container)
        {
            return container.AutoRegister(typeof(TInterface), typeof(TImplement));
        }

        /// <summary>解析接口指定名称的实现类型</summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <param name="container">对象容器</param>
        /// <param name="id">标识</param>
        /// <returns></returns>
        public static Type ResolveType<TInterface>(this IObjectContainer container, Object id = null)
        {
            return container.ResolveType(typeof(TInterface), id);
        }
    }
}