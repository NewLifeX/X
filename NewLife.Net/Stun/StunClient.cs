using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using NewLife.Linq;
using NewLife.Net.Sockets;
using NewLife.Net.Udp;
using System.Net.Sockets;

namespace NewLife.Net.Stun
{
    /// <summary>Stun客户端。Simple Traversal of UDP over NATs，NAT 的UDP简单穿越。RFC 3489</summary>
    /// <remarks>
    /// <a href="http://baike.baidu.com/view/884586.htm">STUN</a>
    /// 
    /// 国内STUN服务器：220.181.126.73、220.181.126.74，位于北京电信，但不清楚是哪家公司
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = StunClient.Query("stun.nnhy.org", 3478);
    /// if(result.Type != StunNetType.UdpBlocked){
    ///     
    /// }
    /// else{
    ///     var publicEP = result.Public;
    /// }
    /// </code>
    /// </example>
    public class StunClient : Netbase
    {
        #region 静态
        /*
        In test I, the client sends a STUN Binding Request to a server, without any flags set in the
        CHANGE-REQUEST attribute, and without the RESPONSE-ADDRESS attribute. This causes the server 
        to send the response back to the address and port that the request came from.
            
        In test II, the client sends a Binding Request with both the "change IP" and "change port" flags
        from the CHANGE-REQUEST attribute set.  
              
        In test III, the client sends a Binding Request with only the "change port" flag set.
                          
                            +--------+
                            |  Test  |
                            |   I    |
                            +--------+
                                    |
                                    |
                                    V
                                   /\              /\
                                N /  \ Y          /  \ Y             +--------+
                UDP      <-------/Resp\--------->/ IP \------------->|  Test  |
                Blocked          \ ?  /          \Same/              |   II   |
                                  \  /            \? /               +--------+
                                   \/              \/                    |
                                                    | N                  |
                                                    |                    V
                                                    V                    /\
                                                +--------+  Sym.      N /  \
                                                |  Test  |  UDP    <---/Resp\
                                                |   II   |  Firewall   \ ?  /
                                                +--------+              \  /
                                                    |                    \/
                                                    V                    |Y
                        /\                         /\                    |
         Symmetric  N  /  \       +--------+   N  /  \                   V
            NAT  <--- / IP \<-----|  Test  |<--- /Resp\               Open
                      \Same/      |   I    |     \ ?  /               Internet
                       \? /       +--------+      \  /
                        \/                         \/
                        |                           |Y
                        |                           |
                        |                           V
                        |                           Full
                        |                           Cone
                        V              /\
                    +--------+        /  \ Y
                    |  Test  |------>/Resp\---->Restricted
                    |   III  |       \ ?  /
                    +--------+        \  /
                                       \/
                                       |N
                                       |       Port
                                       +------>Restricted

    */

        static String[] servers = new String[] { "stun.nnhy.org", "stunserver.org", "stun.xten.com", "stun.fwdnet.net", "stun.iptel.org", "220.181.126.73" };
        private static List<String> _Servers;
        /// <summary>打洞服务器</summary>
        public static List<String> Servers
        {
            get
            {
                if (_Servers == null)
                {
                    var list = new List<String>();
                    list.AddRange(servers);
                    _Servers = list;
                }
                return _Servers;
            }
        }

        /// <summary>在指定协议上执行查询，如果未指定，则内部创建</summary>
        /// <param name="protocol"></param>
        /// <returns></returns>
        public static StunResult Query(ProtocolType protocol)
        {
            var client = NetService.Resolve<ISocketClient>(protocol);
            client.Client.SendTimeout = 2000;
            client.Client.ReceiveTimeout = 2000;
            client.Bind();
            return Query(client.Client);
        }

