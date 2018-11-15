using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Log;
using NewLife.Model;
using NewLife.Threading;

namespace NewLife.Net
{
    /// <summary>Udp会话。仅用于服务端与某一固定远程地址通信</summary>
    class UdpSession : DisposeBase, ISocketSession, ITransport
    {
        #region 属性
        /// <summary>会话编号</summary>
        public Int32 ID { get; set; }

        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>服务器</summary>
        public UdpServer Server { get; set; }

        /// <summary>底层Socket</summary>
        Socket ISocket.Client => Server?.Client;

        ///// <summary>数据流</summary>
        //public Stream Stream { get; set; }

        private NetUri _Local;
        /// <summary>本地地址</summary>
        public NetUri Local
        {
            get
            {
                return _Local ?? (_Local = Server?.Local);
            }
            set { Server.Local = _Local = value; }
        }

        /// <summary>端口</summary>
        public Int32 Port { get { return Local.Port; } set { Local.Port = value; } }

        /// <summary>远程地址</summary>
        public NetUri Remote { get; set; }

        private Int32 _timeout;
        /// <summary>超时。默认3000ms</summary>
        public Int32 Timeout
        {
            get { return _timeout; }
            set
            {
                _timeout = value;
                if (Server != null)
                    Server.Client.ReceiveTimeout = _timeout;
            }
        }

        /// <summary>管道</summary>
        public IPipeline Pipeline { get; set; }

        /// <summary>Socket服务器。当前通讯所在的Socket服务器，其实是TcpServer/UdpServer</summary>
        ISocketServer ISocketSession.Server => Server;

        /// <summary>是否抛出异常，默认false不抛出。Send/Receive时可能发生异常，该设置决定是直接抛出异常还是通过<see cref="Error"/>事件</summary>
        public Boolean ThrowException { get { return Server.ThrowException; } set { Server.ThrowException = value; } }

        /// <summary>异步处理接收到的数据，默认true利于提升网络吞吐量。</summary>
        /// <remarks>异步处理有可能造成数据包乱序，特别是Tcp。false避免拷贝，提升处理速度</remarks>
        public Boolean ProcessAsync { get { return Server.ProcessAsync; } set { Server.ProcessAsync = value; } }

        /// <summary>发送数据包统计信息</summary>
        public ICounter StatSend { get; set; }

        /// <summary>接收数据包统计信息</summary>
        public ICounter StatReceive { get; set; }

        /// <summary>通信开始时间</summary>
        public DateTime StartTime { get; private set; }

        /// <summary>最后一次通信时间，主要表示活跃时间，包括收发</summary>
        public DateTime LastTime { get; private set; }

        /// <summary>缓冲区大小。默认8k</summary>
        public Int32 BufferSize { get { return Server.BufferSize; } set { Server.BufferSize = value; } }
        #endregion

        #region 构造
        public UdpSession(UdpServer server, IPEndPoint remote)
        {
            Name = server.Name;
            //Stream = new MemoryStream();
            StartTime = DateTime.Now;

            Server = server;
            Remote = new NetUri(NetType.Udp, remote);

            StatSend = server.StatSend;
            StatReceive = server.StatReceive;

            // 检查并开启广播
            server.Client.CheckBroadcast(remote.Address);
        }

        public void Start()
        {
            Pipeline = Server.Pipeline;

            //Server.ReceiveAsync();
            Server.Open();

            WriteLog("New {0}", Remote.EndPoint);

            // 管道
            var pp = Pipeline;
            pp?.Open(Server.CreateContext(this));
        }

        protected override void OnDispose(Boolean disposing)
        {
            base.OnDispose(disposing);

            WriteLog("Close {0}", Remote.EndPoint);

            // 管道
            var pp = Pipeline;
            pp?.Close(Server.CreateContext(this), disposing ? "Dispose" : "GC");

            // 释放对服务对象的引用，如果没有其它引用，服务对象将会被回收
            Server = null;
        }
        #endregion

        #region 发送
        public Boolean Send(Packet pk)
        {
            if (Disposed) throw new ObjectDisposedException(GetType().Name);

            return Server.OnSend(pk, Remote.EndPoint);
        }

