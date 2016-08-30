using System;
using System.Net.Sockets;
using NewLife.Net.Sockets;

namespace NewLife.Net.Proxy
{
    /// <summary>通用NAT代理。所有收到的数据，都转发到指定目标</summary>
    public class NATProxy : ProxyBase<NATSession>
    {
        #region 属性
        /// <summary>远程服务器地址</summary>
        public NetUri RemoteServer { get; set; } = new NetUri();
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public NATProxy() { }

        /// <summary>实例化</summary>
        /// <param name="hostname">目标服务器地址</param>
        /// <param name="port">目标服务器端口</param>
        public NATProxy(String hostname, Int32 port) : this(hostname, port, NetType.Tcp) { }

        /// <summary>实例化</summary>
        /// <param name="hostname">目标服务器地址</param>
        /// <param name="port">目标服务器端口</param>
        /// <param name="protocol">协议</param>
        public NATProxy(String hostname, Int32 port, NetType protocol)
            : this()
        {
            RemoteServer = new NetUri(protocol, hostname, port);
            //RemoteServer.Host = hostname;
        }
        #endregion

        #region 方法
        /// <summary>开始</summary>
        protected override void OnStart()
        {
            WriteLog("NAT代理 => {0}", RemoteServer);

            if (RemoteServer.Type == 0) RemoteServer.Type = ProtocolType;

            base.OnStart();
        }

        /// <summary>添加会话。子类可以在添加会话前对会话进行一些处理</summary>
        /// <param name="session"></param>
        protected override void AddSession(INetSession session)
        {
            var ps = session as ProxySession;
            ps.RemoteServerUri = RemoteServer;
            // 如果不是Tcp/Udp，则使用本地协议
            if (!RemoteServer.IsTcp && !RemoteServer.IsUdp)
                ps.RemoteServerUri.Type = Local.Type;

            base.AddSession(session);
        }
        #endregion
    }

    /// <summary>NAT会话</summary>
    public class NATSession : ProxySession<NATProxy, NATSession> { }
}