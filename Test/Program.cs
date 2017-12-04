using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using NewLife;
using NewLife.Agent;
using NewLife.Caching;
using NewLife.Collections;
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
            var user = UserX.FindByKey(1);
            Console.WriteLine(user.Logins);
            using (var tran = UserX.Meta.CreateTrans())
            {
                user.Logins++;
                user.Save();

                Console.WriteLine(user.Logins);

                throw new Exception("xxx");

                tran.Commit();
            }
        }

        static void Test2()
        {
            //Redis.Test();

            var rds = Redis.Create("127.0.0.1:6379", 5);
            rds.Bench();

            //var url = "http://www.baidu.com";

            //var client = new TinyHttpClient();
            //var rs = client.GetAsync(url).Result;
            //Console.WriteLine(rs);

            //var client = new NewLife.Http.HttpClient();
            //client.Remote = new NetUri(url);
            //client.Request.Url = new Uri(url);
            //client.Open();            
        }

        private static TimerX _timer;
        static void Test3()
        {
            //var db = DbFactory.GetDefault("Oracle".GetTypeEx());
            //var dal = DAL.Create("Oracle");
            //var dp = dal.Db.CreateParameter("name", new[] { DateTime.Now, DateTime.MinValue, DateTime.MaxValue });
            //dal.Session.Execute("xxxx", System.Data.CommandType.Text, dp);

            //if (_timer == null) _timer = new TimerX(s =>
            //{
            //    Console.WriteLine();
            //    XTrace.WriteLine("start");
            //    Parallel.For(0, 3, k =>
            //    {
            //        Thread.Sleep(300);
            //        XTrace.WriteLine("pfor {0}", k);
            //    });
            //    XTrace.WriteLine("end");
            //}, null, 1000, 5000);

            //var list = new LinkList<Int32>();
            //list.Add(123);
            //list.Add(456);
            //list.Add(789);

            //Console.WriteLine(list.Contains(456));
            //list.Remove(456);

            //foreach (var item in list)
            //{
            //    Console.WriteLine(item);
            //}

            //var pool = new Pool<TcpClient>();
            //pool.Log = XTrace.Log;
            //pool.Max = 4;
            //Task.Run(() =>
            //{
            //    var st = new Stack<TcpClient>();
            //    for (var i = 0; i < 4; i++)
            //    {
            //        st.Push(pool.Acquire(3000));
            //        Thread.Sleep(500);
            //    }
            //    Thread.Sleep(100);
            //    for (var i = 0; i < 4; i++)
            //    {
            //        pool.Release(st.Pop());
            //        Thread.Sleep(500);
            //    }
            //});
            //Task.Run(() =>
            //{
            //    Thread.Sleep(1900);
            //    var st = new Stack<TcpClient>();
            //    for (var i = 0; i < 4; i++)
            //    {
            //        st.Push(pool.Acquire(2000));
            //        Thread.Sleep(500);
            //    }
            //    Thread.Sleep(1000);
            //    for (var i = 0; i < 4; i++)
            //    {
            //        pool.Release(st.Pop());
            //        Thread.Sleep(500);
            //    }
            //});
            //Parallel.For(0, 2, k =>
            //{
            //    var st = new Stack<TcpClient>();
            //    for (var i = 0; i < 10; i++)
            //    {
            //        if (st.Count == 0 || Rand.Next(2) == 0)
            //            st.Push(pool.Acquire());
            //        else
            //            pool.Release(st.Pop());

            //        Thread.Sleep(Rand.Next(200, 3000));
            //    }
            //});
        }
    }
}