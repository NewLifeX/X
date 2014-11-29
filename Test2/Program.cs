using System;
using System.Threading;
using NewLife.Log;
using NewLife.Net;
using NewLife.Net.Application;
using NewLife.Net.Modbus;
using NewLife.Net.Sockets;
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
                    Test1();
#if !DEBUG
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
#endif

                Console.WriteLine("OK!");
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key != ConsoleKey.C) break;
                //Console.Clear();
            }
        }

        private static void Test1()
        {
            //NewLife.Net.Application.AppTest.TcpConnectionTest();
            AppTest.Start();
        }

        static UdpServer _udpServer;
        static void Test5()
        {
            if (_udpServer != null) return;

            _udpServer = new UdpServer();
            _udpServer.Port = 888;
            _udpServer.Received += _udpServer_Received;
            _udpServer.Open();

            Console.ReadKey();
            _udpServer.Dispose();
            _udpServer = null;
        }

        static void _udpServer_Received(object sender, ReceivedEventArgs e)
        {
            var session = sender as ISocketSession;
            XTrace.WriteLine("{0} [{1}]：{2}", session.Remote, e.Stream.Length, e.Stream.ReadBytes().ToHex());
        }
    }
}