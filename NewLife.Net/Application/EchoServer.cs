using System;
using System.IO;
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
        protected override void OnReceive(ISocketSession session, Stream stream)
        {
            var p = stream.Position;
            if (stream.Length > 100)
                WriteLog("Echo {0} [{1}]", session.Remote, stream.Length);
            else
                WriteLog("Echo {0} [{1}] {2}", session.Remote, stream.Length, stream.ToStr());

            //Send(e.Socket, e.Buffer, e.Offset, stream.Length, e.RemoteEndPoint);
            //session.Send(e.Buffer, e.Offset, stream.Length, e.RemoteEndPoint);
            //session.Send(e.Buffer, e.Offset, stream.Length);
            stream.Position = p;
            session.Send(stream);
        }
    }
}