using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Caching;
using NewLife.Log;
using NewLife.Remoting;
using NewLife.Security;
using NewLife.Serialization;
using NewLife.Threading;
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
                Test8();
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

        private static readonly Int32 _count = 0;
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
            var sb = new StringBuilder();
            sb.Append("HelloWorld");
            sb.Length--;
            sb.Append("Stone");
            Console.WriteLine(sb.ToString());

            //DAL.AddConnStr("Log", "Data Source=tcp://127.0.0.1/ORCL;User Id=scott;Password=tiger;UseParameter=true", null, "Oracle");
            //DAL.AddConnStr("Log", "Server=.;Port=3306;Database=times;Uid=root;Pwd=Pass@word;", null, "MySql");
            //DAL.AddConnStr("Membership", "Server=.;Port=3306;Database=times;Uid=root;Pwd=Pass@word;TablePrefix=xx_", null, "MySql");

            var gs = UserX.FindAll(null, null, null, 0, 10);
            Console.WriteLine(gs.First().Logins);
            var count = UserX.FindCount();
            Console.WriteLine("Count={0}", count);

            LogProvider.Provider.WriteLog("test", "新增", "学无先后达者为师");
            LogProvider.Provider.WriteLog("test", "新增", "学无先后达者为师");
            LogProvider.Provider.WriteLog("test", "新增", "学无先后达者为师");

            var list = new List<UserX>();
            for (var i = 0; i < 4; i++)
            {
                var entity = new UserX
                {
                    Name = "Stone",
                    DisplayName = "大石头",
                    Logins = 1,
                    LastLogin = DateTime.Now,
                    RegisterTime = DateTime.Now
                };
                list.Add(entity);
                entity.SaveAsync();
                //entity.InsertOrUpdate();
            }
            //list.Save();

            var user = gs.First();
            user.Logins++;
            user.SaveAsync();

            count = UserX.FindCount();
            Console.WriteLine("Count={0}", count);
            gs = UserX.FindAll(null, null, null, 0, 10);
            Console.WriteLine(gs.First().Logins);
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
            var svr = new ApiServer(3344)
            {
                Log = XTrace.Log,
                EncoderLog = XTrace.Log,
                StatPeriod = 5
            };
            svr.Start();

            Console.ReadKey(true);
        }

        static void Test4()
        {
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

                var svr = new DbServer
                {
                    Log = XTrace.Log,
                    StatPeriod = 5
                };
                svr.Start();
            }
            else
            {
                DAL.AddConnStr("net", "Server=tcp://admin:newlife@127.0.0.1:3305/Log", null, "network");
                var dal = DAL.Create("net");

                UserOnline.Meta.ConnName = "net";

                var count = UserOnline.Meta.Count;
                Console.WriteLine("count={0}", count);

                var entity = new UserOnline
                {
                    Name = "新生命",
                    OnlineTime = 12345
                };
                entity.Insert();

                Console.WriteLine("id={0}", entity.ID);

                var entity2 = UserOnline.FindByKey(entity.ID);
                Console.WriteLine("user={0}", entity2);

                entity2.Page = Rand.NextString(8);
                entity2.Update();

                entity2.Delete();

                for (var i = 0; i < 100; i++)
                {
                    entity2 = new UserOnline
                    {
                        Name = Rand.NextString(8),
                        Page = Rand.NextString(8)
                    };
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

        static void Test8()
        {
            var t = new TestTable()
            {
                IsHide = false,
                Title = "test abc",
                Content = "abc",
                Counts = 0,
                Level = 1,
                PageSize = 10,
                PId = 0,
                Price = 25.85M,
                Rank = 999,
                TitleColor = "#000"
            };
            t.Save();

            Console.WriteLine($"id:{t.Id}, title:{t.Title}, price:{t.Price}, ishide:{t.IsHide}");
            Console.WriteLine(t.ToJson(true));
        }
    }
}