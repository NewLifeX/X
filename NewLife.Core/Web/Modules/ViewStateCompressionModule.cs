using System;
using System.Collections.Generic;
using System.Text;
using System.Web;

namespace NewLife.Web
{
    /// <summary>ViewState压缩模块</summary>
    public class ViewStateCompressionModule : IHttpModule
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

        void CompressContent(object sender, EventArgs e)
        {

        }

    }
}