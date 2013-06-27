using System;
using System.Net;
using System.Net.Sockets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NewLife.Net.Tcp;

namespace NewLife.Net.Test.Tcp
{
    [TestClass]
    public class TcpServerTest
    {
        [TestMethod]
        public void CtorTest()
        {
            var rnd = new Random((Int32)DateTime.Now.Ticks);

            var address = new IPAddress(rnd.Next(1, 0xFFFF));
            var port = rnd.Next(1, 0xFFFF);

            var server = new TcpServer(port);

            Assert.AreEqual(ProtocolType.Tcp, server.ProtocolType);
            Assert.AreEqual(ProtocolType.Tcp, server.LocalUri.ProtocolType);
            Assert.AreEqual(port, server.Port);
        }
    }
}
