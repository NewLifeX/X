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
            //var stunserver = new StunServer();
            //stunserver.Start();
            //StunClient.Servers.Insert(0, "127.0.0.1:3479");
            StunClient.Servers.Insert(0, "nnhy.eicp.net:3479");

            //var client = new UPnPClient();
            //client.OnNewDevice += new EventHandler<EventArgs<InternetGatewayDevice, bool>>(client_OnNewDevice);
            //client.StartDiscover();

            //Console.WriteLine("正在检测UPnP……");

            //NewLife.Net.P2P.P2PTest.StartHole();

            //var result = StunClient.Query("stunserver.org", 3478);
            var result = StunClient.Query();
            Console.WriteLine("{0} {1}", result.Type, result.Type.GetDescription());

            //AppTest.Start();
            //NetHelper.Wake("00-24-8C-04-C0-9B", "00-24-8C-04-C0-91");

            //var proxy = new XProxy();
            //proxy.Port = 888;
            //proxy.ProtocolType = ProtocolType.Tcp;
            //proxy.ServerAddress = "www.baidu.com";
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