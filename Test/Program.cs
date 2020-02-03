using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using NewLife;
using NewLife.Caching;
using NewLife.Log;
using NewLife.Net;
using NewLife.Reflection;
using NewLife.Remoting;
using NewLife.Security;
using NewLife.Serialization;
using XCode.Code;
using XCode.DataAccessLayer;
using XCode.Membership;
using XCode.Service;
using NewLife.Http;
#if !NET4
using TaskEx = System.Threading.Tasks.Task;
#endif

namespace Test
{
    public class Program
    {
        private static void Main(String[] args)
        {
            Environment.SetEnvironmentVariable("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", "1");

            MachineInfo.RegisterAsync(5_000);
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

        static void Test1()
        {
            XTrace.WriteLine("FullPath:{0}", ".".GetFullPath());
            XTrace.WriteLine("BasePath:{0}", ".".GetBasePath());

            var mi = new MachineInfo();
            mi.Init();

            foreach (var pi in mi.GetType().GetProperties())
            {
                XTrace.WriteLine("{0}:\t{1}", pi.Name, mi.GetValue(pi));
            }

            Console.WriteLine();

#if __CORE__
            foreach (var pi in typeof(RuntimeInformation).GetProperties())
            {
                XTrace.WriteLine("{0}:\t{1}", pi.Name, pi.GetValue(null));
            }
#endif

            //Console.WriteLine();

            //foreach (var pi in typeof(Environment).GetProperties())
            //{
            //    XTrace.WriteLine("{0}:\t{1}", pi.Name, pi.GetValue(null));
            //}

            mi = MachineInfo.Current;
            for (var i = 0; i < 100; i++)
            {
                XTrace.WriteLine("{0} {1} {2}", mi.CpuRate, mi.Temperature, (Double)mi.AvailableMemory / 1024 / 1024);
                Thread.Sleep(1000);
            }

            Console.ReadKey();
        }

        static async void Test2()
        {
            var count = Role.Meta.Count;

            var dal = Role.Meta.Session.Dal;
            var db = dal.Query("select * from role");
            var json = db.ToJson(true, false, true);
            XTrace.WriteLine(json);

            json = db.ToJson();
            XTrace.WriteLine(json);

            db = dal.Query("select id,name,enable 启用 from role");
            json = db.ToJson(true, false, true);
            XTrace.WriteLine(json);
        }

        static void Test3()
        {
            //XTrace.WriteLine("IsConsole={0}", Runtime.IsConsole);
            //Console.WriteLine("IsConsole={0}", Runtime.IsConsole);
            //XTrace.WriteLine("MainWindowHandle={0}", Process.GetCurrentProcess().MainWindowHandle);

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
                var client = new ApiClient("tcp://127.0.0.1:335,tcp://127.0.0.1:1234")
                {
                    Log = XTrace.Log,
                    //EncoderLog = XTrace.Log,
                    StatPeriod = 10,

                    UsePool = true,
                };
                client.Open();

                TaskEx.Run(() =>
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

                TaskEx.Run(() =>
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

                TaskEx.Run(() =>
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

                TaskEx.Run(() =>
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
                    var rds = new Redis("127.0.0.1", null, 9);
                    rds.Counter = new PerfCounter();
                    ch = rds;
                    break;
            }

            var mode = false;
            Console.WriteLine();
            Console.Write("选择测试模式：1，顺序；2，随机 ");
            if (Console.ReadKey().KeyChar != '1') mode = true;

            var batch = 0;
            Console.WriteLine();
            Console.Write("选择输入批大小[0]：");
            batch = Console.ReadLine().ToInt();

            Console.Clear();

            //var batch = 0;
            //if (mode) batch = 1000;

            var rs = ch.Bench(mode, batch);

            XTrace.WriteLine("总测试数据：{0:n0}", rs);
            if (ch is Redis rds2) XTrace.WriteLine(rds2.Counter + "");
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
            var pfx = new X509Certificate2("../newlife.pfx", "newlife");
            //Console.WriteLine(pfx);

            //using var svr = new ApiServer(1234);
            //svr.Log = XTrace.Log;
            //svr.EncoderLog = XTrace.Log;

            //var ns = svr.EnsureCreate() as NetServer;

            using var ns = new NetServer(1234)
            {
                Name = "Server",
                ProtocolType = NetType.Tcp,
                Log = XTrace.Log,
                SessionLog = XTrace.Log,
                SocketLog = XTrace.Log,
                LogReceive = true
            };

            ns.EnsureCreateServer();
            foreach (var item in ns.Servers)
            {
                if (item is TcpServer ts) ts.Certificate = pfx;
            }

            ns.Received += (s, e) =>
            {
                XTrace.WriteLine("收到：{0}", e.Packet.ToStr());
            };
            ns.Start();

            using var client = new TcpSession
            {
                Name = "Client",
                Remote = new NetUri("tcp://127.0.0.1:1234"),
                SslProtocol = SslProtocols.Tls,
                Log = XTrace.Log,
                LogSend = true
            };
            client.Open();

            client.Send("Stone");

            Console.ReadLine();
        }

        static void Test7()
        {
#if __CORE__
            XTrace.WriteLine(RuntimeInformation.OSDescription);
#endif

            //DAL.AddConnStr("membership", "Server=10.0.0.3;Port=3306;Database=Membership;Uid=root;Pwd=Pass@word;", null, "mysql");

            Role.Meta.Session.Dal.Db.ShowSQL = true;
            Role.Meta.Session.Dal.Expire = 10;
            //Role.Meta.Session.Dal.Db.Readonly = true;

            var list = Role.FindAll();
            Console.WriteLine(list.Count);

            list = Role.FindAll(Role._.Name.NotContains("abc"));
            Console.WriteLine(list.Count);

            Thread.Sleep(1000);

            list = Role.FindAll();
            Console.WriteLine(list.Count);

            Thread.Sleep(1000);

            var r = list.Last();
            r.IsSystem = !r.IsSystem;
            r.Update();

            Thread.Sleep(5000);

            list = Role.FindAll();
            Console.WriteLine(list.Count);
        }

        static async void Test8()
        {
            var url = "http://www.mca.gov.cn/article/sj/xzqh/2019/2019/201912251506.html";
            var file = "area.html".GetFullPath();
            if (!File.Exists(file))
            {
                var http = new HttpClient();
                await http.DownloadFileAsync(url, file);
            }

            var txt = File.ReadAllText(file);
            foreach (var item in Area.Parse(txt))
            {
                XTrace.WriteLine("{0} {1}", item.ID, item.Name);
            }
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