using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Net.Sockets;

namespace NewLife.Net.Proxy
{
    /// <summary>代理会话接口。客户端的一次转发请求（或者Tcp连接），就是一个会话。转发的全部操作都在会话中完成。</summary>
    /// <remarks>
    /// 一个会话应该包含两端，两个Socket，服务端和客户端。
    /// 客户端<see cref="Session"/>发来的数据，在这里经过一系列过滤器后，转发给服务端<see cref="Remote"/>；
    /// 服务端<see cref="Remote"/>返回的数据，在这里经过过滤器后，转发给客户端<see cref="Session"/>。
    /// </remarks>
    public interface IProxySession : IDisposable2
    {
        #region 属性
        /// <summary>代理对象</summary>
        IProxy Proxy { get; set; }

        /// <summary>Socket服务器。当前通讯所在的Socket服务器，其实是TcpServer/UdpServer</summary>
        ISocketServer Server { get; set; }

        /// <summary>客户端。跟客户端通讯的那个Socket，其实是服务端TcpClientX/UdpServer</summary>
        ISocketSession Session { get; set; }

        /// <summary>远程客户端。跟目标服务端通讯的那个Socket，其实是客户端TcpClientX/UdpClientX</summary>
        ISocketClient Remote { get; set; }
        #endregion

        #region 方法
        /// <summary>开始会话处理。参数e里面可能含有数据</summary>
        /// <param name="e"></param>
        void Start(NetEventArgs e);
        #endregion

        #region 发送接收
        #endregion
    }
}