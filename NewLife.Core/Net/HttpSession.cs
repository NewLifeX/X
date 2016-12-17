using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NewLife.Net
{
    /// <summary>Http会话</summary>
    public class HttpSession : TcpSession
    {
        #region 属性
        /// <summary>Http方法</summary>
        public String Method { get; set; } = "GET";

        /// <summary>资源路径</summary>
        public String Uri { get; set; } = "/";

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
        #endregion

        #region 构造
        /// <summary>实例化增强TCP</summary>
        public HttpSession() : base()
        {
            Name = GetType().Name;
            Remote.Port = 80;

            //DisconnectWhenEmptyData = false;
        }

        internal HttpSession(ISocketServer server, Socket client)
            : base(server, client)
        {
        }
        #endregion

        #region 方法
        /// <summary>打开</summary>
        protected override Boolean OnOpen()
        {
            // 默认80端口
            if (!Active && Remote.Port == 0) Remote.Port = 80;

            return base.OnOpen();
        }
        #endregion

        #region 收发数据
        /// <summary>发送数据</summary>
        /// <remarks>
        /// 目标地址由<seealso cref="SessionBase.Remote"/>决定
        /// </remarks>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">偏移</param>
        /// <param name="count">数量</param>
        /// <returns>是否成功</returns>
        public override Boolean Send(Byte[] buffer, Int32 offset = 0, Int32 count = -1)
        {
            buffer = Make(buffer);
            return base.Send(buffer, offset, count);
        }

        internal override Boolean SendInternal(Byte[] buffer, IPEndPoint remote)
        {
            buffer = Make(buffer);
            return base.SendInternal(buffer, remote);
        }

        /// <summary>处理收到的数据</summary>
        /// <param name="stream"></param>
        /// <param name="remote"></param>
        internal override void OnReceive(Stream stream, IPEndPoint remote)
        {
            var buffer = Parse(stream.ReadBytes());
            stream = new MemoryStream(buffer);

            base.OnReceive(stream, remote);
        }
        #endregion

        #region Http封包解包
        private Byte[] Make(Byte[] buffer)
        {
            // 分解主机和资源
            var host = Remote.Host;
            var url = Uri;

            if (url.StartsWithIgnoreCase("http"))
            {
                var uri = new Uri(url);
                host = uri.Host;
                url = uri.PathAndQuery;
            }

            // 构建请求头部
            var sb = new StringBuilder();
            sb.AppendFormat("{0} {1} HTTP/1.1\r\n", Method, url);
            sb.AppendFormat("Host:{0}\r\n", host);

            if (Compressed) sb.AppendLine("Accept-Encoding:gzip, deflate");
            if (KeepAlive) sb.AppendLine("Connection:keep-alive");
            if (!UserAgent.IsNullOrEmpty()) sb.AppendFormat("User-Agent:{0}\r\n", UserAgent);

            // 内容长度
            if (buffer?.Length > 0) sb.AppendFormat("Content-Length:{0}\r\n", buffer.Length);

            sb.AppendLine();

            var header = sb.ToString().GetBytes();

            // 如果没有负载，直接返回
            if (buffer == null || buffer.Length == 0) return header;

            var ms = new MemoryStream();
            ms.Write(header);
            ms.Write(buffer);

            return ms.ToArray();
        }

        private Byte[] Parse(Byte[] buffer)
        {
            var p = (Int32)buffer.IndexOf("\r\n\r\n".GetBytes());
            if (p < 0) return buffer;

            // 截取
            var headers = buffer.ReadBytes(0, p).ToStr().Split("\r\n");
            buffer = buffer.ReadBytes(p + 4);

            return buffer;
        }
        #endregion
    }
}