using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Net.Sockets;

namespace NewLife.Net.Proxy
{
    /// <summary>数据转发代理接口</summary>
    public interface IProxy
    {
        #region 属性
        /// <summary>会话集合。</summary>
        ICollection<IProxySession> Sessions { get; }

        /// <summary>代理过滤器集合。</summary>
        ICollection<IProxyFilter> Filters { get; }
        #endregion

        #region 方法
        /// <summary>为会话创建与远程服务器通讯的Socket。可以使用Socket池达到重用的目的。</summary>
        /// <param name="session"></param>
        /// <returns></returns>
        ISocketClient CreateRemote(IProxySession session);
        #endregion
    }
}