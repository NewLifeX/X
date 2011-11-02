using System;
using System.IO.Compression;
using System.Linq;
using System.Web;
using NewLife.Configuration;
using NewLife.Reflection;

namespace NewLife.Web
{
    /// <summary>页面压缩模块</summary>
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

            // 如果已经写入头部，这里就不能压缩了
            if (PropertyInfoX.GetValue<Boolean>(app.Response, "HeadersWritten")) return;

            //压缩
            String url = app.Request.Url.OriginalString.ToLower();
            String files = Config.GetConfig<String>("NewLife.CommonEntity.CompressFiles", ".aspx,.axd,.js,.css");
            if (files.ToLower().Split(new String[] { ",", ";", " " }, StringSplitOptions.RemoveEmptyEntries).Any(t => url.Contains(t)))
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
        /// 检查请求头，确认客户端是否支持压缩编码
        /// </summary>
        private static bool IsEncodingAccepted(string encoding)
        {
            return HttpContext.Current.Request.Headers["Accept-encoding"] != null && HttpContext.Current.Request.Headers["Accept-encoding"].Contains(encoding);
        }

        /// <summary>
        /// 添加压缩编码到响应头
        /// </summary>
        /// <param name="encoding"></param>
        private static void SetEncoding(string encoding)
        {
            HttpContext.Current.Response.AppendHeader("Content-encoding", encoding);
        }
        #endregion
    }
}