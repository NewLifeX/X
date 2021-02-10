using System;
using System.ComponentModel;
using NewLife.Model;

namespace NewLife.Model
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
            //if (instance == null) throw new ArgumentNullException(nameof(instance));

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
        public static IObjectContainer AddSingleton<TService>(this IObjectContainer container, TService instance = null) where TService : class => container.AddSingleton(typeof(TService), instance);

        /// <summary>尝试添加单实例，指定实现类型</summary>
        /// <param name="container"></param>
        /// <param name="serviceType"></param>
        /// <param name="implementationType"></param>
        /// <returns></returns>
        public static IObjectContainer TryAddSingleton(this IObjectContainer container, Type serviceType, Type implementationType)
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
            container.TryAdd(item);

            return container;
        }
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

        /// <summary>添加瞬态实例，指定实现类型</summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="container"></param>
        /// <returns></returns>
        public static IObjectContainer AddTransient<TService>(this IObjectContainer container) where TService : class => container.AddTransient(typeof(TService), typeof(TService));

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

        /// <summary>添加瞬态实例，指定实现类型</summary>
        /// <param name="container"></param>
        /// <param name="serviceType"></param>
        /// <param name="implementationType"></param>
        /// <returns></returns>
        public static IObjectContainer TryAddTransient(this IObjectContainer container, Type serviceType, Type implementationType)
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
            container.TryAdd(item);

            return container;
        }
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

        /// <summary>从对象容器创建应用主机</summary>
        /// <param name="container"></param>
        /// <returns></returns>
        public static IHost BuildHost(this IObjectContainer container)
        {
            // 尝试注册应用主机，如果前面已经注册，则这里无效
            container.TryAddTransient(typeof(IHost), typeof(Host));

            //return new Host(container.BuildServiceProvider());
            return container.BuildServiceProvider().GetService(typeof(IHost)) as IHost;
        }
        #endregion

        #region 旧版方法
        /// <summary>解析类型的实例</summary>
        /// <typeparam name="TService">接口类型</typeparam>
        /// <param name="container">对象容器</param>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static TService Resolve<TService>(this IObjectContainer container) => (TService)container.Resolve(typeof(TService));
        #endregion
    }
}