using System;
using System.Net;
using System.Net.Sockets;
using NewLife.Net.Sockets;

namespace NewLife.Net.Proxy
{
    /// <summary>NAT过滤器。只转发数据，不处理。</summary>
    class NATFilter : ProxyFilterBase
    {
        #region 属性
        private IPAddress _Address;
        /// <summary>地址</summary>
        public IPAddress Address { get { return _Address; } set { _Address = value; } }

        private Int32 _Port;
        /// <summary>端口</summary>
        public Int32 Port { get { return _Port; } set { _Port = value; } }

        private ProtocolType _ProtocolType;
        /// <summary>协议</summary>
        public ProtocolType ProtocolType { get { return _ProtocolType; } set { _ProtocolType = value; } }
        #endregion

        #region 方法
        /// <summary>为会话创建与远程服务器通讯的Socket。可以使用Socket池达到重用的目的。</summary>
        /// <param name="session"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        public override ISocketClient CreateRemote(IProxySession session, NetEventArgs e)
        {
            session.RemoteEndPoint = new IPEndPoint(Address, Port);

            var client = NetService.Resolve<ISocketClient>(ProtocolType);
            client.Connect(Address, Port);

            return client;
        }
        #endregion
    }
}