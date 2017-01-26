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
            //Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;

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
                Test2();
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
                GC.WaitForPendingFinalizers();
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
            //client.Log = XTrace.Log;
            //var html = client.DownloadString("http://yun.wslink.cn/Home/About");
            //Console.WriteLine(html.Length);
            //for (int i = 0; i < 10; i++)
            //{
            //    html = client.DownloadString("http://yun.wslink.cn/Home/About");
            //    XTrace.WriteLine("" + html?.Length);
            //}

            //PluginHelper.LoadPlugin("System.Data.SQLite.SQLiteFactory", null, "System.Data.SQLite.dll", "System.Data.SQLite64Fx40", "http://x.newlifex.com");

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

        static void Test2()
        {
            XCode.Setting.Current.TransactionDebug = true;

            XTrace.WriteLine(Role.Meta.Count + "");
            XTrace.WriteLine(Log.Meta.Count + "");
            Console.Clear();

            Task.Run(() => TestTask(1));
            Thread.Sleep(1000);
            Task.Run(() => TestTask(2));
        }

        static void TestTask(Int32 tid)
        {
            try
            {
                XTrace.WriteLine("TestTask {0} Start", tid);
                using (var tran = Role.Meta.CreateTrans())
                {
                    var role = new Role();
                    role.Name = "R" + DateTime.Now.Millisecond;
                    role.Save();
                    XTrace.WriteLine("role.ID={0}", role.ID);

                    Thread.Sleep(3000);

                    role = new Role();
                    role.Name = "R" + DateTime.Now.Millisecond;
                    role.Save();
                    XTrace.WriteLine("role.ID={0}", role.ID);

                    Thread.Sleep(3000);

                    if (tid == 2) tran.Commit();
                }
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
            finally
            {
                XTrace.WriteLine("TestTask {0} End", tid);
            }
        }
    }
}