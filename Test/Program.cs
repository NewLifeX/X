using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Caching;
using NewLife.Log;
using NewLife.Net;
using NewLife.Remoting;
using NewLife.Security;
using NewLife.Serialization;
using XCode.Code;
using XCode.DataAccessLayer;
using XCode.Membership;
using XCode.Service;

namespace Test
{
    public class Program
    {
        private static void Main(String[] args)
        {
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

        static void Test1()
        {
            var total = UserX.Meta.Count;
            Console.WriteLine("总行数：{0:n0}", total);

            // 查询1000万次，不预热
            var count = 10_000_000;
            var sw = Stopwatch.StartNew();
            for (var i = 0; i < count; i++)
            {
                var user = UserX.FindByName("admin");
            }
            sw.Stop();

            var ms = sw.Elapsed.TotalMilliseconds;
            Console.WriteLine("查询[{0:n0}]次，耗时{1:n0}ms，速度{2:n0}qps", count, ms, count * 1000L / ms);
        }

        static void Test2()
        {
            UserX.Meta.Session.Dal.Db.ShowSQL = true;
            Log.Meta.Session.Dal.Db.ShowSQL = true;
            //var sb = new StringBuilder();
            //sb.Append("HelloWorld");
            //sb.Length--;
            //sb.Append("Stone");
            //Console.WriteLine(sb.ToString());

            //DAL.AddConnStr("Log", "Data Source=tcp://127.0.0.1/ORCL;User Id=scott;Password=tiger;UseParameter=true", null, "Oracle");
            //DAL.AddConnStr("Log", "Server=.;Port=3306;Database=Log;Uid=root;Pwd=root;", null, "MySql");
            //DAL.AddConnStr("Membership", "Server=.;Port=3306;Database=times;Uid=root;Pwd=Pass@word;TablePrefix=xx_", null, "MySql");
            //DAL.AddConnStr("Membership", @"Server=.\JSQL2008;User ID=sa;Password=sa;Database=Membership;", null, "sqlserver");
            //DAL.AddConnStr("Log", @"Server=.\JSQL2008;User ID=sa;Password=sa;Database=Log;", null, "sqlserver");

            var gs = UserX.FindAll(null, null, null, 0, 10);
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
                    Name = "Stone" + i,
                    DisplayName = "大石头" + i,
                    Logins = 1,
                    LastLogin = DateTime.Now,
                    RegisterTime = DateTime.Now
                };
                list.Add(entity);
                entity.SaveAsync();
                //entity.InsertOrUpdate();
            }
            //list.Save();

            var user = gs.FirstOrDefault();
            if (user != null)
            {
                user.Logins++;
                user.SaveAsync();
            }

            Thread.Sleep(3000);

            count = UserX.FindCount();
            Console.WriteLine("Count={0}", count);
            //gs = UserX.FindAll(null, null, null, 0, 10);

            //gs.Delete(true);
        }

