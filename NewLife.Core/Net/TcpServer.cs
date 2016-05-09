using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Log;

namespace NewLife.Net
{
    /// <summary>TCP服务器</summary>
    /// <remarks>
    /// 核心工作：启动服务<see cref="Start"/>时，监听端口，并启用多个（逻辑处理器数的10倍）异步接受操作<see cref="AcceptAsync"/>。
    /// 
    /// 服务器完全处于异步工作状态，任何操作都不可能被阻塞。
    /// 
    /// 注意：服务器接受连接请求后，不会开始处理数据，而是由<see cref="NewSession"/>事件订阅者决定何时开始处理数据。
    /// </remarks>
    public class TcpServer : DisposeBase, ISocketServer
    {
        #region 属性
        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>本地绑定信息</summary>
        public NetUri Local { get; set; }

        /// <summary>端口</summary>
        public Int32 Port { get { return Local.Port; } set { Local.Port = value; } }

        /// <summary>会话超时时间。默认30秒</summary>
        /// <remarks>
        /// 对于每一个会话连接，如果超过该时间仍然没有收到任何数据，则断开会话连接。
        /// </remarks>
        public Int32 SessionTimeout { get; set; }

        /// <summary>自动开始会话的异步接收，默认true。
        /// 接受连接请求后，自动开始会话的异步接收，默认打开，如果会话需要同步接收数据，需要关闭该选项。</summary>
        public Boolean AutoReceiveAsync { get; set; }

        /// <summary>是否异步处理接收到的数据，默认true利于提升网络吞吐量。异步处理有可能造成数据包乱序，特别是Tcp</summary>
        public Boolean UseProcessAsync { get; set; }

        ///// <summary>服务器</summary>
        //public TcpListener Server { get; set; }

        /// <summary>底层Socket</summary>
        public Socket Client { get; private set; }

        /// <summary>是否活动</summary>
        public Boolean Active { get; set; }

        /// <summary>是否抛出异常，默认false不抛出。Send/Receive时可能发生异常，该设置决定是直接抛出异常还是通过<see cref="Error"/>事件</summary>
        public Boolean ThrowException { get; set; }

        /// <summary>最大并行数。默认CPU*1.6</summary>
        public Int32 MaxAsync { get; set; }

        /// <summary>会话统计</summary>
        public IStatistics StatSession { get; set; }

        /// <summary>发送统计</summary>
        public IStatistics StatSend { get; set; }

        /// <summary>接收统计</summary>
        public IStatistics StatReceive { get; set; }
        #endregion

        #region 构造
        /// <summary>构造TCP服务器对象</summary>
        public TcpServer()
        {
            Name = this.GetType().Name;

            Local = new NetUri(NetType.Tcp, IPAddress.Any, 0);
            SessionTimeout = 30;
            AutoReceiveAsync = true;
            UseProcessAsync = true;

            MaxAsync = Environment.ProcessorCount * 16 / 10;

            _Sessions = new SessionCollection(this);
            StatSession = new Statistics();
            StatSend = new Statistics();
            StatReceive = new Statistics();

            Log = Logger.Null;
        }

        /// <summary>构造TCP服务器对象</summary>
        /// <param name="port"></param>
        public TcpServer(Int32 port) : this() { Port = port; }

        /// <summary>已重载。释放会话集合等资源</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            Stop();
        }
        #endregion

        #region 开始停止
        /// <summary>开始</summary>
        public virtual void Start()
        {
            if (Disposed) throw new ObjectDisposedException(this.GetType().Name);

            if (Active || Disposed) return;

            // 开始监听
            //if (Server == null) Server = new TcpListener(Local.EndPoint);
            if (Client == null) Client = NetHelper.CreateTcp(Local.EndPoint.Address.IsIPv4());

            WriteLog("Start {0}", this);

            // 三次握手之后，Accept之前的总连接个数，队列满之后，新连接将得到主动拒绝ConnectionRefused错误
            // 在我（大石头）的开发机器上，实际上这里的最大值只能是200，大于200跟200一个样
            //Server.Start();
            Client.Bind(Local.EndPoint);
            Client.Listen(Int32.MaxValue);

            Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);

            Active = true;

