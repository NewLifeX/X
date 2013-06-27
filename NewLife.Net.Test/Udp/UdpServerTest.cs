using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NewLife.Net.Sockets;
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

        [TestMethod]
        public void ReceiveTest()
        {
            var rnd = new Random((Int32)DateTime.Now.Ticks);

            var address = new IPAddress(rnd.Next(1, 0xFFFF));
            var port = rnd.Next(1, 0xFFFF);

            var server = new UdpServer(port);
            //server.Received += server_Received;
            server.Start();

            var ip = new IPEndPoint(IPAddress.Loopback, port);

            var client = new UdpClient();
            var buf = Encoding.UTF8.GetBytes("我是大石头nnhy!!!");
            client.Send(buf, buf.Length, ip);

            var bts = server.Receive();
            Assert.AreEqual(buf.Length, bts.Length);
            for (int i = 0; i < bts.Length; i++)
            {
                Assert.AreEqual(buf[i], bts[i]);
            }
        }

        void server_Received(object sender, NetEventArgs e)
        {
            // 原样返回
            var session = e.Session;
            var buf = e.GetStream().ReadBytes();

            session.Send(buf);
        }
    }
}
