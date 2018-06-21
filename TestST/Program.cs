using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
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

            Test2();

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
            //foreach (var item in DAL.ConnStrs)
            //{
            //    Console.WriteLine("{0}\t{1}", item.Key, item.Value);
            //}

            //var fact = MySqlClientFactory.Instance;
            var fact = SqliteFactory.Instance;

            //var dal = DAL.Create("Sqlite");
            //DAL.AddConnStr("Membership", "Server=.;Port=3306;Database=world;Uid=root;Pwd=root", null, "MySql");
            var dal = DAL.Create("Membership");
            Console.WriteLine(dal.Db.ConnectionString);

            //var ds = dal.Select("select * from city");
            //Console.WriteLine(ds.Tables[0].Rows.Count);

            var user = UserX.FindByName("admin");
            Console.WriteLine(user);

            //var n = UserX.Meta.Count;
            //Console.WriteLine(n);
        }
    }
}