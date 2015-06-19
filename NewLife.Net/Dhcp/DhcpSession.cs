using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NewLife.Net.Sockets;

namespace NewLife.Net.Dhcp
{
    /// <summary>DHCP会话</summary>
    public class DhcpSession : NetSession
    {
        /// <summary>收到数据时触发</summary>
        /// <param name="e"></param>
        protected override void OnReceive(ReceivedEventArgs e)
        {
            var dhcp = new DhcpEntity();
            dhcp.Read(e.Stream);

            var kind = dhcp.Kind;
            WriteLog("收到：{0} {1}", kind, e.UserState);
            var ds = (this as INetSession).Host as DhcpServer;

            var dme = new DhcpMessageEventArgs();
            dme.Request = dhcp;
            dme.UserState = e.UserState;

            if (OnMessage != null) OnMessage(this, dme);

            ds.RaiseMessage(this, dme);

            if (dme.Response != null)
            {
                var buf = dme.Response.ToArray();
                Send(buf);
            }

            base.OnReceive(e);
        }

        /// <summary>收到消息时触发</summary>
        public event EventHandler<DhcpMessageEventArgs> OnMessage;
    }

    /// <summary>消息事件参数</summary>
    public class DhcpMessageEventArgs : EventArgs
    {
        private DhcpEntity _Request;
        /// <summary>收到的消息</summary>
        public DhcpEntity Request { get { return _Request; } set { _Request = value; } }

        private DhcpEntity _Response;
        /// <summary>响应消息</summary>
        public DhcpEntity Response { get { return _Response; } set { _Response = value; } }

        private Object _UserState;
        /// <summary>用户对象</summary>
        public Object UserState { get { return _UserState; } set { _UserState = value; } }
    }
}