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
        /// <param name="session"></param>
        /// <param name="stream"></param>
        protected override void OnReceive(ISocketSession session, Stream stream)
        {
            if (stream.Length == 0) return;

            if (stream.Length > 100)
                WriteLog("Discard {0} [{1}] {2}...", session.Remote, stream.Length, stream.ReadBytes(1000).ToStr());
            else
                WriteLog("Discard {0} [{1}] {2}", session.Remote, stream.Length, stream.ToStr());
        }
    }
}