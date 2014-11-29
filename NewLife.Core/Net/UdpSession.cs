using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NewLife.Net
{
    /// <summary>Udp会话。仅用于服务端与某一固定远程地址通信</summary>
    class UdpSession : DisposeBase, ISocketSession
    {
        #region 属性
        private UdpServer _Server;
        /// <summary>服务器</summary>
        public UdpServer Server { get { return _Server; } set { _Server = value; } }

        /// <summary>底层Socket</summary>
        Socket ISocket.Socket { get { return _Server == null ? null : _Server.Client.Client; } }

        private Stream _Stream = new MemoryStream();
        /// <summary>数据流</summary>
        public Stream Stream { get { return _Stream; } set { _Stream = value; } }

        /// <summary>本地地址</summary>
        public NetUri Local { get { return Server.Local; } set { Server.Local = value; } }

        /// <summary>端口</summary>
        public Int32 Port { get { return Server.Port; } set { Server.Port = value; } }

        private NetUri _Remote;
        /// <summary>远程地址</summary>
        public NetUri Remote { get { return _Remote; } set { _Remote = value; } }
        #endregion

        #region 构造
        public UdpSession(UdpServer server, IPEndPoint remote)
        {
            Server = server;
            Remote = new NetUri(ProtocolType.Udp, remote);

            server.Received += server_Received;
        }

        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            Server.Received -= server_Received;
        }
        #endregion

        #region 收发
        public void Send(byte[] buffer, int offset = 0, int count = -1)
        {
            //Server.Send(buffer, offset, count);

            if (count <= 0) count = buffer.Length - offset;
            if (offset > 0) buffer = buffer.ReadBytes(offset, count);

            Server.Client.Send(buffer, count, Remote.EndPoint);
        }

        public byte[] Receive()
        {
            // UDP会话的直接读取可能会读到不是自己的数据
            return Server.Receive();
        }

        public int Receive(byte[] buffer, int offset = 0, int count = -1)
        {
            // UDP会话的直接读取可能会读到不是自己的数据
            return Server.Receive(buffer, offset, count);
        }
        #endregion

        #region 异步接收
        public event EventHandler<ReceivedEventArgs> Received;

        void server_Received(object sender, ReceivedEventArgs e)
        {
            if (Received == null) return;

            // 判断是否自己的数据
            var udp = e as UdpReceivedEventArgs;
            if (udp.Remote == Remote.EndPoint)
            {
                Received(this, e);
            }
        }
        #endregion
    }
}