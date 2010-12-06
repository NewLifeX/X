using System;
using NewLife.IO;
using NewLife.Net.Sockets;
using NewLife.Net.Udp;

namespace NewLife.Net.Application
{
    /// <summary>
    /// 基于Udp的数据流服务器
    /// </summary>
    public class UdpStreamServer : UdpNetServer
    {
        private String _StreamHandlerName;
        /// <summary>数据流处理器名称</summary>
        public String StreamHandlerName
        {
            get { return _StreamHandlerName; }
            set { _StreamHandlerName = value; }
        }

        /// <summary>
        /// 已重载。
        /// </summary>
        protected override void EnsureCreateServer()
        {
            if (String.IsNullOrEmpty(StreamHandlerName)) throw new Exception("未指定数据流处理器名称！");

            Name = "数据流服务器（UDP）";

            base.EnsureCreateServer();
        }

        /// <summary>
        /// 已重载。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnReceived(object sender, NetEventArgs e)
        {
            if (e.BytesTransferred > 0) StreamHandler.Process(StreamHandlerName, e.GetStream());
        }
    }
}