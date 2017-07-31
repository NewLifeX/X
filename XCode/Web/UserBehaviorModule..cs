using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.UI;
using NewLife.Log;
using XCode.Membership;

namespace XCode.Web
{
    /// <summary>用户行为模块，在线和操作记录</summary>
    public class UserBehaviorModule : IHttpModule
    {
        #region IHttpModule Members
        void IHttpModule.Dispose() { }

        /// <summary>初始化模块，准备拦截请求。</summary>
        /// <param name="context"></param>
        void IHttpModule.Init(HttpApplication context)
        {
            context.PostRequestHandlerExecute += OnPost;
        }
        #endregion

        /// <summary>输出运行时间</summary>
        void OnPost(Object sender, EventArgs e)
        {
            try
            {
                var set = Setting.Current;

                // 统计网页状态
                if (set.WebOnline) UserOnline.SetWebStatus();

                // 记录用户访问的Url
                if (set.WebBehavior) SaveBehavior();
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
        }

        /// <summary>忽略的后缀</summary>
        public static HashSet<String> ExcludeSuffixes { get; set; } = new HashSet<String>(StringComparer.OrdinalIgnoreCase) {
            ".js", ".css", ".png", ".jpg", ".gif", ".ico",  // 脚本样式图片
            ".woff", ".woff2", ".svg", ".ttf", ".otf", ".eot"   // 字体
        };

        void SaveBehavior()
        {
            var ctx = HttpContext.Current;
            if (ctx == null) return;

            var req = ctx.Request;
            if (req == null) return;

            var p = req.Path;
            if (p.IsNullOrEmpty()) return;

            // 过滤后缀
            var ext = Path.GetExtension(p);
            if (!ext.IsNullOrEmpty() && ExcludeSuffixes.Contains(ext)) return;

            var title = ctx.Items["Title"] + "";
            //if (title.IsNullOrEmpty())
            //{
            //    var route = ctx.Handler as MvcHandler;
            //    if (route != null) title = route + "";
            //}

            if (title.IsNullOrEmpty())
            {
                var page = ctx.Handler as Page;
                if (page != null) title = page.Title;
            }

            var msg = "{0} {1}".F(req.HttpMethod, req.RawUrl);
            if (!title.IsNullOrEmpty()) msg = title + " " + msg;
            LogProvider.Provider.WriteLog("访问", "记录", msg);
        }
    }
}