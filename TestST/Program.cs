using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using NewLife.Agent;
using NewLife.Data;
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

            TsetPacket();//Test2();

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


        static void TsetPacket()
        {
            var d1 = new Byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            var d4 = new Byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            var d2 = new Byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            var d3 = new Byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };


            var s1 = new Byte[] { 8, 9, 1 };
            var s2 = new Byte[] { 8, 9, 0 };
            var s3 = new Byte[] { 7, 9, 0 };

            var s4 = new Byte[] { 0, 1, 2 };
            var s5 = new Byte[] { 0, 0, 1, 2 };


            //测试链表查找 和 设置索引 获取 索引 是否正常  获取可用区数据
            var pk1 = new Packet(d1);
            var pk2 = new Packet(d2, 10);
            var pk3 = new Packet(d3, 10, 10);
            var pk4 = new Packet(d4, 0, 10);

            var spk5 = new Packet(d1);
            //var spk6 = spk5.Next = pk2;
            //var spk7 = spk6.Next = pk3;
            //var spk8 = spk7.Next = pk4;
            spk5.Append(pk2);
            spk5.Append(pk3);
            spk5.Append(pk4);


            var index = pk1.IndexOf(s1);
            Console.WriteLine($" 正确应该是[-1] 实际[{index}]  ----index = pk1.IndexOf(s1);----------------------------- ");
            index = spk5.IndexOf(s2);//异常 
            Console.WriteLine($" 正确应该是[8] 实际[{index}]  ----index = spk5.IndexOf(s1);----------------------------- ");

            var val = spk5[39];
            Console.WriteLine($" 正确应该是[9] 实际[{val}]");

            spk5[40] = 255;
            Console.WriteLine($" 正确应该是[255] 实际[{ spk5[40] }]");

            var ex = TryCatch(new Action(delegate { spk5[-1] = 255; }));
            Console.WriteLine($" 正常应该出现异常 ----->  {ex}\r\n\r\n");
            var ex1 = TryCatch(new Action(delegate { var l = spk5[41]; }));
            Console.WriteLine($" 正常应该出现异常 ----->  {ex1}\r\n\r\n");
            var ex2 = TryCatch(new Action(delegate { var l = spk5[-1]; }));
            Console.WriteLine($" 正常应该出现异常 ----->  {ex2}\r\n\r\n");
        }
        private static string TryCatch(Action action)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                return "出现异常 " + ex.ToString();
            }
            return "没有异常 测试不通过";
        }
    }
}