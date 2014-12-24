using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using NewLife.Model;
using NewLife.Threading;

namespace NewLife.Net
{
    /// <summary>增强的UDP</summary>
    public class UdpServer : SessionBase, ISocketServer, ITransport
    {
        #region 属性
        private UdpClient _Client;
        /// <summary>客户端</summary>
        public UdpClient Client { get { return _Client; } set { _Client = value; } }

        /// <summary>获取Socket</summary>
        /// <returns></returns>
        internal override Socket GetSocket() { return Client == null ? null : Client.Client; }

        private Int32 _Sessions;
        /// <summary>会话数</summary>
        public Int32 Sessions { get { return _Sessions; } private set { _Sessions = value; } }

        ///// <summary>读取的期望帧长度。该参数对UDP无效</summary>
        //Int32 ITransport.FrameSize { get { return 0; } set { } }
        #endregion

        #region 构造
        /// <summary>实例化增强UDP</summary>
        public UdpServer()
        {
            Local = new NetUri(ProtocolType.Udp, IPAddress.Any, 0);
        }

        /// <summary>使用监听口初始化</summary>
        /// <param name="listenPort"></param>
        public UdpServer(Int32 listenPort)
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
                // 根据目标地址适配本地IPv4/IPv6
                if (Remote != null && !Remote.Address.IsAny())
                {
                    Local.Address = Local.Address.GetRightAny(Remote.Address.AddressFamily);
                }

                Client = new UdpClient(Local.EndPoint);
                if (Port == 0) Port = (Socket.LocalEndPoint as IPEndPoint).Port;

                WriteLog("{0}.Open {1}", this.GetType().Name, this);
            }

