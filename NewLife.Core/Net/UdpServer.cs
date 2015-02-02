using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
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

        private Int32 _MaxNotActive = 30;
        /// <summary>最大不活动时间。
        /// 对于每一个会话连接，如果超过该时间仍然没有收到任何数据，则断开会话连接。
        /// 单位秒，默认30秒。时间不是太准确，建议15秒的倍数。为0表示不检查。</summary>
        public Int32 MaxNotActive { get { return _MaxNotActive; } set { _MaxNotActive = value; } }
        #endregion

        #region 构造
        /// <summary>实例化增强UDP</summary>
        public UdpServer()
        {
            Local = new NetUri(ProtocolType.Udp, IPAddress.Any, 0);
            Remote.ProtocolType = ProtocolType.Udp;
            _Sessions = new SessionCollection(this);
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

                // 如果使用了新会话事件，也需要开启异步接收
                if (!UseReceiveAsync && NewSession != null) UseReceiveAsync = true;

                WriteLog("{0}.Open {1}", Name, this);
            }

            return true;
        }

        /// <summary>关闭</summary>
        protected override Boolean OnClose()
        {
            WriteLog("{0}.Close {1}", Name, this);

            if (Client != null)
            {
                var udp = Client;
                Client = null;
                try
                {
                    if (_Async != null && _Async.AsyncWaitHandle != null) _Async.AsyncWaitHandle.Close();

                    CloseAllSession();

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
            if (Disposed) throw new ObjectDisposedException(this.GetType().Name);

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
            if (Disposed) throw new ObjectDisposedException(this.GetType().Name);

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
            if (Disposed) throw new ObjectDisposedException(this.GetType().Name);

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
            if (Disposed) throw new ObjectDisposedException(this.GetType().Name);

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

            Remote.EndPoint = ep;

            // 在用户线程池里面去处理数据
            ThreadPoolX.QueueUserWorkItem(() =>
            {
                // 日志输出放在这里，既保证和处理函数一个线程，又不会被重载覆盖
                //Log.Debug("{0}.OnReceive {1}<={2} [{3}]", Name, this, ep, data.Length);

                OnReceive(data, ep);
            }, ex => OnError("OnReceive", ex));

            // 开始新的监听
            ReceiveAsync();
        }

        /// <summary>处理收到的数据</summary>
        /// <param name="data"></param>
        /// <param name="remote"></param>
        internal protected virtual void OnReceive(Byte[] data, IPEndPoint remote)
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

        #region 会话
        /// <summary>新会话时触发</summary>
        public event EventHandler<SessionEventArgs> NewSession;

        private SessionCollection _Sessions;
        /// <summary>会话集合。用地址端口作为标识，业务应用自己维持地址端口与业务主键的对应关系。</summary>
        public IDictionary<String, ISocketSession> Sessions { get { return _Sessions; } }

        Int32 g_ID = 1;
        /// <summary>创建会话</summary>
        /// <param name="remoteEP"></param>
        /// <returns></returns>
        public virtual ISocketSession CreateSession(IPEndPoint remoteEP)
        {
            if (Disposed) throw new ObjectDisposedException(this.GetType().Name);

            if (!Active)
            {
                // 根据目标地址适配本地IPv4/IPv6
                Local.Address = Local.Address.GetRightAny(remoteEP.AddressFamily);

                if (!Open()) return null;
            }

            // 需要查找已有会话，已有会话不存在时才创建新会话
            var session = _Sessions.Get(remoteEP + "");
            if (session == null)
            {
                var us = new UdpSession(this, remoteEP);
                us.ID = g_ID++;
                session = us;
                //Interlocked.Increment(ref _Sessions);
                //session.OnDisposed += (s, e) => Interlocked.Decrement(ref _Sessions);
                _Sessions.Add(session);

                WriteLog("{0}[{1}].NewSession {2}", Name, us.ID, remoteEP);

                // 触发新会话事件
                if (NewSession != null) NewSession(this, new SessionEventArgs { Session = session });
            }

            return session;
        }

        private void CloseAllSession()
        {
            var sessions = _Sessions;
            if (sessions != null)
            {
                _Sessions = null;

                if (sessions.Count > 0)
                {
                    WriteLog("准备释放会话{0}个！", sessions.Count);
                    sessions.TryDispose();
                    sessions.Clear();
                }
            }
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
            if (Sessions.Count > 0)
                return String.Format("{0} [{1}]", Local, Sessions.Count);
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
        /// <param name="encoding">文本编码，默认null表示UTF-8编码</param>
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
        /// <param name="encoding">文本编码，默认null表示UTF-8编码</param>
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