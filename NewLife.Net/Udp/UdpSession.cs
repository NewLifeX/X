using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NewLife.Net.Common;
using NewLife.Net.Sockets;

namespace NewLife.Net.Udp
{
    class UdpSession : Netbase, ISocketSession
    {
        #region 属性
        private Int32 _ID;
        /// <summary>标识</summary>
        public Int32 ID { get { return _ID; } set { _ID = value; } }

        private ISocket _Host;
        /// <summary>宿主</summary>
        public ISocket Host { get { return _Host; } set { _Host = value; } }

        /// <summary>协议</summary>
        public ProtocolType ProtocolType { get { return ProtocolType.Udp; } }

        private IPEndPoint _RemoteEndPoint;
        /// <summary>远程地址</summary>
        public IPEndPoint RemoteEndPoint { get { return _RemoteEndPoint; } set { _RemoteEndPoint = value; } }

        private IPEndPoint _LocalEndPoint;
        /// <summary>本地终结点</summary>
        public IPEndPoint LocalEndPoint { get { return _LocalEndPoint ?? (_LocalEndPoint = Host.LocalEndPoint); } }

        private NetUri _LocalUri;
        /// <summary>本地地址</summary>
        public NetUri LocalUri { get { return _LocalUri ?? (_LocalUri = new NetUri(ProtocolType, LocalEndPoint)); } }

        private NetUri _RemoteUri;
        /// <summary>远程地址</summary>
        public NetUri RemoteUri { get { return _RemoteUri ?? (_RemoteUri = new NetUri(ProtocolType, RemoteEndPoint)); } }

        private Boolean _Connected;
        /// <summary>是否已连接</summary>
        public Boolean Connected { get { return _Connected; } }

        private Boolean _UseReceiveAsync;
        /// <summary>是否使用异步接收</summary>
        public Boolean UseReceiveAsync { get { return _UseReceiveAsync; } set { _UseReceiveAsync = value; } }
        #endregion

        #region 扩展属性
        /// <summary>对象</summary>
        IUdp Udp { get { return Host as IUdp; } }

        private Stream _Stream;
        /// <summary>会话数据流，供用户程序使用，内部不做处理。可用于解决Tcp粘包的问题，把多余的分片放入该数据流中。</summary>
        public Stream Stream { get { return _Stream; } set { _Stream = value; } }
        #endregion

        #region 构造
        public UdpSession(ISocket host, IPEndPoint remoteEP)
        {
            Host = host;
            RemoteEndPoint = remoteEP;

            var socket = host.Socket;
            _Connected = socket.Connected;
        }

        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            var host = _Host;
            // 如果是已连接的会话，可以销毁
            if (host != null && Connected) _Host.Dispose();
        }
        #endregion

        #region ISocketSession 成员

        public ISocketSession Send(byte[] buffer, int offset = 0, int size = 0) { Udp.Send(buffer, offset, size, RemoteEndPoint); return this; }

        public ISocketSession Send(Stream stream) { Udp.Send(stream, RemoteEndPoint); return this; }

        public ISocketSession Send(string msg, Encoding encoding = null) { Udp.Send(msg, encoding, RemoteEndPoint); return this; }

        public void ReceiveAsync()
        {
            UseReceiveAsync = true;
            Udp.Received += new EventHandler<NetEventArgs>(Udp_Received);
            Udp.ReceiveAsync();
        }

        void Udp_Received(object sender, NetEventArgs e)
        {
            // 判断是否当前远程地址
            if (!Connected)
            {
                // 如果不是当前远程地址，则不触发已收到事件
                if (e.RemoteEndPoint + "" != RemoteEndPoint + "") return;
            }
            if (Received != null) Received(this, new ReceivedEventArgs(e.GetStream()));
        }

        public byte[] Receive() { return Udp.Receive(); }

        public string ReceiveString(Encoding encoding = null) { return Udp.ReceiveString(); }

        public event EventHandler<ReceivedEventArgs> Received;

        #endregion

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            var remote = RemoteEndPoint;
            if (remote != null)
                return String.Format("{0}://{1}=>{2}", ProtocolType, LocalEndPoint, remote);
            else
                return String.Format("{0}://{1}", ProtocolType, LocalEndPoint);
        }
        #endregion
    }
}