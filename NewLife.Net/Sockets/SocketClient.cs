using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NewLife.Messaging;

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

        #region 构造
        /// <summary>实例化</summary>
        public SocketClient()
        {
            // 客户端可能需要阻塞，默认打开延迟
            NoDelay = false;
        }
        #endregion

        #region 连接
        /// <summary>建立与远程主机的连接</summary>
        /// <param name="hostname"></param>
        /// <param name="port"></param>
        public virtual void Connect(String hostname, Int32 port) { Connect(NetHelper.ParseAddress(hostname), port); }

        /// <summary>建立与远程主机的连接</summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        public virtual void Connect(IPAddress address, Int32 port) { Connect(new IPEndPoint(address, port)); }

        /// <summary>建立与远程主机的连接</summary>
        /// <param name="remoteEP">表示远程设备。</param>
        public void Connect(EndPoint remoteEP)
        {
            AddressFamily = remoteEP.AddressFamily;
            if (!Client.IsBound) Bind();
            Client.Connect(remoteEP);

            // 引发基类重设个地址参数
            Socket = Client;
        }
        #endregion

        #region 异步开始
        /// <summary>开始异步接收数据</summary>
        /// <param name="e"></param>
        public virtual void ReceiveAsync(NetEventArgs e = null)
        {
            // 这里居然在委托的CtorClosed方法里面报this为空对错误
            StartAsync(ev =>
            {
                var client = Client;
                if (client == null) return false;
                return client.ReceiveAsync(ev);
            }, e);
        }
        //public virtual void ReceiveAsync(NetEventArgs e = null) { StartAsync(Client.ReceiveAsync, e); }
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

        /// <summary>处理接收到的数据</summary>
        /// <param name="e"></param>
        internal protected void ProcessReceive(NetEventArgs e)
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
                case SocketAsyncOperation.Receive:
                case SocketAsyncOperation.ReceiveFrom:
                case SocketAsyncOperation.ReceiveMessageFrom:
                    OnReceive(e);
                    return;
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
        public virtual void Send(Byte[] buffer, Int32 offset = 0, Int32 size = 0, EndPoint remoteEP = null)
        {
            if (!Client.IsBound) Bind();

            if (size <= 0) size = buffer.Length - offset;
            if (remoteEP != null && Socket == null) AddressFamily = remoteEP.AddressFamily;
            if (remoteEP != null && ProtocolType == ProtocolType.Udp)
                Client.SendTo(buffer, offset, size, SocketFlags.None, remoteEP);
            else
                Client.Send(buffer, offset, size, SocketFlags.None);
        }

        /// <summary>发送字符串</summary>
        /// <param name="msg"></param>
        /// <param name="encoding"></param>
        /// <param name="remoteEP">远程终结点</param>
        public void Send(String msg, Encoding encoding = null, EndPoint remoteEP = null)
        {
            if (String.IsNullOrEmpty(msg)) return;

            if (encoding == null) encoding = Encoding.UTF8;
            Send(encoding.GetBytes(msg), 0, 0, remoteEP);
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

        #region 消息提供者
        private IMessageProvider _provider;
        /// <summary>获取该客户端对应的消息提供者，用于直接操作消息</summary>
        /// <returns></returns>
        public IMessageProvider GetMessageProvider()
        {
            if (_provider == null) _provider = new ClientMessageProvider(this, RemoteEndPoint);
            return _provider;
        }

        class ClientMessageProvider : MessageProvider
        {
            private ISocketClient _Client;
            /// <summary>客户端</summary>
            public ISocketClient Client { get { return _Client; } set { _Client = value; } }

            private IPEndPoint _Remote;
            /// <summary>远程</summary>
            public IPEndPoint Remote { get { return _Remote; } set { _Remote = value; } }

            public ClientMessageProvider(ISocketClient client, IPEndPoint ep)
            {
                Client = client;
                Remote = ep;

                client.Received += new EventHandler<NetEventArgs>(client_Received);
            }

            void client_Received(object sender, NetEventArgs e)
            {
                var message = Message.Read(e.GetStream());
                OnReceive(message);
            }

            public override void Send(Message message)
            {
                Client.Send(message.GetStream(), Remote);
            }
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