using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Web;
using NewLife.Log;
using System.Collections.Generic;
using NewLife.Collections;
using System.IO;

namespace NewLife.Web
{
    /// <summary>全局错误处理模块</summary>
    public class ErrorModule : IHttpModule
    {
        #region IHttpModule Members
        void IHttpModule.Dispose() { }

        /// <summary>初始化模块，准备拦截请求。</summary>
        /// <param name="context"></param>
        void IHttpModule.Init(HttpApplication context)
        {
            context.Error += new EventHandler(OnError);
        }
        #endregion

        #region 业务处理
        /// <summary>是否需要处理</summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        protected virtual Boolean NeedProcess(Exception ex)
        {
            if (ex is ThreadAbortException) return false;
            if (ex is CryptographicException && ex.Message.Contains("填充无效")) return false;

            // 文件不存在的异常只出现一次
            if (ex is HttpException && (ex.Message.Contains("文件不存在") || ex.Message.Contains("Not Found")))
            {
                var url = HttpContext.Current.Request.RawUrl;
                if (!String.IsNullOrEmpty(url))
                {
                    if (fileErrors.Contains(url)) return false;
                    fileErrors.Add(url);
                }
            }

            return true;
        }

        ICollection<String> fileErrors = new HashSet<String>(StringComparer.OrdinalIgnoreCase);

        /// <summary>错误处理方法</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnError(object sender, EventArgs e)
        {
            var Server = HttpContext.Current.Server;
            var Request = HttpContext.Current.Request;
            var Response = HttpContext.Current.Response;

            var ex = Server.GetLastError();
            if (ex == null) return;

            if (!NeedProcess(ex)) return;

            var sb = new StringBuilder();
            sb.AppendLine(ex.ToString());
            sb.AppendFormat("来源：{0}\r\n", Request.UserHostAddress);
            sb.AppendFormat("平台：{0}\r\n", Request.UserAgent);
            sb.AppendFormat("访问：{0}\r\n", Request.RawUrl);
            sb.AppendFormat("引用：{0}\r\n", Request.UrlReferrer);

            var id = Thread.CurrentPrincipal;
            if (id != null && id.Identity != null) sb.AppendFormat("用户：{0}({1})\r\n", id.Identity.Name, id.Identity.AuthenticationType);

            XTrace.WriteLine(sb.ToString());

            OnErrorComplete();
        }

        /// <summary>错误处理完成后执行。一般用于输出友好错误信息</summary>
        protected virtual void OnErrorComplete()
        {
            if (!XTrace.Debug)
            {
                var Server = HttpContext.Current.Server;
                var Response = HttpContext.Current.Response;

                Server.ClearError();
                Response.Write("非常抱歉，服务器遇到错误，请与管理员联系！");
            }
        }
        #endregion
    }
}