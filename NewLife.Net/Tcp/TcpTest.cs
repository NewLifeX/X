using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using NewLife.Net.Sockets;
using NewLife.Log;

namespace NewLife.Net.Tcp
{
    /// <summary>
    /// Tcp测试
    /// </summary>
    public static class TcpTest
    {
        /// <summary>
        /// 开始测试
        /// </summary>
        public static void Test()
        {
            //MaxTest();
            //return;

            Int32 port = 7;
            TcpServer server = new TcpServer(port);
            server.NoDelay = false;
            server.UseThreadPool = true;
            server.Accepted += new EventHandler<NetEventArgs>(server_Accepted);
            server.Error += delegate(Object sender, NetEventArgs e)
            {
                if (e.SocketError != SocketError.Success)
                    XTrace.WriteLine("服务端 {0}错误 {1}", e.RemoteEndPoint, e.SocketError.ToString());
                else
                    XTrace.WriteLine("服务端 {0}断开！", e.RemoteEndPoint);
            };
            server.Start();
            Console.WriteLine("任意键开始……");
            ThreadPool.QueueUserWorkItem(delegate
            {
                while (true)
                {
                    Console.WriteLine("Pool[Create={0} Stock={1} NotStock={2} Sessions={3}]", TcpServer.Pool.CreateCount, TcpServer.Pool.StockCount, TcpServer.Pool.NotStockCount, server.Sessions.Count);

                    Thread.Sleep(1000);
                }
            });
            Console.ReadKey(true);

            MaxTest();

            //String msg = "测试数据！";
            //Byte[] data = Encoding.UTF8.GetBytes(msg);
            //IPEndPoint ip = new IPEndPoint(IPAddress.Any, 0);
            //List<SocketClient> list = new List<SocketClient>();
            //for (int n = 0; n < 100; n++)
            //{
            //    TcpClientEx client = new TcpClientEx();
            //    list.Add(client);
            //    SetEvent(client, false);
            //    client.Connect(IPAddress.Loopback, port);
            //    //client.Connect("192.168.1.101", port);
            //    //client.Connect("192.168.0.100", port);
            //    client.ReceiveAsync();

            //    client.Send("Hello");
            //    //client.Send("我来啦！");
            //    //for (int i = 0; i < 10; i++)
            //    //{
            //    //    client.Send((i + 1).ToString());
            //    //String str = client.ReceiveString();
            //    //Console.WriteLine("客户端 收到{0}数据 {1}", client.RemoteEndPoint, str);
            //    //}
            //    //Thread.Sleep(1000);
            //    //client.Close();
            //}

            //Console.WriteLine("任意键结束客户端！");
            //Console.ReadKey(true);
            //foreach (SocketClient item in list)
            //{
            //    item.Close();
            //}

            Console.WriteLine("任意键停止服务…… {0}", server.Sessions.Count);
            Console.ReadKey(true);
            server.Close();
        }

        static void server_Accepted(object sender, NetEventArgs e)
        {
            TcpSession session = e.UserToken as TcpSession;
            if (session == null) return;

            //if (e.AcceptSocket != null)
            //    Console.WriteLine("{1} 新连接 {0}", e.AcceptSocket.RemoteEndPoint, session.ID);
            //else
            if (e.AcceptSocket == null)
                Console.WriteLine("奇怪");

            //SetEvent(session, true);

            //XTrace.WriteLine(e.GetString());
            //session.Send("欢迎！");
            session.Send(e.GetString());

            session.Push(e);
            session.Close();
        }

        static void SetEvent(TcpClientX session, Boolean isServer)
        {
            String name = isServer ? "服务端" + (session as TcpSession).ID : "客户端";
            session.Received += delegate(Object sender, NetEventArgs e)
            {
                //String address = e.AcceptSocket == null ? "" : e.AcceptSocket.RemoteEndPoint.ToString();
                //XTrace.WriteLine("{2} 收到{0}数据！[{3}]{1}", e.RemoteEndPoint, e.GetString(), name, e.BytesTransferred);

                if (isServer)
                {
                    TcpClientX tc = sender as TcpClientX;
                    tc.Send("你的信息已收到！");
                }
            };
            session.Error += delegate(Object sender, NetEventArgs e)
            {
                //if (e.SocketError != SocketError.Success)
                //    XTrace.WriteLine("{2} {0}错误 {1}", e.RemoteEndPoint, e.SocketError, name);
                //else if (e.UserToken is Exception)
                //    XTrace.WriteLine("{2} {0}错误 {1}", e.RemoteEndPoint, e.UserToken, name);
                ////else
                ////    XTrace.WriteLine("{1} {0}断开！", e.RemoteEndPoint, name);
            };
        }

        static void MaxTest()
        {
            for (int i = 0; i < 100; i++)
            {
                Thread t = new Thread(MaxTest2);
                t.Name = "MaxTest" + i;
                t.Start(i);
            }
        }

        static void MaxTest2(Object state)
        {
            Int32 n = (Int32)state;
            Int32 err = 0;

            for (int i = 0; i < 2000; i++)
            {
                TcpClientX client = new TcpClientX();
                SetEvent(client, false);
                try
                {
                    client.Connect(IPAddress.Loopback, 7);
                    //client.Connect("192.168.1.101", 7);
                    client.ReceiveAsync();

                    client.Send(String.Format("Hello {0} {1}", n, i));
                }
                catch //(Exception ex)
                {
                    //Console.WriteLine("i={0}", i);
                    //Console.WriteLine(ex.ToString());
                    err++;
                    //break;
                }
            }

            Thread.Sleep(10000);
            if (err > 0) Console.WriteLine("线程{0}，失败{1}", n, err);
        }
    }
}