            return true;
        }

        /// <summary>关闭</summary>
        protected override Boolean OnClose()
        {
            WriteLog("{0}.Close {1}", this.GetType().Name, this);

            if (Client != null)
            {
                var udp = Client;
                Client = null;
                try
                {
                    if (_Async != null && _Async.AsyncWaitHandle != null) _Async.AsyncWaitHandle.Close();

                    udp.Close();
                    //NetHelper.Close(Client.Client);
                }
                catch (Exception ex)
                {
                    if (!ex.IsDisposed()) OnError("Close", ex);
                    if (ThrowException) throw;

                    return false;
                }
            }

            return true;
        }

        ///// <summary>连接</summary>
        ///// <param name="remoteEP"></param>
        ///// <returns></returns>
        //protected override Boolean OnConnect(IPEndPoint remoteEP)
        //{
        //    Client.Connect(remoteEP);

        //    return true;
        //}

        /// <summary>发送数据</summary>
        /// <remarks>
        /// 目标地址由<seealso cref="SessionBase.Remote"/>决定，如需精细控制，可直接操作<seealso cref="Client"/>
        /// </remarks>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">偏移</param>
        /// <param name="count">数量</param>
        /// <returns>是否成功</returns>
        public override Boolean Send(Byte[] buffer, Int32 offset = 0, Int32 count = -1)
        {
            if (!Open()) return false;

            if (count < 0) count = buffer.Length - offset;

            try
            {
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
                            sp.Send(buffer, count, Remote.EndPoint);
                        else
                            sp.Send(buffer.ReadBytes(offset, count), count, Remote.EndPoint);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                if (!ex.IsDisposed())
                {
                    OnError("Send", ex);

                    // 发送异常可能是连接出了问题，UDP不需要关闭
                    //Close();

                    if (ThrowException) throw;
                }
                return false;
            }
        }

        /// <summary>接收数据</summary>
        /// <returns></returns>
        public override Byte[] Receive()
        {
            if (!Open()) return null;

            var buf = new Byte[1024 * 2];
            var count = Receive(buf, 0, buf.Length);
            if (count < 0) return null;
            if (count == 0) return new Byte[0];

            return buf.ReadBytes(0, count);
        }

        /// <summary>读取指定长度的数据，一般是一帧</summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">偏移</param>
        /// <param name="count">数量</param>
        /// <returns></returns>
        public override Int32 Receive(Byte[] buffer, Int32 offset = 0, Int32 count = -1)
        {
            if (!Open()) return -1;

            if (count < 0) count = buffer.Length - offset;

            var size = 0;
            var sp = Client;

            try
            {
                IPEndPoint remoteEP = null;
                var data = Client.Receive(ref remoteEP);
                Remote.EndPoint = remoteEP;
                if (data != null && data.Length > 0)
                {
                    size = data.Length;
                    // 计算还有多少可用空间
                    if (size > count) size = count;
                    buffer.Write(offset, data, 0, size);
                }
            }
            catch (Exception ex)
            {
                if (!ex.IsDisposed())
                {
                    OnError("Receive", ex);

                    // 异常可能是连接出了问题，UDP不需要关闭
                    //Close();

                    if (ThrowException) throw;
                }

                return -1;
            }

            return size;
        }
        #endregion

        #region 异步接收
        private IAsyncResult _Async;

        /// <summary>开始监听</summary>
        /// <returns>是否成功</returns>
        public override Boolean ReceiveAsync()
        {
            if (!Open()) return false;

            if (_Async != null) return true;
            try
            {
                // 开始新的监听
                _Async = Client.BeginReceive(OnReceive, Client);
            }
            catch (Exception ex)
            {
                if (!ex.IsDisposed())
                {
                    OnError("ReceiveAsync", ex);

                    // 异常一般是网络错误，UDP不需要关闭
                    //Close();

                    if (ThrowException) throw;
                }
                return false;
            }

            return true;
        }

        void OnReceive(IAsyncResult ar)
        {
            _Async = null;

            if (!Active) return;
            // 接收数据
            var client = ar.AsyncState as UdpClient;
            if (client == null) return;

            IPEndPoint ep = null;
            Byte[] data = null;

            try
            {
                data = client.EndReceive(ar, ref ep);
            }
            catch (Exception ex)
            {
                if (!ex.IsDisposed())
                {
                    OnError("EndReceive", ex);

                    // 异常一般是网络错误，UDP不需要关闭
                    //Close();
                    //if (ex.SocketErrorCode != SocketError.ConnectionReset) Close();

                    // 开始新的监听，避免因为异常就失去网络服务
                    ReceiveAsync();
                }
                return;
            }

            //WriteLog("{0}.OnReceive {1}<={2} [{3}]", this.GetType().Name, this, ep, data.Length);

            Remote.EndPoint = ep;

            // 在用户线程池里面去处理数据
            ThreadPoolX.QueueUserWorkItem(() => OnReceive(data, ep));

            // 开始新的监听
            ReceiveAsync();
        }

        /// <summary>处理收到的数据</summary>
        /// <param name="data"></param>
        /// <param name="remote"></param>
        protected virtual void OnReceive(Byte[] data, IPEndPoint remote)
        {
            // 分析处理
            var e = new UdpReceivedEventArgs();
            e.Data = data;
            e.Remote = remote;

            // 为该连接单独创建一个会话，方便直接通信
            var session = CreateSession(remote);

            RaiseReceive(session, e);

            // 数据发回去
            if (e.Feedback) Client.Send(e.Data, e.Length, e.Remote);
        }
        #endregion

        #region 会话接口
        /// <summary>创建会话</summary>
        /// <param name="remoteEP"></param>
        /// <returns></returns>
        public virtual ISocketSession CreateSession(IPEndPoint remoteEP)
        {
            if (!Active)
            {
                // 根据目标地址适配本地IPv4/IPv6
                Local.Address = Local.Address.GetRightAny(remoteEP.AddressFamily);

                if (!Open()) return null;
            }

            var session = new UdpSession(this, remoteEP);
            Interlocked.Increment(ref _Sessions);
            session.OnDisposed += (s, e) => Interlocked.Decrement(ref _Sessions);
            return session;
        }
        #endregion

        #region IServer接口
        void IServer.Start()
        {
            Open();
        }

        void IServer.Stop()
        {
            Close();
        }
        #endregion

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (Sessions > 0)
                return String.Format("{0} [{1}]", Local, Sessions);
            else
                return Local.ToString();
        }
        #endregion
    }

    /// <summary>收到Udp数据包的事件参数</summary>
    public class UdpReceivedEventArgs : ReceivedEventArgs
    {
        private IPEndPoint _Remote;
        /// <summary>远程地址</summary>
        public IPEndPoint Remote { get { return _Remote; } set { _Remote = value; } }
    }

    /// <summary>Udp扩展</summary>
    public static class UdpHelper
    {
        /// <summary>发送数据流</summary>
        /// <param name="udp"></param>
        /// <param name="stream"></param>
        /// <param name="remoteEP"></param>
        /// <returns>返回自身，用于链式写法</returns>
        public static UdpClient Send(this UdpClient udp, Stream stream, IPEndPoint remoteEP = null)
        {
            Int64 total = 0;

            var size = 1472;
            Byte[] buffer = new Byte[size];
            while (true)
            {
                Int32 n = stream.Read(buffer, 0, buffer.Length);
                if (n <= 0) break;

                udp.Send(buffer, n, remoteEP);
                total += n;

                if (n < buffer.Length) break;
            }
            return udp;
        }

        /// <summary>向指定目的地发送信息</summary>
        /// <param name="udp"></param>
        /// <param name="buffer">缓冲区</param>
        /// <param name="remoteEP"></param>
        /// <returns>返回自身，用于链式写法</returns>
        public static UdpClient Send(this UdpClient udp, Byte[] buffer, IPEndPoint remoteEP = null)
        {
            udp.Send(buffer, buffer.Length, remoteEP);
            return udp;
        }

        /// <summary>向指定目的地发送信息</summary>
        /// <param name="udp"></param>
        /// <param name="message"></param>
        /// <param name="encoding"></param>
        /// <param name="remoteEP"></param>
        /// <returns>返回自身，用于链式写法</returns>
        public static UdpClient Send(this UdpClient udp, String message, Encoding encoding = null, IPEndPoint remoteEP = null)
        {
            if (encoding == null)
                Send(udp, Encoding.UTF8.GetBytes(message), remoteEP);
            else
                Send(udp, encoding.GetBytes(message), remoteEP);
            return udp;
        }

        /// <summary>广播数据包</summary>
        /// <param name="udp"></param>
        /// <param name="buffer">缓冲区</param>
        /// <param name="port"></param>
        public static UdpClient Broadcast(this UdpClient udp, Byte[] buffer, Int32 port)
        {
            if (!udp.EnableBroadcast) udp.EnableBroadcast = true;

            udp.Send(buffer, buffer.Length, new IPEndPoint(IPAddress.Broadcast, port));

            return udp;
        }

        /// <summary>广播字符串</summary>
        /// <param name="udp"></param>
        /// <param name="message"></param>
        /// <param name="port"></param>
        public static UdpClient Broadcast(this UdpClient udp, String message, Int32 port)
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            return Broadcast(udp, buffer, port);
        }

        /// <summary>接收字符串</summary>
        /// <param name="udp"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static String ReceiveString(this UdpClient udp, Encoding encoding = null)
        {
            IPEndPoint ep = null;
            var buffer = udp.Receive(ref ep);
            if (buffer == null || buffer.Length < 1) return null;

            if (encoding == null) encoding = Encoding.UTF8;
            return encoding.GetString(buffer);
        }
    }
}