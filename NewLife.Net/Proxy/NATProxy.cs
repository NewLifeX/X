using System;
using System.Net;
using System.Net.Sockets;
using NewLife.Net.Sockets;

namespace NewLife.Net.Proxy
{
    /// <summary>通用NAT代理。所有收到的数据，都转发到指定目标</summary>
    public class NATProxy : ProxyBase
    {
        #region 属性
        private IPAddress _ServerAddress;
        /// <summary>服务器地址</summary>
        public IPAddress ServerAddress { get { return _ServerAddress ?? NetHelper.ParseAddress(_ServerHost); } set { _ServerAddress = value; } }

        private String _ServerHost;
        /// <summary>服务器主机地址</summary>
        public String ServerHost { get { return _ServerHost ?? "" + _ServerAddress; } set { _ServerHost = value; } }

        private Int32 _ServerPort;
        /// <summary>服务器端口</summary>
        public Int32 ServerPort { get { return _ServerPort; } set { _ServerPort = value; } }

        private ProtocolType _ServerProtocolType;
        /// <summary>服务器协议。默认与客户端协议相同</summary>
        public ProtocolType ServerProtocolType { get { return _ServerProtocolType; } set { _ServerProtocolType = value; } }
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public NATProxy() { }

        /// <summary>实例化</summary>
        /// <param name="hostname">目标服务器地址</param>
        /// <param name="port">目标服务器端口</param>
        public NATProxy(String hostname, Int32 port) : this(hostname, port, ProtocolType.Tcp) { }

        /// <summary>实例化</summary>
        /// <param name="hostname">目标服务器地址</param>
        /// <param name="port">目标服务器端口</param>
        /// <param name="protocol">协议</param>
        public NATProxy(String hostname, Int32 port, ProtocolType protocol)
            : this()
        {
            ServerHost = hostname;
            ServerPort = port;
            ServerProtocolType = protocol;
        }
        #endregion

        #region 方法
        /// <summary>开始</summary>
        protected override void OnStart()
        {
            if (ServerProtocolType == 0) ServerProtocolType = ProtocolType;

            base.OnStart();
        }

        ///// <summary>创建会话</summary>
        ///// <param name="e"></param>
        ///// <returns></returns>
        //protected override INetSession CreateSession(NetEventArgs e)
        //{
        //    var session = new NATSession();
        //    session.RemoteEndPoint = new IPEndPoint(NetHelper.ParseAddress(ServerAddress), ServerPort);
        //    if (ServerProtocolType == ProtocolType.Tcp || ServerProtocolType == ProtocolType.Udp)
        //        session.RemoteProtocolType = ServerProtocolType;
        //    else
        //        session.RemoteProtocolType = e.Socket.ProtocolType;

        //    return session;
        //}

        /// <summary>添加会话。子类可以在添加会话前对会话进行一些处理</summary>
        /// <param name="session"></param>
        protected override void AddSession(INetSession session)
        {
            var s = session as ProxySession;
            s.RemoteEndPoint = new IPEndPoint(ServerAddress, ServerPort);
            if (ServerProtocolType == ProtocolType.Tcp || ServerProtocolType == ProtocolType.Udp)
                s.RemoteProtocolType = ServerProtocolType;
            else
                s.RemoteProtocolType = session.Server.ProtocolType;

            base.AddSession(session);
        }
        #endregion
    }
}