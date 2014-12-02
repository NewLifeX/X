using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using NewLife.Log;
using NewLife.Model;
using NewLife.Net.Tcp;

namespace NewLife.Net.Sockets
{
    /// <summary>网络服务器。可同时支持多个Socket服务器，同时支持IPv4和IPv6，同时支持Tcp和Udp</summary>
    /// <remarks>
    /// 网络服务器模型，所有网络应用服务器可以通过继承该类实现。
    /// 该类仅实现了业务应用对网络流的操作，与具体网络协议无关。
    /// 
    /// 收到请求<see cref="OnAccepted"/>后，会建立<see cref="CreateSession"/>会话，并加入到会话集合<see cref="Sessions"/>中，然后启动<see cref="Start"/>会话处理；
    /// 
    /// 快速用法：
    /// 指定端口后直接<see cref="Start"/>，NetServer将同时监听Tcp/Udp和IPv4/IPv6（会检查是否支持）四个端口。
    /// 
    /// 简单用法：
    /// 重载方法<see cref="EnsureCreateServer"/>来创建一个SocketServer并赋值给<see cref="Server"/>属性，<see cref="EnsureCreateServer"/>将会在<see cref="OnStart"/>时首先被调用。
    /// 
    /// 标准用法：
    /// 使用<see cref="AttachServer"/>方法向网络服务器添加Socket服务，其中第一个将作为默认Socket服务<see cref="Server"/>。
    /// 如果Socket服务集合<see cref="Servers"/>为空，将依据地址<see cref="Address"/>、端口<see cref="Port"/>、地址族<see cref="AddressFamily"/>、协议<see cref="ProtocolType"/>创建默认Socket服务。
    /// 如果地址族<see cref="AddressFamily"/>指定为IPv4和IPv6以外的值，将同时创建IPv4和IPv6两个Socket服务；
    /// 如果协议<see cref="ProtocolType"/>指定为Tcp和Udp以外的值，将同时创建Tcp和Udp两个Socket服务；
    /// 默认情况下，地址族<see cref="AddressFamily"/>和协议<see cref="ProtocolType"/>都是其它值，所以一共将会创建四个Socket服务（Tcp、Tcpv6、Udp、Udpv6）。
    /// </remarks>
    public class NetServer : Netbase, IServer
    {
        #region 属性
        private String _Name;
        /// <summary>服务名</summary>
        public String Name { get { return _Name ?? (_Name = GetType().Name); } set { _Name = value; } }

        private NetUri _Local = new NetUri();
        /// <summary>本地结点</summary>
        public NetUri Local { get { return _Local; } set { _Local = value; } }

        /// <summary>端口</summary>
        public Int32 Port { get { return _Local.Port; } set { _Local.Port = value; } }

        /// <summary>协议类型</summary>
        public ProtocolType ProtocolType { get { return _Local.ProtocolType; } set { _Local.ProtocolType = value; } }

        private AddressFamily _AddressFamily = AddressFamily.Unknown;
        /// <summary>寻址方案</summary>
        public AddressFamily AddressFamily { get { return _AddressFamily; } set { _AddressFamily = value; } }

        private List<ISocketServer> _Servers;
        /// <summary>服务器集合。</summary>
        public IList<ISocketServer> Servers { get { return _Servers ?? (_Servers = new List<ISocketServer>()); } }

        /// <summary>服务器。返回服务器集合中的第一个服务器</summary>
        public ISocketServer Server
        {
            get
            {
                var ss = Servers;
                if (ss.Count <= 0) EnsureCreateServer();

                return ss.Count > 0 ? ss[0] : null;
            }
            set { if (!Servers.Contains(value)) _Servers.Insert(0, value); }
        }

        /// <summary>是否活动</summary>
        public Boolean Active { get { return Server != null && Server.Active; } }

        private Boolean _ShowAbortAsError;
        /// <summary>显示取消操作作为错误</summary>
        public Boolean ShowAbortAsError { get { return _ShowAbortAsError; } set { _ShowAbortAsError = value; } }

