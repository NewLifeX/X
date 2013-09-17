using System;
using System.Threading;
using NewLife.Log;
using NewLife.Net.Application;
using NewLife.Net.Modbus;
using NewLife.Net.Proxy;
using NewLife.Net.Sockets;
using NewLife.Net.Udp;
using NewLife.Security;

namespace Test2
{
    class Program
    {
        static void Main(string[] args)
        {
            XTrace.UseConsole();
            while (true)
            {
#if !DEBUG
                try
                {
#endif
                    Test3();
#if !DEBUG
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
#endif

                Console.WriteLine("OK!");
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key != ConsoleKey.C) break;
                //Console.Clear();
            }
        }

        private static void Test1()
        {
            NewLife.Net.Application.AppTest.TcpConnectionTest();
        }

        static void client_Received(object sender, NetEventArgs e)
        {
            Console.WriteLine("HoleServer数据到来：{0} {1}", e.RemoteIPEndPoint, e.GetString());
        }

        static SerialServer server2;
        static void Test2()
        {
            //var dns = new DNSServer();
            //dns.Parent = "tcp://8.8.8.8,udp://4.4.4.4";
            //Console.WriteLine(dns.Parent);

            //NetHelper.Debug = true;
            //if (server == null)
            //{
            //    server = new NetServer();
            //    server.Port = 23;
            //    server.Received += new EventHandler<NetEventArgs>(server_Received);
            //    server.Start();
            //}
            if (server2 == null)
            {
                server2 = new SerialServer() { PortName = "COM1" };
                server2.Port = 24;
                server2.AutoClose = true;
                server2.ReceivedBytesThreshold = 34;
                server2.Start();
            }
            //if (proxy == null)
            //{
            //    proxy = new NATProxy("192.168.1.10", 24);
            //    proxy.Port = 24;
            //    proxy.Start();
            //}

            //Thread.Sleep(2000);

            String[] ss = new String[] { 
                "7E 2F 44 9D 10 01 01 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 22 01 0D", 
                //"7E 2F 44 8C 10 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 0F 01 0D", 
                //"7E 2F 44 F2 10 01 00 40 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 B6 01 0D", 
                //"7E 2F 44 81 10 99 02 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 9F 01 0D", 
                //"7E 2F 44 F1 10 1A 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 8E 01 0D", 
                //"7E 2F 44 F1 10 12 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 86 01 0D"
            };

            String str = "7E 2F 44 82 10 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 05 01 0D";

            using (var client = new UdpClientX())
            {
                client.Connect("192.168.1.10", 24);
                //for (int i = 0; i < ss.Length; i++)
                //{
                //    var data = DataHelper.FromHex(ss[i].Replace(" ", null));
                //    client.Send(data, 0, data.Length);
                //}
                var data = DataHelper.FromHex(str.Replace(" ", null));
                client.Send(data, 0, data.Length);
                data = client.Receive();

                Console.WriteLine(data.ToHex());
            }

            //Thread.Sleep(2000);

            //using (var client = new UdpClientX())
            //{
            //    client.Connect(NetHelper.GetIPs().First(), 24);
            //    for (int i = 0; i < ss.Length; i++)
            //    {
            //        var data = DataHelper.FromHex(ss[i].Replace(" ", null));
            //        client.Send(data, 0, data.Length);
            //    }
            //}

            //Thread.Sleep(20000);

            //using (var sp = new SerialPort("COM3"))
            //{
            //    sp.Open();
            //    Console.WriteLine(sp.ReadExisting());
            //}
        }

        static void server_Received(object sender, NetEventArgs e)
        {
            Console.WriteLine("{1}收到：{0}", DataHelper.ToHex(e.Buffer, e.Offset, e.BytesTransferred), e.Socket.ProtocolType);
            var session = e.Session;

            if (e.Buffer.StartsWith(new Byte[] { 0xFF, 0xFA })) return;

            Thread.Sleep(1000);
            //session.Send(e.Buffer, e.Offset, e.BytesTransferred, e.RemoteEndPoint);
        }

        static void Test3()
        {
            var ts = new SerialTransport();
            ts.PortName = "COM17";

            var ds = new DataStore();
            ds.Coils.OnWrite += Coils_OnWrite;
            ds.HoldingRegisters.OnWrite += HoldingRegisters_OnWrite;

            var slave = new ModbusSlave();
            //slave.EnableDebug = true;
            slave.Transport = ts;
            slave.DataStore = ds;
            slave.Listen();
        }

        static void HoldingRegisters_OnWrite(int i, int value)
        {
            Console.WriteLine("WriteReg ({0}, {1})", i, value);
        }

        static void Coils_OnWrite(int i, bool value)
        {
            Console.WriteLine("WriteCoil({0}, {1})", i, value);
        }
    }
}