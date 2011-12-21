using System;
using System.Net.Sockets;
using System.Text;
using NewLife.Net.Sockets;

namespace NewLife.Net.Application
{
    /// <summary>Daytime服务器。返回服务端的时间日期</summary>
    public class DaytimeServer : NetAppServer
    {
        /// <summary>
        /// 实例化一个Daytime服务
        /// </summary>
        public DaytimeServer()
        {
            // 默认13端口
            Port = 13;

            Name = "Daytime服务";
        }

        /// <summary>
        /// 已重载。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnAccepted(object sender, NetEventArgs e)
        {
            try
            {
                WriteLog("Daytime {0}", e.RemoteEndPoint);

                base.OnAccepted(sender, e);

                Byte[] buffer = Encoding.ASCII.GetBytes(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff"));
                Send(e.Socket, buffer, 0, buffer.Length, e.RemoteEndPoint);
            }
            finally
            {
                Disconnect(e.Socket);
            }
        }
    }
}