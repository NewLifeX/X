using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NewLife.Net.Sockets
{
    /// <summary>Socket客户端</summary>
    public abstract class SocketClient : SocketBase
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
            AddressFamily = address.AddressFamily;
            Client.Connect(address, port);
        }

        /// <summary>建立与远程主机的连接</summary>
        /// <param name="remoteEP">表示远程设备。</param>
        public void Connect(EndPoint remoteEP)
        {
            AddressFamily = remoteEP.AddressFamily;
            Client.Connect(remoteEP);
        }
        #endregion

        #region 接收
        /// <summary>开始异步接收数据</summary>
        /// <param name="e"></param>
        protected virtual void ReceiveAsync(NetEventArgs e)
        {
            if (!Client.IsBound) Bind();

            // 如果没有传入网络事件参数，从对象池借用
            if (e == null) e = Pop();

            if (!Client.ReceiveAsync(e)) RaiseCompleteAsync(e);
        }

        /// <summary>开始异步接收数据</summary>
        public void ReceiveAsync()
        {
            NetEventArgs e = Pop();
            try
            {
                ReceiveAsync(e);
            }
            catch
            {
                // 拿到参数e后，就应该对它负责
                // 如果当前的ReceiveAsync出错，应该归还
                Push(e);
                throw;
            }
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
                // 3，把回收责任交给别的方法
                ReceiveAsync(e);
                return;
            }

            //ProcessReceive(e);
            // 这里可以改造为使用多线程处理事件

            // 3，把回收责任交给别的方法
            ThreadPoolCallback(ProcessReceive, e);
        }

        /// <summary>处理接收</summary>
        /// <param name="e"></param>
        private void ProcessReceive(NetEventArgs e)
        {
            if (NoDelay) ReceiveAsync();

#if DEBUG
            Int32 n = e.BytesTransferred;
            if (n >= e.Buffer.Length || ProtocolType == ProtocolType.Tcp && n >= 1452 || ProtocolType == ProtocolType.Udp && n >= 1464)
            {
                WriteLog("接收的实际数据大小{0}超过了缓冲区大小，需要根据真实MTU调整缓冲区大小以提高效率！", n);
            }
#endif

            if (Received != null) Received(this, e);

            if (NoDelay)
                Push(e);
            else
                ReceiveAsync(e);
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
        /// <returns></returns>
        public virtual Int64 Send(Stream stream)
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

                Send(buffer, 0, n);
                total += n;

                if (n < buffer.Length) break;
            }
            return total;
        }

        /// <summary>发送数据</summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">位移</param>
        /// <param name="size">写入字节数</param>
        public virtual void Send(Byte[] buffer, Int32 offset, Int32 size) { Client.Send(buffer, offset, size, SocketFlags.None); }

        /// <summary>发送字符串</summary>
        /// <param name="msg"></param>
        /// <param name="encoding"></param>
        public void Send(String msg, Encoding encoding = null)
        {
            if (String.IsNullOrEmpty(msg)) return;

            if (encoding == null) encoding = Encoding.UTF8;
            Byte[] data = encoding.GetBytes(msg);
            Send(data, 0, data.Length);
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
    }
}