        static void Test3()
        {
            if (Console.ReadLine() == "1")
            {
                var svr = new ApiServer(1234)
                //var svr = new ApiServer("http://*:1234")
                {
                    Log = XTrace.Log,
                    //EncoderLog = XTrace.Log,
                    StatPeriod = 10,
                };

                var ns = svr.EnsureCreate() as NetServer;
                ns.EnsureCreateServer();
                var ts = ns.Servers.FirstOrDefault(e => e is TcpServer);
                //ts.ProcessAsync = true;

                svr.Start();

                Console.ReadKey();
            }
            else
            {
                var client = new ApiClient("tcp://127.0.0.1:1234")
                {
                    Log = XTrace.Log,
                    //EncoderLog = XTrace.Log,
                    StatPeriod = 10,

                    UsePool = true,
                };
                client.Open();

                Task.Run(() =>
                {
                    var sw = Stopwatch.StartNew();
                    try
                    {
                        for (var i = 0; i < 10; i++)
                        {
                            client.InvokeAsync<Object>("Api/All", new { state = 111 }).Wait();
                        }
                    }
                    catch (Exception ex)
                    {
                        XTrace.WriteException(ex.GetTrue());
                    }
                    sw.Stop();
                    XTrace.WriteLine("总耗时 {0:n0}ms", sw.ElapsedMilliseconds);
                });

                Task.Run(() =>
                {
                    var sw = Stopwatch.StartNew();
                    try
                    {
                        for (var i = 0; i < 10; i++)
                        {
                            client.InvokeAsync<Object>("Api/All", new { state = 222 }).Wait();
                        }
                    }
                    catch (Exception ex)
                    {
                        XTrace.WriteException(ex.GetTrue());
                    }
                    sw.Stop();
                    XTrace.WriteLine("总耗时 {0:n0}ms", sw.ElapsedMilliseconds);
                });

                Task.Run(() =>
                {
                    var sw = Stopwatch.StartNew();
                    try
                    {
                        for (var i = 0; i < 10; i++)
                        {
                            client.InvokeAsync<Object>("Api/Info", new { state = 333 }).Wait();
                        }
                    }
                    catch (Exception ex)
                    {
                        XTrace.WriteException(ex.GetTrue());
                    }
                    sw.Stop();
                    XTrace.WriteLine("总耗时 {0:n0}ms", sw.ElapsedMilliseconds);
                });

                Task.Run(() =>
                {
                    var sw = Stopwatch.StartNew();
                    try
                    {
                        for (var i = 0; i < 10; i++)
                        {
                            client.InvokeAsync<Object>("Api/Info", new { state = 444 }).Wait();
                        }
                    }
                    catch (Exception ex)
                    {
                        XTrace.WriteException(ex.GetTrue());
                    }
                    sw.Stop();
                    XTrace.WriteLine("总耗时 {0:n0}ms", sw.ElapsedMilliseconds);
                });

                Console.ReadKey();
            }
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
            // 缓存默认实现Cache.Default是MemoryCache，可修改
            //var ic = Cache.Default;
            //var ic = new MemoryCache();

            // 实例化Redis，默认端口6379可以省略，密码有两种写法
            var ic = Redis.Create("127.0.0.1", 7);
            //var ic = Redis.Create("pass@127.0.0.1:6379", 7);
            //var ic = Redis.Create("server=127.0.0.1:6379;password=pass", 7);
            ic.Log = XTrace.Log; // 调试日志。正式使用时注释

            var user = new User { Name = "NewLife", CreateTime = DateTime.Now };
            ic.Set("user", user, 3600);
            var user2 = ic.Get<User>("user");
            XTrace.WriteLine("Json: {0}", ic.Get<String>("user"));
            if (ic.ContainsKey("user")) XTrace.WriteLine("存在！");
            ic.Remove("user");

            var dic = new Dictionary<String, Object>
            {
                ["name"] = "NewLife",
                ["time"] = DateTime.Now,
                ["count"] = 1234
            };
            ic.SetAll(dic, 120);

            var vs = ic.GetAll<String>(dic.Keys);
            XTrace.WriteLine(vs.Join(",", e => $"{e.Key}={e.Value}"));

            var flag = ic.Add("count", 5678);
            XTrace.WriteLine(flag ? "Add成功" : "Add失败");
            var ori = ic.Replace("count", 777);
            var count = ic.Get<Int32>("count");
            XTrace.WriteLine("count由{0}替换为{1}", ori, count);

            ic.Increment("count", 11);
            var count2 = ic.Decrement("count", 10);
            XTrace.WriteLine("count={0}", count2);

            //ic.Bench();
        }

        class User
        {
            public String Name { get; set; }
            public DateTime CreateTime { get; set; }
        }

        static void Test7()
        {
            Parameter.Meta.Session.Dal.Db.ShowSQL = true;

            var p = Parameter.FindByCategoryAndName("量化交易", "交易所");
            if (p == null) p = new Parameter
            {
                Category = "量化交易",
                Name = "交易所"
            };
            var dic = new Dictionary<Int32, String>
            {
                [1] = "上海交易所",
                [2] = "深圳交易所",
                [900] = "纽约交易所"
            };
            p.SetValue(dic);
            p.Save();

            var p2 = Parameter.FindByCategoryAndName("量化交易", "交易所");
            var dic2 = p2.GetHash<Int32, String>();
            foreach (var item in dic2)
            {
                Console.WriteLine("{0}={1}", item.Key, item.Value);
            }
            Console.WriteLine(p2.ToJson(true));
            p2.Delete();
        }

