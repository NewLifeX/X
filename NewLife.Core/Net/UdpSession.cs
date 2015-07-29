using System;
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
        private Int32 _ID;
        /// <summary>会话编号</summary>
        public Int32 ID { get { return _ID; } set { _ID = value; } }

        private String _Name;
        /// <summary>名称</summary>
        public String Name { get { return _Name; } set { _Name = value; } }

        private UdpServer _Server;
        /// <summary>服务器</summary>
        public UdpServer Server { get { return _Server; } set { _Server = value; } }

        /// <summary>底层Socket</summary>
        Socket ISocket.Socket { get { return _Server == null ? null : _Server.Client.Client; } }

        private Stream _Stream = new MemoryStream();
        /// <summary>数据流</summary>
        public Stream Stream { get { return _Stream; } set { _Stream = value; } }

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

        private NetUri _Remote;
        /// <summary>远程地址</summary>
        public NetUri Remote { get { return _Remote; } set { _Remote = value; } }

        /// <summary>Socket服务器。当前通讯所在的Socket服务器，其实是TcpServer/UdpServer</summary>
        ISocketServer ISocketSession.Server { get { return _Server; } }

        /// <summary>是否抛出异常，默认false不抛出。Send/Receive时可能发生异常，该设置决定是直接抛出异常还是通过<see cref="Error"/>事件</summary>
        public Boolean ThrowException { get { return Server.ThrowException; } set { Server.ThrowException = value; } }

        private IStatistics _Statistics = new Statistics();
        /// <summary>统计信息</summary>
        public IStatistics Statistics { get { return _Statistics; } private set { _Statistics = value; } }

        private IPEndPoint _Filter;

        private DateTime _StartTime = DateTime.Now;
        /// <summary>通信开始时间</summary>
        public DateTime StartTime { get { return _StartTime; } }

        private DateTime _LastTime;
        /// <summary>最后一次通信时间，主要表示活跃时间，包括收发</summary>
        public DateTime LastTime { get { return _LastTime; } }
        #endregion

        #region 构造
        public UdpSession(UdpServer server, IPEndPoint remote)
        {
            Name = server.Name;
            Server = server;
            Remote = new NetUri(ProtocolType.Udp, remote);
            _Filter = remote;
        }

        public void Start()
        {
            //server.Received += server_Received;
            Server.ReceiveAsync();
            //server.Error += server_Error;

            WriteLog("New {0}", Remote.EndPoint);
        }

        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            //Server.WriteLog("{0}[{1}].Close {2}", Server.Name, ID, this);
            WriteLog("Close {0}", Remote.EndPoint);

            //Server.Received -= server_Received;
            //Server.Error -= server_Error;
            // 释放对服务对象的引用，如果没有其它引用，服务对象将会被回收
            Server = null;
            //GC.Collect();
        }
        #endregion

        #region 收发
        public Boolean Send(byte[] buffer, int offset = 0, int count = -1)
        {
            if (Disposed) throw new ObjectDisposedException(this.GetType().Name);

            if (count <= 0) count = buffer.Length - offset;
            if (offset > 0) buffer = buffer.ReadBytes(offset, count);

            WriteLog("Send [{0}]: {1}", count, buffer.ToHex(0, Math.Min(count, 32)));

            _LastTime = DateTime.Now;

            try
            {
                Server.Client.Send(buffer, count, Remote.EndPoint);

                return true;
            }
            catch (Exception ex)
            {
                OnError("Send", ex);
                Dispose();
                throw;
            }
        }

        Boolean CheckFilter(IPEndPoint remote)
        {
            // IPAddress是类，不同实例对象当然不相等啦
            if (!_Filter.IsAny())
            {
                //if (_Filter.Address != remote.Address || _Filter.Port != remote.Port) return false;
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

            _LastTime = DateTime.Now;

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

            _LastTime = DateTime.Now;

            return size;
        }
        #endregion

        #region 异步接收
        /// <summary>开始异步接收数据</summary>
        public Boolean ReceiveAsync()
        {
            if (Disposed) throw new ObjectDisposedException(this.GetType().Name);

            return Server.ReceiveAsync();
        }

        public event EventHandler<ReceivedEventArgs> Received;

        //void server_Received(object sender, ReceivedEventArgs e)
        //{
        //    //if (Received == null) return;

        //    // 判断是否自己的数据
        //    //var udp = e as UdpReceivedEventArgs;
        //    //if (CheckFilter(udp.Remote))
        //    var remote = e.UserState as IPEndPoint;
        //    if (CheckFilter(remote)) OnReceive(e);
        //}

        internal void OnReceive(ReceivedEventArgs e)
        {
            _LastTime = DateTime.Now;

            WriteLog("Recv [{0}]: {1}", e.Length, e.Data.ToHex(0, Math.Min(e.Length, 32)));

            if (Received != null) Received(this, e);
        }
        #endregion

        #region 异常处理
        /// <summary>错误发生/断开连接时</summary>
        public event EventHandler<ExceptionEventArgs> Error;

        //void server_Error(object sender, ExceptionEventArgs e)
        //{
        //    OnError(null, e.Exception);
        //}

        /// <summary>触发异常</summary>
        /// <param name="action">动作</param>
        /// <param name="ex">异常</param>
        protected virtual void OnError(String action, Exception ex)
        {
            if (Server.Log != null) Server.Log.Error(LogPrefix + "{0}Error {1} {2}", action, this, ex == null ? null : ex.Message);
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
        private ILog _Log;
        /// <summary>日志提供者</summary>
        public ILog Log { get { return _Log; } set { _Log = value; } }
        
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
        #endregion
    }
}