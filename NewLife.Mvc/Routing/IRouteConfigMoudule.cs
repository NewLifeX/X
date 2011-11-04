namespace NewLife.Mvc
{
    /// <summary>
    /// 模块路由配置,可以在其它路由中
    /// </summary>
    public interface IRouteConfigMoudule
    {
        /// <summary>
        /// 配置方法
        /// </summary>
        /// <param name="cfg"></param>
        void Config(RouteConfigManager cfg);
    }
}