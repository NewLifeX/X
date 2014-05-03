using System;
using System.Net;
using System.Net.Sockets;

namespace NewLife.Net.Sockets
{
    /// <summary>用于指定双方地址的接口</summary>
    public interface ISocketAddress
    {
        #region 属性
        /// <summary>协议类型</summary>
        ProtocolType ProtocolType { get; }

        ///// <summary>监听本地地址</summary>
        //IPAddress Address { get; set; }

        ///// <summary>监听端口</summary>
        //Int32 Port { get; set; }

        ///// <summary>地址族</summary>
        //AddressFamily AddressFamily { get; set; }

        /// <summary>本地终结点</summary>
        IPEndPoint LocalEndPoint { get; }

        /// <summary>本地地址</summary>
        NetUri LocalUri { get; }

        ///// <summary>远程地址</summary>
        //NetUri RemoteUri { get; }

        ///// <summary>远程终结点</summary>
        //IPEndPoint RemoteEndPoint { get; }
        #endregion
    }
}