        private Boolean _UseSession;
        /// <summary>使用会话</summary>
        public Boolean UseSession { get { return _UseSession; } set { _UseSession = value; } }
        #endregion

        #region 构造
        /// <summary>实例化一个网络服务器</summary>
        public NetServer() { }

        /// <summary>通过指定监听地址和端口实例化一个网络服务器</summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        public NetServer(IPAddress address, Int32 port)
        {
            Local.Address = address;
            Port = port;
        }

        /// <summary>通过指定监听地址和端口，还有协议，实例化一个网络服务器，默认支持Tcp协议和Udp协议</summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <param name="protocolType"></param>
        public NetServer(IPAddress address, Int32 port, ProtocolType protocolType)
            : this(address, port)
        {
            Local.ProtocolType = protocolType;
        }

        /// <summary>已重载。释放会话集合等资源</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            if (Active) Stop();

            // 释放托管资源
            if (disposing)
            {
                var sessions = _Sessions;
                if (sessions != null)
                {
                    _Sessions = null;

                    WriteLog("准备释放网络会话{0}个！", sessions.Count);
                    foreach (var item in sessions.Values.ToArray())
                    {
                        item.Dispose();
                    }
                    sessions.Clear();
                }

                var severs = _Servers;
                if (severs != null)
                {
                    _Servers = null;

                    WriteLog("准备释放服务{0}个！", severs.Count);
                    foreach (var item in severs)
                    {
                        item.Dispose();
                    }
                    severs.Clear();
                }
            }
        }
        #endregion

        #region 创建
        /// <summary>添加Socket服务器</summary>
        /// <param name="server"></param>
        /// <returns>添加是否成功</returns>
        public virtual Boolean AttachServer(ISocketServer server)
        {
            if (Servers.Contains(server)) return false;

            if (server.Local.ProtocolType == ProtocolType.Tcp)
            {
                var svr = server as TcpServer;
                svr.Accepted += OnAccepted;
            }
            else if (server.Local.ProtocolType == ProtocolType.Udp)
            {
                var svr = server as UdpServer;
                //svr.Received += OnAccepted;
                svr.Received += OnReceived;
            }
            else
            {
                throw new Exception("不支持的协议类型" + server.Local.ProtocolType + "！");
            }

            server.Error += OnError;

            Servers.Add(server);
            return true;
        }

        /// <summary>同时添加指定端口的IPv4和IPv6服务器，如果协议不是指定的Tcp或Udp，则同时添加Tcp和Udp服务器</summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <param name="protocol"></param>
        /// <param name="family"></param>
        /// <returns></returns>
        public virtual Int32 AddServer(IPAddress address, Int32 port, ProtocolType protocol = ProtocolType.Unknown, AddressFamily family = AddressFamily.Unknown)
        {
            var list = CreateServer(address, port, protocol, family);
            Int32 count = 0;
            foreach (var item in list)
            {
                AttachServer(item);

                count++;
            }
            return count;
        }

        /// <summary>确保建立服务器</summary>
        protected virtual void EnsureCreateServer()
        {
            if (Servers.Count <= 0)
            {
                var list = CreateServer(Local.Address, Port, Local.ProtocolType, AddressFamily);
                foreach (var item in list)
                {
                    AttachServer(item);
                }
            }
        }
        #endregion

        #region 方法
        /// <summary>开始服务</summary>
        public void Start()
        {
            if (Active) throw new InvalidOperationException("服务已经开始！");

            OnStart();

            if (Server == null)
            {
                WriteLog("{0} 没有可用Socket服务器！", Name);

                return;
            }

            Local.ProtocolType = Server.Local.ProtocolType;

            WriteLog("{0} 准备就绪！", Name);
        }

        /// <summary>开始时调用的方法</summary>
        protected virtual void OnStart()
        {
            EnsureCreateServer();

            foreach (var item in Servers)
            {
                item.Start();

                // 如果是随机端口，反写回来，并且修改其它服务器的端口
                if (Port == 0)
                {
                    Port = item.Port;

                    foreach (var elm in Servers)
                    {
                        if (elm != item && elm.Port == 0) elm.Port = Port;
                    }
                }

                WriteLog("{0} 开始监听 {1}", Name, item);
            }
        }

