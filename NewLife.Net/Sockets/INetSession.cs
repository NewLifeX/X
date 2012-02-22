using System;
using System.IO;
using System.Net;
using System.Text;

namespace NewLife.Net.Sockets
{
    /// <summary>网络服务会话接口</summary>
    /// <remarks>
    /// 所有应用服务器以会话<see cref="INetSession"/>作为业务处理核心。
    /// 应用服务器收到新会话请求后，通过<see cref="Start"/>启动一个会话处理。
    /// 会话进行业务处理的过程中，可以通过多个Send方法向客户端发送数据。
    /// </remarks>
    public interface INetSession : IDisposable2
    {
        #region 属性
        /// <summary>编号</summary>
        Int32 ID { get; set; }

        /// <summary>主服务</summary>
        NetServer Host { get; set; }

        /// <summary>Socket服务器。当前通讯所在的Socket服务器，其实是TcpServer/UdpServer</summary>
        ISocketServer Server { get; set; }

        /// <summary>客户端。跟客户端通讯的那个Socket，其实是服务端TcpClientX/UdpServer</summary>
        ISocketSession Session { get; set; }

        /// <summary>客户端远程IP终结点</summary>
        IPEndPoint ClientEndPoint { get; set; }
        #endregion

        #region 方法
        /// <summary>开始会话处理。参数e里面可能含有数据</summary>
        /// <param name="e"></param>
        void Start(NetEventArgs e);
        #endregion

        #region 发送
        /// <summary>发送数据</summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">位移</param>
        /// <param name="size">写入字节数</param>
        void Send(byte[] buffer, int offset = 0, int size = 0);

        /// <summary>发送数据流</summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        long Send(Stream stream);

        /// <summary>发送字符串</summary>
        /// <param name="msg"></param>
        /// <param name="encoding"></param>
        void Send(string msg, Encoding encoding = null);
        #endregion
    }
}