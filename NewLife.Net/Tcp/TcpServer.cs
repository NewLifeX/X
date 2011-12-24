using System;
using System.Net;
using System.Net.Sockets;
using NewLife.Net.Sockets;
using System.Collections.Generic;

namespace NewLife.Net.Tcp
{
    /// <summary>TCP服务器</summary>
    /// <remarks>
    /// 核心工作：启动服务<see cref="OnStart"/>时，监听端口，并启用多个（逻辑处理器数的10倍）异步接受操作<see cref="StartAccept"/>。
    /// 服务器只处理<see cref="SocketAsyncOperation.Accept"/>操作，并创建<see cref="ISocketSession"/>后，
    /// 将其赋值在事件参数的<see cref="NetEventArgs.Socket"/>中，传递给<see cref="Accepted"/>。
    /// 
    /// 服务器完全处于异步工作状态，任何操作都不可能被阻塞。
    /// 
    /// 注意：服务器接受连接请求后，不会开始处理数据，而是由<see cref="Accepted"/>事件订阅者决定何时开始处理数据<see cref="ISocketSession.Start"/>。
    /// </remarks>
    public class TcpServer : SocketServer
    {
        #region 属性
        /// <summary>已重载。</summary>
        public override ProtocolType ProtocolType { get { return ProtocolType.Tcp; } }
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
        /// <summary>开始</summary>
        protected override void OnStart()
        {
            // 开始监听
            base.OnStart();

            // 三次握手之后，Accept之前的总连接个数，队列满之后，新连接将得到主动拒绝ConnectionRefused错误
            // 在我（大石头）的开发机器上，实际上这里的最大值只能是200，大于200跟200一个样
            Server.Listen(Int32.MaxValue);
            //Server.Listen(200);

            // 设定委托
            // 指定10名工人待命，等待处理新连接
            // 一方面避免因没有及时安排工人而造成堵塞，另一方面避免工人中途死亡或逃跑而导致无人迎接客人
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
                // 如果立即返回，则异步处理完成事件
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
        /// <summary>连接完成。在事件处理代码中，事件参数不得另作他用，套接字事件池将会将其回收。</summary>
        public event EventHandler<NetEventArgs> Accepted;

        /// <summary>新客户端到达</summary>
        /// <param name="e"></param>
        protected virtual ISocketSession OnAccept(NetEventArgs e)
        {
            // 再次开始
            if (e.SocketError != SocketError.OperationAborted) StartAccept();

            // Socket错误由各个处理器来处理
            if (e.SocketError != SocketError.Success)
            {
                OnError(e, null);
                return null;
            }

            // 建立会话
            var session = CreateSession(e);
            session.NoDelay = this.NoDelay;
            e.Socket = session;
            if (Accepted != null) Accepted(this, e);

            //var socket = session.Socket;
            //if (socket != null && socket.Connected) session.Start(e);
            ////if (session?.Socket?.Connected) session.Start(e);

            return session;
        }

        /// <summary>已重载。</summary>
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
        private Object _Sessions_lock = new object();
        private ICollection<ISocketSession> _Sessions;
        /// <summary>会话集合</summary>
        public ICollection<ISocketSession> Sessions
        {
            get
            {
                if (_Sessions != null) return _Sessions;
                lock (_Sessions_lock)
                {
                    if (_Sessions != null) return _Sessions;

                    return _Sessions = new TcpSessionCollection();
                }
            }
        }

        /// <summary>创建会话</summary>
        /// <param name="e"></param>
        /// <returns></returns>
        protected virtual ISocketSession CreateSession(NetEventArgs e)
        {
            var session = new TcpClientX();
            session.Socket = e.AcceptSocket;
            session.RemoteEndPoint = e.AcceptSocket.RemoteEndPoint as IPEndPoint;
            if (e.RemoteEndPoint == null) e.RemoteEndPoint = session.RemoteEndPoint;

            Sessions.Add(session);

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
                if (_Sessions != null)
                {
                    //try
                    {
                        WriteLog("准备释放会话{0}个！", _Sessions.Count);
                        if (_Sessions is IDisposable)
                            (_Sessions as IDisposable).Dispose();
                        else
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