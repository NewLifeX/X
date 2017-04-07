using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
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
        public String Method { get; set; } = "GET";

        /// <summary>资源路径</summary>
        public Uri Url { get; set; }

        /// <summary>用户代理</summary>
        public String UserAgent { get; set; }

        /// <summary>是否压缩</summary>
        public Boolean Compressed { get; set; }

        /// <summary>保持连接</summary>
        public Boolean KeepAlive { get; set; }

        /// <summary>头部集合</summary>
        public WebHeaderCollection Headers { get; set; } = new WebHeaderCollection();

        /// <summary>状态码</summary>
        public HttpStatusCode StatusCode { get; set; }

        /// <summary>响应头</summary>
        public WebHeaderCollection ResponseHeaders { get; } = new WebHeaderCollection();
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
                var rs = client ? ResponseHeaders : Headers;

                // 是否全新请求
                if (_next < DateTime.Now || _cache == null)
                {
                    // 分析头部
                    ParseHeader(pk, client);

                    _cache = new MemoryStream();
                }

                if (pk.Count > 0) pk.WriteTo(_cache);
                _next = DateTime.Now.AddSeconds(15);

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
                var header = Session.MakeRequest(pk);

                // 合并
                var ms = new MemoryStream();
                ms.Write(header.GetBytes());
                if (pk.Count > 0) pk.WriteTo(ms);
                pk.Set(ms.ToArray());

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
                var header = Session.MakeResponse(pk);

                // 合并
                var ms = new MemoryStream();
                ms.Write(header.GetBytes());
                if (pk.Count > 0) pk.WriteTo(ms);
                pk.Set(ms.ToArray());
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

        #region Http封包解包
        private String MakeRequest(Packet pk)
        {
            if (pk?.Count > 0) Method = "POST";

            // 分解主机和资源
            var host = Remote.Host;
            var url = Url;
            if (url == null) url = new Uri("/");

            if (url.Scheme.EqualIgnoreCase("http"))
            {
                if (url.Port == 80)
                    host = url.Host;
                else
                    host = "{0}:{1}".F(url.Host, url.Port);
            }
            else if (url.Scheme.EqualIgnoreCase("https"))
            {
                if (url.Port == 443)
                    host = url.Host;
                else
                    host = "{0}:{1}".F(url.Host, url.Port);
            }

            // 构建头部
            var sb = new StringBuilder();
            sb.AppendFormat("{0} {1} HTTP/1.1\r\n", Method, url.PathAndQuery);
            sb.AppendFormat("Host:{0}\r\n", host);

            if (Compressed) sb.AppendLine("Accept-Encoding:gzip, deflate");
            if (KeepAlive) sb.AppendLine("Connection:keep-alive");
            if (!UserAgent.IsNullOrEmpty()) sb.AppendFormat("User-Agent:{0}\r\n", UserAgent);

            // 内容长度
            if (pk?.Count > 0) sb.AppendFormat("Content-Length:{0}\r\n", pk.Count);

            foreach (var item in Headers.AllKeys)
            {
                sb.AppendFormat("{0}:{1}\r\n", item, Headers[item]);
            }

            sb.AppendLine();

            return sb.ToString();
        }

        private String MakeResponse(Packet pk)
        {
            // 构建头部
            var sb = new StringBuilder();
            sb.AppendFormat("HTTP/1.1 {0} {1}\r\n", (Int32)StatusCode, StatusCode);

            // 内容长度
            if (pk?.Count > 0) sb.AppendFormat("Content-Length:{0}\r\n", pk.Count);

            foreach (var item in ResponseHeaders.AllKeys)
            {
                sb.AppendFormat("{0}:{1}\r\n", item, ResponseHeaders[item]);
            }

            sb.AppendLine();

            return sb.ToString();
        }

        private WebHeaderCollection ParseHeader(Packet pk, Boolean client)
        {
            // 客户端收到响应，服务端收到请求
            var rs = client ? ResponseHeaders : Headers;

            var p = (Int32)pk.Data.IndexOf(pk.Offset, pk.Count, "\r\n\r\n".GetBytes());
            if (p < 0) return rs;

#if DEBUG
            WriteLog(pk.ToStr());
#endif

            // 截取
            var headers = pk.Data.ReadBytes(pk.Offset, p).ToStr().Split("\r\n");
            // 重构
            p += 4;
            pk.Set(pk.Data, pk.Offset + p, pk.Count - p);

            // 分析头部
            rs.Clear();
            var line = headers[0];
            for (var i = 1; i < headers.Length; i++)
            {
                line = headers[i];
                p = line.IndexOf(':');
                if (p > 0) rs.Set(line.Substring(0, p), line.Substring(p + 1));
            }

            line = headers[0];
            var ss = line.Split(" ");
            // 分析请求方法 GET / HTTP/1.1
            if (ss.Length >= 3 && ss[2].StartsWithIgnoreCase("HTTP/"))
            {
                Method = ss[0];

                // 构造资源路径
                var host = rs[HttpRequestHeader.Host];
                var uri = "{0}://{1}".F(IsSSL ? "https" : "http", host);
                if (host.IsNullOrEmpty() || !host.Contains(":"))
                {
                    var port = Local.Port;
                    if (IsSSL && port != 443 || !IsSSL && port != 80) uri += ":" + port;
                }
                uri += ss[1];
                Url = new Uri(uri);
            }
            else
            {
                // 分析响应码
                var code = ss[1].ToInt();
                if (code > 0) StatusCode = (HttpStatusCode)code;
            }

            return rs;
        }
        #endregion
    }
}