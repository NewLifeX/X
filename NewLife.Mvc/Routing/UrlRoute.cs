using System;
using System.Web;

namespace NewLife.Mvc
{
    /// <summary>Url路由处理器</summary>
    public class UrlRoute : IHttpModule
    {
        #region IHttpModule 成员

        /// <summary>
        /// 销毁
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// 初始化仅执行一次,在不重新加载应用前
        /// </summary>
        /// <param name="context"></param>
        public void Init(HttpApplication context)
        {
            context.BeginRequest += new EventHandler(context_BeginRequest);
        }

        #endregion IHttpModule 成员

        static RouteConfigManager[] _RootConfig = new RouteConfigManager[] { null };

        /// <summary>
        /// 根路由配置,自动加载实现了IRouteConfig接口的类中配置的路由规则
        /// </summary>
        private static RouteConfigManager RootConfig
        {
            get
            {
                if (_RootConfig[0] == null)
                {
                    lock (_RootConfig)
                    {
                        if (_RootConfig[0] == null)
                        {
                            string exclude = @"mscorlib,
System.Web,System,System.Configuration,System.Xml,System.Web.resources,Microsoft.JScript,System.Data,System.Web.Services,
System.Drawing,System.EnterpriseServices,System.Web.Mobile,NewLife.Core,NewLife.Mvc,System.Runtime.Serialization,
System.IdentityModel,System.ServiceModel,System.ServiceModel.Web,System.WorkflowServices,System.resources,
System.Data.SqlXml,Microsoft.Vsa,System.Transactions,System.Design,System.Windows.Forms,";
                            RouteConfigManager cfg = new RouteConfigManager();
                            foreach (var ass in AppDomain.CurrentDomain.GetAssemblies())
                            {
                                string name = ass.FullName;
                                name = name.Substring(0, name.IndexOf(',') + 1);
                                if (exclude.Contains(name)) continue;
                                foreach (var type in ass.GetTypes())
                                {
                                    if (typeof(IRouteConfig).IsAssignableFrom(type))
                                    {
                                        cfg.Load(type);
                                    }
                                }
                            }
                            cfg.SortConfigRule();
                            _RootConfig[0] = cfg;
                        }
                    }
                }
                return _RootConfig[0];
            }
        }

        private void context_BeginRequest(object sender, EventArgs e)
        {
            HttpApplication app = sender as HttpApplication;
            RouteContext.Current = new RouteContext(app); //每次请求前重置路由上下文
            string path = RouteContext.Current.RoutePath;
            IController c = RootConfig.GetRouteHandler(path);
            if (c == null)
            {
                IControllerFactory factory = Service.Resolve<IControllerFactory>();
                if (factory.Support(path))
                {
                    c = factory.Create();
                }
            }
            if (c != null)
            {
                app.Context.RemapHandler(new HttpHandlerWrap(c));
                return;
            }
        }
    }

    /// <summary>
    /// 将IController包装为IHttpHandler,用于给HttpContext.RemapHandler方法使用
    /// </summary>
    internal class HttpHandlerWrap : IHttpHandler
    {
        private IController Controller;

        public HttpHandlerWrap(IController controller)
        {
            Controller = controller;
        }

        public bool IsReusable
        {
            get { return false; }
        }

        public void ProcessRequest(HttpContext context)
        {
            // TODO 考虑拦截异常,提供运行时和生产时的开关控制产生不同的异常报告以针对不同的用户
            Controller.Execute();
        }
    }
}