using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NewLife.Net
{
    /// <summary>Socket扩展</summary>
    public static class SocketHelper
    {
        /// <summary>异步发送数据</summary>
        /// <param name="socket"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static Task<Int32> SendAsync(this Socket socket, Byte[] buffer)
        {
            var task = Task<Int32>.Factory.FromAsync<Byte[]>((Byte[] buf, AsyncCallback callback, Object state) =>
            {
                return socket.BeginSend(buf, 0, buf.Length, SocketFlags.None, callback, state);
            }, socket.EndSend, buffer, null);

            return task;
        }

        /// <summary>异步发送数据</summary>
        /// <param name="socket"></param>
        /// <param name="buffer"></param>
        /// <param name="remote"></param>
        /// <returns></returns>
        public static Task<Int32> SendToAsync(this Socket socket, Byte[] buffer, IPEndPoint remote)
        {
            var task = Task<Int32>.Factory.FromAsync<Byte[], IPEndPoint>((Byte[] buf, IPEndPoint ep, AsyncCallback callback, Object state) =>
            {
                return socket.BeginSendTo(buf, 0, buf.Length, SocketFlags.None, ep, callback, state);
            }, socket.EndSendTo, buffer, remote, null);

            return task;
        }

        /// <summary>发送数据流</summary>
        /// <param name="socket"></param>
        /// <param name="stream"></param>
        /// <param name="remoteEP"></param>
        /// <returns>返回自身，用于链式写法</returns>
        public static Socket Send(this Socket socket, Stream stream, IPEndPoint remoteEP = null)
        {
            Int64 total = 0;

            var size = 1472;
            var buffer = new Byte[size];
            while (true)
            {
                var n = stream.Read(buffer, 0, buffer.Length);
                if (n <= 0) break;

                socket.SendTo(buffer, 0, n, SocketFlags.None, remoteEP);
                total += n;

                if (n < buffer.Length) break;
            }
            return socket;
        }

        /// <summary>向指定目的地发送信息</summary>
        /// <param name="socket"></param>
        /// <param name="buffer">缓冲区</param>
        /// <param name="remoteEP"></param>
        /// <returns>返回自身，用于链式写法</returns>
        public static Socket Send(this Socket socket, Byte[] buffer, IPEndPoint remoteEP = null)
        {
            socket.SendTo(buffer, 0, buffer.Length, SocketFlags.None, remoteEP);

            return socket;
        }

        /// <summary>向指定目的地发送信息</summary>
        /// <param name="socket"></param>
        /// <param name="message"></param>
        /// <param name="encoding">文本编码，默认null表示UTF-8编码</param>
        /// <param name="remoteEP"></param>
        /// <returns>返回自身，用于链式写法</returns>
        public static Socket Send(this Socket socket, String message, Encoding encoding = null, IPEndPoint remoteEP = null)
        {
            if (encoding == null)
                Send(socket, Encoding.UTF8.GetBytes(message), remoteEP);
            else
                Send(socket, encoding.GetBytes(message), remoteEP);
            return socket;
        }

        /// <summary>广播数据包</summary>
        /// <param name="socket"></param>
        /// <param name="buffer">缓冲区</param>
        /// <param name="port"></param>
        public static Socket Broadcast(this Socket socket, Byte[] buffer, Int32 port)
        {
            if (socket != null && socket.LocalEndPoint != null)
            {
                //var ip = socket.Client.LocalEndPoint as IPEndPoint;
                if (socket.LocalEndPoint.AddressFamily == AddressFamily.InterNetworkV6) throw new NotSupportedException("IPv6不支持广播！");
            }

            if (!socket.EnableBroadcast) socket.EnableBroadcast = true;

            socket.SendTo(buffer, 0, buffer.Length, SocketFlags.None, new IPEndPoint(IPAddress.Broadcast, port));

            return socket;
        }

        /// <summary>广播字符串</summary>
        /// <param name="socket"></param>
        /// <param name="message"></param>
        /// <param name="port"></param>
        public static Socket Broadcast(this Socket socket, String message, Int32 port)
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            return Broadcast(socket, buffer, port);
        }

        /// <summary>接收字符串</summary>
        /// <param name="socket"></param>
        /// <param name="encoding">文本编码，默认null表示UTF-8编码</param>
        /// <returns></returns>
        public static String ReceiveString(this Socket socket, Encoding encoding = null)
        {
            EndPoint ep = null;

            var buf = new Byte[1460];
            var len = socket.ReceiveFrom(buf, ref ep);
            if (len < 1) return null;

            if (encoding == null) encoding = Encoding.UTF8;
            return encoding.GetString(buf, 0, len);
        }
    }
}