        /// <summary>在指定套接字上执行查询，如果未指定，则内部创建</summary>
        /// <param name="socket"></param>
        /// <returns></returns>
        public static StunResult Query(Socket socket = null)
        {
            // 如果是Udp被屏蔽，很有可能是因为服务器没有响应，可以通过轮换服务器来测试
            StunResult result = null;
            foreach (var item in Servers)
            {
                Int32 p = item.IndexOf(":");
                if (p > 0)
                    result = Query(socket, item.Substring(0, p), Int32.Parse(item.Substring(p + 1)));
                else
                    result = Query(socket, item, 3478);
                if (result.Type != StunNetType.Blocked) return result;
            }
            return result;
        }

        /// <summary>在指定套接字上执行查询，如果未指定，则内部创建</summary>
        /// <param name="socket"></param>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static StunResult Query(Socket socket, String host, Int32 port = 3478, Int32 timeout = 2000)
        {
            return Query(socket, NetHelper.ParseAddress(host), port, timeout);
        }

        /// <summary>在指定套接字上执行查询，如果未指定，则内部创建</summary>
        /// <param name="socket"></param>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static StunResult Query(Socket socket, IPAddress address, Int32 port, Int32 timeout = 2000)
        {
            if (socket == null)
            {
                var client = new UdpClientX();
                client.Client.ReceiveTimeout = timeout;
                client.Bind();
                socket = client.Client;
            }

            return Query(socket, address, port);
        }

        static StunResult Query(Socket socket, IPAddress address, Int32 port)
        {
            var remote = new IPEndPoint(address, port);

            // Test I
            // 测试网络是否畅通
            var msg = new StunMessage();
            msg.Type = StunMessageType.BindingRequest;
            var rs = Query(socket, msg, remote);

            // UDP blocked.
            if (rs == null) return new StunResult(StunNetType.Blocked, null);
            WriteLog("Stun服务器：{0}", rs.ServerName);
            WriteLog("映射地址：{0}", rs.MappedAddress);
            WriteLog("源地址：{0}", rs.SourceAddress);
            WriteLog("新地址：{0}", rs.ChangedAddress);
            var remote2 = rs.ChangedAddress;

            // Test II
            // 要求改变IP和端口
            msg.ChangeIP = true;
            msg.ChangePort = true;

            // 如果本地地址就是映射地址，表示没有NAT。这里的本地地址应该有问题，永远都会是0.0.0.0
            //if (client.LocalEndPoint.Equals(test1response.MappedAddress))
            var pub = rs.MappedAddress;
            if (pub != null && (socket.LocalEndPoint as IPEndPoint).Port == pub.Port && NetHelper.GetIPs().Any(e => e.Equals(pub.Address)))
            {
                // 要求STUN服务器从另一个地址和端口向当前映射端口发送消息。如果收到，表明是完全开放网络；如果没收到，可能是防火墙阻止了。
                rs = Query(socket, msg, remote);
                // Open Internet.
                if (rs != null) return new StunResult(StunNetType.OpenInternet, pub);

                // Symmetric UDP firewall.
                return new StunResult(StunNetType.SymmetricUdpFirewall, pub);
            }
            else
            {
                rs = Query(socket, msg, remote);
                // Full cone NAT.
                if (rs != null) return new StunResult(StunNetType.FullCone, pub);

                // Test II
                msg.ChangeIP = false;
                msg.ChangePort = false;

                // 如果是Tcp，这里需要准备第二个重用的Socket
                if (socket.ProtocolType == ProtocolType.Tcp)
                {
                    var ep = socket.LocalEndPoint as IPEndPoint;
                    var sto = socket.SendTimeout;
                    var rto = socket.ReceiveTimeout;

                    // 如果原端口没有启用地址重用，则关闭它
                    Object value = socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress);
                    if (!Convert.ToBoolean(value)) socket.Close();

                    var sk = NetService.Resolve<ISocketClient>(ProtocolType.Tcp);
                    sk.Address = ep.Address;
                    sk.Port = ep.Port;
                    sk.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    sk.Bind();
                    socket = sk.Client;
                    socket.SendTimeout = sto;
                    socket.ReceiveTimeout = rto;
                }

                rs = Query(socket, msg, remote2);
                if (rs == null) return new StunResult(StunNetType.Blocked, pub);

                // 两次映射地址不一样，对称网络
                if (!rs.MappedAddress.Equals(pub)) return new StunResult(StunNetType.Symmetric, pub);

                // Test III
                msg.ChangeIP = false;
                msg.ChangePort = true;

                rs = Query(socket, msg, remote2);
                // 受限
                if (rs != null) return new StunResult(StunNetType.RestrictedCone, pub);

                // 端口受限
                return new StunResult(StunNetType.PortRestrictedCone, pub);
            }
        }
        #endregion