        /// <summary>停止服务</summary>
        public void Stop()
        {
            if (!Active) throw new InvalidOperationException("服务没有开始！");

            foreach (var item in Servers)
            {
                WriteLog("{0} 停止监听 {1}", Name, item);
            }

            OnStop();

            WriteLog("{0} 已停止！", Name);
        }

        /// <summary>停止时调用的方法</summary>
        protected virtual void OnStop() { Dispose(); }
        #endregion

        #region 业务
        /// <summary>连接完成。在事件处理代码中，事件参数不得另作他用，套接字事件池将会将其回收。</summary>
        public event EventHandler<AcceptedEventArgs> Accepted;

        /// <summary>数据到达，在事件处理代码中，事件参数不得另作他用，套接字事件池将会将其回收。</summary>
        public event EventHandler<ReceivedEventArgs> Received;

        /// <summary>接受连接时，对于Udp是收到数据时（同时触发OnReceived）。
        /// 如果业务逻辑简单，不需要使用会话，可以重载<see cref="OnAccepted"/>来屏蔽。</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnAccepted(Object sender, AcceptedEventArgs e)
        {
            var session = e.Session;

            OnAccept(session);

            if (Accepted != null) Accepted(sender, e);
        }

        /// <summary>收到连接时</summary>
        /// <param name="session"></param>
        protected virtual void OnAccept(ISocketSession session)
        {
            var ns = CreateSession(session);
            ns.Server = session.Server;
            ns.Session = session;
            ns.ClientEndPoint = session.Remote.EndPoint;

            //SessionCount++;
            //session.OnDisposed += (s, e2) => SessionCount--;
            Interlocked.Increment(ref _SessionCount);
            session.OnDisposed += (s, e2) => Interlocked.Decrement(ref _SessionCount);

            session.OnDisposed += (s, e2) => ns.Dispose();

            if (UseSession) AddSession(ns);

            var tc = session as TcpSession;
            if (tc != null)
            {
                tc.Received += OnReceived;
                tc.Error += OnError;
            }

            // 开始会话处理
            ns.Start();
        }

        /// <summary>收到数据时</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnReceived(Object sender, ReceivedEventArgs e)
        {
            var session = sender as ISocketSession;

            // 特殊处理Udp.Accept
            if (session.Local.ProtocolType == ProtocolType.Udp) OnAccepted(sender, new AcceptedEventArgs { Session = session });

            OnReceive(session, e.Stream);

            if (Received != null) Received(sender, e);
        }

        /// <summary>收到数据时</summary>
        /// <param name="session"></param>
        /// <param name="stream"></param>
        protected virtual void OnReceive(ISocketSession session, Stream stream) { }

        /// <summary>触发异常</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnError(Object sender, ExceptionEventArgs e)
        {
            //if (!EnableLog) return;
            if (Log.Level < LogLevel.Info) return;

            //if ((e.SocketError == SocketError.OperationAborted || e.SocketError == SocketError.ConnectionReset) && !ShowAbortAsError) return;

            //if (e.SocketError != SocketError.Success || e.Error != null)
            //    WriteLog("{0} {1}错误 {2} {3}", sender, e.LastOperation, e.SocketError, e.Error);
            //else
            //    WriteDebugLog("{0} {1}断开！", sender, e.LastOperation);
        }
        #endregion

        #region 会话
        private IDictionary<Int32, INetSession> _Sessions;
        /// <summary>会话集合。用自增的数字ID作为标识，业务应用自己维持ID与业务主键的对应关系。</summary>
        public IDictionary<Int32, INetSession> Sessions { get { return _Sessions ?? (_Sessions = new Dictionary<Int32, INetSession>()); } }

        private Int32 _SessionCount;
        /// <summary>会话数</summary>
        public Int32 SessionCount { get { return _SessionCount; } set { _SessionCount = value; } }

