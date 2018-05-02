using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Caching;
using NewLife.Data;
using NewLife.Log;
using NewLife.Net;
using NewLife.Net.Application;
using NewLife.Net.Handlers;
using NewLife.Remoting;
using NewLife.Security;
using NewLife.Serialization;
using NewLife.Web;
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
                var sw = Stopwatch.StartNew();
#if !DEBUG
                try
                {
#endif
                Test4();
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
            //var orc = ObjectContainer.Current.ResolveInstance<IDatabase>(DatabaseType.Oracle);
            var db = DbFactory.Create(DatabaseType.Oracle);
            var sql = "select * from table where date>1234 ";
            var sb = new SelectBuilder();
            sb.Parse(sql);

            Console.WriteLine(db.PageSplit(sb, 0, 20));
            Console.WriteLine(db.PageSplit(sb, 20, 0));
            Console.WriteLine(db.PageSplit(sb, 20, 30));

            sql = "select * from table where date>1234 order by cc";
            sb = new SelectBuilder();
            sb.Parse(sql);

            Console.WriteLine(db.PageSplit(sb, 0, 20));
            Console.WriteLine(db.PageSplit(sb, 20, 0));
            Console.WriteLine(db.PageSplit(sb, 20, 30));

            //EntityBuilder.Build("DataCockpit.xml");

            //Role.Meta.Session.Dal.Db.Readonly = true;
            //Role.GetOrAdd("sss");

            var ip = NetHelper.MyIP();
            Console.WriteLine(ip);
        }

        static void Test2()
        {
            using (var mmf = MemoryMappedFile.CreateFromFile("mmf.db", FileMode.OpenOrCreate, "mmf", 1 << 10))
            {
                var ms = mmf.CreateViewStream(8, 64);
                var str = ms.ReadArray().ToStr();
                XTrace.WriteLine(str);

                str = "学无先后达者为师 " + DateTime.Now;
                ms.Position = 0;
                ms.WriteArray(str.GetBytes());
                //ms.Flush();

                //ms.Position = 0;
                //str = ms.ReadArray().ToStr();
                //Console.WriteLine(str);
            }
        }

        //private static TimerX _timer;
        static void Test3()
        {
            var rds = Redis.Create(null, 0);
            rds.Log = XTrace.Log;
            //rds.Set("123", 456);
            //rds.Set("abc", "def");
            //var rs = rds.Remove("123", "abc");
            //Console.WriteLine(rs);

            var queue = rds.GetQueue<String>("q");
            //var queue = Cache.Default.GetQueue<String>("q");

            Console.WriteLine("入队：");
            var ps = new List<String>();
            for (var i = 0; i < 5; i++)
            {
                var str = Rand.NextString(6);
                ps.Add(str);
                Console.WriteLine(str);
            }
            queue.Add(ps);

            Console.WriteLine();
            Console.WriteLine("出队：");
            var bs = queue.Take(5);
            foreach (var item in bs)
            {
                Console.WriteLine(item);
            }
        }

        static void Test4()
        {
            //ApiTest.Main();

            //var ccdc = new CounterCreationDataCollection();
            //var ccd = new CounterCreationData
            //{
            //    CounterName = "示例",
            //    CounterType = PerformanceCounterType.NumberOfItems32
            //};
            //ccdc.Add(ccd);

            //PerformanceCounterCategory.Create("新生命", "新生命项目性能测试示例", PerformanceCounterCategoryType.MultiInstance, ccdc);

            Task.Run(() => Test6());

            //var pcc = new PerformanceCounterCategory(".NET CLR Memory");
            var p = Process.GetCurrentProcess();
            //var instance2 = GetInstanceName(".NET CLR Memory", "Process ID", p);
            //var pc = new PerformanceCounter(".NET CLR Memory", "% Time in GC", instance2);
            var pc = new PerformanceCounter("新生命", "示例", p.Id + "");
            //Console.WriteLine(pc);
            for (var i = 0; i < 1000; i++)
            {
                Console.Title = $"GC={pc.RawValue:n0}";
                Thread.Sleep(1000);
            }
        }

        static void Test6()
        {
            // 初始化计数器实例
            var counter1 = new PerformanceCounter
            {
                CategoryName = "新生命",
                CounterName = "示例",
                InstanceName = Process.GetCurrentProcess().Id + "",
                InstanceLifetime = PerformanceCounterInstanceLifetime.Process,
                ReadOnly = false,
                RawValue = 0
            };

            var rnd = new Random();
            while (true)
            {
                //counter1.IncrementBy(rnd.Next(100));
                counter1.RawValue = rnd.Next(100);

                Thread.Sleep(1000);
            }
        }

        static String GetInstanceName(String categoryName, String counterName, Process p)
        {
            var processcounter = new PerformanceCounterCategory(categoryName);
            var instances = processcounter.GetInstanceNames();
            foreach (var instance in instances)
            {
                var counter = new PerformanceCounter(categoryName, counterName, instance);
                if (counter.NextValue() == p.Id) return instance;
            }

            return null;
        }

        static async void Test5()
        {
            Console.WriteLine("服务端1，客户端2：");
            if (Console.ReadKey().KeyChar == '1')
            {
                var svr = new NetServer(777);
#if DEBUG
                svr.Log = XTrace.Log; svr.LogSend = true; svr.LogReceive = true;
#endif
                //svr.Add<DefaultCodec>();
                svr.Add(new LengthFieldCodec { Size = 4 });
                //svr.Add<BinaryCodec<UserY>>();
                svr.Add<JsonCodec<UserY>>();
                svr.Add<EchoHandler>();
                svr.Start();
            }
            else
            {
                var client = new NetUri("tcp://127.0.0.1:777").CreateRemote();
#if DEBUG
                client.Log = XTrace.Log; client.LogSend = true; client.LogReceive = true;
#endif
                //client.Add<DefaultCodec>();
                client.Add(new LengthFieldCodec { Size = 4 });
                //client.Add<BinaryCodec<UserY>>();
                client.Add<JsonCodec<UserY>>();
                client.Open();

                //client.Send("Stone");
                var user = new UserY { ID = 0x1234, Name = "Stone", DisplayName = "大石头" };
                for (var i = 0; i < 3; i++)
                {
                    var rs = await client.SendAsync(user) as UserY;
                    XTrace.WriteLine("{0} {1}", rs.Name, rs.DisplayName);
                }
            }
        }
        class UserY
        {
            public Int32 ID { get; set; }
            public String Name { get; set; }
            public String DisplayName { get; set; }
        }
    }
}