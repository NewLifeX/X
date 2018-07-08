using System;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Reflection;
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
            foreach (var item in NetworkInterface.GetAllNetworkInterfaces())
            {
                Console.WriteLine("[{1}]<{3}> {0} {2}", item.Name, item.NetworkInterfaceType, item.GetPhysicalAddress(), item.GetIPProperties().GatewayAddresses.Count);
                //if (item.OperationalStatus != OperationalStatus.Up) continue;

                var ip = item.GetIPProperties();
                if (ip != null && ip.UnicastAddresses.Count > 0)
                {
                    var gw = ip.GatewayAddresses.Count;
                    foreach (var elm in ip.UnicastAddresses)
                    {
                        var pf = "";
                        try
                        {
                            pf = elm.PrefixOrigin + "";
                        }
                        catch { }
                        Console.WriteLine("\t[{0}] {1}", pf, elm.Address);
                    }
                }
            }

            Console.WriteLine();
            Console.WriteLine(NetHelper.MyIP());

            var ctrl = new ApiController();
            Console.WriteLine(ctrl.Info().ToJson());

            var svr = new ApiServer(3344);
            svr.Log = XTrace.Log;
            svr.EncoderLog = XTrace.Log;
            svr.StatPeriod = 5;
            svr.Start();

            Console.ReadKey(true);
        }

        static void Test4()
        {

        }
    }
}