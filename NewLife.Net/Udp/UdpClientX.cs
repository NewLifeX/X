using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NewLife.Net.Sockets;

namespace NewLife.Net.Udp
{
    /// <summary>
    /// 增强的UDP客户端
    /// </summary>
    public class UdpClientX : SocketClient
    {
        #region 属性
        /// <summary>
        /// 已重载。
        /// </summary>
        public override ProtocolType ProtocolType
        {
            get { return ProtocolType.Udp; }
        }
        #endregion

        #region 接收
        /// <summary>
        /// 异步接收
        /// </summary>
        /// <param name="e"></param>
        protected override void ReceiveAsync(NetEventArgs e)
        {
            if (!Client.IsBound) Bind();

            // 如果没有传入网络事件参数，从对象池借用
            if (e == null) e = Pop();
            e.RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

            if (!Client.ReceiveFromAsync(e)) RaiseCompleteAsync(e);
        }
        #endregion

        #region 发送
        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="endPoint"></param>
        public void Send(Byte[] buffer, IPEndPoint endPoint)
        {
            Client.SendTo(buffer, buffer.Length, SocketFlags.None, endPoint);
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="endPoint"></param>
        public void Send(String msg, IPEndPoint endPoint)
        {
            if (String.IsNullOrEmpty(msg)) return;

            Byte[] buffer = Encoding.UTF8.GetBytes(msg);
            Client.SendTo(buffer, buffer.Length, SocketFlags.None, endPoint);
        }

        /// <summary>
        /// 广播数据包
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        /// <param name="port"></param>
        public void Broadcast(Byte[] buffer, Int32 offset, Int32 size, Int32 port)
        {
            if (!Client.EnableBroadcast) Client.EnableBroadcast = true;

            Client.SendTo(buffer, offset, size, SocketFlags.None, new IPEndPoint(IPAddress.Broadcast, port));
        }

        /// <summary>
        /// 广播字符串
        /// </summary>
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