using System;
using System.Diagnostics;
using NewLife.Log;
using NewLife.Compression;
using XCode.DataAccessLayer;
using NewLife.Serialization;
using System.IO;
using NewLife.Net.Protocols.DNS;
using NewLife.Net.Udp;
using System.Net;
using NewLife.Net.Sockets;
using System.Globalization;
using NewLife.Net.Common;

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

            String file = "qq.bin";

            DNS_A dns = DNS_A.Read(File.OpenRead("bd.bin"));
            Console.WriteLine(dns);
            Console.ReadKey(true);

            using (FileStream fs = File.Create("qq_.bin"))
            {
                dns.Write(fs);
            }
        }

        static void Test2()
        {
            DNS_A dns = new DNS_A();
            DNSRecord record = new DNSRecord();
            record.Name = "www.baidu.com";
            dns.Questions = new DNSRecord[] { record };

            var ms = new MemoryStream();
            dns.Write(ms);
            ms.Position = 0;

            UdpClientX client = new UdpClientX();
            client.Connect("192.168.1.1", 53);
            client.Error += new EventHandler<NetEventArgs>(client_Error);
            client.Received += new EventHandler<NetEventArgs>(client_Received);
            client.ReceiveAsync();

            Console.WriteLine("正在发送……");
            client.Send(ms);

            Console.WriteLine("正在接收……");
        }

        static void client_Error(object sender, NetEventArgs e)
        {
            Console.WriteLine(e.LastOperation + "错误！" + e.UserToken);
        }

        static void client_Received(object sender, NetEventArgs e)
        {
            var client = sender as UdpClientX;
            Console.WriteLine("收到{0}的数据，共{1}字节", e.RemoteEndPoint, e.BytesTransferred);

            var result = DNS_A.Read(e.GetStream());
            Console.WriteLine(result.Answers[0].DataString);
        }

        static void Test3()
        {
            UdpServer server = new UdpServer();
        }

        static void Test4()
        {
            String mac = "00-24-8C-04-C0-2A";
            //mac = "00-24-8C-04-C0-9B";
            //mac = "00-24-8C-04-BF-9F";

            NetHelper.Wake(mac);

            //using (StreamReader reader = new StreamReader("macs.txt"))
            //{
            //    while (!reader.EndOfStream)
            //    {
            //        String line = reader.ReadLine();
            //        line = ("" + line).Trim();
            //        if (String.IsNullOrEmpty(line)) continue;

            //        String[] ss = line.Split(" ", "\t");
            //        if (ss == null || ss.Length < 4) continue;

            //        Console.WriteLine("正在唤醒 {0} {1} {2}", ss[4], ss[2], ss[1]);
            //        Wake(ss[1]);
            //    }
            //}
        }
    }
}