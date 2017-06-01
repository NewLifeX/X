using System;
using System.Net;
using System.Threading.Tasks;
using NewLife.Data;
using NewLife.Net;

namespace NewLife.Http
{
    /// <summary>Http客户端</summary>
    public class HttpClient : TcpSession
    {
        #region 属性
        /// <summary>是否WebSocket</summary>
        public Boolean IsWebSocket { get; set; }

        /// <summary>是否启用SSL</summary>
        public Boolean IsSSL { get; set; }

        ///// <summary>Http方法</summary>
        //public String Method { get; set; }

        ///// <summary>资源路径</summary>
        //public Uri Url { get; set; }

        /// <summary>请求</summary>
        public HttpRequest Request { get; set; } = new HttpRequest();

        /// <summary>响应</summary>
        public HttpResponse Response { get; set; }
        #endregion

        #region 构造
        /// <summary>实例化增强TCP</summary>
        public HttpClient() : base()
        {
            Name = GetType().Name;
            Remote.Port = 80;

            //DisconnectWhenEmptyData = false;
            ProcessAsync = false;
        }
        #endregion

        #region 方法
        private Boolean handshake;

        /// <summary>打开</summary>
        /// <returns></returns>
        public override Boolean Open()
        {
            if (!base.Open()) return false;

            // WebSocket此时需要发送握手
            if (IsWebSocket)
            {
                Task.Run(() =>
                {
                    handshake = true;
                    try
                    {
                        WriteLog("Handshake");
                        HttpHelper.MakeHandshake(Request);

                        //var pk = Request.Build(null);
                        SendAsync(new Byte[0]).Wait();
                    }
                    catch
                    {
                        Active = false;
                        Close("Handshake");
                        throw;
                    }
                    finally
                    {
                        handshake = false;
                    }
                });
            }

            return true;
        }

        /// <summary>打开</summary>
        protected override Boolean OnOpen()
        {
            // 默认80端口
            if (!Active && Remote.Port == 0) Remote.Port = 80;

            if (Remote.Address.IsAny()) Remote = new NetUri(Request.Url + "");

            //Request.Method = Method;
            //Request.Url = Url;

            // 添加过滤器
            if (SendFilter == null) SendFilter = new HttpRequestFilter { Client = this };

            return base.OnOpen();
        }
        #endregion

        #region 收发数据
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

            try
            {
                var header = Response;

                // 是否全新请求
                if (header == null || !IsWebSocket && (header.Expire < DateTime.Now || header.IsCompleted))
                {
                    Response = new HttpResponse { Expire = DateTime.Now.AddSeconds(5) };
                    header = Response;

                    // 分析头部
                    header.ParseHeader(pk);
#if DEBUG
                    WriteLog(" {0} {1} {2}", (Int32)header.StatusCode, header.StatusCode, header.ContentLength);
#endif
                }

                if (IsWebSocket || header.ParseBody(ref pk)) OnReceive(pk, remote);
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
            public HttpClient Client { get; set; }

            protected override Boolean OnExecute(FilterContext context)
            {
                var pk = context.Packet;

                pk = Client.Request.Build(pk);

                context.Packet = pk;
#if DEBUG
                //Session.WriteLog(pk.ToStr());
#endif

                return true;
            }
        }
        #endregion
    }
}