using System;
using System.IO;
using System.Reflection;
using System.Web;
using NewLife.IO;

namespace NewLife.Web
{
    /// <summary>系统启动模块</summary>
    public class SystemStartModule : IHttpModule
    {
        #region IHttpModule Members
        void IHttpModule.Dispose() { }

        /// <summary>初始化模块，准备拦截请求。</summary>>
        /// <param name="context"></param>
        void IHttpModule.Init(HttpApplication context)
        {
            CheckStarting();
        }
        #endregion

        #region 系统启动中
        static Boolean SystemStarted = false;
        /// <summary>检查系统是否启动中，如果启动中，则显示进度条</summary>>
        public static void CheckStarting()
        {
            if (SystemStarted) return;
            SystemStarted = true;

            if (HttpContext.Current == null) return;

            HttpRequest Request = HttpContext.Current.Request;
            HttpResponse Response = HttpContext.Current.Response;

            // 在用Flush前用一次Session，避免可能出现的问题
            String sessionid = HttpContext.Current.Session.SessionID;

            // 只处理GET，因为处理POST可能丢失提交的表单数据
            if (Request.HttpMethod != "GET") return;

            // 读取资源，输出脚本
            Stream stream = FileSource.GetFileResource(Assembly.GetExecutingAssembly(), "SystemStart.htm");
            if (stream == null || stream.Length <= 0) return;

            StreamReader reader = new StreamReader(stream);
            Response.Write(reader.ReadToEnd());
            Response.Flush();
            Response.End();
        }
        #endregion
    }
}