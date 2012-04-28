using System;
using System.IO;
using System.Net;
using System.Text;
using NewLife.Net.Sockets;

namespace NewLife.Net.Udp
{
    /// <summary>Udp接口</summary>
    interface IUdp
    {
        /// <summary>缓冲区大小</summary>
        Int32 BufferSize { get; }

        /// <summary>发送数据</summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">位移</param>
        /// <param name="size">写入字节数</param>
        /// <param name="remoteEP">远程终结点</param>
        /// <returns>返回自身，用于链式写法</returns>
        void Send(byte[] buffer, int offset = 0, int size = 0, EndPoint remoteEP = null);

        /// <summary>异步接收</summary>
        void ReceiveAsync();

        /// <summary>接收数据</summary>
        /// <returns></returns>
        Byte[] Receive();

        /// <summary>数据到达，在事件处理代码中，事件参数不得另作他用，套接字事件池将会将其回收。</summary>
        event EventHandler<NetEventArgs> Received;
    }

    /// <summary>Udp扩展</summary>
    static class UdpHelper
    {
        /// <summary>发送数据流</summary>
        /// <param name="udp"></param>
        /// <param name="stream"></param>
        /// <param name="remoteEP"></param>
        /// <returns>返回自身，用于链式写法</returns>
        public static IUdp Send(this IUdp udp, Stream stream, EndPoint remoteEP = null)
        {
            Int64 total = 0;

            //var size = stream.CanSeek ? stream.Length - stream.Position : udp.BufferSize;
            var size = 1472;
            Byte[] buffer = new Byte[size];
            while (true)
            {
                Int32 n = stream.Read(buffer, 0, buffer.Length);
                if (n <= 0) break;

                udp.Send(buffer, 0, n, remoteEP);
                total += n;

                if (n < buffer.Length) break;
            }
            return udp;
        }

        /// <summary>向指定目的地发送信息</summary>
        /// <param name="udp"></param>
        /// <param name="buffer"></param>
        /// <param name="remoteEP"></param>
        /// <returns>返回自身，用于链式写法</returns>
        public static IUdp Send(this IUdp udp, Byte[] buffer, EndPoint remoteEP = null) { udp.Send(buffer, 0, buffer.Length, remoteEP); return udp; }

        /// <summary>向指定目的地发送信息</summary>
        /// <param name="udp"></param>
        /// <param name="message"></param>
        /// <param name="encoding"></param>
        /// <param name="remoteEP"></param>
        /// <returns>返回自身，用于链式写法</returns>
        public static IUdp Send(this IUdp udp, String message, Encoding encoding = null, EndPoint remoteEP = null)
        {
            //Send(udp, Encoding.UTF8.GetBytes(message), remoteEP);

            if (encoding == null)
                Send(udp, Encoding.UTF8.GetBytes(message), remoteEP);
            else
                Send(udp, encoding.GetBytes(message), remoteEP);
            return udp;
        }

        /// <summary>接收字符串</summary>
        /// <param name="udp"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static String ReceiveString(this IUdp udp, Encoding encoding = null)
        {
            Byte[] buffer = udp.Receive();
            if (buffer == null || buffer.Length < 1) return null;

            if (encoding == null) encoding = Encoding.UTF8;
            return encoding.GetString(buffer);
        }
    }
}