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
using NewLife.IO;
using NewLife.Security;
using System.IO.Ports;

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
                    Test2();
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
                Console.Clear();
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

            Console.Write("协议（1为Tcp，其它为Udp）：");
            Int32 istcp = Convert.ToInt32(Console.ReadLine().Trim());

            P2PTest.StartClient(name, "jslswb.com", 15, istcp == 1);
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

        static NetServer server;
        static NetServer server2;
        static void Test2()
        {
            NetHelper.Debug = true;
            //if (server == null)
            //{
            //    server = new NetServer();
            //    server.Port = 23;
            //    server.Received += new EventHandler<NetEventArgs>(server_Received);
            //    server.Start();
            //}
            //if (server2 == null)
            //{
            //    server2 = new SerialServer() { PortName = "COM1" };
            //    server2.Port = 24;
            //    //server.Received += new EventHandler<NetEventArgs>(server_Received);
            //    server2.Start();
            //}

            //Thread.Sleep(2000);

            String[] ss = new String[] { 
                "7E 2F 44 9D 10 01 01 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 22 01 0D", 
                //"7E 2F 44 8C 10 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 0F 01 0D", 
                //"7E 2F 44 F2 10 01 00 40 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 B6 01 0D", 
                //"7E 2F 44 81 10 99 02 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 9F 01 0D", 
                //"7E 2F 44 F1 10 1A 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 8E 01 0D", 
                //"7E 2F 44 F1 10 12 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 86 01 0D"
            };

            using (var client = new UdpClientX())
            {
                client.Connect("192.168.1.10", 24);
                for (int i = 0; i < ss.Length; i++)
                {
                    var data = DataHelper.FromHex(ss[i].Replace(" ", null));
                    client.Send(data, 0, data.Length);
                }
            }
        }

        static void server_Received(object sender, NetEventArgs e)
        {
            Console.WriteLine("收到：{0}", DataHelper.ToHex(e.Buffer, e.Offset, e.BytesTransferred));
        }

        //struct Door
        //{
        //    Int16 Head = 0x7E2F;
        //    Byte D3 = 0x44;
        //    Byte D4 = 0;
        //    Byte D5 = 0x10;

        //    Int16 Foot = 0x010D;
        //}
    }
}