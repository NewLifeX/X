using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NewLife.Net.Sockets;

namespace NewLife.Net.Udp
{
    /// <summary>增强的UDP客户端</summary>
    public class UdpClientX : SocketClient
    {
        #region 属性
        /// <summary>已重载。</summary>
        public override ProtocolType ProtocolType { get { return ProtocolType.Udp; } }
        #endregion

        #region 接收
        /// <summary>异步接收</summary>
        /// <param name="e"></param>
        public override void ReceiveAsync(NetEventArgs e = null)
        {
            StartAsync(ev =>
            {
                // 兼容IPV6
                IPAddress address = AddressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any;
                ev.RemoteEndPoint = new IPEndPoint(address, 0);
                // 不能用ReceiveAsync，否则得不到远程地址
                return Client.ReceiveFromAsync(ev);
            }, e);
        }
        #endregion

        #region 发送
        /// <summary></summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        /// <param name="remoteEP">远程终结点</param>
        public override void Send(byte[] buffer, int offset, int size, EndPoint remoteEP = null)
        {
            if (remoteEP != null)
                Client.SendTo(buffer, offset, size, SocketFlags.None, remoteEP);
            else
                base.Send(buffer, offset, size, remoteEP);
        }

        /// <summary>发送数据</summary>
        /// <param name="buffer"></param>
        /// <param name="endPoint"></param>
        public void Send(Byte[] buffer, IPEndPoint endPoint)
        {
            if (Socket == null) AddressFamily = endPoint.AddressFamily;
            Client.SendTo(buffer, buffer.Length, SocketFlags.None, endPoint);
        }

        /// <summary>发送数据</summary>
        /// <param name="msg"></param>
        /// <param name="endPoint"></param>
        public void Send(String msg, IPEndPoint endPoint)
        {
            if (String.IsNullOrEmpty(msg)) return;

            Byte[] buffer = Encoding.UTF8.GetBytes(msg);
            if (Socket == null) AddressFamily = endPoint.AddressFamily;
            Client.SendTo(buffer, buffer.Length, SocketFlags.None, endPoint);
        }

        /// <summary>广播数据包</summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        /// <param name="port"></param>
        public void Broadcast(Byte[] buffer, Int32 offset, Int32 size, Int32 port)
        {
            if (!Client.EnableBroadcast) Client.EnableBroadcast = true;

            Client.SendTo(buffer, offset, size, SocketFlags.None, new IPEndPoint(IPAddress.Broadcast, port));
        }

        /// <summary>广播字符串</summary>
        /// <param name="message"></param>
        /// <param name="port"></param>
        public void Broadcast(String message, Int32 port)
        {
            Byte[] buffer = Encoding.UTF8.GetBytes(message);
            Broadcast(buffer, 0, buffer.Length, port);
        }
        #endregion
    }
}