using System;

namespace NewLife.Model
{
    /// <summary>服务容器基类。使用泛型基类，仅仅是为了引发子类的静态构造函数。</summary>
    /// <typeparam name="TService">具体服务容器类</typeparam>
    /// <remarks>
    /// 建议各个组件通过继承当前类实现一个私有的服务定位器，用于为组件内提供服务定位服务。
    /// 组件内部的默认实现可以在静态构造函数中进行无覆盖注册。
    /// 作为约定，组件内部的服务定位全部通过该类完成，保证服务在使用前已完成了注册。
    /// </remarks>
    public class ServiceContainer<TService> where TService : ServiceContainer<TService>, new()
    {
        #region 静态构造函数
        static ServiceContainer()
        {
            // 实例化一个对象，为了触发子类的静态构造函数
            TService service = new TService();
        }
        #endregion

        #region 当前静态服务容器
        /// <summary>当前对象容器</summary>
        public static IObjectContainer Container { get { return ObjectContainer.Current; } }
        #endregion

        #region 服务
        /// <summary>
        /// 注册类型和名称
        /// </summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <typeparam name="TImplement">实现类型</typeparam>
        /// <param name="name">名称</param>
        /// <param name="priority">优先级</param>
        /// <returns></returns>
        public static IObjectContainer Register<TInterface, TImplement>(String name = null, Int32 priority = 0) { return Container.Register<TInterface, TImplement>(name, priority); }

        /// <summary>
        /// 注册
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="impl"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static IObjectContainer Register<T>(Type impl, String name = null) { return Container.Register(typeof(T), impl, name); }

        /// <summary>
        /// 解析类型指定名称的实例
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <param name="extend"></param>
        /// <returns></returns>
        public static Object Resolve(Type type, String name = null, Boolean extend = false) { return Container.Resolve(type, name, extend); }

        /// <summary>
        /// 解析类型指定名称的实例
        /// </summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <param name="name">名称</param>
        /// <param name="extend">扩展。若为ture，name为null而找不到时，采用第一个注册项；name不为null而找不到时，采用null注册项</param>
        /// <returns></returns>
        public static TInterface Resolve<TInterface>(String name = null, Boolean extend = false) { return Container.Resolve<TInterface>(name, extend); }

        /// <summary>
        /// 解析类型
        /// </summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <param name="name">名称</param>
        /// <param name="extend">扩展。若为ture，name为null而找不到时，采用第一个注册项；name不为null而找不到时，采用null注册项</param>
        /// <returns></returns>
        public static Type ResolveType<TInterface>(String name = null, Boolean extend = false) { return Container.ResolveType(typeof(TInterface), name, extend); }
        #endregion
    }
}