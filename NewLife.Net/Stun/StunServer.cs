using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Net.Sockets;
using System.Net.Sockets;

namespace NewLife.Net.Stun
{
    /// <summary>Stun服务端。Simple Traversal of UDP over NATs，NAT 的UDP简单穿越。RFC 3489</summary>
    /// <remarks>
    /// <a href="http://baike.baidu.com/view/884586.htm">STUN</a>
    /// </remarks>
    public class StunServer : NetServer
    {
        #region 构造
        /// <summary>
        /// 实例化
        /// </summary>
        public StunServer()
        {
            ProtocolType = ProtocolType.Udp;
            Port = 3478;
        }
        #endregion

        #region 方法
        /// <summary>接收到数据时</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnReceived(object sender, NetEventArgs e)
        {
            base.OnReceived(sender, e);

            if (e.BytesTransferred > 0)
            {
                var request = StunMessage.Read(e.GetStream());

                var response = new StunMessage();
                response.Type = (StunMessageType)((UInt16)request.Type | 0x0100);
            }
        }
        #endregion
    }
}