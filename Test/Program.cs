using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading;
using NewLife.Linq;
using NewLife.Log;
using NewLife.Messaging;
using NewLife.Net.Proxy;
using NewLife.Net.Sockets;
using NewLife.Threading;
using NewLife.Net;
using NewLife.Net.Common;
using System.Collections.Generic;

namespace Test
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            XTrace.UseConsole();
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

        static HttpProxy http = null;
        private static void Test1()
        {
            //var server = new HttpReverseProxy();
            //server.Port = 888;
            //server.ServerHost = "www.cnblogs.com";
            //server.ServerPort = 80;
            //server.Start();

            //var ns = Enum.GetNames(typeof(ConsoleColor));
            //var vs = Enum.GetValues(typeof(ConsoleColor));
            //for (int i = 0; i < ns.Length; i++)
            //{
            //    Console.ForegroundColor = (ConsoleColor)vs.GetValue(i);
            //    Console.WriteLine(ns[i]);
            //}

            //NewLife.Net.Application.AppTest.Start();

            http = new HttpProxy();
            http.Port = 8080;
            http.EnableCache = true;
            //http.OnResponse += new EventHandler<HttpProxyEventArgs>(http_OnResponse);
            http.Start();

            var old = HttpProxy.GetIEProxy();
            if (!old.IsNullOrWhiteSpace()) Console.WriteLine("旧代理：{0}", old);
            HttpProxy.SetIEProxy("127.0.0.1:" + http.Port);
            Console.WriteLine("已设置IE代理，任意键结束测试，关闭IE代理！");

            ThreadPoolX.QueueUserWorkItem(ShowStatus);

            Console.ReadKey(true);
            HttpProxy.SetIEProxy(old);

            //server.Dispose();
            http.Dispose();

            //var ds = new DNSServer();
            //ds.Start();

            //for (int i = 5; i < 6; i++)
            //{
            //    var buffer = File.ReadAllBytes("dns" + i + ".bin");
            //    var entity2 = DNSEntity.Read(buffer, false);
            //    Console.WriteLine(entity2);

            //    var buffer2 = entity2.GetStream().ReadBytes();

            //    var p = buffer.CompareTo(buffer2);
            //    if (p != 0)
            //    {
            //        Console.WriteLine("{0:X2} {1:X2} {2:X2}", p, buffer[p], buffer2[p]);
            //    }
            //}
        }

        static void ShowStatus()
        {
            //var pool = PropertyInfoX.GetValue<SocketBase, ObjectPool<NetEventArgs>>("Pool");
            var pool = NetEventArgs.Pool;

            while (true)
            {
                var asyncCount = 0; try
                {
                    foreach (var item in http.Servers)
                    {
                        asyncCount += item.AsyncCount;
                    }
                    foreach (var item in http.Sessions.Values.ToArray())
                    {
                        var remote = (item as IProxySession).Remote;
                        if (remote != null) asyncCount += remote.Host.AsyncCount;
                    }
                }
                catch (Exception ex) { Console.WriteLine(ex.ToString()); }

                Int32 wt = 0;
                Int32 cpt = 0;
                ThreadPool.GetAvailableThreads(out wt, out cpt);
                Int32 threads = Process.GetCurrentProcess().Threads.Count;

                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("异步:{0} 会话:{1} Thread:{2}/{3}/{4} Pool:{5}/{6}/{7}", asyncCount, http.Sessions.Count, threads, wt, cpt, pool.StockCount, pool.FreeCount, pool.CreateCount);
                Console.ForegroundColor = color;

                Thread.Sleep(3000);

                //GC.Collect();
            }
        }

        static void Test2()
        {
            HttpClientMessageProvider client = new HttpClientMessageProvider();
            client.Uri = new Uri("http://localhost:8/Web/MessageHandler.ashx");

            var rm = MethodMessage.Create("Admin.Login", "admin", "admin");
            rm.Header.Channel = 88;

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
            using (var sp = new SerialPort("COM2"))
            {
                sp.Open();

                var b = 0;
                while (true)
                {
                    Console.WriteLine(b);
                    var bs = new Byte[] { (Byte)b };
                    sp.Write(bs, 0, bs.Length);
                    b = b == 0 ? 0xFF : 0;

                    Thread.Sleep(1000);
                }
            }
        }

        static NetServer server = null;
        static IMessageProvider smp = null;
        static IMessageProvider cmp = null;
        static void Test4()
        {
            if (server == null)
            {
                server = new NetServer();
                server.Port = 1234;
                //server.Received += new EventHandler<NetEventArgs>(server_Received);

                var mp = new ServerMessageProvider(server);
                mp.OnReceived += new EventHandler<MessageEventArgs>(smp_OnReceived);
                mp.AutoJoinGroup = true;
                smp = mp;

                server.Start();
            }

            if (cmp == null)
            {
                var client = NetService.CreateSession(new NetUri("tcp://::1:1234"));
                client.ReceiveAsync();
                cmp = new ClientMessageProvider() { Session = client };
                cmp.OnReceived += new EventHandler<MessageEventArgs>(cmp_OnReceived);
            }

            //Message.Debug = true;
            var msg = new EntityMessage();
            var rnd = new Random((Int32)DateTime.Now.Ticks);
            var bts = new Byte[rnd.Next(5000, 10000)];
            rnd.NextBytes(bts);
            msg.Value = bts;

            //var rs = cmp.SendAndReceive(msg, 5000);
            cmp.Send(msg);
        }

        static void smp_OnReceived(object sender, MessageEventArgs e)
        {
            var msg = e.Message;
            Console.WriteLine("服务端收到：[{0}]", msg);
            var rs = new EntityMessage();
            rs.Value = "收到" + msg;
            (sender as IMessageProvider).Send(rs);
        }

        static void cmp_OnReceived(object sender, MessageEventArgs e)
        {
            Console.WriteLine("客户端收到：{0}", e.Message);
        }

        private static Dictionary<Int32, MessageGroup> groups = new Dictionary<Int32, MessageGroup>();
        static void server_Received(object sender, NetEventArgs e)
        {
            var ms = e.GetStream();
            Console.WriteLine("服务端收到：[{0}]", ms.Length);

            Message gm = null;
            while (ms.Position < ms.Length)
            {
                var msg = Message.Read(ms);
                Console.WriteLine("{0}消息：{1}", e.RemoteIPEndPoint, msg);

                gm = OnReceiveMessage(msg, e.Session);
            }

            if (gm != null)
            {
                var rs = new EntityMessage();
                rs.Value = "收到" + gm;
                (sender as ISocketSession).Send(rs.GetStream());
            }
        }

        static Message OnReceiveMessage(Message msg, ISocketSession session)
        {
            // 测试，返回源消息
            if (msg.Kind == MessageKind.Null)
            {
                session.Send(msg.GetStream());
                return null;
            }

            // 组消息需要封包
            if (msg.Kind == MessageKind.Group)
            {
                var gmsg = msg as GroupMessage;

                MessageGroup mg = null;
                if (!groups.TryGetValue(gmsg.Identity, out mg))
                {
                    mg = new MessageGroup();
                    mg.Identity = gmsg.Identity;
                    groups.Add(mg.Identity, mg);
                }

                // 加入到组，如果返回false，表示未收到所有消息
                if (!mg.Add(gmsg)) return null;

                // 否则，表示收到所有消息
                groups.Remove(mg.Identity);

                // 读取真正的消息
                return mg.GetMessage();
            }
            return null;
        }
    }
}