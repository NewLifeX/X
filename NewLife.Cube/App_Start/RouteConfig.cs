using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

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

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );

            // 所有已存在文件的请求都交给Mvc处理，比如Admin目录
            //routes.RouteExistingFiles = true;
        }
    }
}