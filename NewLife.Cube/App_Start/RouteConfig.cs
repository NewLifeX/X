using System.Web.Mvc;
using System.Web.Routing;
using NewLife.Cube.Controllers;

namespace NewLife.Cube
{
    /// <summary>路由配置</summary>
    public class RouteConfig
    {
        /// <summary>注册路由</summary>
        /// <param name="routes"></param>
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.IgnoreRoute("Content/{*relpath}");
            routes.IgnoreRoute("Scripts/{*relpath}");
            routes.IgnoreRoute("Images/{*relpath}");

            if (routes["Cube"] == null)
            {
                // 为魔方注册默认首页，启动魔方站点时能自动跳入后台，同时为Home预留默认过度视图页面
                routes.MapRoute(
                    name: "Cube",
                    url: "{controller}/{action}/{id}",
                    defaults: new { controller = "CubeHome", action = "Index", id = UrlParameter.Optional },
                    namespaces: new[] { typeof(CubeHomeController).Namespace }
                    );
            }
        }
    }
}