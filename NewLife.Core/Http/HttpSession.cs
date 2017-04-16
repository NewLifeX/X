using System;
using System.Net;
using System.Net.Sockets;
using NewLife.Data;
using NewLife.Net;

namespace NewLife.Http
{
    /// <summary>Http会话</summary>
    public class HttpSession : TcpSession
    {
        #region 属性
        /// <summary>请求</summary>
        public HttpRequest Request { get; set; }

        /// <summary>响应</summary>
        public HttpResponse Response { get; set; }
        #endregion

        #region 构造
        internal HttpSession(ISocketServer server, Socket client) : base(server, client)
        {
            Name = GetType().Name;
            Remote.Port = 80;

            //DisconnectWhenEmptyData = false;
            ProcessAsync = false;

            // 添加过滤器
            if (SendFilter == null) SendFilter = new HttpResponseFilter { Session = this };
        }
        #endregion

        #region 收发数据
        /// <summary>处理收到的数据</summary>
        /// <param name="pk"></param>
        /// <param name="remote"></param>
        protected override Boolean OnReceive(Packet pk, IPEndPoint remote)
        {
            /*
             * 解析流程：
             *  首次访问或过期，创建请求对象
             *      判断头部是否完整
             *          --判断主体是否完整
             *              触发新的请求
             *          --加入缓存
             *      加入缓存
             *  触发收到数据
             */

            var header = Request;

            // 是否全新请求
            if (header == null || !header.IsWebSocket && (header.Expire < DateTime.Now || header.IsCompleted))
            {
                Request = new HttpRequest { Expire = DateTime.Now.AddSeconds(5) };
                Response = new HttpResponse();
                header = Request;

                // 分析头部
                header.ParseHeader(pk);
#if DEBUG
                WriteLog(" {0} {1}", header.Method, header.Url);
#endif
            }

            // 增加主体长度
            header.BodyLength += pk.Count;

            // WebSocket
            if (CheckWebSocket(ref pk, remote)) return true;

            if (!header.ParseBody(ref pk)) return true;

            base.OnReceive(pk, remote);

            // 如果还有响应，说明还没发出
            var rs = Response;
            if (rs == null) return true;

            // 请求内容为空
            //var html = "请求 {0} 内容未处理！".F(Request.Url);
            var html = "{0} {1} {2}".F(Request.Method, Request.Url, DateTime.Now);
            Send(new Packet(html.GetBytes()));

            return true;
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

                if (ss.Request.IsWebSocket) pk = HttpHelper.MakeWS(pk);

                //pk = HttpHelper.MakeResponse(ss.StatusCode, ss.Response, pk);
                pk = ss.Response.Build(pk);
                ss.Response = null;

                context.Packet = pk;
#if DEBUG
                //Session.WriteLog(pk.ToStr());
#endif

                return true;
            }
        }
        #endregion

        #region WebSocket
        /// <summary>检查WebSocket</summary>
        /// <param name="pk"></param>
        /// <param name="remote"></param>
        /// <returns></returns>
        protected virtual Boolean CheckWebSocket(ref Packet pk, IPEndPoint remote)
        {
            if (!Request.IsWebSocket)
            {
                var key = Request["Sec-WebSocket-Key"];
                if (key.IsNullOrEmpty()) return false;

                Request.IsWebSocket = true;
                DisconnectWhenEmptyData = false;

                pk = HttpHelper.HandeShake(key);
                if (pk != null) Send(pk);
            }
            else
            {
                pk = HttpHelper.ParseWS(pk);
            }

            return true;
        }
        #endregion
    }
}