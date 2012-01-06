using System.Collections.Generic;
using NewLife.Net.Sockets;
using System.IO;

namespace NewLife.Net.Proxy
{
    /// <summary>数据转发代理接口</summary>
    public interface IProxy
    {
        #region 属性
        /// <summary>会话集合。</summary>
        ICollection<IProxySession> Sessions { get; }

        ///// <summary>代理过滤器集合。</summary>
        //ICollection<IProxyFilter> Filters { get; }

        ///// <summary>主过滤器，同时也是集合，会话主要针对这个操作</summary>
        //IProxyFilter MainFilter { get; }
        #endregion

        #region 方法
        /// <summary>为会话创建与远程服务器通讯的Socket。可以使用Socket池达到重用的目的。</summary>
        /// <param name="session"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        ISocketClient CreateRemote(IProxySession session, NetEventArgs e);

        /// <summary>客户端发数据往服务端时</summary>
        /// <param name="session"></param>
        /// <param name="stream"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        Stream OnClientToServer(IProxySession session, Stream stream, NetEventArgs e);

        /// <summary>服务端发数据往客户端时</summary>
        /// <param name="session"></param>
        /// <param name="stream"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        Stream OnServerToClient(IProxySession session, Stream stream, NetEventArgs e);
        #endregion
    }
}