        /// <summary>发送消息</summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public virtual Boolean SendMessage(Object message)
        {
            var ctx = Server.CreateContext(this);
            message = Pipeline.Write(ctx, message);

            return ctx.FireWrite(message);
        }

        /// <summary>发送消息并等待响应</summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public virtual Task<Object> SendMessageAsync(Object message)
        {
            var ctx = Server.CreateContext(this);
            var source = new TaskCompletionSource<Object>();
            ctx["TaskSource"] = source;

            message = Pipeline.Write(ctx, message);

#if NET4
            if (!ctx.FireWrite(message)) return TaskEx.FromResult((Object)null);
#else
            if (!ctx.FireWrite(message)) return Task.FromResult((Object)null);
#endif

            return source.Task;
        }

        #endregion

        #region 接收
        /// <summary>接收数据</summary>
        /// <returns></returns>
        public Packet Receive()
        {
            if (Disposed) throw new ObjectDisposedException(GetType().Name);

            //var task = SendAsync((Packet)null);
            //if (Timeout > 0 && !task.Wait(Timeout)) return null;

            //return task.Result;

            var ep = Remote.EndPoint as EndPoint;
            var buf = new Byte[BufferSize];
            var size = Server.Client.ReceiveFrom(buf, ref ep);

            return new Packet(buf, 0, size);
        }

        public event EventHandler<ReceivedEventArgs> Received;

        internal void OnReceive(ReceivedEventArgs e)
        {
            LastTime = TimerX.Now;

            if (e != null) Received?.Invoke(this, e);
        }

        /// <summary>处理数据帧</summary>
        /// <param name="data">数据帧</param>
        void ISocketRemote.Process(IData data) => OnReceive(data as ReceivedEventArgs);
        #endregion

        #region 异常处理
        /// <summary>错误发生/断开连接时</summary>
        public event EventHandler<ExceptionEventArgs> Error;

        /// <summary>触发异常</summary>
        /// <param name="action">动作</param>
        /// <param name="ex">异常</param>
        protected virtual void OnError(String action, Exception ex)
        {
            if (Log != null) Log.Error(LogPrefix + "{0}Error {1} {2}", action, this, ex?.Message);
            Error?.Invoke(this, new ExceptionEventArgs { Exception = ex });
        }
        #endregion

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString()
        {
            if (Remote != null && !Remote.EndPoint.IsAny())
                return String.Format("{0}=>{1}", Local, Remote.EndPoint);
            else
                return Local.ToString();
        }
        #endregion

        #region ITransport接口
        Boolean ITransport.Open() => true;

        Boolean ITransport.Close() => true;
        #endregion

        #region 扩展接口
        /// <summary>数据项</summary>
        public IDictionary<String, Object> Items { get; } = new NullableDictionary<String, Object>();

        /// <summary>设置 或 获取 数据项</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Object this[String key] { get => Items[key]; set => Items[key] = value; }
        #endregion

        #region 日志
        /// <summary>日志提供者</summary>
        public ILog Log { get; set; }

        /// <summary>是否输出发送日志。默认false</summary>
        public Boolean LogSend { get; set; }

        /// <summary>是否输出接收日志。默认false</summary>
        public Boolean LogReceive { get; set; }

        private String _LogPrefix;
        /// <summary>日志前缀</summary>
        public virtual String LogPrefix
        {
            get
            {
                if (_LogPrefix == null)
                {
                    var name = Server == null ? "" : Server.Name;
                    _LogPrefix = "{0}[{1}].".F(name, ID);
                }
                return _LogPrefix;
            }
            set { _LogPrefix = value; }
        }

        /// <summary>输出日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args)
        {
            if (Log != null) Log.Info(LogPrefix + format, args);
        }

        /// <summary>输出日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        [Conditional("DEBUG")]
        public void WriteDebugLog(String format, params Object[] args)
        {
            if (Log != null) Log.Debug(LogPrefix + format, args);
        }
        #endregion
    }
}