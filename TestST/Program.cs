using System;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using NewLife.Log;
using NewLife.Net;

namespace TestST
{
    class Program
    {
        static void Main(string[] args)
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
    }
}