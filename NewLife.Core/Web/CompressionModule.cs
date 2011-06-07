using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.IO.Compression;
using NewLife.Configuration;

namespace NewLife.Web
{
    /// <summary>
    /// 页面压缩模块
    /// </summary>
    public class CompressionModule : IHttpModule
    {
        #region IHttpModule Members
        void IHttpModule.Dispose() { }

        /// <summary>
        /// 初始化模块，准备拦截请求。
        /// </summary>
        /// <param name="context"></param>
        void IHttpModule.Init(HttpApplication context)
        {
            context.PostReleaseRequestState += new EventHandler(CompressContent);
        }
        #endregion

        #region Compression
        private const string GZIP = "gzip";
        private const string DEFLATE = "deflate";

        void CompressContent(object sender, EventArgs e)
        {
            HttpApplication app = sender as HttpApplication;
            if (!(app.Context.CurrentHandler is System.Web.UI.Page) || app.Request["HTTP_X_MICROSOFTAJAX"] != null) return;

            //压缩
            String url = app.Request.Url.OriginalString.ToLower();
            String files = Config.GetConfig<String>("NewLife.CommonEntity.CompressFiles", ".aspx,.axd,.js,.css");
            Boolean b = false;
            foreach (String item in files.Split(new String[] { ",", ";", " " }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (url.Contains(item))
                {
                    b = true;
                    break;
                }
            }
            if (b)
            {
                //是否支持压缩协议
                if (IsEncodingAccepted(GZIP))
                {
                    app.Response.Filter = new GZipStream(app.Response.Filter, CompressionMode.Compress);
                    SetEncoding(GZIP);
                }
                else if (IsEncodingAccepted(DEFLATE))
                {
                    app.Response.Filter = new DeflateStream(app.Response.Filter, CompressionMode.Compress);
                    SetEncoding(DEFLATE);
                }
            }
        }

        /// <summary>
        /// Checks the request headers to see if the specified
        /// encoding is accepted by the client.
        /// </summary>
        private static bool IsEncodingAccepted(string encoding)
        {
            return HttpContext.Current.Request.Headers["Accept-encoding"] != null && HttpContext.Current.Request.Headers["Accept-encoding"].Contains(encoding);
        }

        /// <summary>
        /// Adds the specified encoding to the response headers.
        /// </summary>
        /// <param name="encoding"></param>
        private static void SetEncoding(string encoding)
        {
            HttpContext.Current.Response.AppendHeader("Content-encoding", encoding);
        }
        #endregion
    }
}