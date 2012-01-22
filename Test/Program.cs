using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using NewLife;
using NewLife.IO;
using NewLife.Log;
using NewLife.Net.DNS;
using NewLife.Net.Fetion;
using NewLife.Net.Proxy;
using NewLife.Net.Sockets;
using NewLife.Net.Tcp;
using NewLife.Net.Udp;
using NewLife.Net.UPnP;
using XCode.DataAccessLayer;

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
            var server = new HttpReverseProxy();
            server.Port = 888;
            server.ServerHost = "www.cnblogs.com";
            server.ServerPort = 80;
            server.Start();

            var s2 = new HttpProxy();
            s2.Port = 8080;
            s2.Start();

            //HttpProxy.SetIEProxy("127.0.0.1:" + s2.Port);
            Console.WriteLine("已设置IE代理，任意键结束测试，关闭IE代理！");
            Console.ReadKey(true);
            HttpProxy.SetIEProxy(null);

            server.Dispose();
            s2.Dispose();
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

            Console.WriteLine("飞信Wap协议测试：");
            Console.Write("手机号：");
            String user = Console.ReadLine();
            Console.Write("密码：");
            String pass = Console.ReadLine();

            var client = new WapFetion(user, pass);
            //client.ShowResponse = true;
            client.Send(user, String.Format("{0}于{1}已登录{2}！", user, DateTime.Now, client.GetType()));
            //Console.WriteLine("我的好友：");
            //foreach (var item in client.Friends)
            //{
            //    item.Refresh();
            //    Console.WriteLine(item);
            //}
            while (true)
            {
                Console.Write("目标手机号码：");
                var mobile = Console.ReadLine();
                if (String.IsNullOrEmpty(mobile)) break;

                Console.Write("内容：");
                var msg = Console.ReadLine();

                client.Send(mobile, msg);
            }
            //client.AddFriend("15855167890", "阿黄", "大石头");
            //client.Send("15855167890", "WapFetion测试");
            //var mobile = client.GetMobile(185257960);
            //client.AddFriend("13585922759", "云飞扬-张", "大石头");
            //Thread.Sleep(10000);
            //client.Send("13585922759", "WapFetion测试");
            //Console.WriteLine(mobile);
            //client.SendStranger(185257960, "WapFetion测试");
            client.Dispose();
        }
    }
}