using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Reflection;
using System.Threading;
using Microsoft.VisualBasic.Devices;
using NewLife.Common;
using NewLife.Compression;
using NewLife.Log;
using NewLife.Messaging;
using NewLife.Model;
using NewLife.Net;
using NewLife.Net.Dhcp;
using NewLife.Net.IO;
using NewLife.Net.Stress;
using NewLife.Reflection;
using NewLife.Serialization;
using NewLife.Threading;
using NewLife.Xml;
using XCode.DataAccessLayer;
using XCode.Membership;
using XCode.Sync;
using XCode.Transform;

namespace Test
{
    public class Program
    {
        private static void Main(string[] args)
        {
            XTrace.Log = new NetworkLog();
            XTrace.UseConsole();
#if DEBUG
            XTrace.Debug = true;
#endif
            while (true)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
#if !DEBUG
                try
                {
#endif
                Test16();
#if !DEBUG
                }
                catch (Exception ex)
                {
                    XTrace.WriteException(ex);
                }
#endif

                sw.Stop();
                Console.WriteLine("OK! 耗时 {0}", sw.Elapsed);
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key != ConsoleKey.C) break;
            }
        }

        static void Test2()
        {
            HttpClientMessageProvider client = new HttpClientMessageProvider();
            client.Uri = new Uri("http://localhost:8/Web/MessageHandler.ashx");

            var rm = MethodMessage.Create("Admin.Login", "admin", "admin");
            //rm.Header.Channel = 88;

            //Message.Debug = true;
            //var ms = rm.GetStream();
            //var m2 = Message.Read(ms);

            Message msg = client.SendAndReceive(rm, 0);
            var rs = msg as EntityMessage;
            Console.WriteLine("返回：" + rs.Value);

            msg = client.SendAndReceive(rm, 0);
            rs = msg as EntityMessage;
            Console.WriteLine("返回：" + rs.Value);
        }

        static void Test3()
        {
            var uri = new NetUri("udp://x2:3389");

            Console.WriteLine(uri);
            Console.WriteLine(uri.ProtocolType);
            Console.WriteLine(uri.EndPoint);
            Console.WriteLine(uri.Address);
            Console.WriteLine(uri.Host);
            Console.WriteLine(uri.Port);

            var xml = uri.ToXml();
            Console.WriteLine(xml);

            uri = xml.ToXmlEntity<NetUri>();
            Console.WriteLine(uri);
        }

        static void Test5()
        {
            using (var zf = new ZipFile())
            {
                zf.AddFile("XCode.pdb");
                zf.AddFile("NewLife.Core.pdb");

                zf.Write("test.zip");
                zf.Write("test.7z");
            }
            //using (var zf = new ZipFile("Test.lzma.zip"))
            //{
            //    foreach (var item in zf.Entries.Values)
            //    {
            //        Console.WriteLine("{0} {1}", item.FileName, item.CompressionMethod);
            //    }

            //    zf.Extract("lzma");
            //}
        }

        static void Test6()
        {
            Message.DumpStreamWhenError = true;
            //var msg = new EntityMessage();
            //msg.Value = Guid.NewGuid();
            var msg = new MethodMessage();
            msg.TypeName = "Admin";
            msg.Name = "Login";
            msg.Parameters = new Object[] { "admin", "password" };

            var kind = RWKinds.Json;
            var ms = msg.GetStream(kind);
            //ms = new MemoryStream(ms.ReadBytes(ms.Length - 1));
            //Console.WriteLine(ms.ReadBytes().ToHex());
            Console.WriteLine(ms.ToStr());
            //ms = msg.GetStream(RWKinds.Xml);
            //Console.WriteLine(ms.ToStr());

            Message.Debug = true;
            ms.Position = 0;
            var rs = Message.Read(ms, kind);
            Console.WriteLine(rs);
        }

        static void Test7()
        {
            //Console.Write("请输入表达式：");
            //var code = Console.ReadLine();

            //var rs = ScriptEngine.Execute(code, new Dictionary<String, Object> { { "a", 222 }, { "b", 333 } });
            ////Console.WriteLine(rs);

            //var se = ScriptEngine.Create(code);
            //var fm = code.Replace("a", "{0}").Replace("b", "{1}");
            //for (int i = 1; i <= 9; i++)
            //{
            //    for (int j = 1; j <= i; j++)
            //    {
            //        Console.Write(fm + "={2}\t", j, i, se.Invoke(i, j));
            //    }
            //    Console.WriteLine();
            //}

            var se = ScriptEngine.Create("Test.Program.TestMath(k)");
            if (se.Method == null)
            {
                se.Parameters.Add("k", typeof(Double));
                se.Compile();
            }

            var fun = (DM)(Object)Delegate.CreateDelegate(typeof(DM), se.Method as MethodInfo);

            var timer = 1000000;
            var k = 123;
            CodeTimer.ShowHeader();
            CodeTimer.TimeLine("原生", timer, n => TestMath(k));
            CodeTimer.TimeLine("动态", timer, n => se.Invoke(k));
            CodeTimer.TimeLine("动态2", timer, n => fun(k));
        }
        public static Double TestMath(Double k)
        {
            //var bts = File.ReadAllBytes(Assembly.GetExecutingAssembly().Location);
            return Math.Sin(k) * Math.Log10(k) * Math.Exp(k);
        }
        delegate Object DM(Double k);

        static SysConfig Load()
        {
            var filename = SysConfig._.ConfigFile;
            if (filename.IsNullOrWhiteSpace()) return null;
            filename = filename.GetFullPath();
            if (!File.Exists(filename)) return null;

            try
            {
                var config = filename.ToXmlFileEntity<SysConfig>();
                if (config == null) return null;

                //config.OnLoaded();

                //// 第一次加载，建立定时重载定时器
                //if (timer == null && _.ReloadTime > 0) timer = new TimerX(s => Current = null, null, _.ReloadTime * 1000, _.ReloadTime * 1000);

                return config;
            }
            catch (Exception ex) { XTrace.WriteException(ex); return null; }
        }

        static void Test9()
        {
            var user = UserX.FindAllWithCache()[0];
            Console.WriteLine(user.RoleName);
            Console.Clear();

            //var bn = new Binary();
            //bn.EnableTrace();
            var bn = new Xml();
            bn.Write(user);

            var sw = new Stopwatch();
            sw.Start();

            var buf = bn.GetBytes();
            Console.WriteLine(buf.ToHex());
            Console.WriteLine(bn.GetString());

            var ms = new MemoryStream(buf);
            //bn = new Binary();
            bn.Stream = ms;
            //bn.EnableTrace();
            var u = bn.Read<UserX>();

            foreach (var item in UserX.Meta.AllFields)
            {
                if (user[item.Name] == u[item.Name])
                    Console.WriteLine("{0} {1} <=> {2} 通过", item.Name, user[item.Name], u[item.Name]);
                else
                    Console.WriteLine("{0} {1} <=> {2} 失败", item.Name, user[item.Name], u[item.Name]);
            }

            //var hi = HardInfo.Current;
            //sw.Stop();
            //Console.WriteLine(sw.Elapsed);
            //Console.WriteLine(hi);

            //var ci = new ComputerInfo();
            //Console.WriteLine(ci);
        }

        static void Test10()
        {
            NetHelper.ShowTcpParameters();
            Console.WriteLine("k键设置最优Tcp参数，其它键开始测试：");
            var key = Console.ReadKey();
            if (key.KeyChar == 'k') NetHelper.SetTcpMax();

            TcpStress.Main();
        }

        static void Test13()
        {
            var file = @"E:\BaiduYunDownload\xiaomi.db";
            var file2 = Path.ChangeExtension(file, "sqlite");
            DAL.AddConnStr("src", "Data Source=" + file, null, "sqlite");
            DAL.AddConnStr("des", "Data Source=" + file2, null, "sqlite");

            if (!File.Exists(file2))
            {
                var et = new EntityTransform();
                et.SrcConn = "src";
                et.DesConn = "des";
                //et.PartialTableNames.Add("xiaomi");
                //et.PartialCount = 1000000;

                et.Transform();
            }

            var sw = new Stopwatch();

            var dal = DAL.Create("src");
            var eop = dal.CreateOperate(dal.Tables[0].TableName);
            sw.Start();
            var count = eop.Count;
            sw.Stop();
            XTrace.WriteLine("{0} 耗时 {1}ms", count, sw.ElapsedMilliseconds);
            sw.Reset(); sw.Start();
            count = eop.FindCount();
            sw.Stop();
            XTrace.WriteLine("{0} 耗时 {1}ms", count, sw.ElapsedMilliseconds);

            var entity = eop.Create();
            entity["username"] = "Stone";
            entity.Save();
            count = eop.FindCount();
            Console.WriteLine(count);

            entity.Delete();
            count = eop.FindCount();
            Console.WriteLine(count);
        }

        static void Test14()
        {
            Console.Clear();
            //XTrace.Log.Level = LogLevel.Info;
            var server = new FileServer();
            server.Log = XTrace.Log;
            server.Start();

            var count = 0;
            WaitCallback func = s =>
            {
                count++;
                var client = new FileClient();
                try
                {
                    client.Log = XTrace.Log;
                    if (s + "" == "Test.exe")
                        client.Connect("127.0.0.1", server.Port);
                    else
                        client.Connect("::1", server.Port);
                    client.SendFile(s + "");
                }
                finally
                {
                    count--;
                    client.Dispose();
                }
            };

            ThreadPoolX.QueueUserWorkItem(func, "Test.exe");
            ThreadPoolX.QueueUserWorkItem(func, "NewLife.Core.dll");
            ThreadPoolX.QueueUserWorkItem(func, "NewLife.Net.dll");

            var file = @"F:\MS\cn_visual_studio_ultimate_2013_with_update_4_x86_dvd_5935081.iso";
            if (File.Exists(file))
                ThreadPoolX.QueueUserWorkItem(func, file);

            Thread.Sleep(500);
            while (count > 0) Thread.Sleep(200);
            server.Dispose();
        }

        static void Test15()
        {
            //"我是超级大石头！".Speak();

            var tcp = new TcpSession();
            tcp.Log = XTrace.Log;
            tcp.Remote = "tcp://127.0.0.1:8";
            //tcp.MessageDgram = true;
            tcp.AutoReconnect = 0;
            //tcp.Send("我是大石头！");
            tcp.Open();
            tcp.Stream = new PacketStream(tcp.Stream);
            //var ms = new MemoryStream();
            for (int i = 0; i < 10; i++)
            {
                tcp.Send("我是大石头{0}！".F(i + 1));
                //var buf = "我是大石头{0}！".F(i + 1).GetBytes();
                //ms.WriteEncodedInt(buf.Length);
                //ms.Write(buf);
            }
            //ms.Position = 0;
            //tcp.Client.GetStream().Write(ms);

            while (tcp.Active)
            {
                var str = tcp.ReceiveString();
                Console.WriteLine(str);
            }

            //NetHelper.Debug = true;
            //var server = new StunServer();
            //server.Start();

            //Console.WriteLine(NetHelper.MyIP().GetAddress());
            //Console.WriteLine(IPAddress.Any.GetAddress());
            //Console.WriteLine(IPAddress.Loopback.GetAddress());

            //var buf = NetHelper.MyIP().GetAddressBytes();
            //buf[3] = 33;
            //Console.WriteLine(new IPAddress(buf).GetAddress());

            //var ip = NetHelper.ParseAddress("dg.newlifex.com");
            //Console.WriteLine(ip.GetAddress());
            //Console.WriteLine(Ip.GetAddress(ip.ToString()));

            //var client = new StunClient();
            //var rs = client.Query();
            //if (rs != null)
            //{
            //    //if (rs != null && rs.Type == StunNetType.Blocked && rs.Public != null) rs.Type = StunNetType.Symmetric;
            //    XTrace.WriteLine("网络类型：{0} {1}", rs.Type, rs.Type.GetDescription());
            //    XTrace.WriteLine("公网地址：{0} {1}", rs.Public, Ip.GetAddress(rs.Public.Address.ToString()));
            //}
        }

        private static void Test16()
        {
            //var data = "I am BigStone!".GetBytes();
            //var pass = "123321".GetBytes();

            //XTrace.WriteLine("数据:{0} 密码:{1}", data.ToHex(), pass.ToHex());

            //var m = data.RC4(pass);
            //XTrace.WriteLine("加密后数据:{0}", m.ToHex());

            //var n = m.RC4(pass);
            //XTrace.WriteLine("解密后数据:{0}", n.ToHex());


            var dhcp = new DhcpServer();
            dhcp.Log = XTrace.Log;
            dhcp.OnMessage += dhcp_OnMessage;
            dhcp.Start();
        }

        static void dhcp_OnMessage(object sender, DhcpMessageEventArgs e)
        {
            Console.WriteLine(e.Request);
        }

        static void udp_Received(object sender, ReceivedEventArgs e)
        {
            XTrace.WriteLine(e.ToHex());

            var dhcp = new DhcpEntity();
            dhcp.Read(e.Stream);
            Console.WriteLine(dhcp + "");
            //foreach (var pi in dhcp.GetType().GetProperties())
            //{
            //    Console.WriteLine("{0,-16}:{1}", pi.Name, dhcp.GetValue(pi));
            //}
        }
    }
}