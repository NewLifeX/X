using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using System.Web;
using NewLife.Collections;
using NewLife.Log;

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
            context.Error += OnError;
        }
        #endregion

        #region 业务处理
        /// <summary>是否需要处理</summary>
        /// <param name="req"></param>
        /// <param name="ex"></param>
        /// <returns></returns>
        protected virtual Boolean NeedProcess(HttpRequest req, Exception ex)
        {
            if (ex is ThreadAbortException) return false;
            if (ex is CryptographicException && ex.Message.Contains("填充无效")) return false;

            // 文件不存在的异常只出现一次
            if (ex is HttpException && (ex.Message.Contains("文件不存在") || ex.Message.Contains("Not Found") || ex.Message.Contains("未找到路径")))
            {
                var url = req.RawUrl;
                if (!url.IsNullOrEmpty())
                {
                    if (fileErrors.Contains(url)) return false;
                    fileErrors.Add(url);
                }
            }
            // 无效操作，句柄未初始化，不用出现
            if (ex is InvalidOperationException && ex.Message.Contains("句柄未初始化")) return false;

            return true;
        }

        ICollection<String> fileErrors = new HashSet<String>(StringComparer.OrdinalIgnoreCase);

        /// <summary>错误处理方法</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnError(Object sender, EventArgs e)
        {
            //var ctx = HttpContext.Current;
            var ctx = (sender as HttpApplication).Context;
            var Server = ctx.Server;
            var Request = ctx.Request;
            var Response = ctx.Response;

            // 上层可能清空错误，这里不要拦截
            var ex = Server.GetLastError();
            //if (ex == null) return;

            if (!NeedProcess(Request, ex)) return;

            var sb = Pool.StringBuilder.Get();
            if (ex != null) sb.AppendLine(ex.ToString());
            if (!String.IsNullOrEmpty(Request.UserHostAddress))
                sb.AppendFormat("来源：{0}\r\n", Request.UserHostAddress);
            if (!String.IsNullOrEmpty(Request.UserAgent))
                sb.AppendFormat("平台：{0}\r\n", Request.UserAgent);
            sb.AppendFormat("文件：{0}\r\n", Request.CurrentExecutionFilePath);
            sb.AppendFormat("访问：{0}\r\n", Request.Url);
            if (!String.IsNullOrEmpty(Request.UrlReferrer + ""))
                sb.AppendFormat("引用：{0}\r\n", Request.UrlReferrer);
            if (Request.ContentLength > 0)
                sb.AppendFormat("方式：{0} {1:n0}\r\n", Request.HttpMethod, Request.ContentLength);
            else
                sb.AppendFormat("方式：{0}\r\n", Request.HttpMethod);

            var id = Thread.CurrentPrincipal;
            if (id != null && id.Identity != null && !String.IsNullOrEmpty(id.Identity.Name))
                sb.AppendFormat("用户：{0}({1})\r\n", id.Identity.Name, id.Identity.AuthenticationType);

            XTrace.WriteLine(sb.Put(true));
        }
        #endregion
    }
}