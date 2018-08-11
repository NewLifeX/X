using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using NewLife.Caching;
using NewLife.Collections;
using NewLife.Common;
using NewLife.Log;
using NewLife.Net;
using NewLife.Net.Handlers;
using NewLife.Reflection;
using NewLife.Remoting;
using NewLife.Security;
using NewLife.Serialization;
using NewLife.Threading;
using NewLife.Xml;
using XCode;
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

        private static Int32 _count = 0;
        static void Test1()
        {
            var cpu = Environment.ProcessorCount;

            var ts = new List<Task>();
            for (var i = 0; i < 15; i++)
            {
                var t = TaskEx.Run(() =>
                {
                    XTrace.WriteLine("begin");
                    Thread.Sleep(2000);
                    XTrace.WriteLine("end");
                });
                ts.Add(t);
            }

            Task.WaitAll(ts.ToArray());

            Console.WriteLine();
            ts.Clear();
            for (var i = 0; i < 15; i++)
            {
                //var t = Task.Run(() =>
                //{
                //    XTrace.WriteLine("begin");
                //    Thread.Sleep(2000);
                //    XTrace.WriteLine("end");
                //});
                //ts.Add(t);
            }

            Task.WaitAll(ts.ToArray());
        }

        static void Test2()
        {
            //DAL.AddConnStr("Log", "Data Source=tcp://127.0.0.1/ORCL;User Id=scott;Password=tiger;UseParameter=true", null, "Oracle");
            DAL.AddConnStr("Log", "Server=ds6.ztbi.vip;Port=3306;Database=Log;Uid=test;Pwd=Pass@word;", null, "MySql");

            var gs = Log.FindAll(null, null, null, 0, 10);
            var count = Log.FindCount();
            Console.WriteLine("Count={0}", count);

            //LogProvider.Provider.WriteLog("test", "新增", "学无先后达者为师");
            var list = new List<Log>();
            for (var i = 0; i < 10; i++)
            {
                var log = new Log
                {
                    Category = "test",
                    Action = "测试",
                    Remark = "内容" + i,
                    //CreateTime = DateTime.Now
                };
                list.Add(log);
            }
            list.Insert(true);

            count = Log.FindCount();
            Console.WriteLine("Count={0}", count);
        }

        class CacheItem<TValue>
        {
            /// <summary>数值</summary>
            public TValue Value { get; set; }

            /// <summary>过期时间</summary>
            public DateTime ExpiredTime { get; set; }

            /// <summary>是否过期</summary>
            public Boolean Expired => ExpiredTime <= TimerX.Now;

            public CacheItem(TValue value, Int32 seconds)
            {
                Value = value;
                if (seconds > 0) ExpiredTime = TimerX.Now.AddSeconds(seconds);
            }
        }

        static void Test3()
        {
            var svr = new ApiServer(3344);
            svr.Log = XTrace.Log;
            svr.EncoderLog = XTrace.Log;
            svr.StatPeriod = 5;
            svr.Start();

            Console.ReadKey(true);
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