        #region 获取公网地址
        /// <summary>获取公网地址</summary>
        /// <param name="protocol"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static IPAddress GetPublic(ProtocolType protocol, Int32 timeout = 2000)
        {
            var msg = GetPublic(protocol, 0, timeout);
            return msg == null ? null : msg.Address;
        }

        /// <summary>获取指定端口的公网地址，只能是UDP Socket</summary>
        /// <param name="protocol"></param>
        /// <param name="port"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static IPEndPoint GetPublic(ProtocolType protocol, Int32 port, Int32 timeout)
        {
            var client = NetService.Resolve<ISocketClient>(protocol);
            client.Port = port;
            client.Client.SendTimeout = timeout;
            client.Client.ReceiveTimeout = timeout;
            client.Bind();

            var rs = GetPublic(client.Client);
            client.Dispose();
            return rs;
        }

        /// <summary>获取指定Socket的公网地址，只能是UDP Socket</summary>
        /// <param name="socket"></param>
        /// <returns></returns>
        public static IPEndPoint GetPublic(Socket socket)
        {
            var msg = new StunMessage();
            msg.Type = StunMessageType.BindingRequest;
            IPEndPoint ep = null;
            foreach (var item in Servers)
            {
                Int32 p = item.IndexOf(":");
                if (p > 0)
                    ep = new IPEndPoint(NetHelper.ParseAddress(item.Substring(0, p)), Int32.Parse(item.Substring(p + 1)));
                else
                    ep = new IPEndPoint(NetHelper.ParseAddress(item), 3478);
                var rs = Query(socket, msg, ep);
                if (rs != null && rs.MappedAddress != null) return rs.MappedAddress;
            }
            return null;
        }
        #endregion

        #region 业务
        /// <summary>查询</summary>
        /// <param name="request"></param>
        /// <param name="remoteEndPoint"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static StunMessage Query(StunMessage request, IPEndPoint remoteEndPoint, Int32 timeout = 2000)
        {
            var client = new UdpClientX();
            client.Client.ReceiveTimeout = timeout;
            client.Bind();
            return Query(client.Client, request, remoteEndPoint);
        }

        static StunMessage Query(Socket socket, StunMessage request, IPEndPoint remoteEndPoint)
        {
            var ms = new MemoryStream();
            request.Write(ms);
            ms.Position = 0;
            //client.Send(ms, remoteEndPoint);
            //client.Client.ReceiveTimeout = timeout;
            Byte[] buffer = null;
            try
            {
                if (socket.ProtocolType == ProtocolType.Tcp)
                {
                    // Tcp协议不支持更换IP或者端口
                    if (request.ChangeIP || request.ChangePort) return null;

                    if (!socket.Connected) socket.Connect(remoteEndPoint);
                    socket.Send(ms.ToArray());
                }
                else
                    socket.SendTo(ms.ToArray(), remoteEndPoint);

                var data = new Byte[1500];
                Int32 count = socket.Receive(data);
                buffer = new Byte[count];
                Buffer.BlockCopy(data, 0, buffer, 0, count);
            }
            catch { return null; }

            return StunMessage.Read(new MemoryStream(buffer));
        }
        #endregion
    }
}