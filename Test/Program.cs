using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;
using NewLife.Caching;
using NewLife.Log;
using NewLife.Net;
using NewLife.Net.Handlers;
using NewLife.Reflection;
using NewLife.Remoting;
using NewLife.Security;
using NewLife.Serialization;
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
                    Test5();
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
            var sw = Stopwatch.StartNew();
            Thread.Sleep(3000);
            sw.Stop();
            Console.WriteLine("ElapsedTicks=\t{0:n0}", sw.ElapsedTicks);
            Console.WriteLine("Milliseconds=\t{0:n0}", sw.ElapsedMilliseconds);
            Console.WriteLine("比率=\t{0:n0}", sw.ElapsedTicks / sw.ElapsedMilliseconds);

            var tickFrequency = sw.GetType().GetValue("tickFrequency");
            var Frequency = (Int64)sw.GetType().GetValue("Frequency");
            Console.WriteLine("tickFrequency=\t{0:n0}", tickFrequency);
            Console.WriteLine("Frequency=\t{0:n0}", Frequency);

            var svr = new ApiServer(3344);
            svr.Log = XTrace.Log;
            svr.StatPeriod = 5;
            svr.Start();

            Console.ReadKey(true);

            //while (true)
            //{
            //    Thread.Sleep(500);
            //    Console.Title = svr.GetStat();
            //}

            //var client = new ApiClient("tcp://127.0.0.1:7788,udp://127.0.0.1:7788,tcp://127.0.0.1:7788");
            //client.Log = XTrace.Log;
            //client.EncoderLog = client.Log;
            //client.StatPeriod = 5;
            //client.Open();

            //while (true)
            //{
            //    client.InvokeAsync<String[]>("Api/All").Wait();
            //    Thread.Sleep(3000);
            //}
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