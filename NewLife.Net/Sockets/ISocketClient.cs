using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NewLife.Net.Sockets
{
    /// <summary>Socket客户端接口。</summary>
    public interface ISocketClient : ISocket
    {
        /// <summary>基础Socket对象</summary>
        Socket Client { get; set; }

        /// <summary>建立与远程主机的连接</summary>
        /// <param name="remoteEP">表示远程设备。</param>
        void Connect(EndPoint remoteEP);

        /// <summary>建立与远程主机的连接</summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        void Connect(IPAddress address, int port);

        /// <summary>建立与远程主机的连接</summary>
        /// <param name="hostname"></param>
        /// <param name="port"></param>
        void Connect(string hostname, int port);

        /// <summary>开始异步接收数据</summary>
        void ReceiveAsync();

        /// <summary>接收数据</summary>
        /// <returns></returns>
        byte[] Receive();

        /// <summary>接收字符串</summary>
        /// <param name="encoding"></param>
        /// <returns></returns>
        string ReceiveString(Encoding encoding = null);

        /// <summary>发送数据</summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">位移</param>
        /// <param name="size">写入字节数</param>
        /// <param name="remoteEP">远程终结点</param>
        void Send(byte[] buffer, int offset, int size, EndPoint remoteEP = null);

        /// <summary>发送数据流</summary>
        /// <param name="stream"></param>
        /// <param name="remoteEP">远程终结点</param>
        /// <returns></returns>
        long Send(Stream stream, EndPoint remoteEP = null);

        /// <summary>发送字符串</summary>
        /// <param name="msg"></param>
        /// <param name="encoding"></param>
        /// <param name="remoteEP">远程终结点</param>
        void Send(string msg, Encoding encoding = null, EndPoint remoteEP = null);

        /// <summary>数据到达，在事件处理代码中，事件参数不得另作他用，套接字事件池将会将其回收。</summary>
        event EventHandler<NetEventArgs> Received;
    }
}