using System.Web;
using NewLife.Reflection;
using System;

namespace NewLife.Mvc
{
    /// <summary>重定向工厂一般不需要自行实例化,使用RouteConfigManager.Redirect方法即可</summary>>
    public class RedirectRoute : IControllerFactory
    {
        IController ctl = null;

        private RedirectRoute()
        {
        }

        /// <summary>重定向到指定的路径,relativeRoot指定to是否是相对于服务器根路径(仅在to不是以~/开始的路径时)</summary>>
        /// <param name="to"></param>
        /// <param name="relativeRoot"></param>
        public RedirectRoute(string to, bool relativeRoot)
        {
            ctl = new RedirectController() { LocationTo = to, RelativeRoot = relativeRoot };
        }
        /// <summary>重定向到指定委托返回的路径,其中委托参数string类型是IRouteContext.RoutePath从开始一直到当前路由模块匹配的路径</summary>>
        /// <param name="func"></param>
        public RedirectRoute(Func<RouteContext, string, string> func)
        {
            ctl = new RedirectController() { LocationFunc = func };
        }

        /// <summary>实现IControllerFactory接口</summary>>
        /// <param name="context"></param>
        /// <returns></returns>
        public IController GetController(IRouteContext context)
        {
            return ctl;
        }

        /// <summary>实现IControllerFactory接口</summary>>
        /// <param name="handler"></param>
        public void ReleaseController(IController handler)
        {
        }

        /// <summary>重定向控制器</summary>>
        internal class RedirectController : GenericController, IController
        {
            internal Func<RouteContext, string, string> LocationFunc;
            internal string LocationTo;
            internal bool RelativeRoot;

            /// <summary>实现重定向</summary>>
            /// <param name="context"></param>
            public override void ProcessRequest(IRouteContext context)
            {
                Response.Clear();
                string url, routePath = "";

                RouteFrag m = context.Module;
                if (m != null)
                {
                    foreach (RouteFrag item in context)
                    {
                        routePath += item.Path;
                        if (item.Related == m.Related) break;
                    }
                }
                if (LocationFunc != null)
                {
                    Response.Status = "302 Found";
                    HttpCacheConfig.RedirectCache(Response.Cache, false);
                    url = LocationFunc(context as RouteContext, routePath);
                }
                else
                {
                    Response.Status = "301 Moved Permanently";
                    HttpCacheConfig.RedirectCache(Response.Cache, true);
                    if (LocationTo.StartsWith("~/"))
                    {
                        url = VirtualPathUtility.ToAbsolute(LocationTo);
                    }
                    else if (LocationTo.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                        LocationTo.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        url = LocationTo;
                    }
                    else if (RelativeRoot)
                    {
                        url = LocationTo;
                    }
                    else
                    {
                        url = Request.ApplicationPath.TrimEnd('/') + routePath + "/" + LocationTo.TrimStart('/');
                    }
                }
                string query = Request.QueryString.ToString();
                Response.RedirectLocation = url + (!string.IsNullOrEmpty(query) ? "?" + query : "");
            }
        }
    }
}