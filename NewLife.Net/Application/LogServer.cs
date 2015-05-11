using System;
using System.IO;
using NewLife.Net.Sockets;

namespace NewLife.Net.Application
{
    /// <summary>日志服务</summary>
    public class LogServer : NetServer<NewLife.Net.Application.LogServer.LogSession>
    {
        /// <summary>实例化一个日志服务</summary>
        public LogServer()
        {
            // 默认514端口
            Port = 514;

            Name = "日志服务";
        }

        ///// <summary>已重载。</summary>
        ///// <param name="session"></param>
        ///// <param name="stream"></param>
        //protected override void OnReceive(ISocketSession session, Stream stream)
        //{
        //    if (stream.Length == 0) return;

        //    WriteLog("{0} [{1}] {2}", session.Remote, stream.Length, stream.ToStr());
        //}

        /// <summary>日志会话</summary>
        public class LogSession : NetSession
        {
            /// <summary>实例化</summary>
            public LogSession()
            {
                // 不需要日志前缀
                LogPrefix = "";
            }

            /// <summary>已重载。</summary>
            /// <param name="session"></param>
            /// <param name="stream"></param>
            protected override void OnReceive(ReceivedEventArgs e)
            {
                if (e.Length == 0) return;

                WriteLog("{0}", e.ToStr());
            }
        }
    }
}