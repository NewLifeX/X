using System;
using System.Net;
using System.Net.Sockets;
using NewLife.Threading;

namespace NewLife.Net
{
    /// <summary>增强TCP客户端</summary>
    public class TcpSession : SessionBase, ISocketSession, ITransport
    {
        #region 属性
        private TcpClient _Client;
        /// <summary>客户端</summary>
        public TcpClient Client { get { return _Client; } private set { _Client = value; } }

        /// <summary>获取Socket</summary>
        /// <returns></returns>
        internal override Socket GetSocket() { return Client == null ? null : Client.Client; }

        private Boolean _DisconnectWhenEmptyData = true;
        /// <summary>收到空数据时抛出异常并断开连接。</summary>
        public Boolean DisconnectWhenEmptyData { get { return _DisconnectWhenEmptyData; } set { _DisconnectWhenEmptyData = value; } }

        //private Stream _Stream;
        ///// <summary>会话数据流，供用户程序使用，内部不做处理。可用于解决Tcp粘包的问题，把多余的分片放入该数据流中。</summary>
        //public Stream Stream { get { return _Stream; } set { _Stream = value; } }

        ISocketServer _Server;
        /// <summary>Socket服务器。当前通讯所在的Socket服务器，其实是TcpServer/UdpServer。该属性决定本会话是客户端会话还是服务的会话</summary>
        ISocketServer ISocketSession.Server { get { return _Server; } }

        private Boolean _AutoReconnect = true;
        /// <summary>是否自动重连，默认true。发生异常断开连接时，自动重连服务端。</summary>
        public Boolean AutoReconnect { get { return _AutoReconnect; } set { _AutoReconnect = value; } }
        #endregion

        #region 构造
        /// <summary>实例化增强UDP</summary>
        public TcpSession()
        {
            Local = new NetUri(ProtocolType.Tcp, IPAddress.Any, 0);
            Remote = new NetUri(ProtocolType.Tcp, IPAddress.Any, 0);
        }

        /// <summary>使用监听口初始化</summary>
        /// <param name="listenPort"></param>
        public TcpSession(Int32 listenPort)
            : this()
        {
            Port = listenPort;
        }

        /// <summary>用TCP客户端初始化</summary>
        /// <param name="client"></param>
        public TcpSession(TcpClient client)
            : this()
        {
            if (client == null) return;

            Client = client;
            var socket = client.Client;
            if (socket.LocalEndPoint != null) Local.EndPoint = (IPEndPoint)socket.LocalEndPoint;
            if (socket.RemoteEndPoint != null) Remote.EndPoint = (IPEndPoint)socket.RemoteEndPoint;
        }

        internal TcpSession(ISocketServer server, TcpClient client)
            : this(client)
        {
            Active = true;
            _Server = server;
        }
        #endregion

        #region 方法
        /// <summary>打开</summary>
        protected override Boolean OnOpen()
        {
            if (Client == null || !Client.Client.IsBound)
            {
                // 根据目标地址适配本地IPv4/IPv6
                if (Remote != null && !Remote.Address.IsAny())
                {
                    Local.Address = Local.Address.GetRightAny(Remote.Address.AddressFamily);
                }

                Client = new TcpClient(Local.EndPoint);
                if (Port == 0) Port = (Socket.LocalEndPoint as IPEndPoint).Port;

                WriteLog("{0}.Open {1}", Name, this);
            }

            // 打开端口前如果已设定远程地址，则自动连接
            if (Remote == null || Remote.EndPoint.IsAny()) return false;

            //if (Remote != null && !Remote.EndPoint.IsAny())
            {
                try
                {
                    Client.Connect(Remote.EndPoint);
                }
                catch (Exception ex)
                {
                    if (!ex.IsDisposed()) OnError("Connect", ex);
                    if (ThrowException) throw;

                    return false;
                }
            }

            return true;
        }

        /// <summary>关闭</summary>
        protected override Boolean OnClose()
        {
            WriteLog("{0}.Close {1}", Name, this);

            if (Client != null)
            {
                // 提前关闭这个标识，否则Close时可能触发自动重连机制
                Active = false;
                try
                {
                    if (_Async != null && _Async.AsyncWaitHandle != null) _Async.AsyncWaitHandle.Close();

                    Client.Close();
                }
                catch (Exception ex)
                {
                    if (!ex.IsDisposed()) OnError("Close", ex);
                    if (ThrowException) throw;

                    return false;
                }
            }
            Client = null;

            return true;
        }

