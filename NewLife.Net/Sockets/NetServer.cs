using System;
using System.Net;
using System.Net.Sockets;
using NewLife.Net.Tcp;
using NewLife.Net.Udp;

namespace NewLife.Net.Sockets
{
    /// <summary>
    /// 网络服务器
    /// </summary>
    /// <remarks>
    /// 网络服务器模型，所有网络应用服务器可以通过继承该类实现。
    /// 该类仅实现了业务应用对网络流的操作，与具体网络协议无关。
    /// </remarks>
    /// <remarks>
    /// 使用方法：重载方法EnsureCreateServer来创建一个SocketServer并赋值给Server属性，EnsureCreateServer将会在OnStart时首先被调用
    /// </remarks>
    public class NetServer : Netbase
    {
        #region 属性
        private IPAddress _Address = IPAddress.Any;
        /// <summary>监听本地地址</summary>
        public IPAddress Address
        {
            get { return _Address; }
            set
            {
                _Address = value;
                if (value != null) AddressFamily = value.AddressFamily;
            }
        }

        private Int32 _Port;
        /// <summary>端口</summary>
        public Int32 Port
        {
            get { return _Port; }
            set { _Port = value; }
        }

        private AddressFamily _AddressFamily = AddressFamily.InterNetwork;
        /// <summary>地址族</summary>
        public AddressFamily AddressFamily
        {
            get { return _AddressFamily; }
            set { _AddressFamily = value; }
        }

        private SocketServer _Server;
        /// <summary>服务器</summary>
        public SocketServer Server
        {
            get { return _Server; }
            set { _Server = value; }
        }

        /// <summary>是否活动</summary>
        public Boolean Active
        {
            get { return _Server == null ? false : _Server.Active; }
        }

        private String _Name;
        /// <summary>服务名</summary>
        public String Name
        {
            get { return _Name ?? (_Name = GetType().Name); }
            set { _Name = value; }
        }

        private ProtocolType _Protocol = ProtocolType.Unknown;
        /// <summary>协议类型</summary>
        public ProtocolType ProtocolType
        {
            get { return _Protocol; }
            set { _Protocol = value; }
        }
        #endregion

        #region 构造
        /// <summary>
        /// 实例化一个网络服务器
        /// </summary>
        public NetServer() { }

        /// <summary>
        /// 通过指定监听地址和端口实例化一个网络服务器
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        public NetServer(IPAddress address, Int32 port)
        {
            Address = address;
            Port = port;
        }

        /// <summary>
        /// 通过指定监听地址和端口，还有协议，实例化一个网络服务器，默认支持Tcp协议和Udp协议
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <param name="protocolType"></param>
        public NetServer(IPAddress address, Int32 port, ProtocolType protocolType)
            : this(address, port)
        {
            ProtocolType = protocolType;
        }
        #endregion

        #region 方法
        /// <summary>
        /// 开始
        /// </summary>
        public void Start()
        {
            if (Active) throw new InvalidOperationException("服务已经开始！");

            OnStart();

            ProtocolType = Server.ProtocolType;
        }

        /// <summary>
        /// 确保建立服务器
        /// </summary>
        protected virtual void EnsureCreateServer()
        {
            if (Server == null)
            {
                if (ProtocolType == ProtocolType.Unknown) throw new Exception("未指定协议类型！");
                if (ProtocolType == ProtocolType.Tcp)
                {
                    TcpServer svr = new TcpServer(Address, Port);
                    svr.AddressFamily = AddressFamily;
                    svr.Accepted += new EventHandler<NetEventArgs>(OnAccepted);

                    Server = svr;
                }
                else if (ProtocolType == ProtocolType.Udp)
                {
                    UdpServer svr = new UdpServer(Address, Port);
                    svr.AddressFamily = AddressFamily;
                    svr.Received += new EventHandler<NetEventArgs>(OnAccepted);
                    svr.Received += new EventHandler<NetEventArgs>(OnReceived);

                    Server = svr;
                }
                else
                {
                    throw new Exception("不支持的协议类型" + ProtocolType + "！");
                }
            }
        }

        /// <summary>
        /// 开始时调用的方法
        /// </summary>
        protected virtual void OnStart()
        {
            EnsureCreateServer();

            Server.Error += new EventHandler<NetEventArgs>(OnError);
            Server.Start();

            WriteLog("{0} 开始监听{1}", Name, Server.Server.LocalEndPoint);
        }

        /// <summary>
        /// 断开连接/发生错误
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnError(object sender, NetEventArgs e)
        {
            if (e.SocketError != SocketError.Success || e.UserToken is Exception)
                WriteLog("{2}错误 {0} {1}", e.SocketError, e.UserToken as Exception, e.LastOperation);
            else
                WriteLog("{0}断开！", e.LastOperation);
        }

        /// <summary>
        /// 停止
        /// </summary>
        public void Stop()
        {
            if (!Active) throw new InvalidOperationException("服务没有开始！");

            WriteLog("{0}停止监听{1}", this.GetType().Name, Server.Server.LocalEndPoint);

            OnStop();
        }

        /// <summary>
        /// 停止时调用的方法
        /// </summary>
        protected virtual void OnStop()
        {
            Dispose();
        }

        /// <summary>
        /// 子类重载实现资源释放逻辑
        /// </summary>
        /// <param name="disposing">从Dispose调用（释放所有资源）还是析构函数调用（释放非托管资源）</param>
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            // 释放托管资源
            //if (disposing)
            {
                if (Server != null) Server.Stop();
            }
        }
        #endregion

        #region 业务
        /// <summary>
        /// 接受连接时，对于Udp是受到数据时（同时触发OnReceived）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnAccepted(Object sender, NetEventArgs e)
        {
            TcpClientX session = e.UserToken as TcpClientX;
            if (session != null)
            {
                session.Received += OnReceived;
                session.Error += new EventHandler<NetEventArgs>(OnError);
            }
        }

        /// <summary>
        /// 收到数据时
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnReceived(Object sender, NetEventArgs e) { }

        /// <summary>
        /// 把数据发送给客户端
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        /// <param name="remoteEP"></param>
        protected virtual void Send(SocketBase sender, Byte[] buffer, Int32 offset, Int32 size, EndPoint remoteEP)
        {
            if (ProtocolType == ProtocolType.Tcp)
            {
                TcpClientX tc = sender as TcpClientX;
                if (tc != null && tc.Client.Connected) tc.Send(buffer, offset, size);
            }
            else if (ProtocolType == ProtocolType.Udp)
            {
                //if ((remoteEP as IPEndPoint).Address != IPAddress.Any)
                // 兼容IPV6
                IPEndPoint remote = remoteEP as IPEndPoint;
                if (remote != null && remote.Address != IPAddress.Any && remote.Address != IPAddress.IPv6Any)
                {
                    UdpServer us = sender as UdpServer;
                    us.Send(buffer, offset, size, remoteEP);
                }
            }
        }

        /// <summary>
        /// 断开客户端连接
        /// </summary>
        /// <param name="client"></param>
        protected virtual void Disconnect(SocketBase client)
        {
            if (ProtocolType == ProtocolType.Tcp)
            {
                TcpClientX tc = client as TcpClientX;
                if (tc != null && tc.Client.Connected) tc.Close();
            }
            else if (ProtocolType == ProtocolType.Udp)
            {
            }
        }
        #endregion
    }
}