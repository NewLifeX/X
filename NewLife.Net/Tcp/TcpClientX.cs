using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NewLife.Net.Sockets;

namespace NewLife.Net.Tcp
{
    /// <summary>增强TCP客户端</summary>
    public class TcpClientX : SocketClient, ISocketSession
    {
        #region 属性
        /// <summary>已重载。</summary>
        public override ProtocolType ProtocolType { get { return ProtocolType.Tcp; } }

        private Boolean _DisconnectWhenEmptyData = true;
        /// <summary>收到空数据时抛出异常并断开连接。</summary>
        public Boolean DisconnectWhenEmptyData { get { return _DisconnectWhenEmptyData; } set { _DisconnectWhenEmptyData = value; } }

        private Int32 _ID;
        /// <summary>编号</summary>
        Int32 ISocketSession.ID { get { return _ID; } set { if (_ID > 0)throw new NetException("禁止修改会话编号！"); _ID = value; } }

        /// <summary>宿主对象。</summary>
        ISocket ISocketSession.Host { get { return this; } }
        #endregion

        #region 重载
        /// <summary>已重载。设置RemoteEndPoint</summary>
        /// <param name="e"></param>
        protected override void OnComplete(NetEventArgs e)
        {
            SetRemote(e);
            base.OnComplete(e);
        }

        internal void SetRemote(NetEventArgs e)
        {
            IPEndPoint ep = e.RemoteEndPoint as IPEndPoint;
            if ((ep == null || ep.Address.IsAny() && ep.Port == 0) && RemoteEndPoint != null) e.RemoteEndPoint = RemoteEndPoint;
        }

        /// <summary>处理接收到的数据</summary>
        /// <param name="e"></param>
        protected internal override void ProcessReceive(NetEventArgs e)
        {
            if (e.Session == null) e.Session = CreateSession();

            base.ProcessReceive(e);

            if (_Received != null) _Received(this, new ReceivedEventArgs(e.GetStream()));
        }
        #endregion

        #region 方法
        private Boolean _hasStarted = false;
        /// <summary>开始异步接收，同时处理传入的事件参数，里面可能有接收到的数据</summary>
        /// <param name="e"></param>
        internal void Start(NetEventArgs e)
        {
            if (_hasStarted) return;
            _hasStarted = true;

            if (e.BytesTransferred > 0) ProcessReceive(e);

            ReceiveAsync();
        }

        ///// <summary>断开客户端连接。Tcp端口，UdpClient不处理</summary>
        //public void Disconnect()
        //{
        //    //var socket = Socket;
        //    //if (socket != null && socket.Connected) socket.Disconnect(ReuseAddress);

        //    // 释放
        //    Dispose();
        //}
        #endregion

        #region 发送
        /// <summary>发送数据流</summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        /// <returns>返回自身，用于链式写法</returns>
        public virtual ISocketSession Send(Stream stream)
        {
            Int64 total = 0;

            var size = stream.CanSeek ? stream.Length - stream.Position : BufferSize;
            Byte[] buffer = new Byte[size];
            while (true)
            {
                Int32 n = stream.Read(buffer, 0, buffer.Length);
                if (n <= 0) break;

                Send(buffer, 0, n);
                total += n;

                if (n < buffer.Length) break;
            }
            //return total;
            return this;
        }

        /// <summary>发送数据</summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">位移</param>
        /// <param name="size">写入字节数</param>
        /// <returns>返回自身，用于链式写法</returns>
        public virtual ISocketSession Send(Byte[] buffer, Int32 offset = 0, Int32 size = 0)
        {
            if (!Client.IsBound) Bind();

            if (size <= 0) size = buffer.Length - offset;
            Client.Send(buffer, offset, size, SocketFlags.None);

            return this;
        }

        /// <summary>发送字符串</summary>
        /// <param name="msg"></param>
        /// <param name="encoding"></param>
        /// <returns>返回自身，用于链式写法</returns>
        public ISocketSession Send(String msg, Encoding encoding = null)
        {
            if (String.IsNullOrEmpty(msg)) return this;

            if (encoding == null) encoding = Encoding.UTF8;
            Send(encoding.GetBytes(msg), 0, 0);

            return this;
        }
        #endregion

        #region 接收
        private Boolean _UseReceiveAsync;
        /// <summary>是否异步接收数据</summary>
        public Boolean UseReceiveAsync { get { return _UseReceiveAsync; } }

        /// <summary>开始异步接收数据</summary>
        void ISocketSession.ReceiveAsync() { ReceiveAsync(null); }

        /// <summary>开始异步接收数据</summary>
        /// <param name="e"></param>
        public override void ReceiveAsync(NetEventArgs e = null)
        {
            _UseReceiveAsync = true;
            base.ReceiveAsync(e);
            //Received += new EventHandler<NetEventArgs>(TcpClientX_Received);
        }

        //void TcpClientX_Received(object sender, NetEventArgs e)
        //{
        //    if (_Received != null) _Received(this, new ReceivedEventArgs(e.GetStream()));
        //}

        /// <summary>接收数据。已重载。接收到0字节表示连接断开！</summary>
        /// <param name="e"></param>
        protected override void OnReceive(NetEventArgs e)
        {
            if (e.BytesTransferred > 0 || !DisconnectWhenEmptyData)
                base.OnReceive(e);
            else
                OnError(e, null);
        }

        private event EventHandler<ReceivedEventArgs> _Received;
        /// <summary>数据到达，在事件处理代码中，事件参数不得另作他用，套接字事件池将会将其回收。</summary>
        event EventHandler<ReceivedEventArgs> ISocketSession.Received
        {
            add { _Received += value; }
            remove { _Received -= value; }
        }
        #endregion

        #region 创建会话
        /// <summary>为指定地址创建会话。对于无连接Socket，必须指定远程地址；对于有连接Socket，指定的远程地址将不起任何作用</summary>
        /// <param name="remoteEP"></param>
        /// <returns></returns>
        public override ISocketSession CreateSession(IPEndPoint remoteEP = null) { return this; }
        #endregion
    }
}