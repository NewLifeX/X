using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Net.Sockets;
using System.Net;
using System.Net.Sockets;

namespace NewLife.Net.Udp
{
    /// <summary>
    /// Udp实现的网络服务器基类
    /// </summary>
    public abstract class UdpNetServer : NetServer
    {
        /// <summary>
        /// 已重载。
        /// </summary>
        protected override void EnsureCreateServer()
        {
            if (Server == null)
            {
                UdpServer svr = new UdpServer(Address, Port);
                svr.Received += new EventHandler<NetEventArgs>(OnReceived);

                Server = svr;
            }
        }

        /// <summary>
        /// 数据到达时
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnReceived(object sender, NetEventArgs e) { }

        #region 发送
        /// <summary>
        /// 向指定目的地发送信息
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        /// <param name="remoteEP"></param>
        public void Send(Byte[] buffer, Int32 offset, Int32 size, EndPoint remoteEP)
        {
            Server.Server.SendTo(buffer, offset, size, SocketFlags.None, remoteEP);
        }

        /// <summary>
        /// 向指定目的地发送信息
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="remoteEP"></param>
        public void Send(Byte[] buffer, EndPoint remoteEP)
        {
            Send(buffer, 0, buffer.Length, remoteEP);
        }

        /// <summary>
        /// 向指定目的地发送信息
        /// </summary>
        /// <param name="message"></param>
        /// <param name="remoteEP"></param>
        public void Send(String message, EndPoint remoteEP)
        {
            Send(Encoding.UTF8.GetBytes(message), remoteEP);
            //Byte[] buffer = Encoding.UTF8.GetBytes(message);
            //Send(buffer, 0, buffer.Length, remoteEP);
        }
        #endregion
    }
}
