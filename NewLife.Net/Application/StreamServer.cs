using System;
using System.IO;
using NewLife.IO;
using NewLife.Net.Sockets;

namespace NewLife.Net.Application
{
    /// <summary>数据流服务器</summary>
    public class StreamServer : NetServer
    {
        /// <summary>实例化一个数据流服务器</summary>
        public StreamServer()
        {
            //// 默认Udp协议
            //ProtocolType = ProtocolType.Udp;
            // 默认支持所有协议
            // 默认8000端口
            Port = 8000;
        }

        private String _StreamHandlerName;
        /// <summary>数据流处理器名称</summary>
        public String StreamHandlerName { get { return _StreamHandlerName; } set { _StreamHandlerName = value; } }

        /// <summary>已重载。</summary>
        protected override void EnsureCreateServer()
        {
            if (String.IsNullOrEmpty(StreamHandlerName)) throw new Exception("未指定数据流处理器名称！");

            if (String.IsNullOrEmpty(Name))
            {
                Name = String.Format("数据流服务器（{0}）", ProtocolType);
            }

            base.EnsureCreateServer();
        }

        /// <summary>已重载。</summary>
        /// <param name="session"></param>
        /// <param name="stream"></param>
        protected override void OnReceive(ISocketSession session, Stream stream)
        {
            if (stream.Length > 0) StreamHandler.Process(StreamHandlerName, stream);
        }
    }
}