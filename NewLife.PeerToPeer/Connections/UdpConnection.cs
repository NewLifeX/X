using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Net.Udp;
using System.IO;
using NewLife.Net.Sockets;
using System.Net;

namespace NewLife.PeerToPeer.Connections
{
    /// <summary>
    /// Udp连接
    /// </summary>
    public class UdpConnection : Connection
    {
        #region 属性
        private UdpServer _Client;
        /// <summary>属性说明</summary>
        public UdpServer Client
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
            Client = new UdpServer(Address, Port);
            //Client.DataArrived += delegate(Object sender, DataArrivedEventArgs e)
            //{
            //    OnDataArrived(e.Socket, e.State, e.Stream);
            //};
            Client.Start();
            //Client.Connect(address, port);

            base.ProtocolType = Client.ProtocolType;
        }

        /// <summary>
        /// 已重载。
        /// </summary>
        /// <param name="stream"></param>
        public override void Send(Stream stream)
        {
            //Client.Send(stream);
        }
    }
}
