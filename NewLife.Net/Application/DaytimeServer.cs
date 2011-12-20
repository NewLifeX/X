using System;
using System.Net.Sockets;
using System.Text;
using NewLife.Net.Sockets;

namespace NewLife.Net.Application
{
    /// <summary>Daytime服务器。返回服务端的时间日期</summary>
    public class DaytimeServer : NetServer
    {
        /// <summary>
        /// 实例化一个Daytime服务
        /// </summary>
        public DaytimeServer()
        {
            //// 默认Tcp协议
            //ProtocolType = ProtocolType.Tcp;
            // 默认13端口
            Port = 13;

            Name = "Daytime服务";
        }

        ///// <summary>
        ///// 已重载。
        ///// </summary>
        //protected override void EnsureCreateServer()
        //{
        //    base.EnsureCreateServer();

        //    //// 允许同时处理多个数据包
        //    //Server.NoDelay = ProtocolType == ProtocolType.Udp;
        //    //// 使用线程池来处理事件
        //    //Server.UseThreadPool = true;
        //}

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
                Send(e.UserToken as SocketBase, buffer, 0, buffer.Length, e.RemoteEndPoint);
            }
            finally
            {
                Disconnect(e.UserToken as SocketBase);
            }
        }
    }
}