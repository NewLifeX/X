using System;
using System.Collections.Generic;
using System.Text;
using System.Web;

namespace NewLife.Web
{
    /// <summary>页面执行时间模块</summary>
    public class RunTimeModule : IHttpModule
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