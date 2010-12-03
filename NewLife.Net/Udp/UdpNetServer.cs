using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Net.Sockets;

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
            UdpServer svr = new UdpServer(Address, Port);
            svr.Received += new EventHandler<NetEventArgs>(OnReceived);

            Server = svr;
        }

        /// <summary>
        /// 数据到达时
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnReceived(object sender, NetEventArgs e) { }
    }
}
