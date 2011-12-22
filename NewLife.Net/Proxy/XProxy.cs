using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace NewLife.Net.Proxy
{
    /// <summary>通用NAT代理</summary>
    public class XProxy : ProxyBase
    {
        #region 属性
        private NATFilter _nat = new NATFilter();

        /// <summary>服务器地址</summary>
        public IPAddress ServerAddress { get { return _nat.Address; } set { _nat.Address = value; } }

        /// <summary>服务器端口</summary>
        public Int32 ServerPort { get { return _nat.Port; } set { _nat.Port = value; } }

        /// <summary>服务器协议</summary>
        public ProtocolType ServerProtocolType { get { return _nat.ProtocolType; } set { _nat.ProtocolType = value; } }
        #endregion

        #region 构造
        /// <summary>
        /// 实例化
        /// </summary>
        public XProxy()
        {
            Filters.Add(_nat);
        }
        #endregion
    }
}