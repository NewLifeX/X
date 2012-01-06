using System;
using System.Net.Sockets;
using System.Net;

namespace NewLife.Net.Proxy
{
    /// <summary>通用NAT代理。所有收到的数据，都转发到指定目标</summary>
    public class NATProxy : ProxyBase
    {
        #region 属性
        private NATFilter _nat;

        /// <summary>服务器地址</summary>
        public IPAddress ServerAddress { get { return _nat.Address; } set { _nat.Address = value; } }

        /// <summary>服务器端口</summary>
        public Int32 ServerPort { get { return _nat.Port; } set { _nat.Port = value; } }

        /// <summary>服务器协议</summary>
        public ProtocolType ServerProtocolType { get { return _nat.ProtocolType; } set { _nat.ProtocolType = value; } }
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public NATProxy()
        {
            _nat = new NATFilter();
            _nat.Proxy = this;
            Filters.Add(_nat);
        }

        /// <summary>实例化</summary>
        /// <param name="server">目标服务器地址</param>
        /// <param name="port">目标服务器端口</param>
        public NATProxy(String server, Int32 port) : this(server, port, ProtocolType.Tcp) { }

        /// <summary>实例化</summary>
        /// <param name="server">目标服务器地址</param>
        /// <param name="port">目标服务器端口</param>
        /// <param name="protocol">协议</param>
        public NATProxy(String server, Int32 port, ProtocolType protocol)
            : this()
        {
            if (!String.IsNullOrEmpty(server)) ServerAddress = NetHelper.ParseAddress(server);
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
        #endregion
    }
}