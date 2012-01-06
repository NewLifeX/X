using System;
using System.Net;
using System.Net.Sockets;
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

        ///// <summary>套接字</summary>
        //Socket ISocketSession.Socket { get { return base.Client; } set { base.Client = value; } }

        private Int32 _ID;
        /// <summary>编号</summary>
        Int32 ISocketSession.ID { get { return _ID; } set { if (_ID > 0)throw new NetException("禁止修改会话编号！"); _ID = value; } }
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
        #endregion

        #region 方法
        /// <summary>开始异步接收，同时处理传入的事件参数，里面可能有接收到的数据</summary>
        /// <param name="e"></param>
        internal void Start(NetEventArgs e)
        {
            if (e.BytesTransferred > 0) ProcessReceive(e);

            ReceiveAsync();
        }

        /// <summary>断开客户端连接。Tcp端口，UdpClient不处理</summary>
        public void Disconnect()
        {
            if (Socket != null && Socket.Connected) Client.Disconnect(ReuseAddress);
        }
        #endregion

        #region 接收
        /// <summary>接收数据。已重载。接收到0字节表示连接断开！</summary>
        /// <param name="e"></param>
        protected override void OnReceive(NetEventArgs e)
        {
            if (e.BytesTransferred > 0 || !DisconnectWhenEmptyData)
                base.OnReceive(e);
            else
            {
                //// 关闭前回收
                //Push(e);
                //Close();

                OnError(e, null);
            }
        }
        #endregion
    }
}