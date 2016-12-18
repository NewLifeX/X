using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace NewLife.Cube
{
    /// <summary>WebApi配置</summary>
    public static class WebApiConfig
    {
        /// <summary>注册</summary>
        /// <param name="config"></param>
        public static void Register(HttpConfiguration config)
        {
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }
    }
}
