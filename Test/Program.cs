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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Agent;
using NewLife.Common;
using NewLife.Data;
using NewLife.Expressions;
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
        private static void Main(String[] args)
        {
            //Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;

            //XTrace.Log = new NetworkLog();
            XTrace.UseConsole();
#if DEBUG
            XTrace.Debug = true;
#endif
            while (true)
            {
                var sw = new Stopwatch();
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
                GC.WaitForPendingFinalizers();
                var key = Console.ReadKey(true);
                if (key.Key != ConsoleKey.C) break;
            }
        }

        private static Int32 ths = 0;
        static void Test1()
        {
            Console.Title = "SQLite极速插入测试 之 一夜无眠 v1.4 " + AssemblyX.Entry.Compile.ToFullString();

            //Console.WriteLine(DateTime.Now.ToFullString());
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;

            if (ths <= 0)
            {
                var db = "Membership.db".GetFullPath();
                if (File.Exists(db)) File.Delete(db);

                Console.Write("请输入线程数：");
                ths = Console.ReadLine().ToInt();
                if (ths < 1) ths = 1;
            }

            var ds = new XCode.Common.DataSimulation<UserOnline>();
            ds.Log = XTrace.Log;
            //ds.BatchSize = 10000;
            ds.Threads = ths;
            ds.Run(100000);
        }

        class A
        {
            public String Name { get; set; }
            public DateTime Time { get; set; }
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

        static void Test3()
        {
            //var d = ".".GetFullPath();
            //d.AsDirectory().Compress(d.AsDirectory().Name + ".7z");

            //"cfg.7z".AsFile().Extract("cfg");

            var d = "Data".GetFullPath();
            if (Directory.Exists(d)) Directory.Delete(d, true);

            var count = Rand.Next(10, 100);
            XTrace.WriteLine("正在生成{0}个大文件……", count);
            var total = 0;
            for (var i = 0; i < count; i++)
            {
                var sb = new StringBuilder();
                var lines = Rand.Next(100, 10000);
                for (var k = 0; k < lines; k++)
                {
                    if (Rand.Next(4) > 0)
                        sb.AppendLine("学无先后达者为师！");
                    else
                        sb.AppendLine(Rand.NextString(16));
                }
                var f = d.CombinePath(i + ".txt").EnsureDirectory();
                File.WriteAllText(f, sb.ToString());
                total += (Int32)f.AsFile().Length;
            }
            d.AsDirectory().Compress("Data.7z");

            var size = (Int32)"Data.7z".AsFile().Length;
            XTrace.WriteLine("压缩 {0:n0}Byte => {1:n0}Byte 压缩比 {2:p}", total, size, (Double)size / total);
        }
    }
}