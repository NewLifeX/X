using System.Web;
using NewLife.Reflection;

namespace NewLife.Mvc
{
    /// <summary>
    /// 重定向工厂一般不需要自行实例化,使用RouteConfigManager.Redirect方法即可
    /// </summary>
    public class RedirectFactory : IControllerFactory
    {
        // TODO 完善文档

        Func<IController> newRedirectController;

        public RedirectFactory(string to, bool relativeRoot)
        {
            newRedirectController = () => new RedirectController() { LocationTo = to, RelativeRoot = relativeRoot };
        }

        public RedirectFactory(Func<RouteContext, string, string> func)
        {
            newRedirectController = () => new RedirectController() { LocationFunc = func };
        }

        public IController GetController(IRouteContext context)
        {
            return newRedirectController();
        }

        public void ReleaseController(IController handler)
        {
        }

        internal class RedirectController : GenericController, IController
        {
            internal Func<RouteContext, string, string> LocationFunc;
            internal string LocationTo;
            internal bool RelativeRoot;

            public void ProcessRequest(IRouteContext context)
            {
                Response.Clear();
                string url, routePath = "";

                RouteFrag? m = context.Module;
                bool flag = true;
                foreach (RouteFrag item in context)
                {
                    if (flag)
                    {
                        if (m != null && item.Related == m.Value.Related) flag = false;
                        routePath += item.Path;
                    }
                }
                // TODO 考虑默认允许客户端缓存重定向请求
                if (LocationFunc != null)
                {
                    Response.Status = "302 Found";
                    url = LocationFunc(context as RouteContext, routePath);
                }
                else
                {
                    Response.Status = "301 Moved Permanently";
                    if (LocationTo.StartsWith("~/"))
                    {
                        url = VirtualPathUtility.ToAbsolute(LocationTo);
                    }
                    else
                    {
                        url = "";
                        if (!RelativeRoot)
                        {
                            url = routePath;
                        }
                        url = Request.ApplicationPath.TrimEnd('/') + url + LocationTo;
                    }
                }
                string query = Request.QueryString.ToString();
                Response.RedirectLocation = url + (!string.IsNullOrEmpty(query) ? "?" + query : "");
            }

            public bool IsReusable
            {
                get { return true; }
            }
        }
    }
}