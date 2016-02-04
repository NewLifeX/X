using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using NewLife.Log;
using NewLife.Net;
using NewLife.Net.Application;
using NewLife.Net.Modbus;
using NewLife.Net.Proxy;
using NewLife.Net.Sockets;
using NewLife.Net.Stress;
using NewLife.Security;
using NewLife.Threading;

namespace Test2
{
    class Program
    {
        static void Main(string[] args)
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;

            XTrace.UseConsole();
            while (true)
            {
#if !DEBUG
                try
                {
#endif
                    Test3();
#if !DEBUG
                }
                catch (Exception ex)
                {
                    XTrace.WriteException(ex);
                    //Console.WriteLine(ex.ToString());
                }
#endif

                GC.Collect();
                Console.WriteLine("OK!");
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key != ConsoleKey.C) break;
                //Console.Clear();
            }
        }

        private static void Test1()
        {
            AppTest.Start();
            //AppTest.TcpConnectionTest();
            //TcpStress.Main();
        }

        static void Test2()
        {
            var server = new NetServer();
            server.Port = 88;
            server.NewSession += server_NewSession;
            //server.Received += server_Received;
            server.SocketLog = null;
            server.SessionLog = null;
            server.Start();

            var html = "新生命开发团队";

            var sb = new StringBuilder();
            sb.AppendLine("HTTP/1.1 200 OK");
            sb.AppendLine("Server: NewLife.WebServer");
            sb.AppendLine("Connection: keep-alive");
            sb.AppendLine("Content-Type: text/html; charset=UTF-8");
            sb.AppendFormat("Content-Length: {0}", Encoding.UTF8.GetByteCount(html));
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine();
            sb.Append(html);

            response = sb.ToString().GetBytes();

            while (true)
            {
                Console.Title = "会话：{0:n0} 请求：{1:n0} 错误：{2:n0}".F(server.SessionCount, Request, Error);
                Thread.Sleep(500);
            }
        }

        static void server_NewSession(object sender, NetSessionEventArgs e)
        {
            var session = e.Session;
            session.Received += session_Received;
            session.Session.Error += (s, e2) => Error++;
        }

        static Int32 Request;
        static Int32 Error;

        static Byte[] response;
        static void session_Received(object sender, ReceivedEventArgs e)
        {
            Request++;

            var session = sender as INetSession;
            //XTrace.WriteLine("客户端 {0} 收到：{1}", session, e.Stream.ToStr());

            //XTrace.WriteLine(response.ToStr());
            session.Send(response);

            //session.Dispose();
        }

        static void Test3()
        {
            var _server = new NetServer();
            _server.Port = 8888;
            _server.Start();

            while (true)
            {
                var svr = _server.Servers[0] as TcpServer;
                Console.Title = "会话数：{0} 连接：{1} 发送：{2} 接收：{3}".F(_server.SessionCount, svr.StatSession, svr.StatSend, svr.StatReceive);
                Thread.Sleep(1000);
            }
        }

        static UdpServer _udpServer;
        static void Test5()
        {
            if (_udpServer != null) return;

            _udpServer = new UdpServer();
            _udpServer.Port = 888;
            //_udpServer.Received += _udpServer_Received;
            _udpServer.Timeout = 5000;
            _udpServer.Open();

            var session = _udpServer.CreateSession(new IPEndPoint(IPAddress.Any, 0));
            for (int i = 0; i < 5; i++)
            {
                var buf = session.Receive();
                Console.WriteLine(buf.ToHex());
                session.Send("Hello");
            }

            //Console.ReadKey();
            _udpServer.Dispose();
            _udpServer = null;
        }

        static void _udpServer_Received(object sender, ReceivedEventArgs e)
        {
            var session = sender as ISocketSession;
            XTrace.WriteLine("{0} [{1}]：{2}", session.Remote, e.Stream.Length, e.Stream.ReadBytes().ToHex());
        }

        static void Test4()
        {
            //var proxy = new NATProxy("s3.peacemoon.cn", 3389);
            var proxy = new NATProxy("192.168.0.85", 3389);
            //proxy.Port = 89;
            proxy.Local = "tcp://:89";
            proxy.Log = XTrace.Log;
            //proxy.SessionLog = XTrace.Log;
            proxy.Start();

            while (true)
            {
                Console.Title = "会话数：{0}".F(proxy.SessionCount);
                Thread.Sleep(1000);
            }
        }

        static Int32 success = 0;
        static Int32 total = 0;
        static void GetMac(Object state)
        {
            var ip = IPAddress.Parse("192.168.0." + state);
            var mac = ip.GetMac();
            if (mac != null)
            {
                success++;
                Console.WriteLine("{0}\t{1}", ip, mac.ToHex("-"));
            }
            total++;
        }

        static void Test6()
        {
            // UDP没有客户端服务器之分。推荐使用NetUri指定服务器地址
            var udp = new UdpServer();
            udp.Remote = new NetUri("udp://smart.peacemoon.cn:7");
            udp.Received += (s, e) =>
            {
                XTrace.WriteLine("收到：{0}", e.ToStr());
            };
            udp.Open();
            udp.Send("新生命团队");
            udp.Send("学无先后达者为师！");

            // Tcp客户端会话。改用传统方式指定服务器地址
            var tcp = new TcpSession();
            tcp.Remote.Host = "smart.peacemoon.cn";
            tcp.Remote.Port = 13;
            tcp.Open();
            var str = tcp.ReceiveString();
            XTrace.WriteLine(str);

            // 产品级客户端用法。直接根据NetUri创建相应客户端
            var client = new NetUri("tcp://smart.peacemoon.cn:17").CreateRemote();
            client.Received += (s, e) =>
            {
                XTrace.WriteLine("收到：{0}", e.ToStr());
            };
            client.Open();

            Thread.Sleep(1000);
        }

        static void Test7()
        {
            TestNewLife_Net test = new TestNewLife_Net();
            test.StartTest();
            test.StopTest();
        }

        private static void Test8()
        {
            XTrace.WriteLine("启动两个服务端");

            // 不管是哪一种服务器用法，都具有相同的数据接收处理事件
            var onReceive = new EventHandler<ReceivedEventArgs>((s, e) =>
            {
                XTrace.WriteLine("收到 {0}：{1}", e.UserState, e.ToStr());

                // 原样发回去
                var session = s as ISocketSession;
                session.Send(e.Stream);
            });

            // 入门级UDP服务器，直接收数据，UserState表示来源地址
            var udp = new UdpServer(3388);
            udp.Received += onReceive;
            udp.Open();

            // 入门级TCP服务器，先接收会话连接，然后每个连接再分开接收数据
            var tcp = new TcpServer(3388);
            tcp.NewSession += (s, e) =>
            {
                XTrace.WriteLine("新连接 {0}", e.Session);
                e.Session.Received += onReceive;
            };
            tcp.Start();

            // 轻量级应用服务器（不建议作为产品级使用），同时在TCP/TCPv6/UDP/UDPv6监听指定端口，统一事件接收数据
            var svr = new NetServer();
            svr.Port = 3377;
            svr.Received += onReceive;
            svr.Start();

            Console.WriteLine();

            // 构造多个客户端连接上面的服务端
            var uri1 = new NetUri(ProtocolType.Udp, IPAddress.Loopback, 3388);
            var uri2 = new NetUri(ProtocolType.Tcp, IPAddress.Loopback, 3388);
            var uri3 = new NetUri(ProtocolType.Tcp, IPAddress.IPv6Loopback, 3377);
            var clients = new ISocketClient[] { uri1.CreateRemote(), uri2.CreateRemote(), uri3.CreateRemote() };

            // 打开每个客户端，如果是TCP，此时连接服务器。
            // 这一步也可以省略，首次收发数据时也会自动打开连接
            foreach (var item in clients)
            {
                item.Open();
            }

            Thread.Sleep(1000);
            Console.WriteLine();
            XTrace.WriteLine("以下灰色日志为客户端日志，其它颜色为服务端日志，可通过线程ID区分");

            for (int i = 0; i < 3; i++)
            {
                foreach (var item in clients)
                {
                    item.Send("第{0}次{1}发送".F(i + 1, item.Remote.ProtocolType));
                }
                Thread.Sleep(500);
            }
        }

        static void svr_Received(object sender, ReceivedEventArgs e)
        {
            XTrace.WriteLine(e.ToStr());
        }
    }
}