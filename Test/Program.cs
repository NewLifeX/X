using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using NewLife.Agent;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Remoting;
using NewLife.Threading;
using NewLife.Web;
using XCode;
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
                    Test2();
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
                break;
            }
        }

        private static Int32 ths = 0;
        static void Test1()
        {
            //Console.Title = "SQLite极速插入测试 之 天下无贼 v2.0 " + AssemblyX.Entry.Compile.ToFullString();

            ////Console.WriteLine(DateTime.Now.ToFullString());
            //Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;

            //if (ths <= 0)
            //{
            //    foreach (var item in ".".AsDirectory().GetAllFiles("*.db"))
            //    {
            //        item.Delete();
            //    }

            //    //var db = "Membership.db".GetFullPath();
            //    //if (File.Exists(db)) File.Delete(db);

            //    //Console.Write("请输入线程数（推荐1）：");
            //    //ths = Console.ReadLine().ToInt();
            //    //if (ths < 1) ths = 1;
            //    ths = 1;
            //}

            ////var set = XCode.Setting.Current;
            ////set.UserParameter = true;

            ////UserOnline.Meta.Modules.Modules.Clear();
            ////Shard.Meta.ConnName = "Membership";
            //var ds = new XCode.Common.DataSimulation<DemoEntity>
            //{
            //    Log = XTrace.Log,
            //    //ds.BatchSize = 10000;
            //    Threads = ths,
            //    UseSql = true
            //};
            //ds.Run(400000);
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
            var bm = new BaiduMap();
            var kv = bm.GetGeocoderAsync("华腾路555号").Result;
            Console.WriteLine("{0}, {1}", kv.Key, kv.Value);
        }

        class TestModule : EntityModule
        {
            protected override Boolean OnInit(Type entityType)
            {
                return entityType == typeof(UserX);
            }

            protected override Boolean OnValid(IEntity entity, Boolean isNew)
            {
                if (isNew)
                    XTrace.WriteLine("新增实体 " + entity.GetType().Name);
                else
                    XTrace.WriteLine("更新实体 " + entity.GetType().Name);

                return base.OnValid(entity, isNew);
            }

            protected override Boolean OnDelete(IEntity entity)
            {
                XTrace.WriteLine("删除实体 " + entity.GetType().Name);

                return base.OnDelete(entity);
            }

            public static void Test()
            {
                EntityModules.Global.Add<TestModule>();

                var user = new UserX
                {
                    Name = "Stone",
                    RoleID = 1
                };
                user.Save();

                user.Name = "大石头";
                user.Update();

                user.Delete();
            }
        }
    }
}