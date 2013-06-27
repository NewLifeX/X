using System;
using System.Net;
using System.Net.Sockets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NewLife.Net.Common;

namespace NewLife.Net.Test.Common
{
    [TestClass]
    public class NetUriTest
    {
        [TestMethod]
        public void CtorTest()
        {
            var rnd = new Random((Int32)DateTime.Now.Ticks);

            var port = rnd.Next(1, 65536);
            var uri = new NetUri("tcp://127.0.0.1" + port);

            Assert.AreEqual(ProtocolType.Tcp, uri.ProtocolType);
            Assert.AreEqual(IPAddress.Loopback, uri.Address);
            Assert.AreEqual(port, uri.Port);
        }

        [TestMethod]
        public void ParseTest()
        {
            var rnd = new Random((Int32)DateTime.Now.Ticks);

            var port = rnd.Next(1, 65536);
            var uri = new NetUri().Parse("udp://::1" + port);

            Assert.AreEqual(ProtocolType.Udp, uri.ProtocolType);
            Assert.AreEqual(IPAddress.IPv6Loopback, uri.Address);
            Assert.AreEqual(port, uri.Port);
        }

        [TestMethod]
        public void ToStringTest()
        {
            var rnd = new Random((Int32)DateTime.Now.Ticks);

            var port = rnd.Next(1, 65536);
            var uri = new NetUri().Parse("udp://::1" + port);
            uri.Address = IPAddress.IPv6Any;

            Assert.AreEqual("Udp://::1",uri.ToString());
        }
    }
}
