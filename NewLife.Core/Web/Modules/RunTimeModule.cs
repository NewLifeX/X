using System;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using NewLife.Log;

namespace NewLife.Web
{
    /// <summary>页面执行时间模块</summary>
    public class RunTimeModule : IHttpModule
    {
        #region IHttpModule Members
        void IHttpModule.Dispose() { }

        /// <summary>初始化模块，准备拦截请求。</summary>
        /// <param name="context"></param>
        void IHttpModule.Init(HttpApplication context)
        {
            if (!XTrace.Debug) return;

            context.BeginRequest += new EventHandler(OnBeginRequest);
            context.PostReleaseRequestState += new EventHandler(WriteRunTime);
        }

        void OnBeginRequest(Object sender, EventArgs e) => OnInit((sender as HttpApplication)?.Context);

        /// <summary>初始化</summary>
        protected virtual void OnInit(HttpContext context) { }
        #endregion

        #region 运行时输出
        /// <summary>执行时间字符串</summary>
        public static String RunTimeFormat { get; set; } = "页面执行时间{0}毫秒！";

        /// <summary>输出运行时间</summary>
        void WriteRunTime(Object sender, EventArgs e)
        {
            var ctx = (sender as HttpApplication)?.Context;
            var req = ctx.Request;
            var res = ctx.Response;

            if (ctx.Items["HasWrite"] is Boolean b && b) return;
            ctx.Items["HasWrite"] = true;

            //if (!IsWriteRunTime) return;

            if (!req.PhysicalPath.EndsWithIgnoreCase(".aspx")) return;

            //判断是否为Ajax 异步请求，以排除“Sys.WebForms.PageRequestManagerParserErrorException: 未能分析从服务器收到的消息 ”异常
            if (req.Headers["X-MicrosoftAjax"] != null || req.Headers["x-requested-with"] != null) return;

            //if (HasWrite) return;
            //HasWrite = true;

            // 只处理Page页面
            var page = ctx.Handler as Page;
            if (page == null) return;

            var str = Render(ctx);
            if (String.IsNullOrEmpty(str)) return;

            // 尝试找到页面，并在页面上写上信息
            if (page.FindControl("RunTime") is Literal lt)
                lt.Text = str;
            else
                res.Write(str);
        }

        /// <summary>输出</summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected virtual String Render(HttpContext context)
        {
            var ts = DateTime.Now - HttpContext.Current.Timestamp;

            return String.Format(RunTimeFormat, ts.TotalMilliseconds);
        }
        #endregion
    }
}