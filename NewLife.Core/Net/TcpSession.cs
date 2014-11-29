using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace NewLife.Net
{
    /// <summary>增强TCP客户端</summary>
    public class TcpSession : SessionBase, ISocketSession
    {
        #region 属性
        private TcpClient _Client;
        /// <summary>客户端</summary>
        public TcpClient Client { get { return _Client; } set { _Client = value; } }

        //private Boolean _DisconnectWhenEmptyData = false;
        ///// <summary>收到空数据时抛出异常并断开连接。</summary>
        //public Boolean DisconnectWhenEmptyData { get { return _DisconnectWhenEmptyData; } set { _DisconnectWhenEmptyData = value; } }

        //private Stream _Stream;
        ///// <summary>会话数据流，供用户程序使用，内部不做处理。可用于解决Tcp粘包的问题，把多余的分片放入该数据流中。</summary>
        //public Stream Stream { get { return _Stream; } set { _Stream = value; } }
        #endregion

        #region 构造
        /// <summary>实例化增强UDP</summary>
        public TcpSession() { Local = new NetUri(ProtocolType.Tcp, IPAddress.Any, 0); }

        /// <summary>使用监听口初始化</summary>
        /// <param name="listenPort"></param>
        public TcpSession(Int32 listenPort)
            : this()
        {
            Port = listenPort;
        }
        #endregion

        #region 方法
        /// <summary>打开</summary>
        protected override Boolean OnOpen()
        {
            if (Client == null || !Client.Client.IsBound)
            {
                Client = new TcpClient(Local.EndPoint);
                if (Timeout > 0) Client.Client.ReceiveTimeout = Timeout;

                if (Remote != null) Client.Connect(Remote.EndPoint);
            }

            return true;
        }

        /// <summary>关闭</summary>
        protected override Boolean OnClose()
        {
            if (Client != null) Client.Close();
            Client = null;

            return true;
        }

        /// <summary>连接</summary>
        /// <param name="remoteEP"></param>
        /// <returns></returns>
        protected override Boolean OnConnect(IPEndPoint remoteEP)
        {
            Client.Connect(remoteEP);

            return true;
        }

        /// <summary>发送数据</summary>
        /// <remarks>
        /// 目标地址由<seealso cref="SessionBase.Remote"/>决定，如需精细控制，可直接操作<seealso cref="Client"/>
        /// </remarks>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">偏移</param>
        /// <param name="count">数量</param>
        public override void Send(Byte[] buffer, Int32 offset = 0, Int32 count = -1)
        {
            Open();

            if (count < 0) count = buffer.Length - offset;

            Stream.Write(buffer, 0, count);
        }

        /// <summary>接收数据</summary>
        /// <returns></returns>
        public override Byte[] Receive()
        {
            Open();

            var buf = new Byte[1024 * 8];

            var count = Client.GetStream().Read(buf, 0, buf.Length);
            return buf.ReadBytes(0, count);
        }

        /// <summary>读取指定长度的数据，一般是一帧</summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">偏移</param>
        /// <param name="count">数量</param>
        /// <returns></returns>
        public override Int32 Receive(Byte[] buffer, Int32 offset = 0, Int32 count = -1)
        {
            Open();

            if (count < 0) count = buffer.Length - offset;

            return Client.GetStream().Read(buffer, offset, count);
        }
        #endregion

        #region 接收
        /// <summary>开始监听</summary>
        public override void ReceiveAsync()
        {
            if (Client == null) Open();

            // 开始新的监听
            var buf = new Byte[1500];
            Client.GetStream().BeginRead(buf, 0, buf.Length, OnReceive, buf);
        }

        void OnReceive(IAsyncResult ar)
        {
            // 接收数据
            var data = (Byte[])ar.AsyncState;
            var count = Client.GetStream().EndRead(ar);

            // 开始新的监听
            var buf = new Byte[1500];
            Client.GetStream().BeginRead(buf, 0, buf.Length, OnReceive, buf);

            OnReceive(data, count);
        }

        /// <summary>处理收到的数据</summary>
        /// <param name="data"></param>
        /// <param name="count"></param>
        protected virtual void OnReceive(Byte[] data, Int32 count)
        {
            // 分析处理
            var e = new ReceivedEventArgs();
            e.Data = data;

            RaiseReceive(this, e);

            // 数据发回去
            if (e.Feedback) Send(e.Data, 0, e.Length);
        }
        #endregion
    }
}