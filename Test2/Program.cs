using System;
using System.Diagnostics;
using NewLife.Log;
using NewLife.Threading;
using NewLife;
using NewLife.Http;
using NewLife.Net;

namespace Test2
{
    internal class Program
    {
        static void Main(string[] args)
        {
            XTrace.UseConsole();

            TimerScheduler.Default.Log = XTrace.Log;

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

        static void Test1()
        {
            foreach (var item in NetHelper.GetIPs())
            {
                XTrace.WriteLine(item.ToString());
            }

            //var str = $"{DateTime.Now:yyyy}年，学无先后达者为师！";
            //str.SpeakAsync();

            //var client = new TinyHttpClient();
            //var rs = client.GetStringAsync("https://sso.newlifex.com/cube/info").Result;
            //XTrace.WriteLine(rs);
            var uri = new NetUri("http://sso.newlifex.com");
            var client = uri.CreateRemote();
            client.Log = XTrace.Log;
            client.LogSend = true;
            client.LogReceive = true;
            if (client is TcpSession tcp) tcp.MaxAsync = 0;
            client.Open();

            client.Send("GET /cube/info HTTP/1.1\r\nHost: sso.newlifex.com\r\n\r\n");

            var rs = client.ReceiveString();
            XTrace.WriteLine(rs);
        }
    }
}
