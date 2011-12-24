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

        private IPEndPoint _RemoteEndPoint;
        /// <summary>远程终结点</summary>
        public IPEndPoint RemoteEndPoint
        {
            get
            {
                if (_RemoteEndPoint == null)
                {
                    try
                    {
                        _RemoteEndPoint = Socket.RemoteEndPoint as IPEndPoint;
                    }
                    catch { }
                }
                return _RemoteEndPoint;
            }
            set { _RemoteEndPoint = value; }
        }

        private Boolean _DisconnectWhenEmptyData;
        /// <summary>收到空数据时抛出异常并断开连接。</summary>
        public Boolean DisconnectWhenEmptyData { get { return _DisconnectWhenEmptyData; } set { _DisconnectWhenEmptyData = value; } }

        /// <summary>套接字</summary>
        Socket ISocketSession.Socket { get { return base.Client; } set { base.Client = value; } }
        #endregion

        #region 重载
        /// <summary>已重载。设置RemoteEndPoint</summary>
        /// <param name="e"></param>
        protected override void OnComplete(NetEventArgs e)
        {
            IPEndPoint ep = e.RemoteEndPoint as IPEndPoint;
            if (ep == null || (ep.Address == IPAddress.Loopback && ep.Port == 0)) e.RemoteEndPoint = RemoteEndPoint;

            base.OnComplete(e);
        }
        #endregion

        #region 方法
        /// <summary>开始异步接收，同时处理传入的事件参数，里面可能有接收到的数据</summary>
        /// <param name="e"></param>
        void ISocketSession.Start(NetEventArgs e)
        {
            if (e.BytesTransferred > 0)
                base.OnReceive(e);
            else
                base.ReceiveAsync(e);
        }

        /// <summary>断开客户端连接。Tcp端口，UdpClient不处理</summary>
        public void Disconnect() { Client.Disconnect(ReuseAddress); }
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