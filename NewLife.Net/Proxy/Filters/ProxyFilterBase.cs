using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Net.Sockets;

namespace NewLife.Net.Proxy
{
    /// <summary>过滤器基类</summary>
    public abstract class ProxyFilterBase : DisposeBase, IProxyFilter
    {
        #region 属性
        /// <summary>代理对象</summary>
        public IProxy Proxy { get; set; }
        #endregion

        #region 方法
        /// <summary>为会话创建与远程服务器通讯的Socket。可以使用Socket池达到重用的目的。</summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public virtual ISocketClient CreateRemote(IProxySession session) { return null; }
        #endregion
    }
}