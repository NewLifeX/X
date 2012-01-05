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
    }
}