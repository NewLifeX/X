using System;
using System.IO;
using System.Net;
using NewLife.Net.Sockets;
using NewLife.Net.Udp;

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
    public class StunClient
    {
        #region 静态
        /// <summary>查询</summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public static StunResult Query(String host = "220.181.126.73", Int32 port = 3478) { return Query(NetHelper.ParseAddress(host), port); }

        /// <summary>查询</summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public static StunResult Query(IPAddress address, Int32 port)
        {
            IPEndPoint remoteEndPoint = new IPEndPoint(address, port);

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
                      UDP     <-------/Resp\--------->/ IP \------------->|  Test  |
                      Blocked         \ ?  /          \Same/              |   II   |
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
                                                         V                     |Y
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

            var client = new UdpClientX();
            client.Bind();

            // Test I
            StunMessage test1 = new StunMessage();
            test1.Type = StunMessageType.BindingRequest;
            StunMessage test1response = Query(test1, client, remoteEndPoint, 1600);

            // UDP blocked.
            if (test1response == null) return new StunResult(StunNetType.UdpBlocked, null);

            // Test II
            var test2 = new StunMessage();
            test2.Type = StunMessageType.BindingRequest;
            //test2.Change = new StunMessage.ChangeRequest(true, true);
            test2.ChangeIP = true;
            test2.ChangePort = true;

            // No NAT.
            if (client.LocalEndPoint.Equals(test1response.MappedAddress))
            {
                StunMessage test2Response = Query(test2, client, remoteEndPoint, 1600);
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
                StunMessage test2Response = Query(test2, client, remoteEndPoint, 1600);

                // Full cone NAT.
                if (test2Response != null) return new StunResult(StunNetType.FullCone, test1response.MappedAddress);

                /*
                    If no response is received, it performs test I again, but this time, does so to 
                    the address and port from the CHANGED-ADDRESS attribute from the response to test I.
                */

                // Test I(II)
                StunMessage test12 = new StunMessage();
                test12.Type = StunMessageType.BindingRequest;

                StunMessage test12Response = Query(test12, client, test1response.ChangedAddress, 1600);
                if (test12Response == null) throw new Exception("STUN Test I(II) 没有收到响应！");

                // Symmetric NAT
                if (!test12Response.MappedAddress.Equals(test1response.MappedAddress)) return new StunResult(StunNetType.Symmetric, test1response.MappedAddress);

                // Test III
                var test3 = new StunMessage();
                test3.Type = StunMessageType.BindingRequest;
                //test3.Change = new StunMessage.ChangeRequest(false, true);
                test3.ChangeIP = false;
                test3.ChangePort = true;

                StunMessage test3Response = Query(test3, client, test1response.ChangedAddress, 1600);
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
        /// <returns></returns>
        public static StunMessage Query(StunMessage request, IPEndPoint remoteEndPoint)
        {
            var client = new UdpClientX();
            return Query(request, client, remoteEndPoint, 16000);
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