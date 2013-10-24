using System;
using System.IO;
using System.Web;
using NewLife.Reflection;
using NewLife.Security;

namespace NewLife.Messaging
{
    /// <summary>Http消息提供者处理器。为消息提供者提供承载，核心是调用<see cref="M:HttpServerMessageProvider.Instance.Process"/></summary>
    public class HttpMessageProviderHandler : IHttpHandler
    {
        #region IHttpHandler 成员
        /// <summary>是否可重用。</summary>
        public bool IsReusable { get { return true; } }

        /// <summary>处理。</summary>
        /// <param name="context"></param>
        public virtual void ProcessRequest(HttpContext context)
        {
            HttpServerMessageProvider.Instance.Process();
        }
        #endregion
    }

    /// <summary>Http服务器消息提供者。单例模式，通过静态属性<see cref="Instance"/>访问单一实例。</summary>
    /// <remarks>不支持<see cref="M:SendAnReceive"/>方法</remarks>
    public class HttpServerMessageProvider : MessageProvider
    {
        #region 属性
        /// <summary>上下文</summary>
        public HttpContext Context { get { return HttpContext.Current; } }
        #endregion

        #region 静态实例
        private HttpServerMessageProvider() { }

        private static HttpServerMessageProvider _Instance;
        /// <summary>静态实例</summary>
        public static HttpServerMessageProvider Instance { get { return _Instance ?? (_Instance = new HttpServerMessageProvider()); } }
        #endregion

        /// <summary>发送数据流。</summary>
        /// <param name="stream"></param>
        protected override void OnSend(Stream stream) { Context.Response.BinaryWrite(stream.ReadBytes()); }

        /// <summary>已重载。不支持。</summary>
        /// <param name="message"></param>
        /// <param name="millisecondsTimeout"></param>
        /// <returns></returns>
        public override Message SendAndReceive(Message message, int millisecondsTimeout = 0)
        {
            throw new NotSupportedException();
        }

        /// <summary>处理。</summary>
        public void Process()
        {
            var context = Context;
            try
            {
                var s = context.Request.InputStream;
                if (s == null || s.Position >= s.Length)
                {
                    var d = context.Request.Url.Query;
                    if (!String.IsNullOrEmpty(d))
                    {
                        if (d.StartsWith("?")) d = d.Substring(1);
                        s = new MemoryStream(DataHelper.FromHex(d));
                    }
                }
                //var msg = Message.Read(s);
                //var msg = Message.Read(Context.Request.InputStream);

                // 支持多消息
                while (s.Position < s.Length) Process(Message.Read(s));
            }
            catch (Exception ex)
            {
                // 去掉内部异常，以免过大
                if (ex.InnerException != null) ex.SetValue("_innerException", null);
                var msg = new ExceptionMessage() { Value = ex };
                var data = msg.GetStream().ReadBytes();
                context.Response.BinaryWrite(data);
            }
        }
    }
}