using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace NewLife.Net
{
    /// <summary>增强TCP客户端</summary>
    public class TcpSession : SessionBase, ISocketSession
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
        /// <summary>Socket服务器。当前通讯所在的Socket服务器，其实是TcpServer/UdpServer</summary>
        ISocketServer ISocketSession.Server { get { return _Server; } }
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
            if (client.Client.LocalEndPoint != null) Local.EndPoint = (IPEndPoint)client.Client.LocalEndPoint;
            if (client.Client.RemoteEndPoint != null) Remote.EndPoint = (IPEndPoint)client.Client.RemoteEndPoint;
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

                WriteLog("{0}.Open {1}", this.GetType().Name, this);

                if (Remote != null && !Remote.EndPoint.IsAny()) Client.Connect(Remote.EndPoint);
            }

            return true;
        }

        /// <summary>关闭</summary>
        protected override Boolean OnClose()
        {
            WriteLog("{0}.Close {1}", this.GetType().Name, this);

            if (Client != null) Client.Close();
            Client = null;

            return true;
        }

        /// <summary>连接</summary>
        /// <param name="remoteEP"></param>
        /// <returns></returns>
        protected override Boolean OnConnect(IPEndPoint remoteEP)
        {
            //Open();

            // 如果已连接，需要特殊处理
            if (Client.Connected)
            {
                if (Client.Client.RemoteEndPoint.Equals(remoteEP)) return true;

                Client.Client.Disconnect(true);
            }

            Client.Connect(remoteEP);

            return true;
        }

        /// <summary>发送数据</summary>
        /// <remarks>
        /// 目标地址由<seealso cref="SessionBase.Remote"/>决定，如需精细控制，可直接操作<seealso cref="Client"/>
        /// </remarks>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">偏移</param>
        /// <param name="count">数量</param>
        public override void Send(Byte[] buffer, Int32 offset = 0, Int32 count = -1)
        {
            Open();

            if (count < 0) count = buffer.Length - offset;

            Client.GetStream().Write(buffer, 0, count);
        }

        /// <summary>接收数据</summary>
        /// <returns></returns>
        public override Byte[] Receive()
        {
            Open();

            var buf = new Byte[1024 * 2];

            var count = Client.GetStream().Read(buf, 0, buf.Length);
            if (count == 0) return new Byte[0];

            return buf.ReadBytes(0, count);
        }

        /// <summary>读取指定长度的数据，一般是一帧</summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">偏移</param>
        /// <param name="count">数量</param>
        /// <returns></returns>
        public override Int32 Receive(Byte[] buffer, Int32 offset = 0, Int32 count = -1)
        {
            Open();

            if (count < 0) count = buffer.Length - offset;

            return Client.GetStream().Read(buffer, offset, count);
        }
        #endregion

        #region 接收
        /// <summary>开始监听</summary>
        public override void ReceiveAsync()
        {
            if (Client == null) Open();

            // 开始新的监听
            var buf = new Byte[1500];
            Client.GetStream().BeginRead(buf, 0, buf.Length, OnReceive, buf);
        }

        void OnReceive(IAsyncResult ar)
        {
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
            catch (ObjectDisposedException) { return; }
            catch (SocketException ex)
            {
                OnError("EndRead", ex);
                Close();
                return;
            }
            catch (Exception ex) { OnError("EndRead", ex); }

            if (DisconnectWhenEmptyData && count == 0)
            {
                Close();
            }

            // 开始新的监听
            var buf = new Byte[1500];
            try
            {
                client.GetStream().BeginRead(buf, 0, buf.Length, OnReceive, buf);
            }
            catch (ObjectDisposedException) { }
            catch (Exception ex) { OnError("BeginRead", ex); return; }

            OnReceive(data, count);
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