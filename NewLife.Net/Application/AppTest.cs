using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Net.Sockets;
using System.Net.Sockets;
using NewLife.Net.Tcp;
using NewLife.Net.Udp;
using System.Net;
using System.Threading;
using NewLife.Reflection;
using NewLife.Log;

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
            List<NetServer> list = new List<NetServer>();
            foreach (Type item in ts)
            {
                NetServer server = Activator.CreateInstance(item) as NetServer;
                server.Start();
                list.Add(server);

                server = Activator.CreateInstance(item) as NetServer;
                server.ProtocolType = server.ProtocolType == ProtocolType.Tcp ? ProtocolType.Udp : ProtocolType.Tcp;
                server.Start();
                list.Add(server);

                //Invoke(item.Name, server.Port);
            }

            StartEchoServer(7);
            StartDaytimeServer(13);
            StartTimeServer(37);
            StartDiscardServer(9);
            StartChargenServer(19);
        }

        static T CreateClient<T>() where T : SocketClient
        {
            SocketClient client = Activator.CreateInstance(typeof(T)) as SocketClient;
            client.Error += new EventHandler<NetEventArgs>(OnError);
            client.Received += new EventHandler<NetEventArgs>(OnReceived);

            return (T)client;
        }

        static void OnReceived(object sender, NetEventArgs e)
        {
            Type type = sender == null ? null : sender.GetType();
            XTrace.WriteLine("客户端收到 {3}://{0} [{1}] {2}", e.RemoteEndPoint, e.BytesTransferred, e.GetString(), type.Name.Substring(0, 3));
        }

        static void OnError(object sender, NetEventArgs e)
        {
            if (e.SocketError == SocketError.OperationAborted) return;

            //Type type = sender == null ? null : sender.GetType();
            //XTrace.WriteLine("Error {0}", type);
            if (e.SocketError != SocketError.Success || e.UserToken is Exception)
                XTrace.WriteLine("{2}错误 {0} {1}", e.SocketError, e.UserToken as Exception, e.LastOperation);
            else
                XTrace.WriteLine("{0}断开！", e.LastOperation);
        }

        static void Invoke(String name, Object param)
        {
            MethodInfoX mi = MethodInfoX.Create(typeof(AppTest), "Start" + name);
            if (mi == null) return;
            ThreadPool.QueueUserWorkItem(delegate { Thread.Sleep(3000); mi.Invoke(null, param); });
        }

        static void TestSend(String name, ProtocolType protocol, IPEndPoint ep, Boolean isAsync, Boolean isSendData, Boolean isReceiveData)
        {
            String msg = String.Format("{0}Test_{1}_{2}!", name, protocol, isAsync ? "异步" : "同步");
            SocketClient client = null;
            if (protocol == ProtocolType.Tcp)
            {
                TcpClientX tc = CreateClient<TcpClientX>();
                tc.Connect(ep);
                if (isAsync && isReceiveData) tc.ReceiveAsync();
                if (isSendData) tc.Send(msg);
                client = tc;
            }
            else if (protocol == ProtocolType.Udp)
            {
                UdpClientX uc = CreateClient<UdpClientX>();
                if (isAsync && isReceiveData) uc.ReceiveAsync();
                if (isSendData) uc.Send(msg, ep);
                client = uc;
            }
            if (isReceiveData)
            {
                if (!isAsync)
                    XTrace.WriteLine(client.ReceiveString());
                else
                    Thread.Sleep(1000);
            }
            client.Close();
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

            //// Tcp同步
            //TcpClientX tc = CreateClient<TcpClientX>();
            //tc.Connect(ep);
            //tc.Send("EchoTest_Tcp同步!");
            //XTrace.WriteLine(tc.ReceiveString());
            //tc.Close();

            //// Tcp异步
            //tc = CreateClient<TcpClientX>();
            //tc.ReceiveAsync();
            //tc.Connect(ep);
            //tc.Send("EchoTest_Tcp异步!");
            //Thread.Sleep(1000);
            //tc.Close();

            //// Udp同步
            //UdpClientX uc = CreateClient<UdpClientX>();
            //uc.Send("EchoTest_Udp同步!", ep);
            //XTrace.WriteLine(uc.ReceiveString());
            //uc.Close();

            //// Udp同步
            //uc = CreateClient<UdpClientX>();
            //uc.ReceiveAsync();
            //uc.Send("EchoTest_Udp同步!", ep);
            //Thread.Sleep(1000);
            //uc.Close();
        }

        static void StartDaytimeServer(Int32 port)
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Loopback, port);

            TestSends("Daytime", ep, true);
        }

        static void StartTimeServer(Int32 port)
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Loopback, port);

            TestSends("Time", ep, true);
        }

        static void StartDiscardServer(Int32 port)
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Loopback, port);

            TestSends("Discard", ep, true, false);
        }

        static void StartChargenServer(Int32 port)
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Loopback, port);

            TestSends("Chargen", ep, true);
        }
    }
}