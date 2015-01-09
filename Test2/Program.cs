using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using NewLife.Log;
using NewLife.Net;
using NewLife.Net.Application;
using NewLife.Net.Modbus;
using NewLife.Net.Sockets;
using NewLife.Net.Stress;
using NewLife.Security;

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
                    Console.WriteLine(ex.ToString());
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

        static void Test3()
        {
            var server = new NetServer();
            server.Port = 888;
            server.Log = XTrace.Log;
            server.UseSession = true;
            server.EnsureCreateServer();
            foreach (var item in server.Servers)
            {
                item.MaxNotActive = 8;
            }
            server.Start();
        }

        static void Test4()
        {
            var sw = new Stopwatch();
            sw.Start();
            //var ip = IPAddress.Parse("192.168.0.1");
            //var mac = ip.GetMac();
            //Console.WriteLine(mac.ToHex("-"));
            for (int i = 1; i < 256; i++)
            {
                //var ip = IPAddress.Parse("192.168.0." + i);
                //var mac = ip.GetMac();
                //if (mac != null) Console.WriteLine("{0}\t{1}", ip, mac.ToHex("-"));
                ThreadPool.QueueUserWorkItem(GetMac, i);
            }
            while (total < 255)
            {
                Console.Title = String.Format("完成：{0}/{1} {2}", success, total, sw.Elapsed);
                Thread.Sleep(500);
            }
            sw.Stop();
            Console.WriteLine("耗时 {0}", sw.Elapsed);
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