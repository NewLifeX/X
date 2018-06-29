using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using NewLife.Caching;
using NewLife.Log;
using NewLife.Net;
using NewLife.Net.Handlers;
using NewLife.Reflection;
using NewLife.Remoting;
using NewLife.Security;
using NewLife.Serialization;
using NewLife.Threading;
using XCode.DataAccessLayer;
using XCode.Membership;
using XCode.Service;

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

        //private static Int32 ths = 0;
        static void Test1()
        {
            XTrace.WriteLine("线程池分配测试");

            ThreadPool.GetMinThreads(out var min, out var min2);
            ThreadPool.GetMaxThreads(out var max, out var max2);
            ThreadPool.GetAvailableThreads(out var ths, out var ths2);
            XTrace.WriteLine("({0}, {1}) ({2}, {3}) ({4}, {5})", min, min2, max, max2, ths, ths2);

            //ThreadPoolX.Instance.Pool.Log = XTrace.Log;
            var cpu = Environment.ProcessorCount;
            cpu += 5;
            for (var i = 0; i < cpu; i++)
            {
                var idx = i;
                ThreadPoolX.QueueUserWorkItem(() =>
                {
                    XTrace.WriteLine("Item {0} Start", idx);
                    Thread.Sleep(Rand.Next(100, 5000));
                    XTrace.WriteLine("Item {0} End", idx);
                });
            }

            Thread.Sleep(3000);
            ThreadPool.GetAvailableThreads(out ths, out ths2);
            XTrace.WriteLine("({0}, {1}) ({2}, {3}) ({4}, {5})", min, min2, max, max2, ths, ths2);

            for (var i = 0; i < cpu; i++)
            {
                var idx = i;
                ThreadPoolX.QueueUserWorkItem(() =>
                {
                    XTrace.WriteLine("Item {0} Start", idx);
                    Thread.Sleep(Rand.Next(100, 5000));
                    XTrace.WriteLine("Item {0} End", idx);
                });
            }
            //Thread.Sleep(7000);

            //ThreadPool.GetAvailableThreads(out ths, out ths2);
            //XTrace.WriteLine("({0}, {1}) ({2}, {3}) ({4}, {5})", min, min2, max, max2, ths, ths2);

            //var p = ThreadPoolX.Instance.Pool;
            //for (var i = 0; i < 150; i++)
            //{
            //    Console.WriteLine("ThreadPoolX FreeCount={0} BusyCount={1}", p.FreeCount, p.BusyCount);
            //    Thread.Sleep(1000);
            //}
        }

        static void Test2()
        {
            var file = "web.config".GetFullPath();
            if (!File.Exists(file)) file = "{0}.config".F(AppDomain.CurrentDomain.FriendlyName).GetFullPath();

            // 读取配置文件
            var doc = new XmlDocument();
            doc.Load(file);
            var nodes = doc.SelectNodes("/configuration/connectionStrings/add");
            foreach (XmlNode item in nodes)
            {
                var name = item.Attributes["name"]?.Value;
                var connstr = item.Attributes["connectionString"]?.Value;
                var provider = item.Attributes["providerName"]?.Value;

                Console.WriteLine($"name={name} connstr={connstr} provider={provider}");
            }
        }

        static void Test3()
        {
            //var list = Role.FindAllWithCache();
            //Console.WriteLine(list.Count);

            //var log = TextFileLog.Create(".", "{0:yyyy_MM_dd_HHmm}.log");

            ////ThreadPoolX.Instance.Pool.Log = XTrace.Log;

            //for (int i = 0; i < 10000; i++)
            //{
            //    //XTrace.WriteLine("T{0:n0}", i);
            //    log.Info("T {0:n0}", i);

            //    var n = Rand.Next(300, 3000);
            //    Thread.Sleep(n);
            //}

            Log.Meta.Session.Dal.Db.ShowSQL = true;
            for (var i = 0; i < 1000; i++)
            {
                LogProvider.Provider.WriteLog("aa", "bb", "cc");

                Thread.Sleep(1000);
            }
        }

        static void Test4()
        {
            //ApiTest.Main();

            var key = "xxx";
            var v = Rand.NextBytes(32);
            Console.WriteLine(v.ToBase64());

            ICache ch = null;
            //ICache ch = new DbCache();
            //ch.Set(key, v);
            //v = ch.Get<Byte[]>(key);
            //Console.WriteLine(v.ToBase64());
            //ch.Remove(key);

            Console.Clear();

            Console.Write("选择要测试的缓存：1，MemoryCache；2，DbCache；3，Redis ");
            var select = Console.ReadKey().KeyChar;
            switch (select)
            {
                case '1':
                    ch = new MemoryCache();
                    break;
                case '2':
                    ch = new DbCache();
                    break;
                case '3':
                    ch = Redis.Create("127.0.0.1", 9);
                    break;
            }

            var mode = false;
            Console.WriteLine();
            Console.Write("选择测试模式：1，顺序；2，随机 ");
            if (Console.ReadKey().KeyChar != '1') mode = true;

            Console.Clear();

            ch.Bench(mode);
        }

        static void Test5()
        {
            var set = XCode.Setting.Current;
            set.Debug = true;
            set.ShowSQL = true;

            Console.WriteLine("1，服务端；2，客户端");
            if (Console.ReadKey().KeyChar == '1')
            {
                var n = UserOnline.Meta.Count;

                var svr = new DbServer();
                svr.Log = XTrace.Log;
                svr.StatPeriod = 5;
                svr.Start();
            }
            else
            {
                DAL.AddConnStr("net", "Server=tcp://admin:newlife@127.0.0.1:3305/Log", null, "network");
                var dal = DAL.Create("net");

                UserOnline.Meta.ConnName = "net";

                var count = UserOnline.Meta.Count;
                Console.WriteLine("count={0}", count);

                var entity = new UserOnline();
                entity.Name = "新生命";
                entity.OnlineTime = 12345;
                entity.Insert();

                Console.WriteLine("id={0}", entity.ID);

                var entity2 = UserOnline.FindByKey(entity.ID);
                Console.WriteLine("user={0}", entity2);

                entity2.Page = Rand.NextString(8);
                entity2.Update();

                entity2.Delete();

                for (var i = 0; i < 100; i++)
                {
                    entity2 = new UserOnline();
                    entity2.Name = Rand.NextString(8);
                    entity2.Page = Rand.NextString(8);
                    entity2.Insert();

                    Thread.Sleep(5000);
                }
            }

            //var client = new DbClient();
            //client.Log = XTrace.Log;
            //client.EncoderLog = client.Log;
            //client.StatPeriod = 5;

            //client.Servers.Add("tcp://127.0.0.1:3305");
            //client.Open();

            //var db = "Membership";
            //var rs = client.LoginAsync(db, "admin", "newlife").Result;
            //Console.WriteLine((DatabaseType)rs["DbType"].ToInt());

            //var ds = client.QueryAsync("Select * from User").Result;
            //Console.WriteLine(ds);

            //var count = client.QueryCountAsync("User").Result;
            //Console.WriteLine("count={0}", count);

            //var ps = new Dictionary<String, Object>
            //{
            //    { "Logins", 3 },
            //    { "id", 1 }
            //};
            //var es = client.ExecuteAsync("update user set Logins=Logins+@Logins where id=@id", ps).Result;
            //Console.WriteLine("Execute={0}", es);
        }

        static void Test6()
        {
            var list = UserX.FindAll();
        }

        static void Test7()
        {
            //new UserOnline()
            //{
            //    Name = "Test",
            //}.Save();
            var list = UserOnline.FindAll("select * from UserOnline");
            var count = UserOnline.FindCount("select * from UserOnline");
            Console.WriteLine(list.Count + "  " + count);

            var dataset = UserOnline.Meta.Session.Query("select * from UserOnline");

            //var n = UserX.Meta.Count;
            //Console.WriteLine(n);
        }
    }
}