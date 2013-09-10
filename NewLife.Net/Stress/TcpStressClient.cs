using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NewLife;

namespace NewLife.Net.Stress
{
    class TcpStressClient : DisposeBase
    {
        #region 属性
        private TcpStressConfig _Config;
        /// <summary>配置</summary>
        public TcpStressConfig Config { get { return _Config; } set { _Config = value; } }

        private IPEndPoint _EndPoint;
        /// <summary>远程地址</summary>
        public IPEndPoint EndPoint { get { return _EndPoint; } set { _EndPoint = value; } }

        private Byte[] _Buffer;
        /// <summary>数据缓冲区</summary>
        public Byte[] Buffer { get { return _Buffer; } set { _Buffer = value; } }

        Socket socket;
        Timer sendTimer;
        #endregion

        #region 事件
        public event EventHandler Connected;
        public event EventHandler Disconnected;
        public event EventHandler<EventArgs<Int32>> Sent;
        public event EventHandler<EventArgs<Int32>> Received;
        #endregion

        #region 构造
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            Disconnect();
        }
        #endregion

        #region 连接
        public void ConnectAsync()
        {
            var e = new SocketAsyncEventArgs();
            e.Completed += OnConnected;
            e.RemoteEndPoint = EndPoint;

            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //e.UserToken = socket;

            if (socket.ConnectAsync(e)) OnConnected(this, e);
        }

        private void OnConnected(object sender, SocketAsyncEventArgs e)
        {
            e.Completed -= OnConnected;

            if (e.SocketError != SocketError.Success)
            {
                Disconnect();
                return;
            }

            if (Connected != null) Connected(this, e);

            e.Dispose();

            ReceiveAsyc();

            if (Config.WaitForSend > 0)
                sendTimer = new Timer(SendData, null, Config.WaitForSend, Config.SendInterval);
        }
        #endregion

        #region 发送
        Random _rnd = new Random((Int32)DateTime.Now.Ticks);
        private void SendData(Object state)
        {
            try
            {
                lock (this)
                {
                    if (socket.Connected)
                    {
                        var offset = 0;
                        var count = 0;

                        if (!String.IsNullOrEmpty(Config.Data))
                        {
                            offset = _rnd.Next(Config.MinDataLength, _Buffer.Length - 1);
                            count = _rnd.Next(1, _Buffer.Length - offset);
                        }

                        socket.Send(_Buffer, offset, count, SocketFlags.None);

                        if (Sent != null) Sent(this, new EventArgs<Int32>(count));
                    }
                }
            }
            catch
            {
                Disconnect();
            }
        }
        #endregion

        #region 接收
        void ReceiveAsyc()
        {
            var e = new SocketAsyncEventArgs();
            e.Completed += OnReceived;
            //e.UserToken = socket;
            var buf = new Byte[4096];
            e.SetBuffer(buf, 0, buf.Length);
            socket.ReceiveAsync(e);
        }

        void OnReceived(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success || e.BytesTransferred <= 0)
            {
                Disconnect();
                return;
            }

            if (Received != null) Received(this, new EventArgs<int>(e.BytesTransferred));

            socket.ReceiveAsync(e);
        }
        #endregion

        #region 断开
        public void Disconnect()
        {
            lock (this)
            {
                if (sendTimer != null) sendTimer.Dispose();
                try
                {
                    if (socket != null && socket.Connected)
                    {
                        socket.Shutdown(SocketShutdown.Both);
                        socket.Close();
                    }
                }
                catch { }
                OnDisconnected();
            }
        }

        private void OnDisconnected()
        {
            if (Disconnected != null) Disconnected(this, EventArgs.Empty);
        }
        #endregion
    }
}