using System;
using System.IO.Ports;
using System.Threading;
using NewLife.Log;
using NewLife.Net.Application;
using NewLife.Net.ModBus;
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
                    Test1();
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

        static NetServer server;
        static SerialServer server2;
        static NATProxy proxy;
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

        static String pname;
        static void Test3()
        {
            //NewLife.Net.Application.AppTest.StartClient();

            //Console.ReadKey(true);
            //using (var sp = new SerialPort("COM1"))
            //{
            //    sp.Open();
            //    var dt = new Byte[] { 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA };
            //    for (int i = 0; i < 10000; i++)
            //    {
            //        //var dt = i % 2 == 0 ? new Byte[] { 0 } : new Byte[] { 1 };
            //        //Console.WriteLine(dt[0]);
            //        //sp.Write(dt, 0, dt.Length);
            //        //Console.WriteLine(i);
            //        sp.Write(dt, 0, dt.Length);
            //        //Thread.Sleep(100);
            //    }
            //}

            Console.WriteLine("任意键开始测试：");
            Console.ReadKey(true);

            Console.Write("发现串口：");
            foreach (var item in SerialPort.GetPortNames())
            {
                if (pname == null) pname = item;
                Console.Write(" " + item);
            }
            Console.WriteLine();

            Byte host = 1;

            // 01 08 00 00 12 AB AD 14
            var isonline = ReadTest(host);
            Console.WriteLine("连接状态：{0}", isonline);
            if (!isonline) return;

            //Console.WriteLine("内部伺服使能");
            //Write(host, 0x0103, 1);

            //Console.WriteLine("开始读取寄存器状态：");
            //for (int i = 0; i < 4; i += 2)
            //{
            //    for (int j = i * 10; j < 5 + i * 10; j++)
            //    {
            //        Console.Write("P{0}{1:00}=", (Char)('A' + i), j);
            //        Int16 r = 0;
            //        try
            //        {
            //            r = Read(host, (UInt16)((i + 1) * 0x100 + j));
            //        }
            //        catch { }
            //        Console.WriteLine(r);
            //    }
            //}
            ShowStatus(host);

            //Console.WriteLine("任意键停机...");
            //Console.ReadKey(true);

            Console.WriteLine("停机");
            Write(host, 0x0103, 0);

            Write(host, 0x1000, 4);
            Write(host, 0x1000, 0);
            Console.WriteLine("任意键开始点动测试...");
            Console.ReadKey(true);

            Console.WriteLine("10次点动开始");
            Write(host, 0x1000, 1);
            for (int i = 0; i < 10; i++)
            {
                Write(host, 0x1000, 2);
                Thread.Sleep(1000);
                ShowStatus(host);
                Write(host, 0x1000, 4);
                Thread.Sleep(2000);
                ShowStatus(host);
            }

            //Thread.Sleep(3000);
            Console.WriteLine("任意键结束点动测试");
            Console.ReadKey(true);

            Console.WriteLine("点动结束");
            Write(host, 0x1000, 0);

            ShowStatus(host);
        }

        static void ShowStatus(Byte host)
        {
            Console.WriteLine("伺服状态：");
            Int16 rs = 0;
            try
            {
                rs = Read(host, 0x1001);
            }
            catch (Exception ex)
            {
                Console.WriteLine("错误！" + ex.ToString());
            }
            switch (rs)
            {
                case 1:
                    Console.WriteLine("正转运行中");
                    break;
                case 2:
                    Console.WriteLine("反转运行中");
                    break;
                case 3:
                    Console.WriteLine("伺服驱动器待机中");
                    break;
                case 4:
                    Console.WriteLine("故障中");
                    break;
                default:
                    Console.WriteLine("未知！" + rs);
                    break;
            }
        }

        static void Write(Byte host, UInt16 addr, UInt16 data)
        {
            var msg = new WriteRegister();
            msg.Address = host;
            msg.Function = MBFunction.WriteSingleRegister;
            msg.DataAddress = addr;
            msg.Data = data;

            var rs = MBEntity.Process(msg, null, pname);
        }

        static Int16 Read(Byte host, UInt16 addr, UInt16 len = 1)
        {
            var msg = new ReadRegister();
            msg.Address = host;
            msg.Function = MBFunction.ReadHoldingRegisters;
            msg.DataAddress = addr;
            msg.DataLength = len;

            var rs = msg.Process<ReadRegisterResponse>(null, pname);
            if (rs != null) return (Int16)rs.WordData;

            return -1;
        }

        static Boolean ReadTest(Byte host)
        {
            var msg = new Diagnostics();
            msg.Address = host;
            msg.SubFunction = 0;
            msg.Data = 0x12AB;

            var rs = msg.Process<Diagnostics>(null, pname);
            return rs != null && rs.SubFunction == msg.SubFunction && rs.Data == msg.Data;
        }
    }
}