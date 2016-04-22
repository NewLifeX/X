using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Model;

namespace NewLife.Net
{
    /// <summary>增强的UDP</summary>
    /// <remarks>
    /// 如果已经打开异步接收，还要使用同步接收，则同步Receive内部不再调用底层Socket，而是等待截走异步数据。
    /// </remarks>
    public class UdpServer : SessionBase, ISocketServer
    {
        #region 属性
        ///// <summary>客户端</summary>
        //public Socket Client { get; private set; }

        ///// <summary>获取Socket</summary>
        ///// <returns></returns>
        //internal override Socket GetSocket() { return Client; }

        /// <summary>会话超时时间。默认30秒</summary>
        /// <remarks>
        /// 对于每一个会话连接，如果超过该时间仍然没有收到任何数据，则断开会话连接。
        /// </remarks>
        public Int32 SessionTimeout { get; set; }

        /// <summary>最后一次同步接收数据得到的远程地址</summary>
        public IPEndPoint LastRemote { get; set; }

        /// <summary>是否接收来自自己广播的环回数据。默认false</summary>
        public Boolean Loopback { get; set; }

        /// <summary>会话统计</summary>
        public IStatistics StatSession { get; set; }
        #endregion

        #region 构造
        /// <summary>实例化增强UDP</summary>
        public UdpServer()
        {
            SessionTimeout = 30;

            Local = new NetUri(ProtocolType.Udp, IPAddress.Any, 0);
            Remote.ProtocolType = ProtocolType.Udp;
            _Sessions = new SessionCollection(this);

            StatSession = new Statistics();

            //MaxAsync = 1;
            //MaxReceive = Environment.ProcessorCount * 2 + 2;
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
            if (Client == null || !Client.IsBound)
            {
                // 根据目标地址适配本地IPv4/IPv6
                if (Remote != null && !Remote.Address.IsAny())
                {
                    Local.Address = Local.Address.GetRightAny(Remote.Address.AddressFamily);
                }

                //Client = new UdpClient(Local.EndPoint);
                Client = new Socket(Local.Address.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                Client.Bind(Local.EndPoint);
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
                    CloseAllSession();

                    udp.Shutdown();
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
        #endregion

        #region 发送
        /// <summary>发送数据</summary>
        /// <remarks>
        /// 目标地址由<seealso cref="SessionBase.Remote"/>决定
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

            if (StatSend != null) StatSend.Increment(count);

            try
            {
                var sp = Client;
                lock (sp)
                {
                    if (Client.Connected)
                    {
                        if (Log.Enable && LogSend) WriteLog("Send [{0}]: {1}", count, buffer.ToHex(0, Math.Min(count, 32)));

                        sp.Send(buffer, offset, count, SocketFlags.None);
                    }
                    else
                    {
                        if (Log.Enable && LogSend) WriteLog("Send {2} [{0}]: {1}", count, buffer.ToHex(0, Math.Min(count, 32)), Remote.EndPoint);

                        sp.SendTo(buffer, offset, count, SocketFlags.None, Remote.EndPoint);
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

        ///// <summary>异步发送数据</summary>
        ///// <param name="buffer"></param>
        ///// <returns></returns>
        //public override Boolean SendAsync(Byte[] buffer)
        //{
        //    return SendAsync(buffer, Remote.EndPoint);
        //}

        internal override bool OnSendAsync(SocketAsyncEventArgs se)
        {
            return Client.SendToAsync(se);
        }
        #endregion

        #region 接收
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

            //if (StatReceive != null) StatReceive.Increment(count);

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
                EndPoint remoteEP = null;
                size = Client.ReceiveFrom(buffer, offset, count, SocketFlags.None, ref remoteEP);
                LastRemote = remoteEP as IPEndPoint;
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

            if (StatReceive != null) StatReceive.Increment(size);

            return size;
        }

        /// <summary>处理收到的数据</summary>
        /// <param name="data"></param>
        /// <param name="remote"></param>
        internal override void OnReceive(Byte[] data, IPEndPoint remote)
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

#if !Android
            // 更新全局远程IP地址
            NewLife.Web.WebHelper.UserHost = remote.ToString();
#endif
            // 分析处理
            var e = new ReceivedEventArgs();
            e.Data = data;
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
        }

        internal override bool OnReceiveAsync(SocketAsyncEventArgs se)
        {
            // 每次接收以后，这个会被设置为远程地址，这里重置一下，以防万一
            se.RemoteEndPoint = new IPEndPoint(IPAddress.Any.GetRightAny(Local.EndPoint.AddressFamily), 0);

            return Client.ReceiveFromAsync(se);
        }

        ///// <summary>开始监听</summary>
        ///// <returns>是否成功</returns>
        //public override Boolean ReceiveAsync()
        //{
        //    if (_RecvCount >= MaxAsync) return false;

        //}
        #endregion

        #region 会话
        /// <summary>新会话时触发</summary>
        public event EventHandler<SessionEventArgs> NewSession;

        private SessionCollection _Sessions;
        /// <summary>会话集合。用地址端口作为标识，业务应用自己维持地址端口与业务主键的对应关系。</summary>
        public IDictionary<String, ISocketSession> Sessions { get { return _Sessions; } }

        Int32 g_ID = 0;
        /// <summary>创建会话</summary>
        /// <param name="remoteEP"></param>
        /// <returns></returns>
        public virtual ISocketSession CreateSession(IPEndPoint remoteEP)
        {
            if (Disposed) throw new ObjectDisposedException(this.GetType().Name);

            var sessions = _Sessions;
            if (sessions == null) return null;

            // 平均执行耗时260.80ns，其中55%花在sessions.Get上面，Get里面有加锁操作

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
                us.LogSend = LogSend;
                us.LogReceive = LogReceive;
                // UDP不好分会话统计
                //us.StatSend.Parent = StatSend;
                //us.StatReceive.Parent = StatReceive;

                session = us;
                if (sessions.Add(session))
                {
                    //us.ID = g_ID++;
                    // 会话改为原子操作，避免多线程冲突
                    us.ID = Interlocked.Increment(ref g_ID);
                    us.Start();

                    if (StatSession != null) StatSession.Increment(1);

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
        void IServer.Start() { Open(); }

        void IServer.Stop() { Close("服务停止"); }
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