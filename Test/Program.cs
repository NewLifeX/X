using System;
using NewLife.IO;
using System.Diagnostics;
using System.Threading;
using NewLife.Log;
using NewLife.Net.Proxy;
using NewLife.Net.Sockets;
using NewLife.Threading;
using NewLife.Net.DNS;
using System.IO;

namespace Test
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            XTrace.OnWriteLog += new EventHandler<WriteLogEventArgs>(XTrace_OnWriteLog);
            while (true)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
#if !DEBUG
                try
                {
#endif
                    Test1();
#if !DEBUG
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
#endif

                sw.Stop();
                Console.WriteLine("OK! 耗时 {0}", sw.Elapsed);
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key != ConsoleKey.C) break;
            }
        }

        private static void XTrace_OnWriteLog(object sender, WriteLogEventArgs e)
        {
            Console.WriteLine(e.ToString());
        }

        static HttpProxy http = null;
        private static void Test1()
        {
            //var server = new HttpReverseProxy();
            //server.Port = 888;
            //server.ServerHost = "www.cnblogs.com";
            //server.ServerPort = 80;
            //server.Start();

            //NewLife.Net.Application.AppTest.Start();

            //http = new HttpProxy();
            //http.Port = 8080;
            ////http.OnResponse += new EventHandler<HttpProxyEventArgs>(http_OnResponse);
            //http.Start();

            //HttpProxy.SetIEProxy("127.0.0.1:" + http.Port);
            //Console.WriteLine("已设置IE代理，任意键结束测试，关闭IE代理！");

            //ThreadPoolX.QueueUserWorkItem(ShowStatus);

            //Console.ReadKey(true);
            //HttpProxy.SetIEProxy(null);

            ////server.Dispose();
            //http.Dispose();

            //var ds = new DNSServer();
            //ds.Start();

            var buffer = File.ReadAllBytes("dns2.bin");
            var entity2 = DNSEntity.Read(buffer, false);
            Console.WriteLine(entity2);

            var buffer2 = entity2.GetStream().ReadBytes();

            var p = buffer.CompareTo(buffer2);
            if (p != 0)
            {
                Console.WriteLine("{0:X2} {1:X2} {2:X2}", p, buffer[p], buffer2[p]);
            }
        }

        static void ShowStatus()
        {
            while (true)
            {
                var hs = http.Server as SocketBase;
                Int32 max = 0;
                Int32 max2 = 0;
                Int32 min = 0;
                Int32 min2 = 0;
                ThreadPool.GetMaxThreads(out max, out max2);
                ThreadPool.GetMinThreads(out min, out min2);
                Int32 wt = 0;
                Int32 cpt = 0;
                ThreadPool.GetAvailableThreads(out wt, out cpt);
                Console.WriteLine("异步：{0} 会话：{1} 用户线程：{8} 工作线程：{2}/{3}/{4} IOCP线程：{5}/{6}/{7}", hs.AsyncCount, http.Sessions.Count, min, max, wt, min2, max2, cpt, Process.GetCurrentProcess().Threads.Count);

                Thread.Sleep(3000);

                //GC.Collect();
            }
        }

        //static void http_OnResponse(object sender, HttpProxyEventArgs e)
        //{
        //    if (e.Header != null) XTrace.WriteLine(e.Header.ToString());
        //}
    }
}