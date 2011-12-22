using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace NewLife.Net.Sockets
{
    /// <summary>服务端接受客户端请求后，用于与客户端进行通讯的Socket会话</summary>
    /// <remarks>
    /// 对于Tcp来说，它就是<see cref="Tcp.TcpClientX"/>;
    /// 对于Udp来说，它就是<see cref="Udp.UdpServer"/>。
    /// 
    /// 所以，它必须具有收发数据的能力。
    /// </remarks>
    public interface ISocketSession : ISocket
    {
        #region 属性
        /// <summary>套接字</summary>
        Socket Socket { get; set; }

        ///// <summary>远程IP终结点</summary>
        //IPEndPoint RemoteEndPoint { get; set; }
        #endregion

        #region 方法
        /// <summary>开始会话处理。参数e里面可能含有数据</summary>
        /// <param name="e"></param>
        void Start(NetEventArgs e);

        /// <summary>断开客户端连接。Tcp端口，UdpClient不处理</summary>
        void Disconnect();
        #endregion

        #region 发送
        /// <summary>发送数据</summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">位移</param>
        /// <param name="size">写入字节数</param>
        /// <param name="remoteEP">远程地址。仅对Udp有效</param>
        void Send(byte[] buffer, int offset, int size, EndPoint remoteEP = null);

        /// <summary>发送数据流</summary>
        /// <param name="stream"></param>
        /// <param name="remoteEP">远程地址。仅对Udp有效</param>
        /// <returns></returns>
        long Send(Stream stream, EndPoint remoteEP = null);

        /// <summary>发送字符串</summary>
        /// <param name="msg"></param>
        /// <param name="encoding"></param>
        /// <param name="remoteEP">远程地址。仅对Udp有效</param>
        void Send(string msg, Encoding encoding = null, EndPoint remoteEP = null);
        #endregion

        #region 接收
        /// <summary>开始异步接收数据</summary>
        void ReceiveAsync();

        /// <summary>接收数据</summary>
        /// <returns></returns>
        byte[] Receive();

        /// <summary>接收字符串</summary>
        /// <param name="encoding"></param>
        /// <returns></returns>
        string ReceiveString(Encoding encoding = null);

        /// <summary>数据到达，在事件处理代码中，事件参数不得另作他用，套接字事件池将会将其回收。</summary>
        event EventHandler<NetEventArgs> Received;
        #endregion
    }
}