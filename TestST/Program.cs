using System;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
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

            Test3();

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

        }
    }
}