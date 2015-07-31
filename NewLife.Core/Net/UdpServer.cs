using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using NewLife.Model;
using NewLife.Threading;
#if !Android
using NewLife.Web;
#endif

namespace NewLife.Net
{
    /// <summary>增强的UDP</summary>
    /// <remarks>
    /// 如果已经打开异步接收，还要使用同步接收，则同步Receive内部不再调用底层Socket，而是等待截走异步数据。
    /// </remarks>
    public class UdpServer : SessionBase, ISocketServer
    {
        #region 属性
        private UdpClient _Client;
        /// <summary>客户端</summary>
        public UdpClient Client { get { return _Client; } set { _Client = value; } }

        /// <summary>获取Socket</summary>
        /// <returns></returns>
        internal override Socket GetSocket() { return Client == null ? null : Client.Client; }

        private Int32 _MaxNotActive = 30;
        /// <summary>最大不活动时间。默认30秒。</summary>
        /// <remarks>
        /// 对于每一个会话连接，如果超过该时间仍然没有收到任何数据，则断开会话连接。
        /// 时间不是太准确，建议15秒的倍数。为0表示不检查。
        /// </remarks>
        public Int32 MaxNotActive { get { return _MaxNotActive; } set { _MaxNotActive = value; } }

        private IPEndPoint _LastRemote;
        /// <summary>最后一次同步接收数据得到的远程地址</summary>
        public IPEndPoint LastRemote { get { return _LastRemote; } set { _LastRemote = value; } }

        private Boolean _AllowAsyncOnSync = true;
        /// <summary>在异步模式下，使用同步收到数据后，是否允许异步事件继续使用，默认true</summary>
        public Boolean AllowAsyncOnSync { get { return _AllowAsyncOnSync; } set { _AllowAsyncOnSync = value; } }

        private Boolean _Loopback;
        /// <summary>是否接收来自自己广播的环回数据。默认false</summary>
        public Boolean Loopback { get { return _Loopback; } set { _Loopback = value; } }
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
                CheckDynamic();

                // 如果使用了新会话事件，也需要开启异步接收
                if (!UseReceiveAsync && NewSession != null) UseReceiveAsync = true;

                WriteLog("Open {0}", this);
            }

