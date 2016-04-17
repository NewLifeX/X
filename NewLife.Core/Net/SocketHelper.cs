using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NewLife.Net
{
    /// <summary>Socket扩展</summary>
    public static class SocketHelper
    {
        /// <summary>发送数据流</summary>
        /// <param name="udp"></param>
        /// <param name="stream"></param>
        /// <param name="remoteEP"></param>
        /// <returns>返回自身，用于链式写法</returns>
        public static Socket Send(this Socket udp, Stream stream, IPEndPoint remoteEP = null)
        {
            Int64 total = 0;

            var size = 1472;
            var buffer = new Byte[size];
            while (true)
            {
                var n = stream.Read(buffer, 0, buffer.Length);
                if (n <= 0) break;

                udp.SendTo(buffer, 0, n, SocketFlags.None, remoteEP);
                total += n;

                if (n < buffer.Length) break;
            }
            return udp;
        }

        /// <summary>向指定目的地发送信息</summary>
        /// <param name="udp"></param>
        /// <param name="buffer">缓冲区</param>
        /// <param name="remoteEP"></param>
        /// <returns>返回自身，用于链式写法</returns>
        public static Socket Send(this Socket udp, Byte[] buffer, IPEndPoint remoteEP = null)
        {
            udp.SendTo(buffer, 0, buffer.Length, SocketFlags.None, remoteEP);

            return udp;
        }

        /// <summary>向指定目的地发送信息</summary>
        /// <param name="udp"></param>
        /// <param name="message"></param>
        /// <param name="encoding">文本编码，默认null表示UTF-8编码</param>
        /// <param name="remoteEP"></param>
        /// <returns>返回自身，用于链式写法</returns>
        public static Socket Send(this Socket udp, String message, Encoding encoding = null, IPEndPoint remoteEP = null)
        {
            if (encoding == null)
                Send(udp, Encoding.UTF8.GetBytes(message), remoteEP);
            else
                Send(udp, encoding.GetBytes(message), remoteEP);
            return udp;
        }

        /// <summary>广播数据包</summary>
        /// <param name="udp"></param>
        /// <param name="buffer">缓冲区</param>
        /// <param name="port"></param>
        public static Socket Broadcast(this Socket udp, Byte[] buffer, Int32 port)
        {
            if (udp != null && udp.LocalEndPoint != null)
            {
                //var ip = udp.Client.LocalEndPoint as IPEndPoint;
                if (udp.LocalEndPoint.AddressFamily == AddressFamily.InterNetworkV6) throw new NotSupportedException("IPv6不支持广播！");
            }

            if (!udp.EnableBroadcast) udp.EnableBroadcast = true;

            udp.SendTo(buffer, 0, buffer.Length, SocketFlags.None, new IPEndPoint(IPAddress.Broadcast, port));

            return udp;
        }

        /// <summary>广播字符串</summary>
        /// <param name="udp"></param>
        /// <param name="message"></param>
        /// <param name="port"></param>
        public static Socket Broadcast(this Socket udp, String message, Int32 port)
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            return Broadcast(udp, buffer, port);
        }

        /// <summary>接收字符串</summary>
        /// <param name="udp"></param>
        /// <param name="encoding">文本编码，默认null表示UTF-8编码</param>
        /// <returns></returns>
        public static String ReceiveString(this Socket udp, Encoding encoding = null)
        {
            EndPoint ep = null;

            var buf = new Byte[1460];
            var len = udp.ReceiveFrom(buf, ref ep);
            if (len < 1) return null;

            if (encoding == null) encoding = Encoding.UTF8;
            return encoding.GetString(buf, 0, len);
        }
    }
}