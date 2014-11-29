using NewLife.Net.Sockets;

namespace NewLife.Net.Application
{
    /// <summary>Echo服务。把客户端发来的数据原样返回。</summary>
    public class EchoServer : NetServer
    {
        /// <summary>实例化一个Echo服务</summary>
        public EchoServer()
        {
            // 默认7端口
            Port = 7;

            Name = "Echo服务";
        }

        /// <summary>已重载。</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnReceived(object sender, NetEventArgs e)
        {
            var session = e.Session;

            if (e.BytesTransferred > 100)
                WriteLog("Echo {0} [{1}]", session.Remote, e.BytesTransferred);
            else
                WriteLog("Echo {0} [{1}] {2}", session.Remote, e.BytesTransferred, e.GetString());

            //Send(e.Socket, e.Buffer, e.Offset, e.BytesTransferred, e.RemoteEndPoint);
            //session.Send(e.Buffer, e.Offset, e.BytesTransferred, e.RemoteEndPoint);
            session.Send(e.Buffer, e.Offset, e.BytesTransferred);
        }
    }
}