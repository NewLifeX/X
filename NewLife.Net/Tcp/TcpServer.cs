using System;
using System.Net;
using System.Net.Sockets;
using NewLife.Net.Sockets;

namespace NewLife.Net.Tcp
{
    /// <summary>
    /// TCP服务器
    /// </summary>
    public class TcpServer : SocketServer
    {
        #region 属性
        /// <summary>
        /// 已重载。
        /// </summary>
        public override ProtocolType ProtocolType
        {
            get { return ProtocolType.Tcp; }
        }
        #endregion

        #region 构造
        /// <summary>
        /// 构造TCP服务器对象
        /// </summary>
        public TcpServer() : base(IPAddress.Any, 0) { }

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="port"></param>
        public TcpServer(Int32 port) : base(IPAddress.Any, port) { }

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        public TcpServer(IPAddress address, Int32 port) : base(address, port) { }

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="hostname"></param>
        /// <param name="port"></param>
        public TcpServer(String hostname, Int32 port) : base(hostname, port) { }
        #endregion

        #region 开始停止
        /// <summary>
        /// 开始
        /// </summary>
        protected override void OnStart()
        {
            EnsureCreate();
            //// 地址重用，允许该Socket收发同时进行
            //ReuseAddress = true;
            // 开始监听
            base.OnStart();

            // 三次握手之后，Accept之前的总连接个数，队列满之后，新连接将得到主动拒绝ConnectionRefused错误
            // 在我（大石头）的开发机器上，实际上这里的最大值只能是200，大于200跟200一个样
            Server.Listen(Int32.MaxValue);
            //Server.Listen(200);

            // 设定委托
            // 指定10名工人待命，等待处理新连接
            // 一方面避免因没有及时安排工人而造成堵塞，另一方面避免工人中途死亡或逃跑而导致无人迎宾
            // 该安排在一定程度上分担了Listen队列的压力，工人越多，就能及时把任务接过来，尽管处理不了那么快
            // 需要注意的是，该设计会导致触发多次（每个工人一次）Error事件
            for (int i = 0; i < 10 * Environment.ProcessorCount; i++)
            {
                StartAccept();
            }
        }

        void StartAccept()
        {
            NetEventArgs e = Pop();
            e.AcceptSocket = null;

            try
            {
                if (!Server.AcceptAsync(e)) RaiseCompleteAsync(e);
            }
            catch
            {
                Push(e);
                throw;
            }
        }
        #endregion

        #region 事件
        /// <summary>
        /// 连接完成。在事件处理代码中，事件参数不得另作他用，套接字事件池将会将其回收。
        /// </summary>
        public event EventHandler<NetEventArgs> Accepted;

        /// <summary>
        /// 新客户端到达
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnAccept(NetEventArgs e)
        {
            // 再次开始
            if (e.SocketError != SocketError.OperationAborted) StartAccept();

            // Socket错误由各个处理器来处理
            if (e.SocketError != SocketError.Success)
            {
                OnError(e, null);
                return;
            }

            // 建立会话
            TcpSession session = CreateSession(e);
            session.NoDelay = this.NoDelay;
            e.UserToken = session;
            if (Accepted != null) Accepted(this, e);

            if (session.Socket != null && session.Socket.Connected) session.ReceiveAsync(e);
        }

        /// <summary>
        /// 已重载。
        /// </summary>
        /// <param name="e"></param>
        protected override void OnComplete(NetEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Accept:
                    OnAccept(e);
                    return;
                case SocketAsyncOperation.Connect:
                    break;
                case SocketAsyncOperation.Disconnect:
                    break;
                case SocketAsyncOperation.None:
                    break;
                case SocketAsyncOperation.Receive:
                    break;
                case SocketAsyncOperation.ReceiveFrom:
                    break;
                case SocketAsyncOperation.ReceiveMessageFrom:
                    break;
                case SocketAsyncOperation.Send:
                    break;
                case SocketAsyncOperation.SendPackets:
                    break;
                case SocketAsyncOperation.SendTo:
                    break;
                default:
                    break;
            }
            base.OnComplete(e);
        }
        #endregion

        #region 会话
        private TcpSessionCollection _Sessions;
        /// <summary>会话集合</summary>
        public TcpSessionCollection Sessions
        {
            get { return _Sessions ?? (_Sessions = new TcpSessionCollection()); }
        }

        /// <summary>
        /// 创建会话
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        protected virtual TcpSession CreateSession(NetEventArgs e)
        {
            TcpSession session = new TcpSession();
            session.Socket = e.AcceptSocket;
            session.RemoteEndPoint = e.AcceptSocket.RemoteEndPoint as IPEndPoint;
            if (e.RemoteEndPoint == null) e.RemoteEndPoint = session.RemoteEndPoint;
            Sessions.Add(session);
            return session;
        }
        #endregion

        #region 释放资源
        /// <summary>
        /// 已重载。释放会话集合等资源
        /// </summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            // 释放托管资源
            if (disposing)
            {
                if (_Sessions != null)
                {
                    //try
                    {
                        WriteLog("准备释放会话{0}个！", _Sessions.Count);
                        _Sessions.CloseAll();
                        _Sessions.Clear();
                        _Sessions = null;
                    }
                    //catch { }
                }
            }
        }
        #endregion
    }
}