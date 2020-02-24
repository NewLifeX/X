using System.ComponentModel;
using NewLife.Model;

namespace System
{
    /// <summary>对象容器助手。扩展方法专用</summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public static class ObjectContainerHelper
    {
        #region 单实例注册
        /// <summary>添加单实例，指定实现类型</summary>
        /// <param name="container"></param>
        /// <param name="serviceType"></param>
        /// <param name="implementationType"></param>
        /// <returns></returns>
        public static IObjectContainer AddSingleton(this IObjectContainer container, Type serviceType, Type implementationType)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
            if (implementationType == null) throw new ArgumentNullException(nameof(implementationType));

            var item = new ObjectMap
            {
                ServiceType = serviceType,
                ImplementationType = implementationType,
                Lifttime = ObjectLifetime.Singleton,
            };
            container.Add(item);

            return container;
        }

        /// <summary>添加单实例，指定实现类型</summary>
        /// <typeparam name="TService"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        /// <param name="container"></param>
        /// <returns></returns>
        public static IObjectContainer AddSingleton<TService, TImplementation>(this IObjectContainer container) where TService : class where TImplementation : class, TService => container.AddSingleton(typeof(TService), typeof(TImplementation));

        /// <summary>添加单实例，指定实例工厂</summary>
        /// <param name="container"></param>
        /// <param name="serviceType"></param>
        /// <param name="factory"></param>
        /// <returns></returns>
        public static IObjectContainer AddSingleton(this IObjectContainer container, Type serviceType, Func<IServiceProvider, Object> factory)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            var item = new ObjectMap
            {
                ServiceType = serviceType,
                Factory = factory,
                Lifttime = ObjectLifetime.Singleton,
            };
            container.Add(item);

            return container;
        }

        /// <summary>添加单实例，指定实例工厂</summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="container"></param>
        /// <param name="factory"></param>
        /// <returns></returns>
        public static IObjectContainer AddSingleton<TService>(this IObjectContainer container, Func<IServiceProvider, Object> factory) where TService : class => container.AddSingleton(typeof(TService), factory);

        /// <summary>添加单实例，指定实例</summary>
        /// <param name="container"></param>
        /// <param name="serviceType"></param>
        /// <param name="instance"></param>
        /// <returns></returns>
        public static IObjectContainer AddSingleton(this IObjectContainer container, Type serviceType, Object instance)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
            if (instance == null) throw new ArgumentNullException(nameof(instance));

            var item = new ObjectMap
            {
                ServiceType = serviceType,
                Instance = instance,
                Lifttime = ObjectLifetime.Singleton,
            };
            container.Add(item);

            return container;
        }

        /// <summary>添加单实例，指定实例</summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="container"></param>
        /// <param name="instance"></param>
        /// <returns></returns>
        public static IObjectContainer AddSingleton<TService>(this IObjectContainer container, TService instance) where TService : class => container.AddSingleton(typeof(TService), instance);
        #endregion

        #region 瞬态注册
        /// <summary>添加瞬态实例，指定实现类型</summary>
        /// <param name="container"></param>
        /// <param name="serviceType"></param>
        /// <param name="implementationType"></param>
        /// <returns></returns>
        public static IObjectContainer AddTransient(this IObjectContainer container, Type serviceType, Type implementationType)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
            if (implementationType == null) throw new ArgumentNullException(nameof(implementationType));

            var item = new ObjectMap
            {
                ServiceType = serviceType,
                ImplementationType = implementationType,
                Lifttime = ObjectLifetime.Transient,
            };
            container.Add(item);

            return container;
        }

        /// <summary>添加瞬态实例，指定实现类型</summary>
        /// <typeparam name="TService"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        /// <param name="container"></param>
        /// <returns></returns>
        public static IObjectContainer AddTransient<TService, TImplementation>(this IObjectContainer container) where TService : class where TImplementation : class, TService => container.AddTransient(typeof(TService), typeof(TImplementation));

        /// <summary>添加瞬态实例，指定实现工厂</summary>
        /// <param name="container"></param>
        /// <param name="serviceType"></param>
        /// <param name="factory"></param>
        /// <returns></returns>
        public static IObjectContainer AddTransient(this IObjectContainer container, Type serviceType, Func<IServiceProvider, Object> factory)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            var item = new ObjectMap
            {
                ServiceType = serviceType,
                Factory = factory,
                Lifttime = ObjectLifetime.Transient,
            };
            container.Add(item);

            return container;
        }

        /// <summary>添加瞬态实例，指定实现工厂</summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="container"></param>
        /// <param name="factory"></param>
        /// <returns></returns>
        public static IObjectContainer AddTransient<TService>(this IObjectContainer container, Func<IServiceProvider, Object> factory) where TService : class => container.AddTransient(typeof(TService), factory);
        #endregion

        #region 构建
        /// <summary>从对象容器创建服务提供者</summary>
        /// <param name="container"></param>
        /// <returns></returns>
        public static IServiceProvider BuildServiceProvider(this IObjectContainer container)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));

            return new ServiceProvider(container);
        }
        #endregion

        #region 旧版方法
        /// <summary>注册类型和名称</summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <typeparam name="TImplement">实现类型</typeparam>
        /// <param name="container">对象容器</param>
        /// <param name="id">标识</param>
        /// <param name="priority">优先级</param>
        /// <returns></returns>
        [Obsolete]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static IObjectContainer Register<TInterface, TImplement>(this IObjectContainer container, Object id = null, Int32 priority = 0) => container.Register(typeof(TInterface), typeof(TImplement), null);

        /// <summary>注册类型指定名称的实例</summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <param name="container">对象容器</param>
        /// <param name="instance">实例</param>
        /// <param name="id">标识</param>
        /// <param name="priority">优先级</param>
        /// <returns></returns>
        [Obsolete]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static IObjectContainer Register<TInterface>(this IObjectContainer container, Object instance, Object id = null, Int32 priority = 0) => container.Register(typeof(TInterface), null, instance);

        /// <summary>解析类型的实例</summary>
        /// <typeparam name="TService">接口类型</typeparam>
        /// <param name="container">对象容器</param>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static TService Resolve<TService>(this IObjectContainer container) => (TService)container.Resolve(typeof(TService));

        /// <summary>解析类型指定名称的实例</summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <param name="container">对象容器</param>
        /// <param name="id">标识</param>
        /// <returns></returns>
        [Obsolete]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static TInterface Resolve<TInterface>(this IObjectContainer container, Object id) => (TInterface)container.Resolve(typeof(TInterface));

        /// <summary>解析类型指定名称的实例</summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <param name="container">对象容器</param>
        /// <param name="id">标识</param>
        /// <returns></returns>
        [Obsolete]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static TInterface ResolveInstance<TInterface>(this IObjectContainer container, Object id = null) => (TInterface)container.Resolve(typeof(TInterface));
        #endregion
    }
}