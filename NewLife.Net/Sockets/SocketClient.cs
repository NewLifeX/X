using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NewLife.Net.Sockets
{
    /// <summary>Socket客户端</summary>
    /// <remarks>
    /// 处理的过程中，即使使用异步，也允许事件订阅者阻塞<see cref="ISocket.NoDelay"/>下一次接收的开始<see cref="ReceiveAsync"/>，
    /// 因为事件订阅者可能需要处理完手头的数据才开始下一次接收。
    /// </remarks>
    public abstract class SocketClient : SocketBase, ISocketClient
    {
        #region 属性
        /// <summary>基础Socket对象</summary>
        public Socket Client
        {
            get
            {
                if (Socket == null) EnsureCreate();
                return Socket;
            }
            set { Socket = value; }
        }
        #endregion

        #region 连接
        /// <summary>建立与远程主机的连接</summary>
        /// <param name="hostname"></param>
        /// <param name="port"></param>
        public virtual void Connect(String hostname, Int32 port)
        {
            IPAddress[] addresses = Dns.GetHostAddresses(hostname);
            Connect(addresses[0], port);
        }

        /// <summary>建立与远程主机的连接</summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        public virtual void Connect(IPAddress address, Int32 port)
        {
            Connect(new IPEndPoint(address, Port));
        }

        /// <summary>建立与远程主机的连接</summary>
        /// <param name="remoteEP">表示远程设备。</param>
        public void Connect(EndPoint remoteEP)
        {
            AddressFamily = remoteEP.AddressFamily;
            Client.Connect(remoteEP);

            // 引发基类重设个地址参数
            Socket = Client;
        }
        #endregion

        #region 接收
        /// <summary>开始异步接收数据</summary>
        /// <param name="e"></param>
        public virtual void ReceiveAsync(NetEventArgs e = null)
        {
            StartAsync(Client.ReceiveAsync, e);
        }
        #endregion

        #region 事件
        /// <summary>数据到达，在事件处理代码中，事件参数不得另作他用，套接字事件池将会将其回收。</summary>
        public event EventHandler<NetEventArgs> Received;

        /// <summary>接收到数据时</summary>
        /// <remarks>
        /// 网络事件参数使用原则：
        /// 1，得到者负责回收（通过方法参数得到）
        /// 2，正常执行时自己负责回收，异常时顶级负责回收
        /// 3，把回收责任交给别的方法
        /// </remarks>
        /// <param name="e"></param>
        protected virtual void OnReceive(NetEventArgs e)
        {
            // Socket错误由各个处理器来处理
            if (e.SocketError != SocketError.Success)
            {
                OnError(e, null);
                return;
            }

            // 没有接收事件时，马上开始处理重建委托
            if (Received == null)
            {
                ReceiveAsync(e);
                return;
            }

            Process(e, ReceiveAsync, ProcessReceive);
        }

        void ProcessReceive(NetEventArgs e)
        {
            CheckBufferSize(e);
            if (Received != null) Received(this, e);
        }

        /// <summary>已重载。</summary>
        /// <param name="e"></param>
        protected override void OnComplete(NetEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Accept:
                    break;
                case SocketAsyncOperation.Connect:
                    break;
                case SocketAsyncOperation.Disconnect:
                    break;
                case SocketAsyncOperation.None:
                    break;
                case SocketAsyncOperation.Receive:
                case SocketAsyncOperation.ReceiveFrom:
                case SocketAsyncOperation.ReceiveMessageFrom:
                    OnReceive(e);
                    return;
                case SocketAsyncOperation.Send:
                    break;
                case SocketAsyncOperation.SendPackets:
                    break;
                case SocketAsyncOperation.SendTo:
                    break;
                default:
                    break;
            }

            base.OnComplete(e);
        }
        #endregion

        #region 发送
        /// <summary>发送数据流</summary>
        /// <param name="stream"></param>
        /// <param name="remoteEP"></param>
        /// <returns></returns>
        public virtual Int64 Send(Stream stream, EndPoint remoteEP = null)
        {
            Int64 total = 0;

            Byte[] buffer = new Byte[BufferSize];
            while (true)
            {
                Int32 n = stream.Read(buffer, 0, buffer.Length);
                if (n <= 0) break;

#if DEBUG
                if (n >= buffer.Length || ProtocolType == ProtocolType.Tcp && n >= 1452 || ProtocolType == ProtocolType.Udp && n >= 1464)
                {
                    WriteLog("接收的实际数据大小{0}超过了缓冲区大小，需要根据真实MTU调整缓冲区大小以提高效率！", n);
                }
#endif

                Send(buffer, 0, n, remoteEP);
                total += n;

                if (n < buffer.Length) break;
            }
            return total;
        }

        /// <summary>发送数据</summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">位移</param>
        /// <param name="size">写入字节数</param>
        /// <param name="remoteEP">远程终结点</param>
        public virtual void Send(Byte[] buffer, Int32 offset, Int32 size, EndPoint remoteEP = null) { Client.Send(buffer, offset, size, SocketFlags.None); }

        /// <summary>发送字符串</summary>
        /// <param name="msg"></param>
        /// <param name="encoding"></param>
        /// <param name="remoteEP">远程终结点</param>
        public void Send(String msg, Encoding encoding = null, EndPoint remoteEP = null)
        {
            if (String.IsNullOrEmpty(msg)) return;

            if (encoding == null) encoding = Encoding.UTF8;
            Byte[] data = encoding.GetBytes(msg);
            Send(data, 0, data.Length, remoteEP);
        }
        #endregion

        #region 接收
        /// <summary>接收数据</summary>
        /// <returns></returns>
        public Byte[] Receive()
        {
            Byte[] buffer = new Byte[BufferSize];
            if (!Client.IsBound) Bind();

            Int32 size = Client.Receive(buffer);
            if (size <= 0) return null;

            Byte[] data = new Byte[size];
            Buffer.BlockCopy(buffer, 0, data, 0, size);
            return data;
        }

        /// <summary>接收字符串</summary>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public String ReceiveString(Encoding encoding = null)
        {
            Byte[] buffer = Receive();
            if (buffer == null || buffer.Length < 1) return null;

            if (encoding == null) encoding = Encoding.UTF8;
            return encoding.GetString(buffer);
        }
        #endregion

        #region 辅助
        /// <summary>
        /// 已重载。
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var socket = base.Socket;
            if (socket != null && socket.Connected && socket.RemoteEndPoint != null) return base.ToString() + " => " + socket.RemoteEndPoint;

            return base.ToString();
        }
        #endregion
    }
}