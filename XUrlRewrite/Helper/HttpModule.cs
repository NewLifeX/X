using System;
using System.Web;
using NewLife.Log;
using NewLife.Reflection;
using XUrlRewrite.Configuration;
using System.Reflection;

namespace XUrlRewrite.Helper
{
    ///// <summary>
    ///// 处理模板请求的HttpModule,参考<see cref="ProcessTemplate(Object sender, EventArgs e)"/>
    ///// </summary>
    /// <summary>
    /// 处理模板请求的HttpModule
    /// </summary>
    public class HttpModule : IHttpModule
    {
        /// <summary>
        /// 您将需要在您网站的 web.config 文件中配置此模块，
        /// 并向 IIS 注册此模块，然后才能使用。有关详细信息，
        /// 请参见下面的链接: http://go.microsoft.com/?linkid=8101007
        /// </summary>

        #region IHttpModule Members

        public void Dispose()
        {
        }

        ///// <summary>
        ///// <see cref="IHttpModule.Init(HttpApplication context)"/>
        ///// </summary>
        ///// <param name="context"></param>
        /// <summary>
        ///
        /// </summary>
        /// <param name="context"></param>
        public void Init(HttpApplication context)
        {
            context.BeginRequest += ProcessTemplate;
        }

        #endregion

        /// <summary>
        /// 处理请求,符合模板中Url映射规则的就会重写Url
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ProcessTemplate(Object sender, EventArgs e)
        {
            HttpApplication app = sender as HttpApplication;
            Manager manager = Manager.GetConfigManager(app);
            UrlRewriteConfig cfg = manager.GetTemplateConfig();
            if (null != cfg && cfg.Enabled)
            {
                String path = app.Request.AppRelativeCurrentExecutionFilePath.Substring(1);
                String query = app.Request.QueryString.ToString();
                if (IsCustomFilterEnabled(manager, cfg, path, query, app))
                {
#if DEBUG
                Int32 debugInt = 0;
#endif
                    foreach (Object _url in cfg.Urls)
                    {
#if DEBUG
                    debugInt++;
#endif
                        if (_url is UrlElement)
                        {
                            UrlElement url = (UrlElement)_url;
                            if (url.Enabled && url.RewriteUrl(path, query, app, cfg))
                            {
                                break;
                            }
                        }
                    }
#if DEBUG
                if (debugInt==cfg.Urls.Count)
                {
                    if (Manager.Debug) XTrace.WriteLine("Not found any matched:{0}", path);
                }
#endif
                }
            }
        }

        static bool[] IsBindReload = { false };
        static Func<string, string, HttpApplication, bool>[] CustomFilterFunc = { null };
        static bool NeedLoadAssembly = true;

        /// <summary>
        /// 获取自定义过滤器是否通过
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="cfg"></param>
        /// <param name="path"></param>
        /// <param name="query"></param>
        /// <param name="app"></param>
        /// <returns></returns>
        private bool IsCustomFilterEnabled(Manager manager, UrlRewriteConfig cfg, string path, string query, HttpApplication app)
        {
            if (!IsBindReload[0])
            {
                lock (IsBindReload)
                {
                    if (!IsBindReload[0])
                    {
                        manager.LoadConfig += delegate(object sender1, EventArgs e1) { CustomFilterFunc[0] = null; };
                        IsBindReload[0] = true;
                    }
                }
            }

            if (CustomFilterFunc[0] == null)
            {
                lock (CustomFilterFunc)
                {
                    if (CustomFilterFunc[0] == null)
                    {
                        CustomFilterFunc[0] = GetCustomFilterFunc(cfg.CustomFilter);
                    }
                }
            }

            if (CustomFilterFunc[0] == null) return true;
            try
            {
                return CustomFilterFunc[0](path, query, app);
            }
            catch (Exception ex)
            {
                if (Manager.Debug) XTrace.WriteLine("UrlRewrite执行cfg.CustomFilter时发生了异常\r\n{0}", ex);
                return false;
            }
        }

        private static Func<string, string, HttpApplication, bool> GetCustomFilterFunc(string method)
        {
            if (string.IsNullOrEmpty(method)) return EmptyCustomFilterFunc;

            int n = method.LastIndexOf('.');
            string typeName = method.Substring(0, n);
            method = method.Substring(n + 1);

            Type type = null;
            try
            {
                type = TypeX.GetType(typeName, NeedLoadAssembly);
                NeedLoadAssembly = false;
            }
            catch (Exception) { }
            if (type == null)
            {
                if (Manager.Debug) XTrace.WriteLine("Url重写配置的自定义过滤器类型{0}不存在,或不是有效的类", typeName);
                return EmptyCustomFilterFunc;
            }

            MethodInfoX methodInfoX = null;
            try
            {
                methodInfoX = MethodInfoX.Create(type, method, new Type[] { typeof(string), typeof(string), typeof(HttpApplication) });
            }
            catch (Exception) { }
            if (methodInfoX == null)
            {
                if (Manager.Debug) XTrace.WriteLine("Url重写配置的自定义过滤器方法static bool {0}(string, string, HttpApplication)不存在", method);
                return EmptyCustomFilterFunc;
            }

            if (!methodInfoX.Method.IsStatic)
            {
                if (Manager.Debug) XTrace.WriteLine("Url重写配置的自定义过滤器方法不是静态方法", method);
                return EmptyCustomFilterFunc;
            }

            if (methodInfoX.Method.ReturnType != typeof(bool))
            {
                if (Manager.Debug) XTrace.WriteLine("Url重写配置的自定义过滤器方法返回值不是bool", method);
                return EmptyCustomFilterFunc;
            }

            return delegate(string path, string query, HttpApplication app)
            {
                return (bool)methodInfoX.Invoke(null, path, query, app);
            };
        }

        private static bool EmptyCustomFilterFunc(string path, string query, HttpApplication app)
        {
            return true;
        }
    }
}