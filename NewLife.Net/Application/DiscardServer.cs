using System;
using System.Net.Sockets;
using NewLife.Net.Sockets;

namespace NewLife.Net.Application
{
    /// <summary>
    /// Discard服务器
    /// </summary>
    public class DiscardServer : NetServer
    {
        /// <summary>
        /// 实例化一个Discard服务
        /// </summary>
        public DiscardServer()
        {
            // 默认Tcp协议
            ProtocolType = ProtocolType.Tcp;
            // 默认9端口
            Port = 9;
        }

        /// <summary>
        /// 已重载。
        /// </summary>
        protected override void EnsureCreateServer()
        {
            base.EnsureCreateServer();

            Name = String.Format("Discard服务（{0}）", ProtocolType);

            // 允许同时处理多个数据包
            Server.NoDelay = ProtocolType == ProtocolType.Udp;
            // 使用线程池来处理事件
            Server.UseThreadPool = true;
        }

        /// <summary>
        /// 已重载。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnReceived(object sender, NetEventArgs e)
        {
            try
            {
                if (e.BytesTransferred > 100)
                    WriteLog("Discard {0} [{1}]", e.RemoteEndPoint, e.BytesTransferred);
                else
                    WriteLog("Discard {0} [{1}] {2}", e.RemoteEndPoint, e.BytesTransferred, e.GetString());
            }
            finally
            {
                Disconnect(e.UserToken as SocketBase);
            }
        }
    }
}