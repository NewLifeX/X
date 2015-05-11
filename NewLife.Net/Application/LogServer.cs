using System;
using System.IO;
using NewLife.Net.Sockets;

namespace NewLife.Net.Application
{
    /// <summary>日志服务</summary>
    public class LogServer : NetServer
    {
        /// <summary>实例化一个日志服务</summary>
        public LogServer()
        {
            // 默认514端口
            Port = 514;

            Name = "日志服务";
        }

        /// <summary>已重载。</summary>
        /// <param name="session"></param>
        /// <param name="stream"></param>
        protected override void OnReceive(ISocketSession session, Stream stream)
        {
            if (stream.Length == 0) return;

            WriteLog("{0} [{1}] {2}", session.Remote, stream.Length, stream.ToStr());
        }
    }
}