using System;
using NewLife.IO;
using NewLife.Linq;
using NewLife.Log;
using NewLife.Net;
using NewLife.Net.P2P;
using NewLife.Net.Sockets;
using NewLife.Net.Udp;
using NewLife.Security;
using NewLife.Net.Application;
using System.Threading;
using NewLife.Net.Proxy;
using System.IO.Ports;
using NewLife.Net.DNS;
using NewLife.Net.ModBus;
using System.IO;
using System.Text;

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

        static void XTrace_OnWriteLog(object sender, WriteLogEventArgs e)
        {
            Console.WriteLine(e.ToString());
        }

        static void Test1()
        {
            Console.Write("名称：");
            String name = Console.ReadLine();

            Console.Write("协议（1为Tcp，其它为Udp）：");
            Int32 istcp = Convert.ToInt32(Console.ReadLine().Trim());

            P2PTest.StartClient(name, "jslswb.com", 15, istcp == 1);
            ////P2PTest.StartClient(name);

            //var hole = new IPEndPoint(NetHelper.ParseAddress("jslswb.com"), 15);
            //var client = new UdpClientX();
            ////var client = new UdpServer();
            //client.Bind();
            //client.Received += new EventHandler<NetEventArgs>(client_Received);
            //client.ReceiveAsync();
            ////client.Send("reg.gg", null, hole);
            //client.Send("test", null, hole);
            //hole = new IPEndPoint(hole.Address, hole.Port + 1);
            //client.Send("nnhy", null, hole);
            //Console.WriteLine("监听：{0}", client.LocalEndPoint);

            //Thread.Sleep(1000);

            //Int32 port = client.LocalEndPoint.Port;
            //client.Dispose();

            //Thread.Sleep(1000);

            //client = new UdpClientX();
            //client.Port = port;
            //client.Bind();
            //client.Received += new EventHandler<NetEventArgs>(client_Received);
            //client.ReceiveAsync();
            //client.Send("reg.hh", null, hole);
            //Console.WriteLine("监听：{0}", client.LocalEndPoint);
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

        static String pname = null;
        static void Test3()
        {
            //Console.WriteLine("任意键开始测试：");
            //Console.ReadKey(true);

            //Console.WriteLine("点动开始");

            //Byte crc = 0x12;
            //crc = (Byte)~crc;
            //Console.WriteLine(crc);

            Console.Write("发现串口：");
            foreach (var item in SerialPort.GetPortNames())
            {
                if (pname == null) pname = item;
                Console.Write(" " + item);
            }
            Console.WriteLine();

            Read(0x0401, 2);
            Write(0x0101, 5);

            Console.WriteLine("开始读取寄存器状态：");
            for (int i = 0; i < 1; i++)
            {
                for (int j = 0; j < 19; j++)
                {
                    Console.Write("P{0}{1:00}=", (Char)('A' + i), j);
                    UInt16 r = 0;
                    try
                    {
                        r = Read((UInt16)((i + 1) * 0x100 + j));
                    }
                    catch { }
                    Console.WriteLine(r);
                }
            }

            Console.WriteLine("点动开始");
            Write(0x1000, 1);

            Console.WriteLine("伺服状态：");
            UInt16 rs = 0;
            try
            {
                rs = Read(0x1001);
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
                    Console.WriteLine("未知！");
                    break;
            }

            Console.WriteLine("点动结束");
            Write(0x1000, 0);
        }

        static void Write(UInt16 addr, UInt16 data)
        {
            var msg = new WriteRegister();
            msg.IsAscii = true;
            msg.UseAddress = true;
            msg.Address = 0;
            msg.Function = MBFunction.WriteSingleRegister;
            msg.DataAddress = addr;
            msg.Data = data;

            var dt = msg.GetStream().ReadBytes();
            if (msg.IsAscii)
                Console.WriteLine("发送：{0}", Encoding.ASCII.GetString(dt));
            else
                Console.WriteLine("发送：{0}", BitConverter.ToString(dt));

            using (var sp = new SerialPort(pname))
            {
                sp.Open();
                //sp.DiscardOutBuffer();
                sp.Write(dt, 0, dt.Length);
            }
        }

        static UInt16 Read(UInt16 addr, UInt16 len = 1)
        {
            var msg = new ReadRegister();
            msg.IsAscii = true;
            msg.UseAddress = true;
            msg.Address = 0;
            msg.Function = MBFunction.ReadHoldingRegisters;
            msg.DataAddress = addr;
            msg.DataLength = len;

            var dt = msg.GetStream().ReadBytes();
            if (msg.IsAscii)
                Console.WriteLine("发送：{0}", Encoding.ASCII.GetString(dt));
            else
                Console.WriteLine("发送：{0}", BitConverter.ToString(dt));

            using (var sp = new SerialPort(pname))
            {
                sp.Open();
                //sp.DiscardInBuffer();
                //sp.DiscardOutBuffer();
                sp.Write(dt, 0, dt.Length);

                dt = new Byte[100];
                Int32 i = 0;
                Thread.Sleep(500);
                while (i < dt.Length && sp.BytesToRead > 0)
                {
                    var count = sp.Read(dt, i, dt.Length - i);
                    i += count;
                    if (i >= dt.Length) break;
                    Thread.Sleep(1000);
                }
                if (i <= 0) return 0;

                //Console.WriteLine("收到数据：{0}", i);
                Console.WriteLine("接收：{0}", BitConverter.ToString(dt));

                var ms = new MemoryStream(dt, 0, i);
                var rs = MBEntity.Read<ReadRegisterResponse>(ms);
                if (rs != null)
                {
                    dt = rs.Data;
                    Array.Reverse(dt);
                    return BitConverter.ToUInt16(dt, 0);
                }
                return 0;
            }
        }
    }
}