using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using NewLife.IO;
using NewLife.Log;
using NewLife.Net.DNS;
using NewLife.Net.Sockets;
using NewLife.Net.Tcp;
using NewLife.Net.Udp;
using NewLife.Reflection;
using XCode;
using XCode.Code;
using XCode.DataAccessLayer;
using NewLife.Net.Proxy;
using NewLife.Net.Stun;
using NewLife.Net.UPnP;
using NewLife;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using NewLife.Net;
using NewLife.Collections;
using System.Collections.Generic;
using System.Text;
using NewLife.Net.Fetion;

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

        static void Test3()
        {
            //UdpServer server = new UdpServer();
            var dal = DAL.Create("Common");
            var p = dal.Db.Factory.CreateParameter();
            p.ParameterName = "table_name";
            p.Value = "admin";
            //var ds = dal.Session.Query("sp_columns", CommandType.StoredProcedure, p);
            var ds = dal.Session.Query("select @table_name;", CommandType.Text, p);
            Console.Write(ds);
        }

        static void Test4()
        {
            //var stunserver = new StunServer();
            //stunserver.Start();
            //StunClient.Servers.Insert(0, "127.0.0.1:3479");
            //StunClient.Servers.Insert(0, "nnhy.eicp.net");

            //var client = new UPnPClient();
            ////client.OnNewDevice += new EventHandler<EventArgs<InternetGatewayDevice, bool>>(client_OnNewDevice);
            //client.StartDiscover();

            //Console.WriteLine("正在检测UPnP……");
            //Thread.Sleep(2000);

            //foreach (var item in client.Gateways.Values)
            //{
            //    Console.WriteLine();
            //    Console.WriteLine("{0}上的UPnP映射：", item);
            //    foreach (var elm in item.GetMapByIndexAll())
            //    {
            //        Console.WriteLine(elm);
            //    }
            //}

            ////NewLife.Net.P2P.P2PTest.StartHole();

            //Console.WriteLine();
            //Console.WriteLine("正在检测网络类型……");
            //Console.WriteLine();
            ////var result = StunClient.Query("stunserver.org", 3478);
            //var result = StunClient.Query();
            //Console.WriteLine("UDP {0} {1}", result.Type, result.Type.GetDescription());
            //Console.WriteLine();
            //result = StunClient.Query(ProtocolType.Tcp);
            //Console.WriteLine("TCP {0} {1}", result.Type, result.Type.GetDescription());
            //Console.WriteLine();

            //AppTest.Start();
            //while (true)
            //{
            //    NewLife.Net.NetHelper.Wake( "00-24-8C-04-C0-91");
            //    Thread.Sleep(1000);
            //}

            //var proxy = new NATProxy();
            //proxy.Port = 888;
            //proxy.ProtocolType = ProtocolType.Tcp;
            //proxy.ServerAddress = NetHelper.ParseAddress("www.baidu.com");
            //proxy.ServerPort = 80;
            //var filter = new HttpFilter();
            //filter.Proxy = proxy;
            //proxy.Filters.Add(filter);
            //proxy.Start();

            //var proxy = new XProxy("nnhy.org",3389);
            //proxy.Port = 89;
            //proxy.Start();

            //var client = new TcpClientX();
            //client.Connect("jslswb.com", 12);
            ////client.Connect("192.168.1.9", 12);
            //var ep = client.LocalEndPoint;
            //var msg = client.ReceiveString();
            //Console.WriteLine("收到：{0}", msg);
            ////client.Close();
            //Console.WriteLine("本地监听：{0}", ep);

            //var server = new TcpServer();
            //server.Address = ep.Address;
            //server.Port = ep.Port;
            //server.Accepted += new EventHandler<NetEventArgs>(server_Accepted2);
            //server.Start();

            //var server = new TcpServer();
            //server.Port = 12;
            //server.Accepted += new EventHandler<NetEventArgs>(server_Accepted);
            //server.Start();
            //Console.WriteLine("打洞服务器等待连接……");

        }

        static void client_OnNewDevice(object sender, EventArgs<InternetGatewayDevice, bool> e)
        {
            Console.WriteLine("网关 {0}{1}", e.Arg1, e.Arg2 ? " [缓存]" : "");
            if (e.Arg2) return;

            Console.WriteLine();
            Console.WriteLine("{0}上的UPnP映射：", e.Arg1);
            foreach (var item in e.Arg1.GetMapByIndexAll())
            {
                Console.WriteLine(item);
            }
        }

        static void server_Accepted(object sender, NetEventArgs e)
        {
            Console.WriteLine("新连接：{0}", e.RemoteEndPoint);
            ISocketSession session = e.Socket as ISocketSession;
            session.Send("连接收到，准备反向连接！");

            // 连回去
            try
            {
                Console.WriteLine("连回去");
                var client = new TcpClientX();
                client.Port = 12;
                Console.WriteLine("绑定：{0} 准备连接：{1}", client.LocalEndPoint, e.RemoteEndPoint);
                client.Connect(e.RemoteEndPoint);
                Console.WriteLine("连接成功，发送信息");
                client.Send("Hello nnhy!");
                Console.WriteLine("发送完毕，接收信息");
                var msg = client.ReceiveString();
                Console.WriteLine("收到：{0}", msg);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            session.Send("已进行反向连接！");
        }

        static void server_Accepted2(object sender, NetEventArgs e)
        {
            Console.WriteLine("反向连接：{0}", e.RemoteEndPoint);

            ISocketSession session = e.Socket as ISocketSession;
            Console.WriteLine("接收信息");
            var msg = session.ReceiveString();
            Console.WriteLine("收到：{0}", msg);

            Console.WriteLine("发送信息");
            session.Send("完成!");
        }

        static void Test5()
        {
            //ObjectPoolTest<NetEventArgs>.Start();

            var client = new WapFetion("15150588224", "NN#hY9");
            //client.Login();
            client.Send("15062221331", "WapFetion测试");
        }
    }
}