        private Int32 sessionID = 0;
        /// <summary>添加会话。子类可以在添加会话前对会话进行一些处理</summary>
        /// <param name="session"></param>
        protected virtual void AddSession(INetSession session)
        {
            var dic = Sessions;
            lock (dic)
            {
                // sessionID变大后，可能达到最大值，然后变为-1，再变为0，所以不用担心
                session.ID = ++sessionID;
                if (session.Host == null) session.Host = this;
                session.OnDisposed += (s, e) => { lock (dic) { dic.Remove((s as INetSession).ID); } };
                dic[session.ID] = session;
            }
        }

        /// <summary>创建会话</summary>
        /// <param name="session"></param>
        /// <returns></returns>
        protected virtual INetSession CreateSession(ISocketSession session)
        {
            var ns = NetService.Container.Resolve<INetSession>();
            ns.Host = this;

            return ns;
        }
        #endregion

        #region 创建Tcp/Udp、IPv4/IPv6服务
        /// <summary>创建Tcp/Udp、IPv4/IPv6服务</summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <param name="protocol"></param>
        /// <param name="family"></param>
        /// <returns></returns>
        protected static ISocketServer[] CreateServer(IPAddress address, Int32 port, ProtocolType protocol, AddressFamily family)
        {
            if (protocol == ProtocolType.Tcp) return CreateServer<TcpServer>(address, port, family);
            if (protocol == ProtocolType.Udp) return CreateServer<UdpServer>(address, port, family);

            var list = new List<ISocketServer>();

            // 其它未知协议，同时用Tcp和Udp
            list.AddRange(CreateServer<TcpServer>(address, port, family));
            list.AddRange(CreateServer<UdpServer>(address, port, family));

            return list.ToArray();
        }

        static ISocketServer[] CreateServer<TServer>(IPAddress address, Int32 port, AddressFamily family) where TServer : ISocketServer, new()
        {
            var list = new List<ISocketServer>();
            switch (family)
            {
                case AddressFamily.InterNetwork:
                case AddressFamily.InterNetworkV6:
                    var svr = new TServer();
                    svr.Local.Address = address.GetRightAny(family);
                    svr.Port = port;
                    //svr.AddressFamily = family;

                    // 协议端口不能是已经被占用
                    //if (!NetHelper.IsUsed(svr.Local.ProtocolType, svr.Local.Address, svr.Port)) list.Add(svr);
                    if (!svr.Local.CheckPort()) list.Add(svr);
                    break;
                default:
                    // 其它情况表示同时支持IPv4和IPv6
#if !NET4
                    if (Socket.SupportsIPv4) list.AddRange(CreateServer<TServer>(address, port, AddressFamily.InterNetwork));
#else
                    if (Socket.OSSupportsIPv4) list.AddRange(CreateServer<TServer>(address, port, AddressFamily.InterNetwork));
#endif
                    if (Socket.OSSupportsIPv6) list.AddRange(CreateServer<TServer>(address, port, AddressFamily.InterNetworkV6));
                    break;
            }

            return list.ToArray();
        }
        #endregion

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            var servers = Servers;
            if (servers == null || servers.Count < 1) return Name;

            if (servers.Count == 1) return Name + " " + servers[0].ToString();

            var sb = new StringBuilder();
            foreach (var item in servers)
            {
                if (sb.Length > 0) sb.Append(" ");
                sb.Append(item);
            }
            return Name + " " + sb.ToString();
        }
        #endregion
    }

    /// <summary>网络服务器</summary>
    /// <typeparam name="TSession"></typeparam>
    public class NetServer<TSession> : NetServer where TSession : class, INetSession, new()
    {
        /// <summary>创建会话</summary>
        /// <param name="session"></param>
        /// <returns></returns>
        protected override INetSession CreateSession(ISocketSession session) { return new TSession(); }

        /// <summary>获取指定标识的会话</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public TSession GetSession(Int32 id)
        {
            if (id <= 0) return null;

            INetSession ns = null;
            if (!Sessions.TryGetValue(id, out ns)) return null;

            return ns as TSession;
        }
    }
}