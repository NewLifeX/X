using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using NewLife.Log;

namespace NewLife.Net.Tcp
{
    /// <summary>TCP服务器</summary>
    /// <remarks>
    /// 核心工作：启动服务<see cref="Start"/>时，监听端口，并启用多个（逻辑处理器数的10倍）异步接受操作<see cref="AcceptAsync"/>。
    /// 
    /// 服务器完全处于异步工作状态，任何操作都不可能被阻塞。
    /// 
    /// 注意：服务器接受连接请求后，不会开始处理数据，而是由<see cref="Accepted"/>事件订阅者决定何时开始处理数据。
    /// </remarks>
    public class TcpServer : DisposeBase, ISocketServer
    {
        #region 属性
        private NetUri _Local = new NetUri(ProtocolType.Tcp, IPAddress.Any, 0);
        /// <summary>本地绑定信息</summary>
        public NetUri Local { get { return _Local; } set { _Local = value; } }

        /// <summary>端口</summary>
        public Int32 Port { get { return _Local.Port; } set { _Local.Port = value; } }

        private Int32 _MaxNotActive = 30;
        /// <summary>最大不活动时间。
        /// 对于每一个会话连接，如果超过该时间仍然没有收到任何数据，则断开会话连接。
        /// 单位秒，默认30秒。时间不是太准确，建议15秒的倍数。为0表示不检查。</summary>
        public Int32 MaxNotActive { get { return _MaxNotActive; } set { _MaxNotActive = value; } }

        private Boolean _AutoReceiveAsync = true;
        /// <summary>自动开始会话的异步接收。
        /// 接受连接请求后，自动开始会话的异步接收，默认打开，如果会话需要同步接收数据，需要关闭该选项。</summary>
        public Boolean AutoReceiveAsync { get { return _AutoReceiveAsync; } set { _AutoReceiveAsync = value; } }

        private TcpListener _Server;
        /// <summary>服务器</summary>
        public TcpListener Server { get { return _Server; } set { _Server = value; } }

        /// <summary>底层Socket</summary>
        Socket ISocket.Socket { get { return _Server == null ? null : _Server.Server; } }

        private Boolean _Active;
        /// <summary>是否活动</summary>
        public Boolean Active { get { return _Active; } set { _Active = value; } }
        #endregion

        #region 构造
        /// <summary>构造TCP服务器对象</summary>
        public TcpServer() { }

        /// <summary>构造TCP服务器对象</summary>
        /// <param name="port"></param>
        public TcpServer(Int32 port) { Port = port; }
        #endregion

        #region 开始停止
        /// <summary>开始</summary>
        public virtual void Start()
        {
            // 开始监听
            if (Server == null) Server = new TcpListener(Local.EndPoint);

            // 三次握手之后，Accept之前的总连接个数，队列满之后，新连接将得到主动拒绝ConnectionRefused错误
            // 在我（大石头）的开发机器上，实际上这里的最大值只能是200，大于200跟200一个样
            Server.Start();

            AcceptAsync();
        }

        /// <summary>停止</summary>
        public virtual void Stop()
        {
            if (Server != null) Server.Stop();
            Server = null;
        }
        #endregion

        #region 连接处理
        /// <summary>连接完成。在事件处理代码中，事件参数不得另作他用，套接字事件池将会将其回收。</summary>
        /// <remarks>这里一定不需要再次ReceiveAsync，因为TcpServer在处理完成Accepted事件后，会调用Start->ReceiveAsync</remarks>
        public event EventHandler<AcceptedEventArgs> Accepted;

        void AcceptAsync()
        {
            Server.BeginAcceptTcpClient(OnAccept, null);
        }

        void OnAccept(IAsyncResult ar)
        {
            var client = Server.EndAcceptTcpClient(ar);

            AcceptAsync();

            var session = CreateSession(client);
            if (Accepted != null) Accepted(this, new AcceptedEventArgs { Session = session });

            Sessions.Add(session.Remote.EndPoint, session);

            // 设置心跳时间
            //client.Client.SetTcpKeepAlive(true);

            // 自动开始异步接收处理
            if (AutoReceiveAsync) session.ReceiveAsync();
        }
        #endregion

        #region 会话
        private Object _Sessions_lock = new object();
        private IDictionary<IPEndPoint, TcpSession> _Sessions;
        /// <summary>会话集合。用自增的数字ID作为标识，业务应用自己维持ID与业务主键的对应关系。</summary>
        public IDictionary<IPEndPoint, TcpSession> Sessions
        {
            get
            {
                if (_Sessions != null) return _Sessions;
                lock (_Sessions_lock)
                {
                    if (_Sessions != null) return _Sessions;

                    return _Sessions = new TcpSessionCollection() { Server = this };
                }
            }
        }

        /// <summary>创建会话</summary>
        /// <param name="client"></param>
        /// <returns></returns>
        protected virtual TcpSession CreateSession(TcpClient client)
        {
            var session = new TcpSession();

            return session;
        }
        #endregion

        #region 释放资源
        /// <summary>已重载。释放会话集合等资源</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            // 释放托管资源
            if (disposing)
            {
                var sessions = _Sessions;
                if (sessions != null)
                {
                    _Sessions = null;

                    XTrace.WriteLine("准备释放Tcp会话{0}个！", sessions.Count);
                    sessions.TryDispose();
                    sessions.Clear();
                }
            }
        }
        #endregion
    }

    /// <summary>接受连接时触发</summary>
    public class AcceptedEventArgs : EventArgs
    {
        private TcpSession _Session;
        /// <summary>会话</summary>
        public TcpSession Session { get { return _Session; } set { _Session = value; } }
    }
}