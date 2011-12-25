using System;
using NewLife.Linq;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NewLife.Log;
using NewLife.Model;
using NewLife.Net.Sockets;

namespace NewLife.Net.Application
{
    /// <summary>
    /// 网络应用程序测试
    /// </summary>
    public static class AppTest
    {
        /// <summary>
        /// 开始测试
        /// </summary>
        public static void Start()
        {
            Type[] ts = new Type[] { typeof(ChargenServer), typeof(DaytimeServer), typeof(DiscardServer), typeof(EchoServer), typeof(TimeServer) };
            //Type[] ts = new Type[] { typeof(EchoServer) };
            List<NetServer> list = new List<NetServer>();
            foreach (Type item in ts)
            {
                NetServer server = Activator.CreateInstance(item) as NetServer;
                server.Start();
                server.Servers.ForEach(s => s.UseThreadPool = false);
                list.Add(server);
            }

            StartEchoServer(7);
            //StartDaytimeServer(13);
            //StartTimeServer(37);
            //StartDiscardServer(9);
            //StartChargenServer(19);
        }

        static T CreateClient<T>() where T : ISocketClient
        {
            var client = Activator.CreateInstance(typeof(T)) as ISocketClient;
            client.Error += new EventHandler<NetEventArgs>(OnError);
            client.Received += new EventHandler<NetEventArgs>(OnReceived);

            return (T)client;
        }

        static void OnReceived(object sender, NetEventArgs e)
        {
            XTrace.WriteLine("客户端{3} 收到 {0} [{1}] {2}", e.RemoteEndPoint, e.BytesTransferred, e.GetString(), sender);
        }

        static void OnError(object sender, NetEventArgs e)
        {
            //if (e.SocketError == SocketError.OperationAborted) return;

            //Type type = sender == null ? null : sender.GetType();
            //XTrace.WriteLine("Error {0}", type);
            if (e.SocketError != SocketError.Success || e.Error != null)
                XTrace.WriteLine("客户端{0} {3}错误 {1} {2}", sender, e.SocketError, e.Error, e.LastOperation);
            else
                XTrace.WriteLine("客户端{0} {1}断开！", sender, e.LastOperation);
        }

        static void TestSend(String name, ProtocolType protocol, IPEndPoint ep, Boolean isAsync, Boolean isSendData, Boolean isReceiveData)
        {
            Console.WriteLine();

            String msg = String.Format("{0}Test_{1}_{2}!", name, protocol, isAsync ? "异步" : "同步");
            ISocketClient client = ObjectContainer.Current.Resolve<ISocketClient>(protocol.ToString());
            client.Error += new EventHandler<NetEventArgs>(OnError);
            client.Received += new EventHandler<NetEventArgs>(OnReceived);
            client.AddressFamily = ep.AddressFamily;
            if (protocol == ProtocolType.Tcp) client.Connect(ep);
            client.Client.ReceiveTimeout = 60000;
            if (isAsync && isReceiveData) client.ReceiveAsync();
            if (isSendData) client.Send(msg, null, ep);
            // 异步的多发一个
            //if (isAsync)
            //{
            //    Thread.Sleep(300);
            //    if (isSendData) client.Send(msg, null, ep);
            //}
            if (isReceiveData)
            {
                if (!isAsync)
                    XTrace.WriteLine("客户端" + client + " " + client.ReceiveString());
                else
                    Thread.Sleep(1000);
            }
            client.Close();
            Thread.Sleep(100);
            XTrace.WriteLine("结束！");
        }

        static void TestSends(String name, IPEndPoint ep, Boolean isSendData, Boolean isReceiveData = true)
        {
            Console.WriteLine();
            Console.WriteLine("{0}：", name);
            TestSend(name, ProtocolType.Udp, ep, false, isSendData, isReceiveData);
            TestSend(name, ProtocolType.Udp, ep, true, isSendData, isReceiveData);
            TestSend(name, ProtocolType.Tcp, ep, false, isSendData, isReceiveData);
            TestSend(name, ProtocolType.Tcp, ep, true, isSendData, isReceiveData);
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