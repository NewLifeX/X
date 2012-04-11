using System;
using System.Diagnostics;
using System.Threading;
using NewLife.Linq;
using NewLife.Log;
using NewLife.Net.Proxy;
using NewLife.Net.Sockets;
using NewLife.Threading;
using System.Net.Sockets;

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
            NewLife.Net.Application.AppTest.TcpConnectionTest();
        }
    }
}