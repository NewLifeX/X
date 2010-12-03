using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NewLife.Net.Sockets;
using NewLife.Log;
using System.Threading;

namespace NewLife.Net.Udp
{
    /// <summary>
    /// UDP测试
    /// </summary>
    public static class UdpTest
    {
        /// <summary>
        /// 测试入口
        /// </summary>
        public static void Test()
        {
            Int32 port = 7;
            UdpServer server = new UdpServer(port);
            Dictionary<EndPoint, Int32> dic = new Dictionary<EndPoint, int>();
            server.Received += delegate(object sender, NetEventArgs e)
            {
                XTrace.WriteLine("服务端 收到{0}数据！{1}", e.RemoteEndPoint, e.GetString());
                lock (dic)
                {
                    if (!dic.ContainsKey(e.RemoteEndPoint)) dic.Add(e.RemoteEndPoint, 0);
                    dic[e.RemoteEndPoint]++;
                }

                if ((e.RemoteEndPoint as IPEndPoint).Address != IPAddress.Any)
                {
                    UdpServer us = sender as UdpServer;
                    us.Send("你的信息已收到！", e.RemoteEndPoint);
                }
            };
            server.Error += delegate(Object sender, NetEventArgs e)
            {
                if (e.SocketError != SocketError.Success)
                    Console.WriteLine("服务端 {0}错误 {1}", e.RemoteEndPoint, e.SocketError.ToString());
                else
                    Console.WriteLine("服务端 {0}断开！", e.RemoteEndPoint);
            };
            server.Start();
            Console.WriteLine("任意键开始……");
            ThreadPool.QueueUserWorkItem(delegate
            {
                while (true)
                {
                    Console.WriteLine("Pool[Create={0} Stock={1} NotStock={2}]", UdpServer.Pool.CreateCount, UdpServer.Pool.StockCount, UdpServer.Pool.NotStockCount);

                    Thread.Sleep(1000);
                }
            });
            Console.ReadKey(true);

            String msg = "测试数据！";
            Byte[] data = Encoding.UTF8.GetBytes(msg);
            IPEndPoint ip = new IPEndPoint(IPAddress.Any, 0);
            //List<SocketClient> list = new List<SocketClient>();
            for (int n = 0; n < 10000; n++)
            {
                UdpClientX client = new UdpClientX();
                //list.Add(client);
                client.Received += delegate(object sender, NetEventArgs e)
                {
                    //Console.WriteLine("客户端 收到{0}数据！{1}", e.RemoteEndPoint, e.GetString());
                };
                client.Error += delegate(Object sender, NetEventArgs e)
                {
                    if (e.SocketError != SocketError.Success)
                        Console.WriteLine("客户端 {0}错误 {1}", e.RemoteEndPoint, e.SocketError.ToString());
                    else
                        Console.WriteLine("客户端 {0}断开！", e.RemoteEndPoint);
                };
                client.Connect(IPAddress.Loopback, port);
                client.ReceiveAsync();

                for (int i = 0; i < 10; i++)
                {
                    client.Send((i + 1).ToString());
                    //client.Broadcast((i + 1).ToString(), port);
                    //client.Client.Receive(ref ip);
                    //String str = client.ReadString(out ip);
                    //Console.WriteLine("客户端 收到{0}数据 {1}", ip, str);
                    //Thread.Sleep(GetRnd(100, 2000));
                }
                //Thread.Sleep(100);
                client.Dispose();
            }

            Console.WriteLine("地址数：{0}", dic.Count);
            Console.WriteLine("任意键结束客户端！");
            Console.ReadKey(true);
            //foreach (SocketClient item in list)
            //{
            //    item.Close();
            //}

            //Int32 m = 0;
            //foreach (EndPoint item in dic.Keys)
            //{
            //    if (dic[item] < 10)
            //    {
            //        Console.WriteLine("{0} 不合格！", item);
            //        m++;
            //    }
            //}
            //Console.WriteLine("不合格地址数：{0}", m);
            Console.WriteLine("任意键结束服务端！");
            Console.ReadKey(true);

            server.Stop();

            GC.Collect();
        }

        static Int32 GetRnd(Int32 min, Int32 max)
        {
            return new Random((Int32)DateTime.Now.Ticks).Next(min, max);
        }
    }
}