            return true;
        }

        /// <summary>关闭</summary>
        protected override Boolean OnClose(String reason)
        {
            if (Client != null)
            {
                WriteLog("Close {0} {1}", reason, this);

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
                        if (Log.Enable && LogSend) WriteLog("Send [{0}]: {1}", count, buffer.ToHex(0, Math.Min(count, 32)));

                        if (offset == 0)
                            sp.Send(buffer, count);
                        else
                            sp.Send(buffer.ReadBytes(offset, count), count);
                    }
                    else
                    {
                        if (Log.Enable && LogSend) WriteLog("Send {2} [{0}]: {1}", count, buffer.ToHex(0, Math.Min(count, 32)), Remote.EndPoint);

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

        /// <summary>同步字典，等待</summary>
        private List<SyncItem> _sync = new List<SyncItem>();
        /// <summary>同步对象</summary>
        class SyncItem
        {
            public Int32 ThreadID;
            public AutoResetEvent Event;
            public IPEndPoint EndPoint;
            public Byte[] Data;
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
                // 如果已经打开异步，这里可能永远无法同步收到数据
                if (!UseReceiveAsync)
                {
                    IPEndPoint remoteEP = null;
                    var data = Client.Receive(ref remoteEP);
                    LastRemote = remoteEP;
                    if (data != null && data.Length > 0)
                    {
                        size = data.Length;
                        // 计算还有多少可用空间
                        if (size > count) size = count;
                        buffer.Write(offset, data, 0, size);
                    }
                }
                else
                {
                    return ReceiveWait(buffer, offset, count);
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

        Int32 ReceiveWait(Byte[] buffer, Int32 offset = 0, Int32 count = -1)
        {
            //WriteLog("已使用异步接收，等待异步数据 {0}", Remote.EndPoint);

            var si = new SyncItem();
            // 当前线程
            si.ThreadID = Thread.CurrentThread.ManagedThreadId;
            // 要等待的地址
            if (!Remote.EndPoint.IsAny()) si.EndPoint = Remote.EndPoint;
            // 等待事件
            var e = new AutoResetEvent(false);
            si.Event = e;

            // 加入同步字典，异步接收事件里面会查找
            lock (_sync)
            {
                _sync.Add(si);
            }

            // 等待异步收到数据交给我
            var time = Client.Client.ReceiveTimeout;
            if (time <= 0) time = 1000;

            try
            {
                // 如果超时了还没有收到数据，则返回失败
                if (!e.WaitOne(time))
                {
                    //WriteLog("等待异步数据包超时 {0}毫秒", time);
                    return -1;
                }

                //WriteLog("拿到异步数据包 [{0}]", si.Data.Length);

                // 数据在Data里面
                var data = si.Data;
                LastRemote = si.EndPoint;
                if (data != null && data.Length > 0)
                {
                    var size = data.Length;
                    // 计算还有多少可用空间
                    if (size > count) size = count;
                    buffer.Write(offset, data, 0, size);

                    return size;
                }
            }
            finally
            {
                lock (_sync)
                {
                    _sync.Remove(si);
                }
                si.Event.Close();
            }

            return -1;
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

            //if (_Async != null) return true;
            if (!UseReceiveAsync) UseReceiveAsync = true;
            try
            {
#if DEBUG
                if (_checker == null) _checker = new TimerX(AsyncChecker, null, 1000, 1000);
#endif
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

#if DEBUG
        TimerX _checker;
        void AsyncChecker(Object state)
        {
            var ac = _Async;
            if (ac == null) return;

            if (ac.IsCompleted) WriteLog("已完成");
        }
#endif

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
                    // 屏蔽连接重置的异常
                    var sex = ex as SocketException;
                    if (sex == null || sex.SocketErrorCode != SocketError.ConnectionReset) OnError("EndReceive", ex);

                    // 异常一般是网络错误，UDP不需要关闭
                    //Close();
                    //if (ex.SocketErrorCode != SocketError.ConnectionReset) Close();

                    // 开始新的监听，避免因为异常就失去网络服务
                    ReceiveAsync();
                }
                return;
            }

            // 在用户线程池里面去处理数据
            ThreadPoolX.QueueUserWorkItem(() => OnReceive(data, ep), ex => OnError("OnReceive", ex));

            // 开始新的监听
            ReceiveAsync();
        }

        /// <summary>处理收到的数据</summary>
        /// <param name="data"></param>
        /// <param name="remote"></param>
        internal protected virtual void OnReceive(Byte[] data, IPEndPoint remote)
        {
            // 过滤自己广播的环回数据。放在这里，兼容UdpSession
            if (!Loopback && remote.Port == Port)
            {
                if (!Local.Address.IsAny())
                {
                    if (remote.Address.Equals(Local.Address)) return;
                }
                else
                {
                    foreach (var item in NetHelper.GetIPsWithCache())
                    {
                        if (remote.Address.Equals(item)) return;
                    }
                }
            }

            // 可能有同步等待
            if (_sync.Count > 0)
            {
                //WriteLog("收到异步数据包{0}，有人在等待", remote);
                lock (_sync)
                {
                    if (_sync.Count > 0)
                    {
                        foreach (var item in _sync)
                        {
                            // 如果设定了只需要该地址的数据，则处理
                            if (item.EndPoint.IsAny() || item.EndPoint.Equals(remote))
                            {
                                // 放好数据，告诉它，数据来了
                                item.Data = data;
                                item.Event.Set();

                                // 如果不允许异步继续使用，跳出
                                if (!AllowAsyncOnSync) return;
                            }
                        }
                    }
                }
            }
#if !Android
            // 更新全局远程IP地址
            WebHelper.UserHost = remote.ToString();
#endif
            // 分析处理
            var e = new ReceivedEventArgs();
            e.Data = data;
            //e.Remote = remote;
            e.UserState = remote;

            // 为该连接单独创建一个会话，方便直接通信
            var session = CreateSession(remote);
            // 数据直接转交给会话，不再经过事件，那样在会话较多时极为浪费资源
            var us = session as UdpSession;
            if (us != null)
                us.OnReceive(e);
            else
            {
                // 没有匹配到任何会话时，才在这里显示日志。理论上不存在这个可能性
                if (Log.Enable && LogReceive) WriteLog("Recv [{0}]: {1}", e.Length, e.Data.ToHex(0, Math.Min(e.Length, 32)));
            }

            if (session != null) RaiseReceive(session, e);

            // 数据发回去
            if (e.Feedback)
            {
                // 有没有可能事件处理者修改了这个用户对象？要求转发给别人？
                remote = e.UserState as IPEndPoint;
                Client.Send(e.Data, e.Length, remote);
            }
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
            var sessions = _Sessions;
            if (sessions == null) return null;

            if (!Active)
            {
                // 根据目标地址适配本地IPv4/IPv6
                Local.Address = Local.Address.GetRightAny(remoteEP.AddressFamily);

                if (!Open()) return null;
            }

            // 需要查找已有会话，已有会话不存在时才创建新会话
            var session = sessions.Get(remoteEP + "");
            if (session == null)
            {
                var us = new UdpSession(this, remoteEP);
                us.Log = Log;
                session = us;
                if (sessions.Add(session))
                {
                    us.ID = g_ID++;
                    us.Start();

                    // 触发新会话事件
                    if (NewSession != null) NewSession(this, new SessionEventArgs { Session = session });
                }
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
            Close("服务停止");
        }
        #endregion

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            var ss = Sessions;
            if (ss != null && ss.Count > 0)
                return String.Format("{0} [{1}]", Local, ss.Count);
            else
                return Local.ToString();
        }
        #endregion
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
            if (udp.Client != null && udp.Client.LocalEndPoint != null)
            {
                //var ip = udp.Client.LocalEndPoint as IPEndPoint;
                if (udp.Client.LocalEndPoint.AddressFamily == AddressFamily.InterNetworkV6) throw new NotSupportedException("IPv6不支持广播！");
            }

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