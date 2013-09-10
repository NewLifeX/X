using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace NewLife.Net.Stress
{
    class TcpStressClient : DisposeBase
    {
        #region 属性
        private IPEndPoint _EndPoint;
        /// <summary>远程地址</summary>
        public IPEndPoint EndPoint { get { return _EndPoint; } set { _EndPoint = value; } }

        private Byte[] _Buffer;
        /// <summary>数据缓冲区</summary>
        public Byte[] Buffer { get { return _Buffer; } set { _Buffer = value; } }

        private Int32 _Interval;
        /// <summary>发送间隔</summary>
        public Int32 Interval { get { return _Interval; } set { _Interval = value; } }

        private Int32 _Times = 100;
        /// <summary>发送次数</summary>
        public Int32 Times { get { return _Times; } set { _Times = value; } }

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

            if (!socket.ConnectAsync(e)) OnConnected(this, e);
        }

        private void OnConnected(object sender, SocketAsyncEventArgs e)
        {
            e.Completed -= OnConnected;

            if (e.SocketError != SocketError.Success)
            {
                Close();
                return;
            }

            if (Connected != null)
            {
                Connected(this, e);
                Connected = null;
            }

            e.Dispose();

            if (Received != null) ReceiveAsyc();
        }
        #endregion

        #region 发送
        /// <summary>开始发送</summary>
        public void StartSend()
        {
            sendTimer = new Timer(SendData, null, Interval, Interval);
        }

        private void SendData(Object state)
        {
            if (_Times-- <= 0)
            {
                if (sendTimer != null) sendTimer.Dispose();
                return;
            }

            try
            {
                lock (this)
                {
                    if (socket.Connected)
                    {
                        socket.Send(_Buffer, 0, _Buffer.Length, SocketFlags.None);

                        if (Sent != null) Sent(this, new EventArgs<Int32>(_Buffer.Length));
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

            if (!socket.ReceiveAsync(e)) OnReceived(this, e);
        }

        void OnReceived(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success || e.BytesTransferred <= 0)
            {
                Disconnect();
                return;
            }

            if (Received != null) Received(this, new EventArgs<int>(e.BytesTransferred));

            if (!socket.ReceiveAsync(e)) OnReceived(this, e);
        }
        #endregion

        #region 断开
        public void Disconnect()
        {
            lock (this)
            {
                if (sendTimer != null) sendTimer.Dispose();
                Close();
                OnDisconnected();
            }
        }

        void Close()
        {
            try
            {
                if (socket != null && socket.Connected)
                {
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                }
            }
            catch { }
        }

        private void OnDisconnected()
        {
            if (Disconnected != null)
            {
                Disconnected(this, EventArgs.Empty);
                Disconnected = null;
            }
        }
        #endregion
    }
}