using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using NewLife.Log;
using NewLife.Model;
using NewLife.Web;
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
            context.AcquireRequestState += OnSession;
            //context.PreRequestHandlerExecute += OnSession;
            context.PostRequestHandlerExecute += OnPost;
            context.Error += OnPost;
        }

        private void OnSession(Object sender, EventArgs e)
        {
            // 会话状态已创建一个会话 ID，但由于响应已被应用程序刷新而无法保存它
            // 避免后面使用SessionID时报错
            var sid = HttpContext.Current?.Session?.SessionID;
        }
        #endregion

        /// <summary>Web在线</summary>
        public static Boolean WebOnline { get; set; }

        /// <summary>访问记录</summary>
        public static Boolean WebBehavior { get; set; }

        void OnPost(Object sender, EventArgs e)
        {
            if (!WebOnline && !WebBehavior) return;

            var ctx = HttpContext.Current;
            if (ctx == null) return;

            var req = ctx.Request;
            if (req == null) return;

            var user = ctx.User?.Identity as IManageUser;
            var userid = user != null ? user.ID : 0;

            var ip = WebHelper.UserHost;

            try
            {
                var page = GetPage(req);
                var msg = GetMessage(ctx, req);

                // 统计网页状态
                if (WebOnline && ctx.Session != null) UserOnline.SetWebStatus(ctx.Session.SessionID, page, msg, user, ip);

                // 记录用户访问的Url
                if (WebBehavior) SaveBehavior(ctx, req, userid, ip, page, msg);
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
        }

        String GetMessage(HttpContext ctx, HttpRequest req)
        {
            var title = GetTitle(ctx, req);
            var msg = "{0} {1}".F(req.HttpMethod, req.RawUrl);
            if (!title.IsNullOrEmpty()) msg = title + " " + msg;

            var err = ctx.Error?.Message;
            if (!err.IsNullOrEmpty()) msg += " " + err;

            return msg;
        }

        String GetTitle(HttpContext ctx, HttpRequest req)
        {
            var title = ctx.Items["Title"] + "";
            if (title.IsNullOrEmpty())
            {
                if (ctx.Handler is Page page) title = page.Title;
            }

            // 有些标题是 Description，需要截断处理
            if (title.Contains(",")) title = title.Substring(null, ",");
            if (title.Contains("，")) title = title.Substring(null, "，");
            if (title.Contains("。")) title = title.Substring(null, "。");

            return title;
        }

        String GetPage(HttpRequest req)
        {
            var p = req.Path;
            if (p.IsNullOrEmpty()) return null;

            var ss = p.Split('/');
            if (ss.Length == 0) return p;

            // 如果最后一段是数字，则可能是参数，需要去掉
            if (ss[ss.Length - 1].ToInt() > 0) p = "/" + ss.Take(ss.Length - 1).Join("/");

            return p;
        }

        /// <summary>忽略的后缀</summary>
        public static HashSet<String> ExcludeSuffixes { get; set; } = new HashSet<String>(StringComparer.OrdinalIgnoreCase) {
            ".js", ".css", ".png", ".jpg", ".gif", ".ico",  // 脚本样式图片
            ".woff", ".woff2", ".svg", ".ttf", ".otf", ".eot"   // 字体
        };

        void SaveBehavior(HttpContext ctx, HttpRequest req, Int32 userid, String ip, String page, String msg)
        {
            if (page.IsNullOrEmpty()) return;

            // 过滤后缀
            var ext = Path.GetExtension(page);
            if (!ext.IsNullOrEmpty() && ExcludeSuffixes.Contains(ext)) return;

            LogProvider.Provider.WriteLog("访问", "记录", msg);

            var title = GetTitle(ctx, req);
            var ts = DateTime.Now - ctx.Timestamp;

            // 访问统计
            VisitStat.Add(page, title, (Int32)ts.TotalMilliseconds, userid, ip, ctx.Error?.Message);
        }
    }
}