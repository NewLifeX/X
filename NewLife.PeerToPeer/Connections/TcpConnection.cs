using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Net.Tcp;
using NewLife.Net.Sockets;
using System.IO;
using System.Net.Sockets;
using System.Net;

namespace NewLife.PeerToPeer.Connections
{
    /// <summary>
    /// Tcp连接
    /// </summary>
    public class TcpConnection : Connection
    {
        #region 属性
        private TcpClientX _Client;
        /// <summary>属性说明</summary>
        public TcpClientX Client
        {
            get { return _Client; }
            set { _Client = value; }
        }

        #endregion

        /// <summary>
        /// 已重载。
        /// </summary>
        public override void Connect(IPAddress address, Int32 port)
        {
            //TcpClient tc = new TcpClient(new IPEndPoint(Address, Port));
            //Client = new TcpClientEx(tc);
            //Client.DataArrived += delegate(Object sender, DataArrivedEventArgs e)
            //{
            //    OnDataArrived(e.Socket, e.State, e.Stream);
            //};
            //Client.Client.Connect(address, port);
            //// 开始异步接收数据
            //Client.BeginRead();

            ////base.ProtocolType = Client.Client.Client.ProtocolType;
        }

        /// <summary>
        /// 已重载。
        /// </summary>
        /// <param name="stream"></param>
        public override void Send(Stream stream)
        {
            Int64 n = Client.Send(stream);
            Console.WriteLine("发送信息！" + n);
        }
    }
}
