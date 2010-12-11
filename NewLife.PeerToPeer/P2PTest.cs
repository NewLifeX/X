using System;
using System.IO;
using System.Net;
using NewLife.Net.Common;
using NewLife.PeerToPeer.Client;
using NewLife.PeerToPeer.Messages;
using NewLife.PeerToPeer.Server;

namespace NewLife.PeerToPeer
{
#if DEBUG
    /// <summary>
    /// P2P测试
    /// </summary>
    public static class P2PTest
    {
        /// <summary>
        /// 
        /// </summary>
        public static void Main()
        {
            //TestMessage();
            TestTracker();
        }

        /// <summary>
        /// 测试信息
        /// </summary>
        public static void TestMessage()
        {
            P2PMessage.Init();

            PingMessage msg = new PingMessage();
            msg.Token = Guid.NewGuid();
            msg.Private = NetHelper.GetIPV4();

            Byte[] buffer = msg.ToArray();
            Console.WriteLine(BitConverter.ToString(buffer));

            MemoryStream stream = new MemoryStream(buffer);
            msg = null;
            msg = P2PMessage.Deserialize(stream) as PingMessage;
            Console.WriteLine(msg == null);
        }

        /// <summary>
        /// 测试Tracker
        /// </summary>
        public static void TestTracker()
        {
            //UdpTrackerServer us = new UdpTrackerServer();
            //us.Port = 2010;

            //TrackerServer server = new TrackerServer();
            //server.Tracker = us;
            //server.Start();

            //P2PClient app = new P2PClient();
            //app.TrackerServer = new IPEndPoint(IPAddress.Loopback, 2010);
            //app.Start();

            //IPEndPoint ip = new IPEndPoint(IPAddress.Loopback, 2010);
            //app.Test(ip, "测试指令1");

            Console.ReadKey();


            //server.Stop();
        }

        /// <summary>
        /// 测试客户端
        /// </summary>
        public static void TestClient()
        {
            P2PClient app = new P2PClient();
            app.TrackerServer = new IPEndPoint(IPAddress.Parse("192.168.1.101"), 2010);
            app.Start();

            Console.ReadKey();
            app.Stop();
        }
    }
#endif
}