        static void Test8()
        {
            //XCode.Setting.Current.Debug = false;

            var dal = UserX.Meta.Session.Dal;
            var dt = UserX.Meta.Table.DataTable;
            dal.Db.ShowSQL = false;

            File.Delete("member3.db");
            dal.Sync(dt, "member3");

            dal.Backup(dt.TableName);

            File.Delete("member2.db");
            //DAL.AddConnStr("member2", "Server=.;Port=3306;Database=member2;Uid=root;Pwd=root;", null, "MySql");
            //DAL.AddConnStr("member2", "Server=.;Port=3306;Database=member2;Uid=root;Pwd=root;", null, "Oracle");
            var dal2 = DAL.Create("member2");
            dal2.Db.ShowSQL = false;
            dal2.Restore("user.table", dt);

            //dal.BackupAll(null, "backup", true);
            //dal2.RestoreAll("backup");
        }

        static async void Test9()
        {
            //var rds = new Redis();
            //rds.Server = "127.0.0.1";
            //if (rds.Pool is ObjectPool<RedisClient> pp) pp.Log = XTrace.Log;
            //rds.Bench();

            //Console.ReadKey();

            var svr = new ApiServer(3379)
            {
                Log = XTrace.Log
            };
            svr.Start();

            var client = new ApiClient("tcp://127.0.0.1:3379")
            {
                Log = XTrace.Log
            };
            client.Open();

            for (var i = 0; i < 10; i++)
            {
                XTrace.WriteLine("Invoke {0}", i);
                var sw = Stopwatch.StartNew();
                var rs = await client.InvokeAsync<String[]>("Api/All");
                sw.Stop();
                XTrace.WriteLine("{0}=> {1:n0}us", i, sw.Elapsed.TotalMilliseconds * 1000);
                //XTrace.WriteLine(rs.Join(","));
            }

            Console.WriteLine();
            Parallel.For(0, 10, async i =>
            {
                XTrace.WriteLine("Invoke {0}", i);
                var sw = Stopwatch.StartNew();
                var rs = await client.InvokeAsync<String[]>("Api/All");
                sw.Stop();
                XTrace.WriteLine("{0}=> {1:n0}us", i, sw.Elapsed.TotalMilliseconds * 1000);
                //XTrace.WriteLine(rs.Join(","));
            });
        }

        static void Test10()
        {
            var dt1 = new DateTime(1970, 1, 1);
            //var x = dt1.ToFileTimeUtc();

            var yy = Int64.Parse("-1540795502468");

            //var yy = "1540795502468".ToInt();
            Console.WriteLine(yy);

            var dt = 1540795502468.ToDateTime();
            var y = dt.ToUniversalTime();
            Console.WriteLine(dt1.ToLong());
        }

        static void Test11()
        {
            var xmlFile = Path.Combine(Directory.GetCurrentDirectory(), "../X/XCode/Model.xml");
            var output = Path.Combine(Directory.GetCurrentDirectory(), "../");
            EntityBuilder.Build(xmlFile, output);
        }

        /// <summary>测试序列化</summary>
        static void Test12()
        {
            var bdic = new Dictionary<String, Object>
            {
                { "x", "1" },
                { "y", "2" }
            };

            var flist = new List<foo>
            {
                new foo() { A = 3, B = "e", AList = new List<String>() { "E", "F", "G" }, ADic = bdic }
            };

            var dic = new Dictionary<String, Object>
            {
                { "x", "1" },
                { "y", "2" }
            };


            var entity = new foo()
            {
                A = 1,
                B = "2",
                C = DateTime.Now,
                AList = new List<String>() { "A", "B", "C" },
                BList = flist,
                CList = new List<String>() { "A1", "B1", "C1" },
                ADic = dic,
                BDic = bdic
            };

            var json = entity.ToJson();

            var fentity = json.ToJsonEntity(typeof(foo));
        }
    }

    class foo
    {
        public Int32 A { get; set; }

        public String B { get; set; }

        public DateTime C { get; set; }

        public IList<String> AList { get; set; }

        public IList<foo> BList { get; set; }

        public List<String> CList { get; set; }

        public Dictionary<String, Object> ADic { get; set; }

        public IDictionary<String, Object> BDic { get; set; }
    }
}