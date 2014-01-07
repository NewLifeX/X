using System;
using System.Net;
using System.Web;
using NewLife.IO;
using NewLife.Reflection;

namespace NewLife.Web
{
    /// <summary>HTTP输入输出流</summary>
    public class HttpStream : ReadWriteStream
    {
        #region 属性
        private HttpContext _Context;
        /// <summary>HTTP上下文</summary>
        public HttpContext Context
        {
            get { return _Context; }
            private set { _Context = value; }
        }

        private IPEndPoint _RemoteEndPoint;
        /// <summary>远程地址</summary>
        public IPEndPoint RemoteEndPoint
        {
            get
            {
                if (_RemoteEndPoint == null && Context != null)
                {
                    IPAddress ip = IPAddress.Any;
                    Int32 port = 0;
                    if (IPAddress.TryParse(Context.Request.UserHostAddress, out ip))
                    {
                        // 尝试获取端口
                        try
                        {
                            var wr = Context.GetValue("WorkerRequest", false) as HttpWorkerRequest;
                            if (wr != null)
                            {
                                port = wr.GetRemotePort();
                            }
                        }
                        catch { }
                    }
                    _RemoteEndPoint = new IPEndPoint(ip, port);
                }
                return _RemoteEndPoint;
            }
            //set { _RemoteEndPoint = value; }
        }
        #endregion

        #region 构造
        /// <summary>初始化</summary>
        /// <param name="context"></param>
        public HttpStream(HttpContext context)
            : base(context.Request.InputStream, context.Response.OutputStream)
        {
            Context = context;
        }
        #endregion

        /// <summary>已重载。</summary>
        public override void Flush()
        {
            Context.Response.Flush();
        }
    }
}