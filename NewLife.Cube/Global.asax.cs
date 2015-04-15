using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using NewLife.Threading;
using XCode.Membership;

namespace NewLife.Cube
{
    /// <summary>
    /// 注意: 有关启用 IIS6 或 IIS7 经典模式的说明，
    /// 请访问 http://go.microsoft.com/?LinkId=9394801
    /// </summary>

    public class MvcApplication : System.Web.HttpApplication
    {
        /// <summary>应用程序启动</summary>
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            //WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            //AuthConfig.RegisterAuth();

        }
    }
}