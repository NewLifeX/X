using System.ComponentModel;
using NewLife.Model;
using NewLife.Reflection;

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
        /// <param name="extend">扩展。若为ture，name为null而找不到时，采用第一个注册项；name不为null而找不到时，采用null注册项</param>
        /// <returns></returns>
        public static TInterface Resolve<TInterface>(this IObjectContainer container, Object id = null, Boolean extend = false)
        {
            return (TInterface)container.Resolve(typeof(TInterface), id, extend);
        }

        /// <summary>解析类型指定名称的实例</summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <param name="container">对象容器</param>
        /// <param name="id">标识</param>
        /// <param name="extend">扩展。若为ture，name为null而找不到时，采用第一个注册项；name不为null而找不到时，采用null注册项</param>
        /// <returns></returns>
#if !DEBUG
        public static TInterface ResolveInstance<TInterface>(this IObjectContainer container, Object id = null, Boolean extend = false)
        {
            return (TInterface)container.Resolve(typeof(TInterface), id, extend);
        }
#else
        public static TInterface ResolveInstance<TInterface>(this IObjectContainer container, Object id = null, Boolean extend = false)
        {
            var obj = container.Resolve(typeof(TInterface), id, extend);
            try
            {
                return (TInterface)obj;
            }
            catch (InvalidCastException ex)
            {
                var t = obj.GetType();
                NewLife.Log.XTrace.WriteLine("ObjectType：{0} {1}", t.AssemblyQualifiedName, t.Assembly.Location);
                t = typeof(TInterface);
                NewLife.Log.XTrace.WriteLine("InterfaceType：{0} {1}", t.AssemblyQualifiedName, t.Assembly.Location);
                throw ex;
            }
        }
#endif

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

        /// <summary>遍历所有程序集的所有类型，自动注册实现了指定接口或基类的类型。如果没有注册任何实现，则默认注册第一个排除类型</summary>
        /// <remarks>自动注册一般用于单实例功能扩展型接口</remarks>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <typeparam name="TImplement">要排除的类型，一般是内部默认实现</typeparam>
        /// <param name="container">对象容器</param>
        /// <param name="getidCallback">用于从外部类型对象中获取标识的委托</param>
        /// <param name="id">标识</param>
        /// <param name="priority">优先级</param>
        /// <returns></returns>
        public static IObjectContainer AutoRegister<TInterface, TImplement>(this IObjectContainer container, Func<Object, Object> getidCallback = null, Object id = null, Int32 priority = 0)
        {
            return container.AutoRegister(typeof(TInterface), getidCallback, id, priority, typeof(TImplement));
        }

        /// <summary>解析接口指定名称的实现类型</summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <param name="container">对象容器</param>
        /// <param name="id">标识</param>
        /// <param name="extend">扩展。若为ture，name为null而找不到时，采用第一个注册项；name不为null而找不到时，采用null注册项</param>
        /// <returns></returns>
        public static Type ResolveType<TInterface>(this IObjectContainer container, Object id = null, Boolean extend = false)
        {
            return container.ResolveType(typeof(TInterface), id, extend);
        }
    }
}