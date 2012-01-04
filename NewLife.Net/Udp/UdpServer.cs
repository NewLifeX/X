using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NewLife.Net.Sockets;

namespace NewLife.Net.Udp
{
    /// <summary>UDP服务器</summary>
    /// <remarks>
    /// 核心工作：启动服务<see cref="OnStart"/>时，监听端口，并启用多个（逻辑处理器数的10倍）异步接收操作<see cref="ReceiveAsync"/>。
    /// 接收到的数据全部转接到<see cref="Received"/>事件中。
    /// 
    /// 服务器完全处于异步工作状态，任何操作都不可能被阻塞。
    /// 
    /// <see cref="ISocket.NoDelay"/>的设置会影响异步操作数，不启用时，只有一个异步操作。
    /// </remarks>
    public class UdpServer : SocketServer, ISocketSession
    {
        #region 属性
        /// <summary>已重载。</summary>
        public override ProtocolType ProtocolType { get { return ProtocolType.Udp; } }

        ///// <summary>套接字</summary>
        //Socket ISocketSession.Socket { get { return base.Server; } set { base.Server = value; } }

        private Int32 _ID;
        /// <summary>编号</summary>
        Int32 ISocketSession.ID { get { return _ID; } set { _ID = value; } }
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
            //Server.ExclusiveAddressUse = true;

            base.OnStart();

            // 设定委托
            // 指定10名工人待命，等待处理新连接
            // 一方面避免因没有及时安排工人而造成堵塞，另一方面避免工人中途死亡或逃跑而导致无人迎宾

            //Int32 count = NoDelay ? 10 * Environment.ProcessorCount : 1;
            //for (int i = 0; i < count; i++)
            // 这里不能开多个，否则可能会造成不同事件的RemoteEndPoint错乱
            // 这里http://stackoverflow.com/questions/5802998/is-this-receivefromasync-bug
            // 暂时未找到根本原因，先这样用着
            {
                ReceiveAsync();
            }
        }

        /// <summary>开始异步接收数据</summary>
        /// <param name="e"></param>
        public virtual void ReceiveAsync(NetEventArgs e = null)
        {
            StartAsync(ev =>
            {
                // 兼容IPV6
                IPAddress address = AddressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any;
                ev.RemoteEndPoint = new IPEndPoint(address, 0);
                // 不能用ReceiveAsync，否则得不到远程地址
                return Server.ReceiveFromAsync(ev);
            }, e);
        }

        ///// <summary>开始异步接收，同时处理传入的事件参数，里面可能有接收到的数据</summary>
        ///// <param name="e"></param>
        //void ISocketSession.Start(NetEventArgs e) { }

        /// <summary>断开客户端连接。Tcp断开，UdpClient不处理</summary>
        void ISocketSession.Disconnect() { }
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

            Process(e, ReceiveAsync, ProcessReceive);
        }

        void ProcessReceive(NetEventArgs e)
        {
            // 统计接收数
            IncCounter();

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

            var size = stream.CanSeek ? stream.Length - stream.Position : BufferSize;
            Byte[] buffer = new Byte[size];
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