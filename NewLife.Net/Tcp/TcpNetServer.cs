using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Net.Sockets;

namespace NewLife.Net.Tcp
{
    /// <summary>
    /// Tcp实现的网络服务器基类
    /// </summary>
    public class TcpNetServer : NetServer
    {
        /// <summary>
        /// 已重载。
        /// </summary>
        protected override void EnsureCreateServer()
        {
            TcpServer svr = new TcpServer(Address, Port);
            svr.Accepted += new EventHandler<NetEventArgs>(OnAccepted);

            Server = svr;
        }

        /// <summary>
        /// 接受连接时
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnAccepted(Object sender, NetEventArgs e)
        {
            TcpClientX session = e.UserToken as TcpClientX;
            if (session == null) return;

            session.Received += OnReceived;
            session.Error += new EventHandler<NetEventArgs>(OnError);
        }

        /// <summary>
        /// 收到数据时
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnReceived(Object sender, NetEventArgs e) { }
    }
}