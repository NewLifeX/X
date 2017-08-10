using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using NewLife.Caching;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Security;
using NewLife.Serialization;
using NewLife.Threading;
using NewLife.Web;
using NewLife.Xml;
using XCode;
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
            //EntityBuilder.Build(@"E:\ZTO\ZTO.GK.Web.Report\GK.Report\GK.Report.xml");

            Console.WriteLine(UserX.Meta.Count);
            Console.Clear();
            TestModule.Test();
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

                var user = new UserX();
                user.Name = "Stone";
                user.RoleID = 1;
                user.Save();

                user.Name = "大石头";
                user.Update();

                user.Delete();
            }
        }

        static void Test3()
        {
            RedisSetting._.Debug = true;

            var set = RedisSetting.Current;
            if (set.Items.Count == 0 || set.Items.All(e => e.Name.IsNullOrEmpty()))
            {
                set.Items.Add(new RedisSetting.Item { Name = "aaa", Url = "bbb" });
                set.Items.Add(new RedisSetting.Item { Name = "xxx", Url = "yyy" });
            }
            set.Save();
        }
    }

    /// <summary>Redis配置</summary>
    [Description("Redis配置")]
    [XmlConfigFile("Config/Redis.config", 15000)]
    public class RedisSetting : XmlConfig<RedisSetting>
    {
        #region 属性
        /// <summary>调试开关。默认true</summary>
        [Description("调试开关。默认true")]
        public Boolean Debug { get; set; } = true;

        /// <summary>配置项</summary>
        [Description("配置项")]
        public List<Item> Items { get; set; } = new List<Item>();
        #endregion

        /// <summary>配置项</summary>
        public class Item
        {
            [XmlAttribute]
            public String Name { get; set; }
            [XmlAttribute]
            public String Url { get; set; }
        }
    }
}