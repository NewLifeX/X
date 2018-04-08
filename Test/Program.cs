using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using NewLife;
using NewLife.Agent;
using NewLife.Caching;
using NewLife.Collections;
using NewLife.Common;
using NewLife.Data;
using NewLife.Http;
using NewLife.Log;
using NewLife.Net;
using NewLife.Reflection;
using NewLife.Remoting;
using NewLife.Security;
using NewLife.Serialization;
using NewLife.Threading;
using NewLife.Web;
using NewLife.Yun;
using XCode;
using XCode.Code;
using XCode.DataAccessLayer;
using XCode.Membership;
using XCode.Statistics;

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
                    Test1();
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
            for (int i = 0; i < 5; i++)
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
            var str = "~/Sso/Login";
            var uri2 = new Uri("Sso/Login", UriKind.Absolute);
            //var uri = str.AsUri("http://xxx.yyy.zzz/ss/dd/ff".AsUri());
            var uri = str.AsUri();
            //var cfg = CacheConfig.Current;
            //Console.WriteLine(cfg.GetOrAdd("Bill01"));

            //var set = cfg.GetOrAdd("aa_test", "redis");
            //Console.WriteLine(set);

            WebClientX.SetAllowUnsafeHeaderParsing(true);

            var url = "https://api.github.com/user?access_token=ccb5c1363318ee2fa1d9374e87961bdf01a4c682";

            var client = new WebClientX(true, true);
            //var buf = client.DownloadDataAsync(url).Result;
            //var ms = new MemoryStream(buf);
            //var ms2 = ms.DecompressGZip();
            //buf = ms2.ReadBytes();
            var html = client.GetHtml(url);
            Console.WriteLine(html);

            var ip = "223.5.5.5";
            ip = ip.IPToAddress();
            Console.WriteLine(ip);
        }
    }
}