using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Log;
using NewLife.Net.Tcp;
using System.Net;
using NewLife.Net.Sockets;
using test;
using XCode;
using XCode.DataAccessLayer;
using System.IO;

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
            IPAddress address = IPAddress.Loopback;
            for (int i = 0; i < 10000; i++)
            {
                TcpClientX tc = new TcpClientX();
                tc.Connect(address, 7);
                tc.Received += new EventHandler<NewLife.Net.Sockets.NetEventArgs>(tc_Received);
                tc.ReceiveAsync();

                tc.Send("我是大石头" + i + "号！");
            }
        }

        static void tc_Received(object sender, NetEventArgs e)
        {
            Console.WriteLine("[{0}] {1}", e.RemoteEndPoint, e.GetString());
        }

        static void Test2()
        {
            DAL dal = DAL.Create("Common1");
            IList<IDataTable> list = dal.Tables;
            String xml = dal.Export();
            Console.WriteLine(xml);

            File.WriteAllText("dal.xml", xml);
        }
    }
}
