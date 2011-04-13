using System;
using NewLife.Net.Sockets;
using NewLife.Net.Tcp;

namespace NewLife.Net.Application
{
    /// <summary>
    /// Tcp实现的Echo服务
    /// </summary>
    [Obsolete("请使用EchoServer代替！")]
    public class TcpEchoServer : TcpNetServer
    {
        /// <summary>
        /// 已重载。
        /// </summary>
        protected override void EnsureCreateServer()
        {
            Name = "Echo服务（TCP）";

            base.EnsureCreateServer();

            TcpServer svr = Server as TcpServer;
            // 允许同时处理多个数据包
            svr.NoDelay = false;
            // 使用线程池来处理事件
            svr.UseThreadPool = true;
        }

        /// <summary>
        /// 已重载。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnReceived(object sender, NetEventArgs e)
        {
            TcpClientX tc = sender as TcpClientX;

            try
            {
                if (e.BytesTransferred > 1024)
                {
                    WriteLog("{0}的数据包大于1k，抛弃！", tc.RemoteEndPoint);
                }
                else
                {
                    WriteLog("{0} [{1}] {2}", tc.RemoteEndPoint, e.BytesTransferred, e.GetString());

                    if (tc != null && tc.Client.Connected) tc.Send(e.Buffer, e.Offset, e.BytesTransferred);
                }
            }
            finally
            {
                tc.Close();
            }
        }
    }
}