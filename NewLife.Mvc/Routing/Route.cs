using System;
using System.Web;
using System.Web.SessionState;
using NewLife.Configuration;
using NewLife.Log;
using NewLife.Reflection;

namespace NewLife.Mvc
{
    /// <summary>Url路由处理器</summary>
    /// <remarks>
    ///
    /// </remarks>
    public class Route : IHttpModule
    {
        #region IHttpModule 成员

        /// <summary>销毁</summary>>
        public void Dispose()
        {
        }

        /// <summary>初始化仅执行一次,在不重新加载应用前</summary>>
        /// <param name="context"></param>
        public void Init(HttpApplication context)
        {
            context.BeginRequest += new EventHandler(context_BeginRequest);
            context.EndRequest += new EventHandler(app_EndRequest);
        }

        #endregion IHttpModule 成员

        private void context_BeginRequest(object sender, EventArgs e)
        {
            HttpApplication app = sender as HttpApplication;
            HttpRequest req = app.Request;

            RouteContext ctx = new RouteContext(req.Path.Substring(req.ApplicationPath.TrimEnd('/').Length));
            RouteContext.Current = ctx;
            IController controller = null;
            try
            {
                controller = ctx.RouteTo(null, RootModule, f => null);
            }
            catch (Exception ex)
            {
                if (LogException(app.Response, ex, "发生路由错误"))
                {
                    app.CompleteRequest();
                }
                else
                {
                    throw;
                }
            }
            if (!IgnoreRoute.IsIgnore(controller))
            {
                ctx.Routed = true;
                app.Context.RemapHandler(HttpHandlerWrap.Create(ctx, controller));
                return;
            }
        }

        private void app_EndRequest(object sender, EventArgs e)
        {
            RouteContext.Current = null; // 复位路由上下文
        }

        static ModuleRule[] _RootModule = { null };

        /// <summary>根路由配置,自动加载实现了IRouteConfig接口的类中配置的路由规则</summary>>
        public static ModuleRule RootModule
        {
            get
            {
                if (_RootModule[0] == null)
                {
                    lock (_RootModule)
                    {
                        if (_RootModule[0] == null)
                        {
                            RouteConfigManager cfg = new RouteConfigManager();
                            // 找到所有实现IRouteConfig接口的类
                            foreach (Type item in AssemblyX.FindAllPlugins(typeof(IRouteConfig)))
                            {
                                cfg.Load(item);
                            }
                            //cfg.RouteToFactory("", () => Service.Resolve<IControllerFactory>()); // 从对象容器中取默认控制器工厂
                            _RootModule[0] = new ModuleRule() { Config = cfg };
                        }
                    }
                }
                return _RootModule[0];
            }
        }

        /// <summary>
        /// 向指定Http响应写入异常标识信息,同时将异常以对应标识写入日志文件,方便根据异常标识查找异常信息
        ///
        /// 只在生产环境下模式下返回true,打开调试开关将返回false,方便调试时查错
        /// </summary>
        /// <param name="resp"></param>
        /// <param name="ex"></param>
        /// <param name="exceptName"></param>
        /// <returns></returns>
        public static bool LogException(HttpResponse resp, Exception ex, string exceptName)
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

        /// <summary>控制器路由调试开关,打开将会在路由和控制器执行期间发生异常时输出详细的异常信息,默认为false</summary>>
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

    /// <summary>将IController包装为IHttpHandler,用于给HttpContext.RemapHandler方法使用</summary>
    internal class HttpHandlerWrap : IHttpHandler
    {
        private IRouteContext Context;
        private IController Controller;

        protected HttpHandlerWrap(IRouteContext context, IController controller)
        {
            Context = context;
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
                Controller.ProcessRequest(Context);
            }
            catch (Exception ex)
            {
                // TODO 是否需要从对象容器中取得处理对象来处理异常
                Exception controllException = new Exception(string.Format("{0} {1}",
                    context.Request.HttpMethod, context.Request.Url), ex);
                if (Route.LogException(context.Response, controllException, "控制器运行时发生错误"))
                {
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>创建指定控制器实例的IHttpHandler包装实例,会根据需要创建可以读写Session的IHttpHandler实现</summary>>
        /// <param name="context"></param>
        /// <param name="controller"></param>
        /// <returns></returns>
        public static HttpHandlerWrap Create(IRouteContext context, IController controller)
        {
            if (controller is IReadOnlySessionState)
            {
                return new ReadOnlySession(context, controller);
            }
            else if (controller is IRequiresSessionState)
            {
                return new ReadWriteSession(context, controller);
            }
            else
            {
                return new HttpHandlerWrap(context, controller);
            }
        }

        /// <summary>提供只读访问HttpSession的HttpHandlerWrap子类</summary>>
        private class ReadOnlySession : HttpHandlerWrap, IReadOnlySessionState
        {
            public ReadOnlySession(IRouteContext context, IController controller) : base(context, controller) { }
        }

        /// <summary>提供读写访问HttpSession的HttpHandlerWrap子类</summary>>
        private class ReadWriteSession : HttpHandlerWrap, IRequiresSessionState
        {
            public ReadWriteSession(IRouteContext context, IController controller) : base(context, controller) { }
        }
    }
}