using System;
using System.Collections.Generic;
using System.Threading;
using NewLife.Linq;
using NewLife.Log;
using NewLife.Net;
using NewLife.Net.Application;
using NewLife.Net.Sockets;
using NewLife.Net.Tcp;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using NewLife.Net.P2P;
using NewLife.Net.Udp;

namespace Test2
{
    class Program
    {
        static void Main(string[] args)
        {
            XTrace.OnWriteLog += new EventHandler<WriteLogEventArgs>(XTrace_OnWriteLog);
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
                ConsoleKeyInfo key = Console.ReadKey();
                if (key.Key != ConsoleKey.C) break;
            }
        }

        static void XTrace_OnWriteLog(object sender, WriteLogEventArgs e)
        {
            Console.WriteLine(e.ToString());
        }

        static void Test1()
        {
            Console.Write("名称：");
            String name = Console.ReadLine();

            //Console.Write("端口：");
            //Int32 port = Convert.ToInt32(Console.ReadLine().Trim());

            P2PTest.StartClient(name, "jslswb.com", 15);
            ////P2PTest.StartClient(name);

            //var hole = new IPEndPoint(NetHelper.ParseAddress("jslswb.com"), 15);
            //var client = new UdpClientX();
            ////var client = new UdpServer();
            //client.Bind();
            //client.Received += new EventHandler<NetEventArgs>(client_Received);
            //client.ReceiveAsync();
            ////client.Send("reg.gg", null, hole);
            //client.Send("test", null, hole);
            //hole = new IPEndPoint(hole.Address, hole.Port + 1);
            //client.Send("nnhy", null, hole);
            //Console.WriteLine("监听：{0}", client.LocalEndPoint);

            //Thread.Sleep(1000);

            //Int32 port = client.LocalEndPoint.Port;
            //client.Dispose();

            //Thread.Sleep(1000);

            //client = new UdpClientX();
            //client.Port = port;
            //client.Bind();
            //client.Received += new EventHandler<NetEventArgs>(client_Received);
            //client.ReceiveAsync();
            //client.Send("reg.hh", null, hole);
            //Console.WriteLine("监听：{0}", client.LocalEndPoint);
        }

        static void client_Received(object sender, NetEventArgs e)
        {
            Console.WriteLine("HoleServer数据到来：{0} {1}", e.RemoteIPEndPoint, e.GetString());
        }
    }
}