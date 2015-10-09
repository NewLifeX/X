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
                    Test2();
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
            var server = new NetServer();
            server.Name = "美女";
            server.Port = 89;
            //server.Local = "udp://:89";
            //server.Log = XTrace.Log;
            server.Log = null;

            server.Start();

            while (true)
            {
                var str = "会话数：{0}".F(server.SessionCount);
                Console.Title = str;
                Console.WriteLine(str);
                Thread.Sleep(1000);
            }
        }

        static void Test7()
        {
            TestNewLife_Net test = new TestNewLife_Net();
            test.StartTest();
            test.StopTest();
        }
    }
}