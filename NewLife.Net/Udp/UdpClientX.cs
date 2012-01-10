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
        /// <summary>广播数据包</summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
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
            Byte[] buffer = Encoding.UTF8.GetBytes(message);
            Broadcast(buffer, 0, buffer.Length, port);
        }
        #endregion
    }
}