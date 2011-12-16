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

        /// <summary>
        /// 获取Path属性使用/分割后的片段,不包含空白的,对于/foo/bar.foo这样的路径,将会返回["foo","bar.foo"]
        /// </summary>
        string[] PathFragments { get; }

        /// <summary>
        /// 当前路由处理的原始路径,完整的路径,从Url中网站根路径开始的
        /// </summary>
        string RoutePath { get; }

        /// <summary>
        /// 返回路由最近的一个控制器,如果没有路由进控制器则返回null
        /// </summary>
        RouteFrag? Controller { get; }

        /// <summary>
        /// 返回路由最近的一个控制器工厂,如果没有路由进工厂则返回null
        /// </summary>
        RouteFrag? Factory { get; }

        /// <summary>
        /// 返回路由最近的一个模块
        /// </summary>
        RouteFrag? Module { get; }

        /// <summary>
        /// 当前路由最近的一个路由配置
        /// </summary>
        RouteFrag? Config { get; }

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
        RouteFrag? FindFrag(Func<RouteFrag, bool> filter);

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
        /// 路由当前路径到指定类型的模块路由配置
        ///
        /// 一般在控制器工厂中使用,用于运行时路由,相对应的是IRouteConfigModule配置路由
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        IController RouteTo(Type type);

        /// <summary>
        /// 路由当前路径到指定的路由配置模块,如果cfg尚未有任何路由规则,则使用module配置的规则
        ///
        /// 一般在控制器工厂中使用,用于运行时路由,相对应的是IRouteConfigModule配置路由
        /// </summary>
        /// <param name="module"></param>
        /// <param name="cfg">一般应提供一个缓存的RouteConfigManager,避免每次都实例化RouteConfigManager</param>
        /// <returns></returns>
        IController RouteTo(IRouteConfigModule module, RouteConfigManager cfg);

        /// <summary>
        /// 路由当前路径到指定的路由配置
        ///
        /// 一般在控制器工厂中使用,用于运行时路由,相对应的是IRouteConfigModule配置路由
        ///
        /// 建议使用RouteTo(IRouteConfigModule module, RouteConfigManager cfg),可以在上下文中留下模块路由信息,这个只能留下路由配置信息
        /// </summary>
        /// <param name="cfg"></param>
        /// <returns></returns>
        IController RouteTo(RouteConfigManager cfg);
    }
}