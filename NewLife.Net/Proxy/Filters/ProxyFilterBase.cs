using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Net.Sockets;
using System.IO;

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
        public virtual ISocketClient CreateRemote(IProxySession session, NetEventArgs e) { return null; }

        /// <summary>客户端发数据往服务端时</summary>
        /// <param name="session"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        public virtual Stream OnClientToServer(IProxySession session, Stream stream, NetEventArgs e) { return stream; }

        /// <summary>服务端发数据往客户端时</summary>
        /// <param name="session"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        public virtual Stream OnServerToClient(IProxySession session, Stream stream, NetEventArgs e) { return stream; }
        #endregion
    }
}