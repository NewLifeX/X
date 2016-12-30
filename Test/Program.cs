using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Agent;
using NewLife.Common;
using NewLife.Log;
using NewLife.Net;
using NewLife.Net.DNS;
using NewLife.Net.IO;
using NewLife.Net.Proxy;
using NewLife.Net.Stress;
using NewLife.Reflection;
using NewLife.Remoting;
using NewLife.Security;
using NewLife.Serialization;
using NewLife.Threading;
using NewLife.Web;
using NewLife.Xml;
using XCode.DataAccessLayer;
using XCode.Membership;
using XCode.Transform;

namespace Test
{
    public class Program
    {
        private static void Main(string[] args)
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;

            //XTrace.Log = new NetworkLog();
            XTrace.UseConsole();
#if DEBUG
            XTrace.Debug = true;
#endif
            while (true)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
#if !DEBUG
                try
                {
#endif
                    Test1();
#if !DEBUG
                }
                catch (Exception ex)
                {
                    XTrace.WriteException(ex);
                }
#endif

                sw.Stop();
                Console.WriteLine("OK! 耗时 {0}", sw.Elapsed);
                //Thread.Sleep(5000);
                GC.Collect();
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key != ConsoleKey.C) break;
            }
        }

        static void Test1()
        {
            ApiTest.Main();
            //NewLife.MessageQueue.MQTest.TestBase();
            //NewLife.MessageQueue.MQTest.Main();
            //TestService.ServiceMain();
            //PacketProvider.Test();
            //var svr = new DNSServer();
            //svr.Start();

            //var uri = new Uri("http://yun.wslink.cn/Home/About");
            //var http = uri.CreateRemote();
            //http.Send("");
            //var html = http.ReceiveString();
            //Console.WriteLine(html.Length);

            //var client = new WebClientX(true, true);
            //var html = client.DownloadString("http://yun.wslink.cn/Home/About");
            //Console.WriteLine(html.Length);
            //for (int i = 0; i < 10; i++)
            //{
            //    html = client.DownloadString("http://yun.wslink.cn/Home/About");
            //    XTrace.WriteLine("" + html?.Length);
            //}

            //uri = new Uri("ws://yun.wslink.cn");
            //http = uri.CreateRemote();
            //http.Send("");
            //html = http.ReceiveString();
            //Console.WriteLine(html.Length);

            //var t1 = new TimerX(TestTimer, 1, 100, 1000);
            //var ts = TimerScheduler.Create("Test");
            //var t2 = new TimerX(TestTimer, 2, 300, 777, ts);
            //Console.ReadKey();
        }

        static void TestTimer(Object state)
        {
            XTrace.WriteLine("State={0} Timer={1} Scheduler={2}", state, TimerX.Current, TimerScheduler.Current);
        }
    }
}