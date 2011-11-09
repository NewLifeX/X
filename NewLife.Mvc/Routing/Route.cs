using System;
using System.Web;
using System.Web.SessionState;
using NewLife.Configuration;
using NewLife.Log;

namespace NewLife.Mvc
{
    /// <summary>Url路由处理器</summary>
    public class Route : IHttpModule
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
                            cfg.RouteToFactory("", () => Service.Resolve<IControllerFactory>()); // 从对象容器中取默认控制器工厂
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
            RouteContext.Current = new RouteContext(app); // 每次请求前重置路由上下文
            string path = RouteContext.Current.RoutePath;
            IController c = null;
            try
            {
                c = RootConfig.GetRouteHandler(path);
            }
            catch (Exception ex)
            {
                if (TryLogExceptionIfNonDebug(app.Response, ex, "发生路由错误"))
                {
                    app.CompleteRequest();
                }
                else
                {
                    throw;
                }
            }
            if (c != null)
            {
                app.Context.RemapHandler(HttpHandlerWrap.Create(c));
                return;
            }
        }

        /// <summary>
        /// 尝试将指定的异常信息写入到日志,如果当前是非Debug模式,Debug开关是NewLife.Mvc.Route.Debug配置项
        ///
        /// 返回是否已写入,非Debug模式会返回true
        /// </summary>
        /// <param name="resp"></param>
        /// <param name="ex"></param>
        /// <param name="exceptName"></param>
        /// <returns></returns>
        public static bool TryLogExceptionIfNonDebug(HttpResponse resp, Exception ex, string exceptName)
        {
            if (!Debug)
            {
                string logId = Guid.NewGuid().ToString();
                XTrace.WriteLine("{2} {0}\r\n{1}", logId, ex, exceptName);
                resp.Clear();
                resp.StatusCode = 500;
                resp.ContentType = "text/html";
                resp.Write(string.Format(@"{2},异常日志标识: <code style='font-weight:bold;'>{1} {0}</code>", logId, DateTime.Now, exceptName));
                return true;
            }
            return false;
        }

        private static bool? _Debug;

        /// <summary>
        /// 控制器路由调试开关,打开将会在路由和控制器执行期间发生异常时输出详细的异常信息,默认为false
        /// </summary>
        public static bool Debug
        {
            get
            {
                if (_Debug == null)
                {
                    _Debug = Config.GetConfig<bool>("NewLife.Mvc.Route.Debug", false);
                }
                return _Debug.Value;
            }
        }
    }

    /// <summary>
    /// 将IController包装为IHttpHandler,用于给HttpContext.RemapHandler方法使用
    /// </summary>
    internal class HttpHandlerWrap : IHttpHandler
    {
        private IController Controller;

        protected HttpHandlerWrap(IController controller)
        {
            Controller = controller;
        }

        public bool IsReusable
        {
            get { return false; }
        }

        public void ProcessRequest(HttpContext context)
        {
            try
            {
                Controller.Execute();
            }
            catch (Exception ex)
            {
                // TODO 是否需要从对象容器中取得处理对象来处理异常
                Exception controllException = new Exception(string.Format("{0} {1}",
                    context.Request.HttpMethod, context.Request.Url), ex);
                if (Route.TryLogExceptionIfNonDebug(context.Response, controllException, "控制器运行时发生错误"))
                {
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// 创建指定控制器实例的IHttpHandler包装实例,会根据需要创建可以读写Session的IHttpHandler实现
        /// </summary>
        /// <param name="controller"></param>
        /// <returns></returns>
        public static HttpHandlerWrap Create(IController controller)
        {
            if (controller is IReadOnlySessionState)
            {
                return new ReadOnlySession(controller);
            }
            else if (controller is IRequiresSessionState)
            {
                return new ReadWriteSession(controller);
            }
            else
            {
                return new HttpHandlerWrap(controller);
            }
        }

        /// <summary>
        /// 提供只读访问HttpSession的HttpHandlerWrap子类
        /// </summary>
        private class ReadOnlySession : HttpHandlerWrap, IReadOnlySessionState
        {
            public ReadOnlySession(IController controller) : base(controller) { }
        }

        /// <summary>
        /// 提供读写访问HttpSession的HttpHandlerWrap子类
        /// </summary>
        private class ReadWriteSession : HttpHandlerWrap, IRequiresSessionState
        {
            public ReadWriteSession(IController controller) : base(controller) { }
        }
    }
}