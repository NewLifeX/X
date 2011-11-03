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
        /// 初始化仅执行一次,再不重新加载应用前
        /// </summary>
        /// <param name="context"></param>
        public void Init(HttpApplication context)
        {
            context.BeginRequest += new EventHandler(context_BeginRequest);
        }
        #endregion

        static RouteConfigManager[] rootConfig = new RouteConfigManager[] { null };

        void context_BeginRequest(object sender, EventArgs e)
        {
            HttpApplication app = sender as HttpApplication;

            if (rootConfig[0] == null)
            {
                lock (rootConfig)
                {
                    if (rootConfig[0] == null)
                    {
                        LoadRootConfig();
                    }
                }
            }
            RouteContext.Current = new RouteContext(app);
            string path = RouteContext.Current.RoutePath;
            IHttpHandler handler = rootConfig[0].GetRouteHandler(path);
            if (handler == null)
            {
                IControllerFactory factory = Service.Resolve<IControllerFactory>();
                if (factory.Support(path))
                {
                    handler = new HttpHandlerWrap(factory.Create());
                }
            }
            if (handler != null)
            {
                app.Context.RemapHandler(handler);
                return;
            }
            // TODO http 404?
        }

        void LoadRootConfig()
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
            rootConfig[0] = cfg;
        }
    }
}