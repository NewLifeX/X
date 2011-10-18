using System;
using System.Collections.Generic;
using System.Globalization;

namespace NewLife.Model
{
    /// <summary>服务容器</summary>
    /// <remarks>
    /// 建议各个组件通过继承当前类实现一个私有的服务定位器，用于为组件内提供服务定位服务。
    /// 组件内部的默认实现可以在静态构造函数中进行无覆盖注册。
    /// 作为约定，组件内部的服务定位全部通过该类完成，保证服务在使用前已完成了注册。
    /// </remarks>
    public class ServiceContainer
    {
        #region 当前静态服务容器
        /// <summary>当前对象容器</summary>
        public static IObjectContainer Container { get { return ObjectContainer.Current; } }
        #endregion

        #region 服务
        /// <summary>
        /// 注册
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="impl"></param>
        /// <param name="name"></param>
        public static void Register<T>(Type impl, String name)
        {
            Container.Register(typeof(T), impl, name);
        }

        /// <summary>
        /// 解析类型指定名称的实例
        /// </summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static TInterface Resolve<TInterface>(String name)
        {
            return Container.Resolve<TInterface>(name);
        }

        /// <summary>
        /// 解析类型
        /// </summary>
        /// <typeparam name="TInterface"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Type ResolveType<TInterface>(String name)
        {
            return Container.ResolveType(typeof(TInterface), name);
        }
        #endregion
    }
}