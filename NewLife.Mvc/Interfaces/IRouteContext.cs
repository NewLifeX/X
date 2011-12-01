namespace NewLife.Mvc
{
    /// <summary>运行时的路由上下文</summary>
    public interface IRouteContext
    {
        /// <summary>
        /// 当前的路径,在不同的上下文环境中有不同的含义
        ///  在模块路由中:路由路径中,匹配当前模块后剩下的路径
        ///  在控制器工厂中,路由路径中,匹配当前控制器工厂后剩下的路径
        ///  在控制器中,路由路径中,匹配当前控制器后剩下的路径
        /// </summary>
        string Path { get; }

        /// <summary>
        /// 路由当前的路径到指定的模块路由配置类
        ///
        /// 一般在控制器工厂中使用
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>如果没有任何匹配的路由规则返回null</returns>
        IController RouteTo<T>() where T : IRouteConfigModule;

        /// <summary>
        /// 路由当前路径到指定的模块路由配置实例
        ///
        /// 一般在控制器工厂中使用
        /// </summary>
        /// <param name="module"></param>
        /// <returns>如果没有任何匹配的路由规则返回null</returns>
        IController RouteTo(IRouteConfigModule module);

        /// <summary>
        /// 路由当前路径到指定的路由配置
        ///
        /// 一般在控制器工厂中使用
        /// </summary>
        /// <param name="cfg"></param>
        /// <returns>如果没有任何匹配的路由规则返回null</returns>
        IController RouteTo(RouteConfigManager cfg);
    }
}