using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Net.Proxy
{
    /// <summary>NAT会话</summary>
    public class NATSession : ProxySession
    {
        /// <summary>代理对象</summary>
        public new NATProxy Proxy { get { return base.Proxy as NATProxy; } set { base.Proxy = value; } }
    }
}