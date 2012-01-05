using System;
using System.IO;
using System.Net;
using NewLife.Net.Sockets;
using NewLife.Net.Udp;
using NewLife.Linq;

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
    /// var result = StunClient.Query("stunserver.org", 3478);
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

        /// <summary>查询</summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static StunResult Query(String host = "220.181.126.73", Int32 port = 3478, Int32 timeout = 1000) { return Query(NetHelper.ParseAddress(host), port, timeout); }

        /// <summary>查询</summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static StunResult Query(IPAddress address, Int32 port, Int32 timeout = 1000)
        {
            IPEndPoint remoteEndPoint = new IPEndPoint(address, port);

            var client = new UdpClientX();
            client.Bind();

            // Test I
            // 测试网络是否畅通
            StunMessage test1 = new StunMessage();
            test1.Type = StunMessageType.BindingRequest;
            StunMessage test1response = Query(test1, client, remoteEndPoint, timeout);

            // UDP blocked.
            if (test1response == null) return new StunResult(StunNetType.UdpBlocked, null);
            WriteLog("服务器：{0}", test1response.ServerName);
            WriteLog("映射地址：{0}", test1response.MappedAddress);
            WriteLog("源地址：{0}", test1response.SourceAddress);
            WriteLog("新地址：{0}", test1response.ChangedAddress);

            // Test II
            // 要求改变IP和端口
            var test2 = new StunMessage();
            test2.Type = StunMessageType.BindingRequest;
            //test2.Change = new StunMessage.ChangeRequest(true, true);
            test2.ChangeIP = true;
            test2.ChangePort = true;

            // No NAT.
            // 如果本地地址就是映射地址，表示没有NAT。这里的本地地址应该有问题，永远都会是0.0.0.0
            //if (client.LocalEndPoint.Equals(test1response.MappedAddress))
            var ep = test1response.MappedAddress;
            if (ep != null && client.LocalEndPoint.Port == ep.Port && NetHelper.GetIPV4().Any(e => e.Equals(ep.Address)))
            {
                // 要求STUN服务器从另一个地址和端口向当前映射端口发送消息。如果收到，表明是完全开放网络；如果没收到，可能是防火墙阻止了。
                var test2Response = Query(test2, client, remoteEndPoint, timeout);
                // Open Internet.
                if (test2Response != null)
                {
                    return new StunResult(StunNetType.OpenInternet, test1response.MappedAddress);
                }
                // Symmetric UDP firewall.
                else
                {
                    return new StunResult(StunNetType.SymmetricUdpFirewall, test1response.MappedAddress);
                }
            }
            // NAT
            else
            {
                var test2Response = Query(test2, client, remoteEndPoint, timeout);

                // Full cone NAT.
                if (test2Response != null) return new StunResult(StunNetType.FullCone, test1response.MappedAddress);

                /*
                    If no response is received, it performs test I again, but this time, does so to 
                    the address and port from the CHANGED-ADDRESS attribute from the response to test I.
                */

                // Test I(II)
                var test12 = new StunMessage();
                test12.Type = StunMessageType.BindingRequest;

                var test12Response = Query(test12, client, test1response.ChangedAddress, timeout);
                if (test12Response == null) throw new Exception("STUN Test I(II) 没有收到响应！");

                // Symmetric NAT
                // 两次映射地址不一样，对称网络
                if (!test12Response.MappedAddress.Equals(test1response.MappedAddress)) return new StunResult(StunNetType.Symmetric, test1response.MappedAddress);

                // Test III
                var test3 = new StunMessage();
                test3.Type = StunMessageType.BindingRequest;
                //test3.Change = new StunMessage.ChangeRequest(false, true);
                test3.ChangeIP = false;
                test3.ChangePort = true;

                StunMessage test3Response = Query(test3, client, test1response.ChangedAddress, timeout);
                // Restricted
                if (test3Response != null)
                {
                    return new StunResult(StunNetType.RestrictedCone, test1response.MappedAddress);
                }
                // Port restricted
                else
                {
                    return new StunResult(StunNetType.PortRestrictedCone, test1response.MappedAddress);
                }
            }
        }
        #endregion

        #region 业务
        /// <summary>查询</summary>
        /// <param name="request"></param>
        /// <param name="remoteEndPoint"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static StunMessage Query(StunMessage request, IPEndPoint remoteEndPoint, Int32 timeout = 1000)
        {
            var client = new UdpClientX();
            return Query(request, client, remoteEndPoint, timeout);
        }

        static StunMessage Query(StunMessage request, ISocketClient client, IPEndPoint remoteEndPoint, int timeout)
        {
            var ms = new MemoryStream();
            request.Write(ms);
            ms.Position = 0;
            client.Send(ms, remoteEndPoint);
            client.Client.ReceiveTimeout = timeout;
            Byte[] buffer = null;
            try
            {
                buffer = client.Receive();
            }
            catch { return null; }

            return StunMessage.Read(new MemoryStream(buffer));
        }
        #endregion
    }
}