        /// <summary>发送数据</summary>
        /// <remarks>
        /// 目标地址由<seealso cref="SessionBase.Remote"/>决定，如需精细控制，可直接操作<seealso cref="Client"/>
        /// </remarks>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">偏移</param>
        /// <param name="count">数量</param>
        /// <returns>是否成功</returns>
        public override Boolean Send(Byte[] buffer, Int32 offset = 0, Int32 count = -1)
        {
            if (!Open()) return false;

            if (count < 0) count = buffer.Length - offset;

            try
            {
                Client.GetStream().Write(buffer, 0, count);
            }
            catch (Exception ex)
            {
                if (!ex.IsDisposed())
                {
                    OnError("Send", ex);

                    // 发送异常可能是连接出了问题，需要关闭
                    Close();
                    Reconnect();

                    if (ThrowException) throw;
                }

                return false;
            }

            LastTime = DateTime.Now;

            return true;
        }

        /// <summary>接收数据</summary>
        /// <returns>收到的数据。如果没有数据返回0长度数组，如果出错返回null</returns>
        public override Byte[] Receive()
        {
            if (!Open()) return null;

            var buf = new Byte[1024 * 2];

            var count = Receive(buf, 0, buf.Length);
            if (count < 0) return null;
            if (count == 0) return new Byte[0];

            LastTime = DateTime.Now;

            return buf.ReadBytes(0, count);
        }

        /// <summary>读取指定长度的数据，一般是一帧</summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">偏移</param>
        /// <param name="count">数量</param>
        /// <returns></returns>
        public override Int32 Receive(Byte[] buffer, Int32 offset = 0, Int32 count = -1)
        {
            if (!Open()) return -1;

            if (count < 0) count = buffer.Length - offset;

            var rs = 0;
            try
            {
                rs = Client.GetStream().Read(buffer, offset, count);
            }
            catch (Exception ex)
            {
                if (!ex.IsDisposed())
                {
                    OnError("Receive", ex);

                    // 发送异常可能是连接出了问题，需要关闭
                    Close();
                    Reconnect();

                    if (ThrowException) throw;
                }

                return -1;
            }

            LastTime = DateTime.Now;

            return rs;
        }
        #endregion

        #region 异步接收
        private IAsyncResult _Async;

        /// <summary>开始监听</summary>
        /// <returns>是否成功</returns>
        public override Boolean ReceiveAsync()
        {
            if (Disposed || !Open()) return false;

            if (_Async != null) return true;
            try
            {
                // 开始新的监听
                var buf = new Byte[1500];
                _Async = Client.GetStream().BeginRead(buf, 0, buf.Length, OnReceive, buf);
            }
            catch (Exception ex)
            {
                if (!ex.IsDisposed())
                {
                    OnError("ReceiveAsync", ex);

                    // 异常一般是网络错误
                    Close();
                    Reconnect();

                    if (ThrowException) throw;
                }
                return false;
            }

            return true;
        }

        void OnReceive(IAsyncResult ar)
        {
            _Async = null;

            if (!Active) return;

            var client = Client;
            if (client == null || !client.Connected) return;

            // 接收数据
            var data = (Byte[])ar.AsyncState;
            var count = 0;
            try
            {
                count = client.GetStream().EndRead(ar);
            }
            catch (Exception ex)
            {
                if (!ex.IsDisposed())
                {
                    OnError("EndReceive", ex);

                    // 异常一般是网络错误
                    Close();
                    Reconnect();
                }
                return;
            }

            if (DisconnectWhenEmptyData && count == 0)
            {
                Close();
            }

            LastTime = DateTime.Now;

            if (UseProcessAsync)
                // 在用户线程池里面处理数据
                ThreadPoolX.QueueUserWorkItem(() => OnReceive(data, count), ex => OnError("OnReceive", ex));
            else
            {
                try
                {
                    OnReceive(data, count);
                }
                catch (Exception ex)
                {
                    OnError("OnReceive", ex);
                    if (Disposed) return;
                }
            }

            // 开始新的监听
            ReceiveAsync();
        }

        /// <summary>处理收到的数据</summary>
        /// <param name="data"></param>
        /// <param name="count"></param>
        protected virtual void OnReceive(Byte[] data, Int32 count)
        {
            // 分析处理
            var e = new ReceivedEventArgs();
            e.Data = data;
            e.Length = count;

            RaiseReceive(this, e);

            // 数据发回去
            if (e.Feedback) Send(e.Data, 0, e.Length);
        }
        #endregion

        #region 自动重连
        void Reconnect()
        {
            if (!AutoReconnect || Disposed) return;

            WriteLog("{0}.Reconnect {1}", Name, this);

            Open();
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
    }
}