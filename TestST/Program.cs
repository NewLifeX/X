using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NewLife.Log;
using NewLife.Net;
using XCode.DataAccessLayer;
using XCode.Membership;

namespace TestST
{
    class Program
    {
        static void Main(String[] args)
        {
            XTrace.UseConsole();

            var sw = Stopwatch.StartNew();

            Test4();

            sw.Stop();
            Console.WriteLine("OK! {0:n0}ms", sw.ElapsedMilliseconds);

            Console.ReadKey();
        }

        static void Test1()
        {
            XTrace.WriteLine("学无先后达者为师！");
            Console.WriteLine(".".GetFullPath());

            var svr = new NetServer();
            svr.Port = 8080;
            svr.Received += Svr_Received;
            svr.Log = XTrace.Log;
            svr.SessionLog = svr.Log;
            svr.LogReceive = true;
            svr.Start();

            Console.ReadKey();
        }

        private static void Svr_Received(Object sender, ReceivedEventArgs e)
        {
            XTrace.WriteLine(e.ToStr());
        }

        static void Test2()
        {
            var cs = DAL.ConnStrs;
            foreach (var item in cs)
            {
                Console.WriteLine("{0}={1}", item.Key, item.Value);
            }
        }

        static void Test3()
        {
            var os = "";
            var fr = "/etc/os-release";
            if (File.Exists(fr))
            {
                var dic = File.ReadAllText(fr).SplitAsDictionary("=", "\n", true);
                os = dic["PRETTY_NAME"];
                XTrace.WriteLine(os);

                Console.WriteLine();
                foreach (var item in dic)
                {
                    Console.WriteLine("{0}\t={1}", item.Key, item.Value);
                }
            }

        }

        static void Test4()
        {
            //var list = Role.FindAll();
            //Console.WriteLine(list.Count);

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
    }
}