using System;
using System.Text;
using NewLife.Net.Sockets;

namespace NewLife.Net.Application
{
    /// <summary>Daytime服务器。返回服务端的时间日期</summary>
    public class DaytimeServer : NetServer
    {
        /// <summary>实例化一个Daytime服务</summary>
        public DaytimeServer()
        {
            // 默认13端口
            Port = 13;

            Name = "Daytime服务";
        }

        /// <summary>已重载。</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnAccept(ISocketServer server, ISocketSession session)
        {
            WriteLog("Daytime {0}", session.Remote);

            Byte[] buffer = Encoding.ASCII.GetBytes(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff"));
            //Send(e.Socket, buffer, 0, buffer.Length, e.RemoteEndPoint);
            //session.Send(buffer, 0, buffer.Length, e.RemoteEndPoint);
            session.Send(buffer);

            //    // 等一秒，等客户端接收数据
            //    Thread.Sleep(1000);
            //}
            //finally
            //{
            //    //session.Disconnect();
            //    session.Dispose();
            //}
        }
    }
}