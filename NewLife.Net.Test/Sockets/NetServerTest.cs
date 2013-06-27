using System;
using System.Net;
using System.Net.Sockets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NewLife.Net.Sockets;

namespace NewLife.Net.Test.Sockets
{
    [TestClass]
    public class NetServerTest
    {
        [TestMethod]
        public void CtorTest()
        {
            var rnd = new Random((Int32)DateTime.Now.Ticks);

            var address = new IPAddress(rnd.Next(1, 0xFFFF));
            var port = rnd.Next(1, 0xFFFF);
            var pt = rnd.Next(0, 2) == 0 ? ProtocolType.Tcp : ProtocolType.Udp;
            //var server = new NetServer(address, port, pt);

            // 默认创建四个
            var server = new NetServer();
            server.Port = port;

            Assert.AreEqual(4, server.Servers);

            var ts = server.Servers[0];
            Assert.AreEqual(AddressFamily.InterNetwork, ts.AddressFamily);
            Assert.AreEqual(ProtocolType.Tcp, ts.ProtocolType);
            Assert.AreEqual(port, ts.Port);

            ts = server.Servers[1];
            Assert.AreEqual(AddressFamily.InterNetworkV6, ts.AddressFamily);
            Assert.AreEqual(ProtocolType.Tcp, ts.ProtocolType);
            Assert.AreEqual(port, ts.Port);

            ts = server.Servers[2];
            Assert.AreEqual(AddressFamily.InterNetwork, ts.AddressFamily);
            Assert.AreEqual(ProtocolType.Udp, ts.ProtocolType);
            Assert.AreEqual(port, ts.Port);

            ts = server.Servers[3];
            Assert.AreEqual(AddressFamily.InterNetworkV6, ts.AddressFamily);
            Assert.AreEqual(ProtocolType.Udp, ts.ProtocolType);
            Assert.AreEqual(port, ts.Port);

            // 指定地址家族
            server = new NetServer();
            server.AddressFamily = AddressFamily.InterNetworkV6;
            server.Port = port;

            Assert.AreEqual(2, server.Servers);
            Assert.AreEqual(AddressFamily.InterNetworkV6, server.Servers[0].AddressFamily);
            Assert.AreEqual(AddressFamily.InterNetworkV6, server.Servers[1].AddressFamily);

            // 指定协议类型
            server = new NetServer();
            server.ProtocolType = pt;
            server.Port = port;

            Assert.AreEqual(2, server.Servers);
            Assert.AreEqual(pt, server.Servers[0].AddressFamily);
            Assert.AreEqual(pt, server.Servers[0].AddressFamily);
        }
    }
}
