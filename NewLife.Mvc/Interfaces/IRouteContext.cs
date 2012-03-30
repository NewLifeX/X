using System;
using System.Collections.Generic;
using NewLife.Reflection;

namespace NewLife.Mvc
{
    /// <summary>运行时的路由上下文</summary>
    public interface IRouteContext : IEnumerable<RouteFrag>
    {
        /// <summary>
        /// 当前的路径,在不同的上下文环境中有不同的含义
        ///  在模块路由中:路由路径中,匹配当前模块后剩下的路径
        ///  在控制器工厂中,路由路径中,匹配当前控制器工厂后剩下的路径
        ///  在控制器中,路由路径中,匹配当前控制器后剩下的路径
        /// </summary>
        string Path { get; }

        /// <summary>获取Path属性使用/分割后的片段,不包含空白的,对于/foo/bar.foo这样的路径,将会返回["foo","bar.foo"]</summary>
        string[] PathFragments { get; }

        /// <summary>当前路由处理的原始路径,完整的路径,从Url中网站根路径开始的</summary>
        string RoutePath { get; }

        /// <summary>当前Mvc路由是否已经路由到一个有效的控制器,忽略的路由IgnoreRoute不算有效的控制器</summary>
        bool Routed { get; }

        /// <summary>返回路由最近的一个控制器,如果没有路由进控制器则返回null</summary>
        RouteFrag Controller { get; }

        /// <summary>返回路由最近的一个控制器工厂,如果没有路由进工厂则返回null</summary>
        RouteFrag Factory { get; }

        /// <summary>返回路由最近的一个模块</summary>
        RouteFrag Module { get; }

        /// <summary>当前路由最近的一个路由配置</summary>
        [Obsolete("不再使用Config类型的上下文,不需要使用这个方法了")]
        RouteFrag Config { get; }

        /// <summary>
        /// 当前路由的片段,Url从左向右,分别表示数组下标从0开始的路由片段
        ///
        /// 实现IEnumerable接口 遍历时也是如此的顺序
        /// </summary>
        RouteFrag[] Frags { get; }

        /// <summary>
        /// 在Frags中查找第一个符合指定条件的RouteFrag
        ///
        /// 匹配的Url片段将按照从右向左遍历,不同于Frags属性返回的是从左向右的
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        RouteFrag FindFrag(Func<RouteFrag, bool> filter);

        /// <summary>
        /// 在Frags中查找符合指定条件的RouteFrag
        ///
        /// 匹配的Url片段将按照从右向左遍历,不同于Frags属性返回的是从左向右的
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        List<RouteFrag> FindAllFrag(Func<RouteFrag, bool> filter);

        /// <summary>
        /// 路由当前路径到指定类的模块路由配置
        ///
        /// 一般在控制器工厂中使用,用于运行时路由,相对应的是IRouteConfigModule配置路由
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        IController RouteTo<T>() where T : IRouteConfigModule, new();

        /// <summary>
        /// 路由当前路径到指定类的模块路由配置
        ///
        /// 一般在控制器工厂中使用,用于运行时路由,对应的静态路由是通过实现IRouteConfigModule接口配置路由
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="match">匹配到的路径,需要是当前Path属性的开始部分</param>
        /// <returns></returns>
        IController RouteTo<T>(string match) where T : IRouteConfigModule, new();

        /// <summary>
        /// 路由当前路径到指定类型的模块路由配置
        ///
        /// 一般在控制器工厂中使用,用于运行时路由,相对应的是IRouteConfigModule配置路由
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        IController RouteTo(Type type);
        /// <summary>
        /// 路由当前路径到指定类型的模块路由配置
        ///
        /// 一般在控制器工厂中使用,用于运行时路由,对应的静态路由是通过实现IRouteConfigModule接口配置路由
        /// </summary>
        /// <param name="match"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        IController RouteTo(string match, Type type);

        /// <summary>
        /// 路由当前路径到指定的模块路由规则
        ///
        /// 一般在控制器工厂中使用,用于运行时路由,对应的静态路由是通过实现IRouteConfigModule接口配置路由
        /// </summary>
        /// <param name="match"></param>
        /// <param name="rule"></param>
        /// <returns></returns>
        IController RouteTo(string match, ModuleRule rule);

                /// <summary>
        /// 路由当前路径到指定的模块路由规则
        ///
        /// 一般在控制器工厂中使用,用于运行时路由,对应的静态路由是通过实现IRouteConfigModule接口配置路由
        /// </summary>
        /// <param name="match"></param>
        /// <param name="rule"></param>
        /// <param name="adjustRouteFrag">对路由上下文片段的微调回调,如果返回null则表示不进出路由上下文,如果指定为null则不做微调</param>
        /// <returns></returns>
        IController RouteTo(string match, ModuleRule rule, Func<RouteFrag, RouteFrag> adjustRouteFrag);
    }
}