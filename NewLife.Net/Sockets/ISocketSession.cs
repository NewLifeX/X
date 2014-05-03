using System;
using System.IO;
using System.Text;
using System.Net;

namespace NewLife.Net.Sockets
{
    /// <summary>用于与对方进行通讯的Socket会话，仅具有收发功能，也专用于上层应用收发数据</summary>
    /// <remarks>
    /// 对于Tcp来说，它就是<see cref="Tcp.TcpClientX"/>自身，不管客户端还是服务端的会话。
    /// 对于Udp来说，需要额外创建一个对象，包括自身和远程地址。
    /// 
    /// Socket会话发送数据不需要指定远程地址，因为内部已经具有。
    /// 接收数据时，Tcp接收全部数据，而Udp只接受来自所属远方的数据。
    /// 
    /// Socket会话不具有连接和断开的能力，所以需要外部连接好之后再创建Socket会话。
    /// 但是会话可以销毁，来代替断开。
    /// 对于Udp额外创建的会话来说，仅仅销毁会话而已。
    /// 
    /// 所以，它必须具有收发数据的能力。
    /// </remarks>
    public interface ISocketSession : ISocketAddress, IDisposable2
    {
        #region 属性
        /// <summary>编号</summary>
        Int32 ID { get; set; }

        /// <summary>宿主对象。除了<see cref="Udp.UdpServer"/>外，都是<see cref="ISocketClient"/>接口。</summary>
        ISocket Host { get; }

        /// <summary>会话数据流，供用户程序使用，内部不做处理。可用于解决Tcp粘包的问题，把多余的分片放入该数据流中。</summary>
        Stream Stream { get; set; }

        /// <summary>远程地址</summary>
        NetUri RemoteUri { get; }

        /// <summary>远程终结点</summary>
        IPEndPoint RemoteEndPoint { get; }
        #endregion

        #region 发送
        /// <summary>发送数据</summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">位移</param>
        /// <param name="size">写入字节数</param>
        /// <returns>返回自身，用于链式写法</returns>
        ISocketSession Send(byte[] buffer, int offset = 0, int size = 0);

        ///// <summary>发送数据流</summary>
        ///// <param name="stream"></param>
        ///// <returns></returns>
        ///// <returns>返回自身，用于链式写法</returns>
        //ISocketSession Send(Stream stream);

        ///// <summary>发送字符串</summary>
        ///// <param name="msg"></param>
        ///// <param name="encoding"></param>
        ///// <returns>返回自身，用于链式写法</returns>
        //ISocketSession Send(string msg, Encoding encoding = null);
        #endregion

        #region 接收
        /// <summary>是否异步接收数据</summary>
        Boolean UseReceiveAsync { get; }

        /// <summary>开始异步接收数据</summary>
        void ReceiveAsync();

        /// <summary>接收数据</summary>
        /// <returns></returns>
        byte[] Receive();

        ///// <summary>接收字符串</summary>
        ///// <param name="encoding"></param>
        ///// <returns></returns>
        //string ReceiveString(Encoding encoding = null);

        /// <summary>数据到达，在事件处理代码中，事件参数不得另作他用，套接字事件池将会将其回收。</summary>
        event EventHandler<ReceivedEventArgs> Received;
        #endregion
    }

    /// <summary>Socket会话扩展</summary>
    public static class SocketSessionHelper
    {
        #region 发送
        /// <summary>发送数据流</summary>
        /// <param name="session">会话</param>
        /// <param name="stream"></param>
        /// <returns></returns>
        /// <returns>返回自身，用于链式写法</returns>
        public static ISocketSession Send(this ISocketSession session, Stream stream)
        {
            var size = 1460;
            var buffer = new Byte[size];
            while (true)
            {
                var n = stream.Read(buffer, 0, buffer.Length);
                if (n <= 0) break;

                session.Send(buffer, 0, n);

                if (n < buffer.Length) break;
            }
            return session;
        }

        /// <summary>发送字符串</summary>
        /// <param name="session">会话</param>
        /// <param name="msg"></param>
        /// <param name="encoding"></param>
        /// <returns>返回自身，用于链式写法</returns>
        public static ISocketSession Send(this ISocketSession session, String msg, Encoding encoding = null)
        {
            if (String.IsNullOrEmpty(msg)) return session;

            if (encoding == null) encoding = Encoding.UTF8;
            session.Send(encoding.GetBytes(msg), 0, 0);

            return session;
        }
        #endregion

        #region 接收
        /// <summary>接收字符串</summary>
        /// <param name="session">会话</param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static String ReceiveString(this ISocketSession session, Encoding encoding = null)
        {
            var buffer = session.Receive();
            if (buffer == null || buffer.Length < 1) return null;

            if (encoding == null) encoding = Encoding.UTF8;
            return encoding.GetString(buffer);
        }
        #endregion
    }
}