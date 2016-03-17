using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NewLife.Net;
using NewLife.Net.Sockets;

namespace NewLife.MessageQueue
{
    /// <summary>MQ服务器</summary>
    public class MQServer : NetServer<MQSession>
    {
        /// <summary>实例化</summary>
        public MQServer()
        {
            Port = 2234;
        }
    }

    /// <summary>MQ会话</summary>
    public class MQSession : NetSession
    {
        protected override void OnReceive(ReceivedEventArgs e)
        {
            base.OnReceive(e);

            WriteLog("MQ会话收到：{0}", e.ToStr());
        }
    }
}