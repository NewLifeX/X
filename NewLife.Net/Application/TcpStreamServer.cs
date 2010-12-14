using System;
using NewLife.IO;
using NewLife.Net.Sockets;
using NewLife.Net.Tcp;

namespace NewLife.Net.Application
{
    /// <summary>
    /// 基于Tcp的数据流服务器
    /// </summary>
    public class TcpStreamServer : TcpNetServer
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

            if (String.IsNullOrEmpty(Name)) Name = "数据流服务器（TCP）";

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