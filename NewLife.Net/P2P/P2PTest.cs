using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Net.Sockets;
using NewLife.Net.Tcp;
using System.Threading;

namespace NewLife.Net.P2P
{
    /// <summary>P2P测试</summary>
    public static class P2PTest
    {
        /// <summary>开始</summary>
        public static void StartHole()
        {
            var server = new HoleServer();
            server.Port = 15;
            server.Start();
        }

        public static void StartClient(String name, String server, Int32 serverport, Int32 port)
        {
            var client = new TcpClientX();
            client.Port = port;
            client.Connect(server, serverport);

            client.Send("reg:" + name);
            Console.WriteLine(client.ReceiveString());

            client.Received += new EventHandler<NetEventArgs>(client_Received);
            client.ReceiveAsync();

            while (true)
            {
                String cmd = Console.ReadLine();
                if (cmd == "exit") return;

                client.Send(cmd);
            }
        }

        static void client_Received(object sender, NetEventArgs e)
        {
            String cmd = e.GetString();
            Console.WriteLine(e.RemoteIPEndPoint + " " + cmd);

            var ss = cmd.Split(":");
            ss[0] = ss[0].ToLower();
            if (ss[0] == "invite")
            {
                Console.WriteLine("收到邀请，断开本地");

                var port = e.Socket.LocalEndPoint.Port;
                e.Socket.Dispose();

                Random rnd = new Random((Int32)DateTime.Now.Ticks);
                Thread.Sleep(rnd.Next(100, 1000));
                Console.WriteLine("准备连接 {0}:{1}", ss[1], ss[2]);

                var client = new TcpClientX();
                client.Port = port;
                client.Connect(ss[1], Int32.Parse(ss[2]));
                client.Received += new EventHandler<NetEventArgs>(client_Received2);
                client.ReceiveAsync();

                Console.WriteLine("连接完成，准备发送消息");

                client.Send("Hello");
            }
        }

        static void client_Received2(object sender, NetEventArgs e)
        {
            String cmd = e.GetString();
            Console.WriteLine(e.RemoteIPEndPoint + " " + cmd);
        }
    }
}