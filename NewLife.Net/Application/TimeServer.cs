using System;
using System.Net;
using System.Net.Sockets;
using NewLife.Net.Sockets;

namespace NewLife.Net.Application
{
    /// <summary>
    /// Time服务器
    /// </summary>
    public class TimeServer : NetServer
    {
        /// <summary>实例化一个Time服务。向请求者返回1970年1月1日以来的所有秒数</summary>
        public TimeServer()
        {
            //// 默认Tcp协议
            //ProtocolType = ProtocolType.Tcp;
            // 默认37端口
            Port = 37;

            Name = "Time服务";
        }

        ///// <summary>
        ///// 已重载。
        ///// </summary>
        //protected override void EnsureCreateServer()
        //{
        //    base.EnsureCreateServer();

        //    Name = String.Format("Time服务（{0}）", ProtocolType);

        //    //// 允许同时处理多个数据包
        //    //Server.NoDelay = ProtocolType == ProtocolType.Udp;
        //    //// 使用线程池来处理事件
        //    //Server.UseThreadPool = true;
        //}

        static readonly DateTime StartTime = new DateTime(1970, 1, 1);

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

                TimeSpan ts = DateTime.Now - StartTime;
                Int32 s = (Int32)ts.TotalSeconds;
                // 因为要发往网络，这里调整网络字节序
                s = IPAddress.HostToNetworkOrder(s);
                Byte[] buffer = BitConverter.GetBytes(s);
                Send(e.UserToken as SocketBase, buffer, 0, buffer.Length, e.RemoteEndPoint);
            }
            finally
            {
                Disconnect(e.UserToken as SocketBase);
            }
        }
    }
}