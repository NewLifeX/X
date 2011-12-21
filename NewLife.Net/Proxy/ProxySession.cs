using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Net.Sockets;

namespace NewLife.Net.Proxy
{
    /// <summary>代理会话。客户端的一次转发请求（或者Tcp连接），就是一个会话。转发的全部操作都在会话中完成。</summary>
    /// <remarks>
    /// 一个会话应该包含两端，两个Socket，服务端和客户端
    /// </remarks>
    public class ProxySession : Netbase, IProxySession
    {
        #region 属性
        private SocketBase _Client;
        /// <summary>客户端。跟客户端通讯的那个Socket，其实是服务端TcpSession/UdpServer</summary>
        public SocketBase Client { get { return _Client; } set { _Client = value; } }

        private SocketBase _Server;
        /// <summary>服务端。跟目标服务端通讯的那个Socket，其实是客户端TcpClientX/UdpClientX</summary>
        public SocketBase Server { get { return _Server; } set { _Server = value; } }
        #endregion

        #region 构造
        /// <summary>实例化一个代理会话</summary>
        public ProxySession() { }

        /// <summary>通过指定的Socket对象实例化一个代理会话</summary>
        /// <param name="client"></param>
        public ProxySession(SocketBase client) { Client = client; }
        #endregion
    }
}