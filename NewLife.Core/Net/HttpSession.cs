using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using NewLife.Collections;
using NewLife.Data;

namespace NewLife.Net
{
    /// <summary>Http会话</summary>
    public class HttpSession : TcpSession
    {
        #region 属性
        /// <summary>是否WebSocket</summary>
        public Boolean IsWebSocket { get; set; }

        /// <summary>是否启用SSL</summary>
        public Boolean IsSSL { get; set; }

        /// <summary>Http方法</summary>
        public String Method { get; set; }

        /// <summary>资源路径</summary>
        public Uri Url { get; set; }

        /// <summary>用户代理</summary>
        public String UserAgent { get; set; }

        /// <summary>是否压缩</summary>
        public Boolean Compressed { get; set; }

        /// <summary>保持连接</summary>
        public Boolean KeepAlive { get; set; }

        /// <summary>头部集合</summary>
        public IDictionary<String, Object> Headers { get; set; } = new NullableDictionary<String, Object>(StringComparer.OrdinalIgnoreCase);

        /// <summary>状态码</summary>
        public HttpStatusCode StatusCode { get; set; }

        /// <summary>响应头</summary>
        public IDictionary<String, Object> ResponseHeaders { get; set; } = new NullableDictionary<String, Object>(StringComparer.OrdinalIgnoreCase);
        #endregion

        #region 构造
        /// <summary>实例化增强TCP</summary>
        public HttpSession() : base()
        {
            Name = GetType().Name;
            Remote.Port = 80;

            //DisconnectWhenEmptyData = false;
            ProcessAsync = false;
        }

        internal HttpSession(ISocketServer server, Socket client) : base(server, client)
        {
            // 添加过滤器
            if (SendFilter == null) SendFilter = new HttpResponseFilter { Session = this };
        }
        #endregion

        #region 方法
        /// <summary>打开</summary>
        protected override Boolean OnOpen()
        {
            // 默认80端口
            if (!Active && Remote.Port == 0) Remote.Port = 80;

            // 添加过滤器
            if (SendFilter == null) SendFilter = new HttpRequestFilter { Session = this };

            return base.OnOpen();
        }
        #endregion

        #region 收发数据
        private MemoryStream _cache;
        private DateTime _next;
        /// <summary>处理收到的数据</summary>
        /// <param name="pk"></param>
        /// <param name="remote"></param>
        internal override void ProcessReceive(Packet pk, IPEndPoint remote)
        {
            if (pk.Count == 0 && DisconnectWhenEmptyData)
            {
                Close("收到空数据");
                Dispose();

                return;
            }

            try
            {
                var client = _Server == null;
                // 客户端收到响应，服务端收到请求
                IDictionary<String, Object> rs = null;

                // 是否全新请求
                if (_next < DateTime.Now || _cache == null)
                {
                    // 分析头部
                    //ParseHeader(pk, client);
                    rs = HttpHelper.ParseHeader(pk);
                    if (client)
                    {
                        ResponseHeaders = rs;
                        StatusCode = (HttpStatusCode)rs["StatusCode"];
                    }
                    else
                    {
                        Headers = rs;
                        Method = rs["Method"] as String;
                        Url = rs["Url"] as Uri;
                    }

                    _cache = new MemoryStream();
                }

                if (pk.Count > 0) pk.WriteTo(_cache);
                _next = DateTime.Now.AddSeconds(1);

                // 如果长度不足
                var len = rs["Content-Length"].ToInt();
                if (len > 0 && _cache.Length < len) return;

                _cache.Position = 0;
                pk = new Packet(_cache.ReadBytes());
                _cache = null;

                if (client)
                    base.OnReceive(pk, remote);
                else
                    OnRequest(pk, remote);
            }
            catch (Exception ex)
            {
                if (!ex.IsDisposed()) OnError("OnReceive", ex);
            }
        }
        #endregion

        #region Http客户端
        class HttpRequestFilter : FilterBase
        {
            public HttpSession Session { get; set; }

            protected override Boolean OnExecute(FilterContext context)
            {
                var pk = context.Packet;
                var ss = Session;
                if (ss.Compressed) ss.Headers["Accept-Encoding"] = "gzip, deflate";
                if (ss.KeepAlive) ss.Headers["Connection"] = "keep-alive";
                if (!ss.UserAgent.IsNullOrEmpty()) ss.Headers["User-Agent"] = ss.UserAgent;
                pk = HttpHelper.MakeRequest(ss.Method, ss.Url, ss.Headers, pk);

                context.Packet = pk;
#if DEBUG
                Session.WriteLog(pk.ToStr());
#endif

                return true;
            }
        }
        #endregion

        #region Http服务端
        class HttpResponseFilter : FilterBase
        {
            public HttpSession Session { get; set; }

            protected override Boolean OnExecute(FilterContext context)
            {
                var pk = context.Packet;
                var ss = Session;
                pk = HttpHelper.MakeResponse(ss.StatusCode, ss.ResponseHeaders, pk);

                context.Packet = pk;
#if DEBUG
                Session.WriteLog(pk.ToStr());
#endif

                return true;
            }
        }

        /// <summary>收到请求</summary>
        /// <param name="pk"></param>
        /// <param name="remote"></param>
        /// <returns></returns>
        protected virtual Boolean OnRequest(Packet pk, IPEndPoint remote)
        {
            StatusCode = HttpStatusCode.OK;

            if (pk.Count > 0) return base.OnReceive(pk, remote);

            // 请求内容为空
            var html = "请求 {0} 内容为空！".F(Url);
            Send(new Packet(html.GetBytes()));

            return true;
        }
        #endregion
    }
}