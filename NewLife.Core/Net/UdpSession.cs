using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using NewLife.Log;

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
        Socket ISocket.Client { get { return Server == null ? null : Server.Client; } }

        /// <summary>数据流</summary>
        public Stream Stream { get; set; }

        private NetUri _Local;
        /// <summary>本地地址</summary>
        public NetUri Local
        {
            get
            {
                return _Local ?? (_Local = Server == null ? null : Server.Local);
            }
            set { Server.Local = _Local = value; }
        }

        /// <summary>端口</summary>
        public Int32 Port { get { return Local.Port; } set { Local.Port = value; } }

        /// <summary>远程地址</summary>
        public NetUri Remote { get; set; }

        /// <summary>Socket服务器。当前通讯所在的Socket服务器，其实是TcpServer/UdpServer</summary>
        ISocketServer ISocketSession.Server { get { return Server; } }

        /// <summary>是否抛出异常，默认false不抛出。Send/Receive时可能发生异常，该设置决定是直接抛出异常还是通过<see cref="Error"/>事件</summary>
        public Boolean ThrowException { get { return Server.ThrowException; } set { Server.ThrowException = value; } }

        /// <summary>发送数据包统计信息，默认关闭，通过<see cref="IStatistics.Enable"/>打开。</summary>
        public IStatistics StatSend { get; set; }

        /// <summary>接收数据包统计信息，默认关闭，通过<see cref="IStatistics.Enable"/>打开。</summary>
        public IStatistics StatReceive { get; set; }

        private IPEndPoint _Filter;

        /// <summary>通信开始时间</summary>
        public DateTime StartTime { get; private set; }

        /// <summary>最后一次通信时间，主要表示活跃时间，包括收发</summary>
        public DateTime LastTime { get; private set; }
        #endregion

        #region 构造
        public UdpSession(UdpServer server, IPEndPoint remote)
        {
            Name = server.Name;
            Stream = new MemoryStream();
            StartTime = DateTime.Now;

            Server = server;
            Remote = new NetUri(NetType.Udp, remote);
            _Filter = remote;

            //StatSend = new Statistics();
            //StatReceive = new Statistics();
            StatSend = server.StatSend;
            StatReceive = server.StatReceive;
        }

        public void Start()
        {
            Server.ReceiveAsync();

            WriteLog("New {0}", Remote.EndPoint);
        }

        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            WriteLog("Close {0}", Remote.EndPoint);

            // 释放对服务对象的引用，如果没有其它引用，服务对象将会被回收
            Server = null;
        }
        #endregion

        #region 发送
        public Boolean Send(byte[] buffer, int offset = 0, int count = -1)
        {
            if (Disposed) throw new ObjectDisposedException(this.GetType().Name);

            if (count <= 0) count = buffer.Length - offset;
            if (offset > 0) buffer = buffer.ReadBytes(offset, count);

            if (StatSend != null) StatSend.Increment(count);
            if (Log.Enable && LogSend) WriteLog("Send [{0}]: {1}", count, buffer.ToHex(0, Math.Min(count, 32)));

            LastTime = DateTime.Now;

            try
            {
                Server.Client.SendTo(buffer, 0, count, SocketFlags.None, Remote.EndPoint);

                return true;
            }
            catch (Exception ex)
            {
                OnError("Send", ex);
                Dispose();
                throw;
            }
        }

        /// <summary>异步发送数据</summary>
        /// <param name="buffer"></param>
        /// <param name="remote"></param>
        /// <returns></returns>
        Boolean ISocketRemote.SendAsync(Byte[] buffer, IPEndPoint remote)
        {
            if (Server == null) return false;

            return Server.SendAsync(buffer, remote);
        }

        /// <summary>异步发送数据</summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public Boolean SendAsync(Byte[] buffer)
        {
            if (Server == null) return false;

            return Server.SendAsync(buffer, Remote.EndPoint);
        }
        #endregion

        #region 接收
        Boolean CheckFilter(IPEndPoint remote)
        {
            // IPAddress是类，不同实例对象当然不相等啦
            if (!_Filter.IsAny())
            {
                if (!_Filter.Equals(remote)) return false;
            }

            return true;
        }

        public byte[] Receive()
        {
            if (Disposed) throw new ObjectDisposedException(this.GetType().Name);

            // UDP会话的直接读取可能会读到不是自己的数据，所以尽量不要两个会话一起读
            var buf = Server.Receive();

            var ep = Server.Remote.EndPoint;
            if (!CheckFilter(ep))
            {
                // 交给其它会话
                Server.OnReceive(buf, ep);
                return new Byte[0];
            }

            Remote.EndPoint = ep;

            LastTime = DateTime.Now;
            //if (StatReceive != null) StatReceive.Increment(buf.Length);

            return buf;
        }

        public int Receive(byte[] buffer, int offset = 0, int count = -1)
        {
            if (Disposed) throw new ObjectDisposedException(this.GetType().Name);

            // UDP会话的直接读取可能会读到不是自己的数据，所以尽量不要两个会话一起读
            var size = Server.Receive(buffer, offset, count);

            var ep = Server.Remote.EndPoint;
            if (!CheckFilter(ep))
            {
                // 交给其它会话
                Server.OnReceive(buffer.ReadBytes(offset, size), ep);
                return 0;
            }

            Remote.EndPoint = ep;

            LastTime = DateTime.Now;
            //if (StatReceive != null) StatReceive.Increment(size);

            return size;
        }
        /// <summary>开始异步接收数据</summary>
        public Boolean ReceiveAsync()
        {
            if (Disposed) throw new ObjectDisposedException(this.GetType().Name);

            return Server.ReceiveAsync();
        }

        public event EventHandler<ReceivedEventArgs> Received;

        internal void OnReceive(ReceivedEventArgs e)
        {
            LastTime = DateTime.Now;
            //if (StatReceive != null) StatReceive.Increment(e.Length);

            if (Log.Enable && LogReceive) WriteLog("Recv [{0}]: {1}", e.Length, e.Data.ToHex(0, Math.Min(e.Length, 32)));

            if (Received != null) Received(this, e);
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
            if (Log != null) Log.Error(LogPrefix + "{0}Error {1} {2}", action, this, ex == null ? null : ex.Message);
            if (Error != null) Error(this, new ExceptionEventArgs { Exception = ex });
        }
        #endregion

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (Remote != null && !Remote.EndPoint.IsAny())
                return String.Format("{0}=>{1}", Local, Remote.EndPoint);
            else
                return Local.ToString();
        }
        #endregion

        #region ITransport接口
        bool ITransport.Open() { return true; }

        bool ITransport.Close() { return true; }
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