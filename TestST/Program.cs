using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using NewLife.Agent;
using NewLife.Log;
using NewLife.Net;
using NewLife.Remoting;
using NewLife.Serialization;
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

            Test2();

            sw.Stop();
            Console.WriteLine("OK! {0:n0}ms", sw.ElapsedMilliseconds);

            //Console.ReadKey();
            Thread.Sleep(-1);
        }

        private static ApiServer _Server;
        static void Test2()
        {
            //new AgentService().Main();
            //"圣诞快乐".SpeakAsync();

            var svr = new ApiServer(1234);
            svr.Log = XTrace.Log;
            svr.EncoderLog = XTrace.Log;
            svr.StatPeriod = 10;
            svr.Start();

            _Server = svr;
        }

        static void Test3()
        {
            var user = new UserX
            {
                ID = 1234,
                Name = "Stone",
                DisplayName = "大石头",
                RegisterTime = DateTime.Now,
                LastLogin = DateTime.Now,
            };

            var js = user.ToJson(true);
            Console.WriteLine(js);
            Console.WriteLine("json={0}", js.GetBytes().Length);

            var pk = user.ToPacket();
            Console.WriteLine("binary={0}", pk.Total);
            Console.WriteLine(pk.ToHex());

            var user2 = pk.ToEntity<UserX>();
            var js2 = user2.ToJson(false);
            Console.WriteLine(js2);
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