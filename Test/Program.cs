using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Caching;
using NewLife.Data;
using NewLife.Log;
using NewLife.Remoting;
using NewLife.Security;
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
                    Test9();
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
            var dal = UserX.Meta.Session.Dal;
            dal.Db.DataCache = 3;

            var list = UserX.FindAll();
            var u = UserX.FindByID(1);
            var n = UserX.FindCount();

            var sql = "select * from user";
            var ds = dal.Select(sql);
            ds = dal.Select(sql, CommandType.Text);
            ds = dal.Select(sql, CommandType.Text, new Dictionary<String, Object>());
            var dt = dal.Query(sql, new Dictionary<String, Object>());
            n = dal.SelectCount(sql, CommandType.Text);

            var sb = SelectBuilder.Create("select roleid,count(*) from user group by roleid order by count(*) desc");
            ds = dal.Select(sb, 3, 5);
            dt = dal.Query(sb, 4, 6);
            n = dal.SelectCount(sb);

            for (var i = 0; i < 10; i++)
            {
                Console.WriteLine(i);

                list = UserX.FindAll();
                u = UserX.FindByKey(1);
                n = UserX.FindCount();

                ds = dal.Select(sql);
                ds = dal.Select(sql, CommandType.Text);
                ds = dal.Select(sql, CommandType.Text, new Dictionary<String, Object>());
                dt = dal.Query(sql, new Dictionary<String, Object>());
                n = dal.SelectCount(sql, CommandType.Text);

                ds = dal.Select(sb, 3, 5);
                dt = dal.Query(sb, 4, 6);
                n = dal.SelectCount(sb);

                Thread.Sleep(1000);
            }
        }

        static void Test8()
        {
            var user = new UserX();
            for (var i = 0; i < 1_000_000; i++)
            {
                user.RoleID++;

                if (i % 3 == 0) user.Logins++;
            }

            Console.WriteLine("总量：{0:n0} 成功：{1:n0} 成功率：{2:p2}", user.RoleID, user.Logins, (Double)user.Logins / user.RoleID);

            user.RoleID = 0;
            user.Logins = 0;
            Parallel.For(0, 1_000_000, k =>
            {
                user.RoleID++;

                if (k % 3 == 0) user.Logins++;
            });

            Console.WriteLine("总量：{0:n0} 成功：{1:n0} 成功率：{2:p2}", user.RoleID, user.Logins, (Double)user.Logins / user.RoleID);
        }

        static void Test9()
        {
            var str = @"D:\资料\1810\".AsDirectory().GetAllFiles("*.csv").FirstOrDefault()?.FullName;

            var csv = new CsvFile(str);
            var header = csv.ReadLine();
            var data = csv.ReadAll();
            Console.WriteLine(data);

            str = Path.GetDirectoryName(str).CombinePath("test.csv");
            var csv2 = new CsvFile(str, true);
            csv2.WriteLine(header);
            csv2.WriteAll(data);
            csv2.Dispose();
        }
    }
}