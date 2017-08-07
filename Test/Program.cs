using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Caching;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Security;
using NewLife.Serialization;
using NewLife.Threading;
using NewLife.Web;
using NewLife.Xml;
using XCode.Code;
using XCode.DataAccessLayer;
using XCode.Membership;

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
                Test3();
#if !DEBUG
                }
                catch (Exception ex)
                {
                    XTrace.WriteException(ex?.GetTrue());
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
            Console.Title = "SQLite极速插入测试 之 天下无贼 v2.0 " + AssemblyX.Entry.Compile.ToFullString();

            //Console.WriteLine(DateTime.Now.ToFullString());
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;

            if (ths <= 0)
            {
                var db = "Membership.db".GetFullPath();
                if (File.Exists(db)) File.Delete(db);

                Console.Write("请输入线程数（推荐1）：");
                ths = Console.ReadLine().ToInt();
                if (ths < 1) ths = 1;
            }

            var ds = new XCode.Common.DataSimulation<UserOnline>();
            ds.Log = XTrace.Log;
            //ds.BatchSize = 10000;
            ds.Threads = ths;
            ds.UseSql = true;
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
            // 多线程测试缓存
            var mc = Cache.Default;
            var total = 0;
            var success = 0;
            for (int i = 0; i < 8; i++)
            {
                Task.Run(() =>
                {
                    for (int k = 0; k < 1000; k++)
                    {
                        var key = Rand.Next(0, 10) + "";
                        var value = Rand.NextString(8);

                        Interlocked.Increment(ref total);
                        if (mc.Contains(key)) Interlocked.Increment(ref success);

                        mc.Add(key, value);

                        Thread.Sleep(10);
                    }
                });
            }

            for (int i = 0; i < 1000; i++)
            {
                Thread.Sleep(500);

                Console.Title = "次数{0:n0} 成功{1:n0} 成功率{2:p} ".F(total, success, total == 0 ? 0 : (Double)success / total);
            }
        }

        static void Test3()
        {
            //var table = UserX.Meta.Table.DataTable;
            //var builder = new ClassBuilder
            //{
            //    Table = table,
            //};

            //builder.Execute();

            //Console.WriteLine();
            //Console.WriteLine(builder.ToString());
            //builder.Save(".Biz.cs");

            //Console.WriteLine();
            //Console.WriteLine();
            //builder.Pure = true;
            //builder.Namespace = "xxx.Entities";

            //builder.Execute();
            //Console.WriteLine(builder.ToString());
            //builder.Save();

            //Console.WriteLine();
            //Console.WriteLine();
            //builder.Interface = true;

            //builder.Execute();
            //Console.WriteLine(builder.ToString());
            //builder.Save();

            //var b = new EntityBuilder();
            //b.Table = Shard.Meta.Table.DataTable;
            //b.ConnName = Shard.Meta.ConnName;
            //b.Namespace = Shard.Meta.ThisType.Namespace;
            //b.GenericType = true;
            //b.BaseClass = "LogEntity";
            //b.Execute();
            //b.Save();

            //b.Business = true;
            //b.Execute();
            //b.Save(".Biz.cs", true);

            //var tables = DAL.Import(File.ReadAllText("..\\Src\\XCode\\Membership\\Member.xml".GetFullPath()));
            //foreach (var item in tables)
            //{
            //    var b = new EntityBuilder();
            //    b.Table = item;
            //    b.AllTables = tables;
            //    //b.GenericType = true;
            //    //b.BaseClass = "LogEntity";
            //    b.Execute();
            //    b.Output = "TE";
            //    b.Save();

            //    b.Business = true;
            //    b.Execute();
            //    b.Save(".Biz.cs", true);
            //}

            var count = EntityBuilder.Build("..\\Src\\XCode\\Membership\\Member.xml", null, "XCode.Membership");
            count += EntityBuilder.Build("..\\Src\\XCode\\Sharding\\Shard.xml", null, "XCode.Sharding", "Shard");
            Console.WriteLine(count);
        }
    }
}