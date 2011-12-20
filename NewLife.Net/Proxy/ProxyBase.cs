using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Net.Sockets;

namespace NewLife.Net.Proxy
{
    /// <summary>网络数据转发代理基类</summary>
    public abstract class ProxyBase : NetServer, IProxy
    {
        #region 属性
        private ICollection<IProxySession> _Sessions;
        /// <summary>会话集合。</summary>
        public ICollection<IProxySession> Sessions { get { return _Sessions; } set { _Sessions = value; } }

        private ICollection<IProxyFilter> _Filters;
        /// <summary>过滤器集合。</summary>
        public ICollection<IProxyFilter> Filters { get { return _Filters; } set { _Filters = value; } }
        #endregion
    }
}