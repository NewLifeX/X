using System;
using System.Net;
using System.Net.Sockets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NewLife.Net.Udp;

namespace NewLife.Net.Test.Udp
{
    [TestClass]
    public class UdpServerTest
    {
        [TestMethod]
        public void CtorTest()
        {
            var rnd = new Random((Int32)DateTime.Now.Ticks);

            var address = new IPAddress(rnd.Next(1, 0xFFFF));
            var port = rnd.Next(1, 0xFFFF);

            var server = new UdpServer(port);

            Assert.AreEqual(ProtocolType.Udp, server.ProtocolType);
            Assert.AreEqual(ProtocolType.Udp, server.LocalUri.ProtocolType);
            Assert.AreEqual(port, server.Port);
        }
    }
}
