using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using NewLife.IO;
using NewLife.Log;
using NewLife.Messaging;
using NewLife.Net;
using NewLife.Net.DNS;
using NewLife.Net.Proxy;
using NewLife.Net.Sockets;
using NewLife.Net.Udp;
using NewLife.Collections;

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

        private static void Test1()
        {
            var server = new HttpReverseProxy();
            server.Port = 888;
            server.ServerHost = "www.cnblogs.com";
            server.ServerPort = 80;
            server.Start();

            var http = new HttpProxy();
            http.Port = 8080;
            //http.OnResponse += new EventHandler<HttpProxyEventArgs>(http_OnResponse);
            http.Start();

            HttpProxy.SetIEProxy("127.0.0.1:" + http.Port);
            Console.WriteLine("已设置IE代理，任意键结束测试，关闭IE代理！");
            Console.ReadKey(true);
            HttpProxy.SetIEProxy(null);

            server.Dispose();
            http.Dispose();
        }

        //static void http_OnResponse(object sender, HttpProxyEventArgs e)
        //{
        //    if (e.Header != null) XTrace.WriteLine(e.Header.ToString());
        //}
    }
}