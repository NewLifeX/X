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

            WriteLog("收到：{0}", dhcp);

            base.OnReceive(e);
        }
    }
}