            for (int i = 0; i < MaxAsync; i++)
            {
                var se = new SocketAsyncEventArgs();
                se.Completed += (s, e) => ProcessAccept(e);

                AcceptAsync(se, false);
            }
        }

        /// <summary>停止</summary>
        public virtual void Stop()
        {
            if (!Active) return;

            WriteLog("Stop {0}", this);

            // 关闭的时候会除非一系列异步回调，提前清空Client
            Active = false;

            CloseAllSession();

            Client.Shutdown();
            Client = null;
        }
        #endregion

        #region 连接处理
        /// <summary>新会话时触发</summary>
        public event EventHandler<SessionEventArgs> NewSession;

        /// <summary>开启异步接受新连接</summary>
        /// <param name="se"></param>
        /// <param name="io">是否IO线程</param>
        /// <returns>开启异步是否成功</returns>
        Boolean AcceptAsync(SocketAsyncEventArgs se, Boolean io)
        {
            if (!Active || Client == null)
            {
                se.TryDispose();
                return false;
            }

            var rs = false;
            try
            {
                //_Async = Server.BeginAcceptTcpClient(OnAccept, null);
                rs = Client.AcceptAsync(se);
            }
            catch (Exception ex)
            {
                if (!ex.IsDisposed()) OnError("AcceptAsync", ex);

                if (!io) throw;

                return false;
            }

            if (!rs)
            {
                if (io)
                    ProcessAccept(se);
                else
                    Task.Factory.StartNew(() => ProcessAccept(se));
            }

            return true;
        }

        void ProcessAccept(SocketAsyncEventArgs se)
        {
            if (!Active || Client == null)
            {
                se.TryDispose();
                return;
            }

            // 判断成功失败
            if (se.SocketError != SocketError.Success)
            {
                // 未被关闭Socket时，可以继续使用
                //if (!se.IsNotClosed())
                {
                    var ex = se.GetException();
                    if (ex != null) OnError("AcceptAsync", ex);

                    se.TryDispose();
                    return;
                }
            }
            else
            {
                // 直接在IO线程调用业务逻辑
                try
                {
                    // 估算完成时间，执行过长时提示
                    using (var tc = new TimeCost("{0}.OnAccept".F(this.GetType().Name), 200))
                    {
                        tc.Log = Log;

                        OnAccept(se.AcceptSocket);
                    }
                }
                catch (Exception ex)
                {
                    if (!ex.IsDisposed()) OnError("EndAccept", ex);
                }
                finally
                {
                    se.AcceptSocket = null;
                }
            }

            // 开始新的征程
            AcceptAsync(se, true);
        }

        Int32 g_ID = 0;
        /// <summary>收到新连接时处理</summary>
        /// <param name="client"></param>
        protected virtual void OnAccept(Socket client)
        {
            var session = CreateSession(client);

            // 设置心跳时间
            client.SetTcpKeepAlive(true);

            if (_Sessions.Add(session))
            {
                //session.ID = g_ID++;
                // 会话改为原子操作，避免多线程冲突
                session.ID = Interlocked.Increment(ref g_ID);
                //WriteLog("{0}新会话 {1}", this, client.Client.RemoteEndPoint);
                session.WriteLog("New {0}", session.Remote.EndPoint);

                if (StatSession != null) StatSession.Increment(1);

                if (NewSession != null) NewSession(this, new SessionEventArgs { Session = session });

                // 自动开始异步接收处理
                if (AutoReceiveAsync) session.ReceiveAsync();
            }
        }
        #endregion

        #region 会话
        private SessionCollection _Sessions;
        /// <summary>会话集合。用地址端口作为标识，业务应用自己维持地址端口与业务主键的对应关系。</summary>
        public IDictionary<String, ISocketSession> Sessions { get { return _Sessions; } }

        /// <summary>创建会话</summary>
        /// <param name="client"></param>
        /// <returns></returns>
        protected virtual TcpSession CreateSession(Socket client)
        {
            var session = new TcpSession(this, client);
            // 服务端不支持掉线重连
            session.AutoReconnect = 0;
            session.Log = Log;
            session.LogSend = LogSend;
            session.LogReceive = LogReceive;
            session.StatSend.Parent = StatSend;
            session.StatReceive.Parent = StatReceive;

            return session;
        }

        private void CloseAllSession()
        {
            var sessions = _Sessions;
            if (sessions != null)
            {
                if (sessions.Count > 0)
                {
                    WriteLog("准备释放会话{0}个！", sessions.Count);
                    sessions.TryDispose();
                    sessions.Clear();
                }
            }
        }
        #endregion

        #region 异常处理
        /// <summary>错误发生/断开连接时</summary>
        public event EventHandler<ExceptionEventArgs> Error;

        /// <summary>触发异常</summary>
        /// <param name="action">动作</param>
        /// <param name="ex">异常</param>
        protected virtual void OnError(String action, Exception ex)
        {
            if (Log != null) Log.Error("{0}{1}Error {2} {3}", LogPrefix, action, this, ex == null ? null : ex.Message);
            if (Error != null) Error(this, new ExceptionEventArgs { Action = action, Exception = ex });
        }
        #endregion

        #region 日志
        private String _LogPrefix;
        /// <summary>日志前缀</summary>
        public virtual String LogPrefix
        {
            get
            {
                if (_LogPrefix == null)
                {
                    var name = Name == null ? "" : Name.TrimEnd("Server", "Session", "Client");
                    _LogPrefix = "{0}.".F(name);
                }
                return _LogPrefix;
            }
            set { _LogPrefix = value; }
        }

        /// <summary>日志对象</summary>
        public ILog Log { get; set; }

        /// <summary>是否输出发送日志。默认false</summary>
        public Boolean LogSend { get; set; }

        /// <summary>是否输出接收日志。默认false</summary>
        public Boolean LogReceive { get; set; }

        /// <summary>输出日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args)
        {
            if (Log != null && Log.Enable) Log.Info(LogPrefix + format, args);
        }
        #endregion

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            var ss = Sessions;
            var count = ss != null ? ss.Count : 0;
            if (count > 0)
                return String.Format("{0} [{1}]", Local, count);
            else
                return Local.ToString();
        }
        #endregion
    }
}