using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using NewLife.IO;
using NewLife.Log;
using NewLife.Net.DNS;
using NewLife.Net.Proxy;
using NewLife.Net.Sockets;
using NewLife.Net.Udp;

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
                    Test4();
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
            //ZipFile.CompressDirectory("db", "db.zip");
            //ZipFile.Extract("db_20111219162114.zip", null);

            //var eop = DAL.Create("Common").CreateOperate("Log");

            //String file = "qq.bin";

            //DNS_A dns = DNS_A.Read(File.OpenRead("qqrs.bin"));
            //Console.WriteLine(dns);
            //Console.ReadKey(true);

            //using (FileStream fs = File.Create("qq_.bin"))
            //{
            //    dns.Write(fs);
            //}
        }

        static void Test2()
        {
            var client = new UdpClientX();
            //var client = new TcpClientX();
            //client.Connect("218.2.135.1", 53);
            client.Connect("8.8.8.8", 53);
            client.Error += new EventHandler<NetEventArgs>(client_Error);
            client.Received += new EventHandler<NetEventArgs>(client_Received);
            client.ReceiveAsync();

            var ptr = new DNS_PTR();
            ptr.Address = (client.Client.RemoteEndPoint as IPEndPoint).Address;
            //client.Send(ptr.GetStream());
            var s = ptr.GetStream();
            File.WriteAllBytes("udp.bin", s.ReadBytes());
            client.Send(s);

            Console.WriteLine("正在接收……");

            String name = null;
            while (true)
            {
                Thread.Sleep(1000);
                Console.WriteLine();
                Console.Write("要查询的域名：");
                name = Console.ReadLine();
                if (name.EqualIgnoreCase("exit")) break;

                DNS_A dns = new DNS_A();
                dns.Name = name;
                client.Send(dns.GetStream());
            }
        }

        static void client_Error(object sender, NetEventArgs e)
        {
            Console.WriteLine(e.LastOperation + "错误！" + e.SocketError + " " + e.Error);
        }

        static void client_Received(object sender, NetEventArgs e)
        {
            if (e.BytesTransferred <= 0) return;

            var client = sender as UdpClientX;
            //Console.WriteLine("收到{0}的数据，共{1}字节", e.RemoteEndPoint, e.BytesTransferred);

            var result = DNSEntity.Read(e.GetStream());
            Console.WriteLine();
            Console.WriteLine("查询：{0}", result.Name);
            Console.WriteLine("结果：{0}", result.DataString);
            Console.WriteLine("全部地址：");
            foreach (var item in result.Answers)
            {
                Console.WriteLine("{0,2} {1} {2}", item.Type, item.DataString, item.TTL);
            }
        }

        static void Test4()
        {
            var server = new HttpReverseProxy();
            server.Port = 888;
            server.ServerHost = "www.cnblogs.com";
            server.ServerPort = 80;
            server.Start();

            var s2 = new HttpProxy();
            s2.Port = 8080;
            s2.Start();

            HttpProxy.SetIEProxy("127.0.0.1:" + s2.Port);
            Console.WriteLine("已设置IE代理，任意键结束测试，关闭IE代理！");
            Console.ReadKey(true);
            HttpProxy.SetIEProxy(null);

            server.Dispose();
            s2.Dispose();
        }
    }
}