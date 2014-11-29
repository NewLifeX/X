using System;
using System.IO;
using NewLife.Net.Sockets;

namespace NewLife.Net.Application
{
    /// <summary>Discard服务器。抛弃所有收到的数据包，不做任何响应</summary>
    public class DiscardServer : NetServer
    {
        /// <summary>实例化一个Discard服务</summary>
        public DiscardServer()
        {
            // 默认9端口
            Port = 9;

            Name = "Discard服务";
        }

        /// <summary>已重载。</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnReceive(ISocketSession session, Stream stream)
        {
            try
            {
                if (stream.Length > 100)
                    WriteLog("Discard {0} [{1}]", session.Remote, stream.Length);
                else
                    WriteLog("Discard {0} [{1}] {2}", session.Remote, stream.Length, stream.ToStr());
            }
            finally
            {
                //(e.Session).Disconnect();
                session.Dispose();
            }
        }
    }
}