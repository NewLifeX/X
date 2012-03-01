using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NewLife.Linq;
using NewLife.Net.Common;
using NewLife.Net.Sockets;
using NewLife.IO;

namespace NewLife.Net.Application
{
    /// <summary>网络应用程序测试</summary>
    public static class AppTest
    {
        /// <summary>开始测试</summary>
        public static void Start()
        {
            var ts = new Type[] { typeof(ChargenServer), typeof(DaytimeServer), typeof(DiscardServer), typeof(EchoServer), typeof(TimeServer) };
            //Type[] ts = new Type[] { typeof(EchoServer) };
            var list = new List<NetServer>();
            foreach (var item in ts)
            {
                var server = Activator.CreateInstance(item) as NetServer;
                server.Start();
                server.Servers.ForEach(s => s.UseThreadPool = false);
                list.Add(server);
            }

            StartEchoServer(7);
            StartDaytimeServer(13);
            StartTimeServer(37);
            StartDiscardServer(9);
            StartChargenServer(19);
        }

        static AutoResetEvent _are = new AutoResetEvent(true);
        static void OnReceived(object sender, ReceivedEventArgs e)
        {
            var session = sender as ISocketSession;
            Console.WriteLine("客户端{3} 收到 {0} [{1}] {2}", session.RemoteEndPoint, e.Stream.Length, e.Stream.ToStr(), sender);

            _are.Set();
        }

        static void OnError(object sender, NetEventArgs e)
        {
            if (e.SocketError == SocketError.OperationAborted) return;

            if (e.SocketError != SocketError.Success || e.Error != null)
                Console.WriteLine("客户端{0} {3}错误 {1} {2}", sender, e.SocketError, e.Error, e.LastOperation);
            else
                Console.WriteLine("客户端{0} {1}断开！", sender, e.LastOperation);
        }

        static void TestSend(String name, NetUri uri, Boolean isAsync, Boolean isSendData, Boolean isReceiveData)
        {
            Console.WriteLine();

            String msg = String.Format("{0}Test_{1}_{2}!", name, uri.ProtocolType, isAsync ? "异步" : "同步");
            var session = NetService.CreateSession(uri);
            session.Host.Error += new EventHandler<NetEventArgs>(OnError);
            //session.Host.Socket.ReceiveTimeout = 3000;
            if (isAsync && isReceiveData)
            {
                _are.Reset();
                session.Received += new EventHandler<ReceivedEventArgs>(OnReceived);
                session.ReceiveAsync();
            }
            if (isSendData) session.Send(msg);
            if (isReceiveData)
            {
                if (!isAsync)
                    Console.WriteLine("客户端" + session + " " + session.ReceiveString());
                else
                {
                    _are.WaitOne(2000);
                }
            }
            session.Dispose();
            Console.WriteLine("结束！");
        }

        static void TestSends(String name, IPEndPoint ep, Boolean isSendData, Boolean isReceiveData = true)
        {
            Console.WriteLine();
            Console.WriteLine("{0}：", name);
            //TestSend(name, ProtocolType.Udp, ep, false, isSendData, isReceiveData);
            //TestSend(name, ProtocolType.Udp, ep, true, isSendData, isReceiveData);
            //TestSend(name, ProtocolType.Tcp, ep, false, isSendData, isReceiveData);
            //TestSend(name, ProtocolType.Tcp, ep, true, isSendData, isReceiveData);
            var uri = new NetUri(ProtocolType.Udp, ep);
            TestSend(name, uri, false, isSendData, isReceiveData);
            TestSend(name, uri, true, isSendData, isReceiveData);
            uri.ProtocolType = ProtocolType.Tcp;
            TestSend(name, uri, false, isSendData, isReceiveData);
            TestSend(name, uri, true, isSendData, isReceiveData);

            GC.Collect();
        }

        static void StartEchoServer(Int32 port)
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Loopback, port);

            TestSends("Echo", ep, true);

            ep = new IPEndPoint(IPAddress.IPv6Loopback, port);

            TestSends("Echo IPv6", ep, true);
        }

        static void StartDaytimeServer(Int32 port)
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Loopback, port);

            TestSends("Daytime", ep, true);

            ep = new IPEndPoint(IPAddress.IPv6Loopback, port);

            TestSends("Daytime IPv6", ep, true);
        }

        static void StartTimeServer(Int32 port)
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Loopback, port);

            TestSends("Time", ep, true);

            ep = new IPEndPoint(IPAddress.IPv6Loopback, port);

            TestSends("Time IPv6", ep, true);
        }

        static void StartDiscardServer(Int32 port)
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Loopback, port);

            TestSends("Discard", ep, true, false);

            ep = new IPEndPoint(IPAddress.IPv6Loopback, port);

            TestSends("Discard IPv6", ep, true, false);
        }

        static void StartChargenServer(Int32 port)
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Loopback, port);

            TestSends("Chargen", ep, true);

            ep = new IPEndPoint(IPAddress.IPv6Loopback, port);

            TestSends("Chargen IPv6", ep, true);
        }
    }
}