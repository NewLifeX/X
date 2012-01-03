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

            Console.Write("端口：");
            Int32 port = Convert.ToInt32(Console.ReadLine().Trim());

            P2PTest.StartClient(name, "jslswb.com", 15, port);
        }
    }
}