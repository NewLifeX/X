using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NewLife.Net.Sockets;

namespace NewLife.Net.Udp
{
    /// <summary>增强的UDP客户端</summary>
    public class UdpClientX : SocketClient, IUdp
    {
        #region 属性
        /// <summary>已重载。</summary>
        public override ProtocolType ProtocolType { get { return ProtocolType.Udp; } }
        #endregion

        #region 接收
        /// <summary>异步接收</summary>
        public override void ReceiveAsync()
        {
            StartAsync(e =>
            {
                var client = Client;
                if (client == null || Disposed) { e.Cancel = true; return false; }

                // 兼容IPV6
                var address = AddressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any;
                e.RemoteEndPoint = new IPEndPoint(address, 0);
                // 不能用ReceiveAsync，否则得不到远程地址
                return client.ReceiveFromAsync(e);
            });
        }
        #endregion

        #region 发送
        /// <summary>发送数据</summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">位移</param>
        /// <param name="size">写入字节数</param>
        /// <param name="remoteEP">远程终结点</param>
        public virtual void Send(Byte[] buffer, Int32 offset = 0, Int32 size = 0, EndPoint remoteEP = null)
        {
            var socket = Client;
            if (!socket.IsBound) Bind();

            if (size <= 0) size = buffer.Length - offset;
            if (remoteEP != null && Socket == null) AddressFamily = remoteEP.AddressFamily;
            //if (remoteEP != null && ProtocolType == ProtocolType.Udp)
            if (socket.Connected)
                socket.Send(buffer, offset, size, SocketFlags.None);
            else
                socket.SendTo(buffer, offset, size, SocketFlags.None, remoteEP);

            //return this;
        }

        /// <summary>广播数据包</summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">偏移</param>
        /// <param name="size"></param>
        /// <param name="port"></param>
        public void Broadcast(Byte[] buffer, Int32 offset, Int32 size, Int32 port)
        {
            if (!Client.EnableBroadcast) Client.EnableBroadcast = true;

            if (size <= 0) size = buffer.Length - offset;
            Client.SendTo(buffer, offset, size, SocketFlags.None, new IPEndPoint(IPAddress.Broadcast, port));
        }

        /// <summary>广播字符串</summary>
        /// <param name="message"></param>
        /// <param name="port"></param>
        public void Broadcast(String message, Int32 port)
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            Broadcast(buffer, 0, buffer.Length, port);
        }
        #endregion

        #region 创建会话
        private ISocketSession _session;

        /// <summary>为指定地址创建会话。对于无连接Socket，必须指定远程地址；对于有连接Socket，指定的远程地址将不起任何作用</summary>
        /// <param name="remoteEP"></param>
        /// <returns></returns>
        public override ISocketSession CreateSession(IPEndPoint remoteEP = null)
        {
            var socket = Client;
            if (!socket.Connected)
            {
                if (remoteEP == null) throw new ArgumentNullException("remoteEP", "未连接Udp必须指定远程地址！");

                return new UdpSession(this, remoteEP);
            }
            else
            {
                // 已连接。返回已有的
                if (_session == null) _session = new UdpSession(this, remoteEP);
                return _session;
            }
        }
        #endregion
    }
}