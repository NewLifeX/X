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
using System.Net;
using System.Net.Sockets;

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
                TestClient();
            }
        }

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

#if !DEBUG
            NetHelper.Debug = false;
#endif

            while (true)
            {
                Thread.Sleep(3000);

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
            Console.Write("目标地址（默认127.0.0.1，回车表示默认）：");
            String cmd = Console.ReadLine();
            String host = String.IsNullOrEmpty(cmd) ? "127.0.0.1" : cmd;

            Console.Write("任务数（默认1000，回车表示默认）：");
            cmd = Console.ReadLine();
            if (String.IsNullOrEmpty(cmd) || !Int32.TryParse(cmd, out total)) total = 1000;

            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Idle;

            Random rnd = new Random((Int32)DateTime.Now.Ticks);

            //Int32[] ports = new Int32[] { 19, 13, 9, 7, 37 };
            Int32[] ports = new Int32[] { 7 };

            Thread[] ths = new Thread[10 * Environment.ProcessorCount];
            success = 0;
            unsuccess = 0;
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

            Int32 t = ths.Length * total;
            while (true)
            {
                Thread.Sleep(3000);

                var err = Error;
                Error = null;
                Console.WriteLine("成功：{0,5:n0} 失败：{1,5:n0} {2}", success, unsuccess, err != null ? err.Message : null);

                if (success + unsuccess >= t) break;
            }
            Console.WriteLine("全部完成，成功率：{0:p}", (Double)success / t);
        }

        static Int32 total;
        static Int32 success;
        static Int32 unsuccess;
        static Exception Error;

        static void TestClient_One(Object state)
        {
            Object[] objs = (Object[])state;
            Int32 id = (Int32)objs[0];
            String host = (String)objs[1];
            Int32 port = (Int32)objs[2];
            IPEndPoint ep = new IPEndPoint(NetHelper.ParseAddress(host), port);

            for (int i = 0; i < total; i++)
            {
                var client = new TcpClientX();
                client.Completed += new EventHandler<NetEventArgs>(client_Completed);
                //client.Connect(host, port);
                var e = client.Pop();
                e.RemoteEndPoint = ep;

                e.UserToken = id * 1000000 + i + 1;

                client.Client.ConnectAsync(e);
                Thread.Sleep(100);
            }
        }

        static void client_Completed(object sender, NetEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                unsuccess++;
                return;
            }
            if (e.LastOperation == SocketAsyncOperation.Connect)
            {
                var client = sender as TcpClientX;
                Int32 num = (Int32)e.UserToken;
                Int32 i = num % 1000000;
                Int32 id = num / 1000000;
                String msg = String.Format("Test_{0}_{1}", id, i);

                try
                {
                    client.Send(msg);
                    if (msg == client.ReceiveString())
                        success++;
                    else
                        unsuccess++;
                }
                catch (Exception ex)
                {
                    Error = ex;
                    unsuccess++;
                }

                e.Cancel = true;
            }
        }
    }
}