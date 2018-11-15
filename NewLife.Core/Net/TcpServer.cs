using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Log;
using NewLife.Model;

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

        /// <summary>会话超时时间</summary>
        /// <remarks>
        /// 对于每一个会话连接，如果超过该时间仍然没有收到任何数据，则断开会话连接。
        /// </remarks>
        public Int32 SessionTimeout { get; set; }

        /// <summary>异步处理接收到的数据，默认false。</summary>
        /// <remarks>异步处理有可能造成数据包乱序，特别是Tcp。true利于提升网络吞吐量。false避免拷贝，提升处理速度</remarks>
        public Boolean ProcessAsync { get; set; }

        /// <summary>底层Socket</summary>
        public Socket Client { get; private set; }

        /// <summary>是否活动</summary>
        public Boolean Active { get; set; }

        /// <summary>是否抛出异常，默认false不抛出。Send/Receive时可能发生异常，该设置决定是直接抛出异常还是通过<see cref="Error"/>事件</summary>
        public Boolean ThrowException { get; set; }

        /// <summary>最大并行接收连接数。默认CPU*1.6</summary>
        public Int32 MaxAsync { get; set; }

        /// <summary>启用Http，数据处理时截去请求响应头，默认false</summary>
        public Boolean EnableHttp { get; set; }

        /// <summary>管道</summary>
        public IPipeline Pipeline { get; set; }

        /// <summary>会话统计</summary>
        public ICounter StatSession { get; set; }

        /// <summary>发送统计</summary>
        public ICounter StatSend { get; set; }

        /// <summary>接收统计</summary>
        public ICounter StatReceive { get; set; }
        #endregion

        #region 构造
        /// <summary>构造TCP服务器对象</summary>
        public TcpServer()
        {
            Name = GetType().Name;

            Local = new NetUri(NetType.Tcp, IPAddress.Any, 0);
            SessionTimeout = Setting.Current.SessionTimeout;
            MaxAsync = Environment.ProcessorCount * 16 / 10;
            _Sessions = new SessionCollection(this);

            if (Setting.Current.Debug) Log = XTrace.Log;
        }

        /// <summary>构造TCP服务器对象</summary>
        /// <param name="port"></param>
        public TcpServer(Int32 port) : this() => Port = port;

        /// <summary>已重载。释放会话集合等资源</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(Boolean disposing)
        {
            base.OnDispose(disposing);

            if (Active) Stop(GetType().Name + (disposing ? "Dispose" : "GC"));
        }
        #endregion

        #region 开始停止
        /// <summary>开始</summary>
        public virtual void Start()
        {
            if (Disposed) throw new ObjectDisposedException(GetType().Name);

            if (Active || Disposed) return;

            // 统计
            if (StatSession == null) StatSession = new PerfCounter();
            if (StatSend == null) StatSend = new PerfCounter();
            if (StatReceive == null) StatReceive = new PerfCounter();

            var sock = Client;

            // 开始监听
            //if (Server == null) Server = new TcpListener(Local.EndPoint);
            if (sock == null) Client = sock = NetHelper.CreateTcp(Local.EndPoint.Address.IsIPv4());

            WriteLog("Start {0}", this);

            // 三次握手之后，Accept之前的总连接个数，队列满之后，新连接将得到主动拒绝ConnectionRefused错误
            // 在我（大石头）的开发机器上，实际上这里的最大值只能是200，大于200跟200一个样
            //Server.Start();
            sock.Bind(Local.EndPoint);
            sock.Listen(Int32.MaxValue);

#if !__CORE__
            sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
#endif

            Active = true;

            for (var i = 0; i < MaxAsync; i++)
            {
                var se = new SocketAsyncEventArgs();
                se.Completed += (s, e) => ProcessAccept(e);

                AcceptAsync(se, false);
            }
        }

        /// <summary>停止</summary>
        /// <param name="reason">关闭原因。便于日志分析</param>
        public virtual void Stop(String reason)
        {
            if (!Active) return;

            WriteLog("Stop {0} {1}", reason, this);

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
                    OnAccept(se.AcceptSocket);
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

            //// 设置心跳时间，默认10秒
            //client.SetTcpKeepAlive(true);

            if (_Sessions.Add(session))
            {
                // 会话改为原子操作，避免多线程冲突
                session.ID = Interlocked.Increment(ref g_ID);
                session.WriteLog("New {0}", session.Remote.EndPoint);

                StatSession?.Increment(1, 0);

                NewSession?.Invoke(this, new SessionEventArgs { Session = session });

                // 自动开始异步接收处理
                session.Start();
            }
        }
        #endregion

        #region 会话
        private SessionCollection _Sessions;
        /// <summary>会话集合。用地址端口作为标识，业务应用自己维持地址端口与业务主键的对应关系。</summary>
        public IDictionary<String, ISocketSession> Sessions => _Sessions;

        /// <summary>创建会话</summary>
        /// <param name="client"></param>
        /// <returns></returns>
        protected virtual TcpSession CreateSession(Socket client)
        {
            //var session = EnableHttp ? new HttpSession(this, client) : new TcpSession(this, client);
            var session = new TcpSession(this, client)
            {
                // 服务端不支持掉线重连
                AutoReconnect = 0,
                NoDelay = true,
                Log = Log,
                LogSend = LogSend,
                LogReceive = LogReceive,
                StatSend = StatSend,
                StatReceive = StatReceive,
                ProcessAsync = ProcessAsync,
                Pipeline = Pipeline
            };

            // 为了降低延迟，服务端不要合并小包
            client.NoDelay = true;

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
            if (Log != null) Log.Error("{0}{1}Error {2} {3}", LogPrefix, action, this, ex?.Message);
            Error?.Invoke(this, new ExceptionEventArgs { Action = action, Exception = ex });
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
        public override String ToString()
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