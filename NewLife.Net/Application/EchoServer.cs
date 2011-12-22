using System;
using System.Net.Sockets;
using NewLife.Net.Sockets;
using System.Threading;

namespace NewLife.Net.Application
{
    /// <summary>Echo服务。把客户端发来的数据原样返回。</summary>
    public class EchoServer : NetAppServer
    {
        /// <summary>
        /// 实例化一个Echo服务
        /// </summary>
        public EchoServer()
        {
            // 默认7端口
            Port = 7;

            Name = "Echo服务";
        }

        /// <summary>
        /// 已重载。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnReceived(object sender, NetEventArgs e)
        {
            var session = e.Socket as ISocketSession;
            try
            {
                if (e.BytesTransferred > 100)
                    WriteLog("Echo {0} [{1}]", e.RemoteEndPoint, e.BytesTransferred);
                else
                    WriteLog("Echo {0} [{1}] {2}", e.RemoteEndPoint, e.BytesTransferred, e.GetString());

                //Send(e.Socket, e.Buffer, e.Offset, e.BytesTransferred, e.RemoteEndPoint);
                session.Send(e.Buffer, e.Offset, e.BytesTransferred, e.RemoteEndPoint);

                // 等一秒，等客户端接收数据
                Thread.Sleep(1000);
            }
            finally
            {
                session.Disconnect();
            }
        }
    }
}