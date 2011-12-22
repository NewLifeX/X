using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Model;
using NewLife.Net.Proxy;
using NewLife.Reflection;
using NewLife.Net.Sockets;
using System.Net.Sockets;
using NewLife.Net.Tcp;
using NewLife.Net.Udp;

namespace NewLife.Net
{
    /// <summary>网络服务对象提供者</summary>
    class NetService : ServiceContainer<NetService>
    {
        static NetService()
        {
            IObjectContainer container = Container;
            container.Register<IProxySession, ProxySession>()
                .Register<ISocketServer, TcpServer>(ProtocolType.Tcp.ToString())
                .Register<ISocketServer, UdpServer>(ProtocolType.Udp.ToString())
                .Register<ISocketClient, TcpClientX>(ProtocolType.Tcp.ToString())
                .Register<ISocketClient, UdpClientX>(ProtocolType.Udp.ToString())
                .Register<ISocketSession, TcpClientX>(ProtocolType.Tcp.ToString())
                .Register<ISocketSession, UdpServer>(ProtocolType.Udp.ToString());
        }

        #region 方法
        public static Type ResolveType<TInterface>(Func<IObjectMap, Boolean> func)
        {
            foreach (IObjectMap item in Container.ResolveAllMaps(typeof(TInterface)))
            {
                if (func(item)) return item.ImplementType;
            }

            return null;
        }

        public static T Resolve<T>(ProtocolType protocol) where T : ISocket
        {
            return Resolve<T>(protocol.ToString());
        }
        #endregion
    }
}