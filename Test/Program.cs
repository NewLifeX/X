using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using NewLife;
using NewLife.CommonEntity;
using NewLife.IO;
using NewLife.Log;
using NewLife.Messaging;
using NewLife.Net.Common;
using NewLife.Net.Sockets;
using NewLife.Net.Udp;
using NewLife.Net.UPnP;
using NewLife.Reflection;
using NewLife.Remoting;
using NewLife.Threading;
using XCode.DataAccessLayer;
using XCode;
using System.Runtime.InteropServices;
using NewLife.Net.Application;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            ////Console.WindowWidth /= 2;
            ////Console.WindowHeight /= 2;

            //Console.WindowWidth = 80;
            //Console.WindowHeight = 20;

            ////Console.BufferWidth /= 10;
            ////Console.BufferHeight /= 10;
            //Console.BufferWidth = 160;
            //Console.BufferHeight = 500;

            XTrace.OnWriteLog += new EventHandler<WriteLogEventArgs>(XTrace_OnWriteLog);
            while (true)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
#if !DEBUG
                try
                {
#endif
                    Test1();
                    //ThreadPoolTest.Main2(args);
#if !DEBUG
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
#endif

                sw.Stop();
                Console.WriteLine("OK! 耗时 {0}", sw.Elapsed);
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key != ConsoleKey.C) break;
            }
            //Console.ReadKey();
        }

        static void XTrace_OnWriteLog(object sender, WriteLogEventArgs e)
        {
            Console.WriteLine(e.ToString());
        }

        static void Test1()
        {
            
        }
    }
}