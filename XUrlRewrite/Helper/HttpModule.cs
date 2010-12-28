using System;
using System.Web;
using XUrlRewrite.Configuration;
using NewLife.Log;


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
            UrlRewriteConfig cfg = Manager.GetConfig(app);
            if (null != cfg && cfg.Enabled)
            {
                String path = app.Request.AppRelativeCurrentExecutionFilePath.Substring(1);
                String query = app.Request.QueryString.ToString();
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
                    if (XTrace.Debug) XTrace.WriteLine("[XTemplate] Not found any matched:{0}", path);
                }
#endif
            }
        }
    }
}
