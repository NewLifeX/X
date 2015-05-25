using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
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
            XTrace.UseConsole();
            while (true)
            {
#if !DEBUG
                try
                {
#endif
                    Test4();
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
            var client = new TcpSession();
            //client.Debug = true;
            client.Received += client_Received;
            client.Remote = "tcp://114.80.156.91:8848";
            client.Open();
            //client.Connect("114.80.156.91", 8848);

            var ms = new MemoryStream();
            ms.Write(new Byte[4]);
            ms.WriteByte(1);
            ms.WriteByte(0x10);
            ms.Write(new Byte[0x10]);
            //ms.Write(0x12);
            //ms.Write(0x34);

            var crc = new Crc32().Update(ms).Value;
            ms.Write(BitConverter.GetBytes(crc));

            client.Send(ms.ToArray());

            Thread.Sleep(50000);
            client.Dispose();
        }

        static void client_Received(object sender, ReceivedEventArgs e)
        {
            var session = sender as ISocketSession;
            XTrace.WriteLine("客户端 {0} 收到：{1}", session, e.Stream.ToStr());
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

        static TimerX _timer;
        static void Test3()
        {
            var server = new TcpServer();
            //server.Log = XTrace.Log;
            server.Port = 8;
            server.MaxNotActive = 0;
            server.NewSession += server_NewSession;
            server.Start();

            //ThreadPoolX.QueueUserWorkItem(ShowSessions);
            _timer = new TimerX(ShowSessions, server, 1000, 1000);

            NetHelper.ShowTcpParameters();
            Console.WriteLine("k键设置最优Tcp参数，其它键开始测试：");
            var key = Console.ReadKey();
            if (key.KeyChar == 'k') NetHelper.SetTcpMax();
        }

        static HashSet<String> _ips = new HashSet<string>();
        static void server_NewSession(object sender, SessionEventArgs e)
        {
            var ip = e.Session.Remote.Address;
            if (!_ips.Contains(ip.ToString()))
            {
                _ips.Add(ip.ToString());

                XTrace.WriteLine("{0,15} {1}", ip, ip.GetAddress());
            }
        }

        static Int32 _max = 0;
        static void ShowSessions(Object state)
        {
            var server = state as TcpServer;
            if (server == null) return;

            var count = server.Sessions.Count;
            if (count > _max) _max = count;
            Console.Title = "会话数：{0} 最大：{1}".F(count, _max);
        }

        static void session_Received(object sender, ReceivedEventArgs e)
        {
            var session = sender as ISocketSession;

            Console.WriteLine(e.ToStr());
            session.Send("收到：" + e.ToStr());
        }

        static void Test4()
        {
            //var proxy = new NATProxy("s3.peacemoon.cn", 3389);
            var proxy = new NATProxy("192.168.0.85", 3389);
            //proxy.Port = 89;
            proxy.Local = "tcp://:89";
            proxy.Log = XTrace.Log;
            proxy.SessionLog = XTrace.Log;
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
    }
}