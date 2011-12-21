using System;
using System.Diagnostics;
using System.IO;
using NewLife.Log;
using NewLife.Net.Application;
using NewLife.Net.Protocols.DNS;
using NewLife.Net.Sockets;
using NewLife.Net.Udp;
using XCode.DataAccessLayer;
using NewLife.Threading;
using System.Collections.Generic;
using XCode;
using XCode.Code;
using NewLife.Reflection;

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
                    Test5();
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
            Console.WriteLine(e.LastOperation + "错误！" + e.Error);
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
            AppTest.Start();
            //NetHelper.Wake("00-24-8C-04-C0-9B", "00-24-8C-04-C0-91");
        }

        static void Test5()
        {
            DAL.ShowSQL = false;

            var asm = EntityAssembly.Create("User", DAL.Import(File.ReadAllText("user.xml")));
            var eop = EntityFactory.CreateOperate(AssemblyX.Create(asm).GetType("User"));
            if (DAL.Create(eop.ConnName).DbType == DatabaseType.SqlServer)
                eop.Execute("truncate table " + eop.FormatName(eop.TableName));
            else
                eop.Execute("delete from " + eop.FormatName(eop.TableName));

            String file = "User.sql";
            var fi = new FileInfo(file);
            while (!fi.Exists && fi.Directory != fi.Directory.Root) fi = new FileInfo(Path.Combine(fi.Directory.Parent.FullName, fi.Name));
            Console.WriteLine("分析文件：{0}", fi.FullName);

            Int32 total = 0;
            using (StreamReader reader = new StreamReader(fi.FullName))
            {
                //eop.BeginTransaction();
                CodeTimer.TimeLine("导入", 6430000, index =>
                {
                    if (reader.EndOfStream)
                    {
                        eop.Commit();
                        return;
                    }
                    String line = reader.ReadLine();
                    if (line.IsNullOrWhiteSpace()) return;

                    if (index == 0) eop.BeginTransaction();

                    Int32 p1 = line.IndexOf('#');
                    Int32 p2 = line.LastIndexOf('#');

                    String user = line.Substring(0, p1).Trim();
                    String pass = line.Substring(p1 + 1, p2 - p1 - 1).Trim();
                    String mail = line.Substring(p2 + 1).Trim();

                    // 入库
                    var entity = eop.Create();
                    entity.SetItem("Name", user);
                    entity.SetItem("Pass", pass);
                    entity.SetItem("Mail", mail);
                    entity.Insert();

                    if (index % 1000 == 0)
                    {
                        eop.Commit();
                        eop.BeginTransaction();
                    }

                    total++;
                }, false);
                //eop.Commit();
            }
            Console.WriteLine(total);
        }
    }
}