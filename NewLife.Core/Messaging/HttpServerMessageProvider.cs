using System;
using System.Web;
using NewLife.IO;
using System.IO;
using NewLife.Security;

namespace NewLife.Messaging
{
    /// <summary>Http消息提供者处理器。为消息提供者提供承载</summary>
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

    /// <summary>Http服务器消息提供者</summary>
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

        /// <summary>发送消息。如果有响应，可在消息到达事件中获得。</summary>
        /// <param name="message"></param>
        public override void Send(Message message)
        {
            var rs = Context.Response;
            rs.BinaryWrite(message.GetStream().ReadBytes());
        }

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
                var msg = Message.Read(s);
                //var msg = Message.Read(Context.Request.InputStream);
                Process(msg);
            }
            catch (Exception ex)
            {
                var msg = new ExceptionMessage() { Value = ex };
                var data = msg.GetStream().ReadBytes();
                context.Response.BinaryWrite(data);
            }
        }
    }
}