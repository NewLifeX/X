using System;
using System.Net.Sockets;
using NewLife.Net.Sockets;

namespace NewLife.Net.Application
{
    /// <summary>Discard服务器。抛弃所有收到的数据包，不做任何响应</summary>
    public class DiscardServer : NetAppServer
    {
        /// <summary>
        /// 实例化一个Discard服务
        /// </summary>
        public DiscardServer()
        {
            // 默认9端口
            Port = 9;

            Name = "Discard服务";
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
                (e.Socket as ISocketSession).Disconnect();
            }
        }
    }
}