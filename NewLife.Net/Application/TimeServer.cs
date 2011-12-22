using System;
using System.Net;
using System.Net.Sockets;
using NewLife.Net.Sockets;

namespace NewLife.Net.Application
{
    /// <summary>
    /// Time服务器
    /// </summary>
    public class TimeServer : NetAppServer
    {
        /// <summary>实例化一个Time服务。向请求者返回1970年1月1日以来的所有秒数</summary>
        public TimeServer()
        {
            // 默认37端口
            Port = 37;

            Name = "Time服务";
        }

        static readonly DateTime StartTime = new DateTime(1970, 1, 1);

        /// <summary>
        /// 已重载。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnAccepted(object sender, NetEventArgs e)
        {
            var session = e.Socket as ISocketSession;
            try
            {
                WriteLog("Daytime {0}", e.RemoteEndPoint);

                base.OnAccepted(sender, e);

                TimeSpan ts = DateTime.Now - StartTime;
                Int32 s = (Int32)ts.TotalSeconds;
                // 因为要发往网络，这里调整网络字节序
                s = IPAddress.HostToNetworkOrder(s);
                Byte[] buffer = BitConverter.GetBytes(s);
                //Send(e.Socket, buffer, 0, buffer.Length, e.RemoteEndPoint);
                session.Send(buffer, 0, buffer.Length, e.RemoteEndPoint);
            }
            finally
            {
                session.Disconnect();
            }
        }
    }
}