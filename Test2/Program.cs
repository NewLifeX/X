using System;
using System.Collections.Generic;
using System.Threading;
using NewLife.Linq;
using NewLife.Log;
using NewLife.Net;
using NewLife.Net.Application;
using NewLife.Net.Sockets;
using NewLife.Net.Tcp;
using System.Diagnostics;

namespace Test2
{
    class Program
    {
        static void Main(string[] args)
        {
            XTrace.OnWriteLog += new EventHandler<WriteLogEventArgs>(XTrace_OnWriteLog);
            while (true)
            {
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

                Console.WriteLine("OK!");
                ConsoleKeyInfo key = Console.ReadKey();
                if (key.Key != ConsoleKey.C) break;
            }
        }

        static void XTrace_OnWriteLog(object sender, WriteLogEventArgs e)
        {
            Console.WriteLine(e.ToString());
        }

        static void Test1()
        {
            Console.Write("工作模式（1服务端，2客户端）：");
            Char cmd = Console.ReadKey().KeyChar;
            Console.WriteLine();

            if (cmd == '1')
                TestServer();
            else if (cmd == '2')
            {
                //TestClient();
                //for (int i = 0; i < 10000; i++)
                {
                    TestClient();
                }
            }
        }

        //static List<Int32> Ports = new List<int>();

        static void TestServer()
        {
            //Type[] ts = new Type[] { typeof(ChargenServer), typeof(DaytimeServer), typeof(DiscardServer), typeof(EchoServer), typeof(TimeServer) };
            Type[] ts = new Type[] { typeof(EchoServer) };
            List<NetServer> list = new List<NetServer>();
            foreach (Type item in ts)
            {
                NetServer server = Activator.CreateInstance(item) as NetServer;
                server.Start();
                server.Servers.ForEach(s => s.UseThreadPool = false);
                //Ports.Add(server.Port);
                list.Add(server);
            }

            NetHelper.Debug = false;

            while (true)
            {
                Thread.Sleep(3000);

                //String cmd = Console.ReadLine();
                //if (String.IsNullOrEmpty(cmd)) continue;
                //if (cmd.EqualIgnoreCase("exit")) break;

                Console.WriteLine();
                Console.WriteLine(DateTime.Now);
                Console.WriteLine("{0,10} {1,4} {2,4} {3,4} {4,4}", "名称", "会话", "每分", "最大", "每小时");
                foreach (var item in list)
                {
                    var server = item.Server as TcpServer;
                    Console.WriteLine("{0,10}:{1,6:n0} {2,6:n0} {3,6:n0} {4,6:n0}", item.Name, server.Sessions.Count, server.TotalPerMinute, server.MaxPerMinute, server.TotalPerHour);
                }
            }

            list.ForEach(s => s.Dispose());
        }

        static void TestClient()
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Idle;

            Random rnd = new Random((Int32)DateTime.Now.Ticks);

            String host = "127.0.0.1";
            //Int32[] ports = new Int32[] { 19, 13, 9, 7, 37 };
            Int32[] ports = new Int32[] { 7 };

            Thread[] ths = new Thread[10];
            for (int i = 0; i < ths.Length; i++)
            {
                String name = String.Format("Test_{0}", (i + 1).ToString("00000"));
                Console.WriteLine("准备启动 {0}", name);

                ths[i] = new Thread(TestClient_One);
                ths[i].Name = name;
                ths[i].Priority = ThreadPriority.BelowNormal;
                ths[i].Start(new Object[] { i + 1, host, ports[rnd.Next(0, ports.Length)] });
                Thread.Sleep(100);
            }
        }

        static void TestClient_One(Object state)
        {
            Object[] objs = (Object[])state;
            Int32 id = (Int32)objs[0];
            String host = (String)objs[1];
            Int32 port = (Int32)objs[2];

            // 开10个Tcp连接
            TcpClientX[] ts = new TcpClientX[1000];
            try
            {
                for (int i = 0; i < ts.Length; i++)
                {
                    ts[i] = new TcpClientX();
                    //ts[i].rec
                    //while (true)
                    {
                        //try
                        {
                            ts[i].Connect(host, port);
                            //break;
                        }
                        //catch { continue; }
                    }
                    Thread.Sleep(100);
                }

                Random rnd = new Random((Int32)DateTime.Now.Ticks);

                // 发送
                Int32 count = rnd.Next(2, 10);
                for (int i = 0; i < count; i++)
                {
                    if (i > 0) Thread.Sleep(500);

                    String msg = String.Format("Test_{0}_{1}", id, i);
                    foreach (var item in ts)
                    {
                        if (item != null) item.Send(msg);
                    }
                }

                // 销毁
                Thread.Sleep(5000);
            }
            catch (Exception ex) { Console.WriteLine(ex.ToString()); }
            //finally
            //{
            //    ts.ForEach(t => t.Dispose());
            //}

            Console.WriteLine("Test_{0} 完成！", id);
        }
    }
}