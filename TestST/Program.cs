using System;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
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

            Test3();

            sw.Stop();
            Console.WriteLine("OK! {0:n0}ms", sw.ElapsedMilliseconds);

            Console.ReadKey();
        }

        static void Test1()
        {
            XTrace.WriteLine("学无先后达者为师！");

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
            var css = new ConfigurationBuilder()
                .AddXmlFile("TestST.dll.config")
                .Build().GetSection("connectionStrings").GetSection("add");
            foreach (var item in css.GetChildren())
            {
                Console.WriteLine("{0} {1} {2} {3}", item.Key, item["name"], item["connectionString"], item["providerName"]);
            }
        }

        static void Test3()
        {
            //foreach (var item in DAL.ConnStrs)
            //{
            //    Console.WriteLine("{0}\t{1}", item.Key, item.Value);
            //}

            var fact = MySqlClientFactory.Instance;

            //var dal = DAL.Create("Sqlite");
            DAL.AddConnStr("Membership", "Server=.;Port=3306;Database=mysql;Uid=root;Pwd=root", null, "MySql");
            var dal = DAL.Create("Membership");
            Console.WriteLine(dal.Db.ConnectionString);

            var ds = dal.Select("select * from 'user'");
            Console.WriteLine(ds.Tables.Count);

            //var n = UserX.Meta.Count;
            //Console.WriteLine(n);
        }
    }
}