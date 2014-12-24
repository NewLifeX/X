using System;
using System.Net;
using System.Net.Sockets;
using NewLife.Log;

namespace NewLife.Net
{
    /// <summary>UDP传输</summary>
    public class UdpTransport : ITransport, IDisposable
    {
        #region 属性
        private Int32 _Port;
        /// <summary>端口</summary>
        public Int32 Port { get { return _Port; } set { _Port = value; } }

        private IPEndPoint _Remote;
        /// <summary>远程结点地址</summary>
        public IPEndPoint Remote { get { return _Remote; } set { _Remote = value; } }

        private Int32 _Timeout = 3000;
        /// <summary>超时。默认3000ms</summary>
        public Int32 Timeout { get { return _Timeout; } set { _Timeout = value; } }

        private UdpClient _Client;
        /// <summary>客户端</summary>
        public UdpClient Client { get { return _Client; } set { _Client = value; } }

        private Int32 _FrameSize;
        /// <summary>读取的期望帧长度，小于该长度为未满一帧，读取不做返回</summary>
        public Int32 FrameSize { get { return _FrameSize; } set { _FrameSize = value; } }
        #endregion

        #region 构造
        /// <summary>使用监听口初始化</summary>
        /// <param name="listenPort"></param>
        public UdpTransport(Int32 listenPort)
        {
            Port = listenPort;
        }

        /// <summary>初始化</summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        public UdpTransport(String host, Int32 port)
        {
            Remote = new IPEndPoint(NetUri.ParseAddress(host), port);
        }

        /// <summary>析构</summary>
        ~UdpTransport() { Dispose(false); }

        /// <summary>销毁</summary>
        public void Dispose() { Dispose(true); }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(Boolean disposing)
        {
            if (disposing) GC.SuppressFinalize(this);

            Close();
        }
        #endregion

        #region 方法
        /// <summary>打开</summary>
        public void Open()
        {
            if (Client == null || !Client.Client.IsBound)
            {
                if (Remote != null)
                    Client = new UdpClient(Remote);
                else
                    Client = new UdpClient(Port);
                if (Timeout > 0) Client.Client.ReceiveTimeout = Timeout;
            }
        }

        /// <summary>关闭</summary>
        public void Close()
        {
            if (Client != null) Client.Close();
        }

        /// <summary>写入数据</summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">偏移</param>
        /// <param name="count">数量</param>
        public void Send(Byte[] buffer, Int32 offset = 0, Int32 count = -1)
        {
            Open();

            WriteLog("Write:{0}", BitConverter.ToString(buffer));

            if (count < 0) count = buffer.Length - offset;

            var sp = Client;
            lock (sp)
            {
                if (Client.Client.Connected)
                {
                    if (offset == 0)
                        sp.Send(buffer, count);
                    else
                        sp.Send(buffer.ReadBytes(offset, count), count);
                }
                else
                {
                    if (offset == 0)
                        sp.Send(buffer, count, Remote);
                    else
                        sp.Send(buffer.ReadBytes(offset, count), count, Remote);
                }
            }
        }

        /// <summary>读取指定长度的数据，一般是一帧</summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">偏移</param>
        /// <param name="count">数量</param>
        /// <returns></returns>
        public Int32 Receive(Byte[] buffer, Int32 offset = 0, Int32 count = -1)
        {
            Open();

            if (count < 0) count = buffer.Length - offset;

            var size = 0;
            var sp = Client;
            lock (sp)
            {
                try
                {
                    var remoteEP = new IPEndPoint(IPAddress.Any, 0);
                    var data = sp.Receive(ref remoteEP);
                    Remote = remoteEP;
                    if (data != null && data.Length > 0)
                    {
                        size = data.Length;
                        // 计算还有多少可用空间
                        if (size > count) size = count;
                        buffer.Write(offset, data, 0, size);
                    }
                }
                catch { }
            }

            WriteLog("Read:{0}", BitConverter.ToString(buffer, offset, size));

            return size;
        }
        #endregion

        #region 异步接收
        /// <summary>开始监听</summary>
        public void ReceiveAsync()
        {
            if (Client == null)
            {
                Client = new UdpClient(Port);
                if (Timeout > 0) Client.Client.ReceiveTimeout = Timeout;
            }

            // 开始新的监听
            Client.BeginReceive(OnReceive, Client);
        }

        void OnReceive(IAsyncResult ar)
        {
            // 接收数据
            var server = ar.AsyncState as UdpClient;
            var ep = new IPEndPoint(IPAddress.Any, 0);
            var data = server.EndReceive(ar, ref ep);
            Remote = ep;

            // 开始新的监听
            server.BeginReceive(OnReceive, server);

            // 分析处理
            if (Received != null)
            {
                var e = new UdpReceivedEventArgs { Data = data };
                e.Remote = ep;
                e.UserState = ep;
                Received(this, e);

                // 数据发回去
                if (e.Feedback) server.Send(data, data.Length, ep);
            }
        }

        /// <summary>数据到达事件，事件里调用<see cref="Receive"/>读取数据</summary>
        public event EventHandler<ReceivedEventArgs> Received;
        #endregion

        #region 日志
        /// <summary>输出日志</summary>
        /// <param name="formart"></param>
        /// <param name="args"></param>
        public static void WriteLog(String formart, params Object[] args)
        {
            XTrace.WriteLine(formart, args);
        }
        #endregion
    }
}