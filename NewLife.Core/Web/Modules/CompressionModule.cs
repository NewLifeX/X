#if !__CORE__
using System;
using System.ComponentModel;
using System.IO.Compression;
using System.Linq;
using System.Web;
using NewLife.Reflection;

namespace NewLife.Web
{
    /// <summary>页面压缩模块</summary>
    public class CompressionModule : IHttpModule
    {
        #region IHttpModule Members
        void IHttpModule.Dispose() { }

        /// <summary>初始化模块，准备拦截请求。</summary>
        /// <param name="context"></param>
        void IHttpModule.Init(HttpApplication context)
        {
            context.PostReleaseRequestState += CompressContent;
        }
        #endregion

        /// <summary>网页压缩文件</summary>
        [Description("网页压缩文件")]
        public String WebCompressFiles { get; set; } = ".aspx,.axd,.js,.css";

        #region Compression
        private const String GZIP = "gzip";
        private const String DEFLATE = "deflate";

        void CompressContent(Object sender, EventArgs e)
        {
            var ctx = (sender as HttpApplication)?.Context;
            if (!(ctx.CurrentHandler is System.Web.UI.Page) || ctx.Request["HTTP_X_MICROSOFTAJAX"] != null) return;

            var req = ctx.Request;
            var res = ctx.Response;

            // 如果已经写入头部，这里就不能压缩了
            if (res.TryGetValue("HeadersWritten", out var rs) && (Boolean)rs) return;
            // .net 2.0没有HeadersWritten

            //压缩
            var url = req.Url.OriginalString.ToLower();
            if (CanCompress(url))
            {
                //是否支持压缩协议
                if (IsEncodingAccepted(req, GZIP))
                {
                    res.Filter = new GZipStream(res.Filter, CompressionMode.Compress);
                    SetEncoding(res, GZIP);
                }
                else if (IsEncodingAccepted(req, DEFLATE))
                {
                    res.Filter = new DeflateStream(res.Filter, CompressionMode.Compress);
                    SetEncoding(res, DEFLATE);
                }
            }
        }

        private String[] exts;
        /// <summary>是否可压缩文件</summary>
        /// <param name="url"></param>
        /// <returns></returns>
        protected virtual Boolean CanCompress(String url)
        {
            if (exts == null)
            {
                //String files = Config.GetMutilConfig<String>(".aspx,.axd,.js,.css", "NewLife.Web.CompressFiles", "NewLife.CommonEntity.CompressFiles");
                var files = WebCompressFiles;
                exts = files.ToLower().Split(",", ";", " ");
            }
            return exts.Any(t => url.Contains(t));
        }

        /// <summary>检查请求头，确认客户端是否支持压缩编码</summary>
        /// <param name="req"></param>
        /// <param name="encoding"></param>
        private static Boolean IsEncodingAccepted(HttpRequest req, String encoding)
        {
            return req.Headers["Accept-encoding"] != null && req.Headers["Accept-encoding"].Contains(encoding);
        }

        /// <summary>添加压缩编码到响应头</summary>
        /// <param name="res"></param>
        /// <param name="encoding"></param>
        private static void SetEncoding(HttpResponse res, String encoding)
        {
            res.AppendHeader("Content-encoding", encoding);
        }
        #endregion
    }
}
#endif