using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NewLife.Net.Sockets;
using System.IO;

namespace NewLife.Net.Udp
{
    /// <summary>UDP服务器</summary>
    /// <remarks>主要针对APM模型进行简单封装</remarks>
    public class UdpServer : SocketServer, ISocketSession
    {
        #region 属性
        /// <summary>已重载。</summary>
        public override ProtocolType ProtocolType { get { return ProtocolType.Udp; } }
        #endregion

        #region 构造
        /// <summary>
        /// 构造
        /// </summary>
        public UdpServer() : base(IPAddress.Any, 0) { }

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="port"></param>
        public UdpServer(Int32 port) : base(IPAddress.Any, port) { }

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        public UdpServer(IPAddress address, Int32 port) : base(address, port) { }

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="hostname"></param>
        /// <param name="port"></param>
        public UdpServer(String hostname, Int32 port) : base(hostname, port) { }
        #endregion

        #region 开始停止
        /// <summary>开始</summary>
        protected override void OnStart()
        {
            Server.EnableBroadcast = true;
            Server.ExclusiveAddressUse = true;

            base.OnStart();

            // 设定委托
            // 指定10名工人待命，等待处理新连接
            // 一方面避免因没有及时安排工人而造成堵塞，另一方面避免工人中途死亡或逃跑而导致无人迎宾
            for (int i = 0; i < 10 * Environment.ProcessorCount; i++)
            {
                ReceiveAsync();
            }
        }

        /// <summary>开始异步接收数据</summary>
        /// <param name="e"></param>
        protected virtual void ReceiveAsync(NetEventArgs e)
        {
            if (!Server.IsBound) Bind();

            // 如果没有传入网络事件参数，从对象池借用
            if (e == null) e = Pop();

            //e.RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            // 兼容IPV6
            IPAddress address = AddressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any;
            e.RemoteEndPoint = new IPEndPoint(address, 0);

            if (!Server.ReceiveFromAsync(e))
            {
                if (e.BytesTransferred > 0)
                    RaiseCompleteAsync(e);
                else
                    Push(e);
            }
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
        /// <summary>数据到达。在事件处理代码中，事件参数不得另作他用，套接字事件池将会将其回收。</summary>
        public event EventHandler<NetEventArgs> Received;

        /// <summary>接收到数据时</summary>
        /// <param name="e"></param>
        protected virtual void OnReceive(NetEventArgs e)
        {
            // Socket错误由各个处理器来处理
            if (e.SocketError == SocketError.OperationAborted)
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

            //ProcessReceive(e);
            // 这里可以改造为使用多线程处理事件

            ThreadPoolCallback(ProcessReceive, e);
        }

        /// <summary>处理接收</summary>
        /// <param name="e"></param>
        private void ProcessReceive(NetEventArgs e)
        {
            if (NoDelay && e.SocketError != SocketError.OperationAborted) ReceiveAsync();

            try
            {
                // Socket错误由各个处理器来处理
                if (e.SocketError != SocketError.Success)
                {
                    OnError(e, null);
                    // OnError里面已经被回收，赋值为null，否则后面finally里面的Push会出错
                    e = null;
                    return;
                }

#if DEBUG
                Int32 n = e.BytesTransferred;
                if (n >= e.Buffer.Length || ProtocolType == ProtocolType.Tcp && n >= 1452 || ProtocolType == ProtocolType.Udp && n >= 1464)
                {
                    WriteLog("接收的实际数据大小{0}超过了缓冲区大小，需要根据真实MTU调整缓冲区大小以提高效率！", n);
                }
#endif

                if (Received != null) Received(this, e);
            }
            finally
            {
                if (NoDelay)
                    Push(e);
                else
                    ReceiveAsync(e);
            }
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

                Send(buffer, 0, n);
                total += n;

                if (n < buffer.Length) break;
            }
            return total;
        }

        /// <summary>向指定目的地发送信息</summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        /// <param name="remoteEP"></param>
        public void Send(Byte[] buffer, Int32 offset, Int32 size, EndPoint remoteEP = null) { Server.SendTo(buffer, offset, size, SocketFlags.None, remoteEP); }

        /// <summary>向指定目的地发送信息</summary>
        /// <param name="buffer"></param>
        /// <param name="remoteEP"></param>
        public void Send(Byte[] buffer, EndPoint remoteEP = null) { Send(buffer, 0, buffer.Length, remoteEP); }

        /// <summary>向指定目的地发送信息</summary>
        /// <param name="message"></param>
        /// <param name="encoding"></param>
        /// <param name="remoteEP"></param>
        public void Send(String message, Encoding encoding = null, EndPoint remoteEP = null)
        {
            Byte[] buffer = Encoding.UTF8.GetBytes(message);
            Send(buffer, 0, buffer.Length, remoteEP);
        }
        #endregion

        #region 接收
        /// <summary>接收数据</summary>
        /// <returns></returns>
        public Byte[] Receive()
        {
            Byte[] buffer = new Byte[BufferSize];
            if (!Server.IsBound) Bind();

            Int32 size = Server.Receive(buffer);
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