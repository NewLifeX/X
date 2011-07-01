using System;
using System.Net.Sockets;
using NewLife.Net.Sockets;
using System.Threading;

namespace NewLife.Net.Application
{
    /// <summary>
    /// Echo服务
    /// </summary>
    public class EchoServer : NetServer
    {
        /// <summary>
        /// 实例化一个Echo服务
        /// </summary>
        public EchoServer()
        {
            // 默认Tcp协议
            ProtocolType = ProtocolType.Tcp;
            // 默认7端口
            Port = 7;
        }

        /// <summary>
        /// 已重载。
        /// </summary>
        protected override void EnsureCreateServer()
        {
            base.EnsureCreateServer();

            Name = String.Format("Echo服务（{0}）", ProtocolType);

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
                    WriteLog("Echo {0} [{1}]", e.RemoteEndPoint, e.BytesTransferred);
                else
                    WriteLog("Echo {0} [{1}] {2}", e.RemoteEndPoint, e.BytesTransferred, e.GetString());

                Send(e.UserToken as SocketBase, e.Buffer, e.Offset, e.BytesTransferred, e.RemoteEndPoint);

                // 等一秒，等客户端接收数据
                Thread.Sleep(1000);
            }
            finally
            {
                Disconnect(e.UserToken as SocketBase);
            }
        }
    }
}