using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Net.Udp;

namespace NewLife.Net.TDP
{
    class TDPClient
    {
        #region 属性
        private UdpClientX _Udp;
        /// <summary>UDP客户端</summary>
        public UdpClientX Udp { get { return _Udp; } set { _Udp = value; } }
        #endregion

        #region 连接
        #endregion
    }
}