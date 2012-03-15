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
                Console.Clear();
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

        /*
        送别
        歌手:青燕子演唱组
        专辑:森林和原野
        作词:李叔同(弘一大师)
        */
        const Int32 ONE_BEEP = 600;
        const Int32 HALF_BEEP = 300;
        //{
        //NOTE_1 = 440;
        //NOTE_2 = 495;
        //NOTE_3 = 550;
        //NOTE_4 = 587;
        //NOTE_5 = 660;
        //NOTE_6 = 733;
        //NOTE_7 = 825;
        //}
        const Int32 NOTE_1 = 440 * 1;
        const Int32 NOTE_2 = 495 * 1;
        const Int32 NOTE_3 = 550 * 1;
        const Int32 NOTE_4 = 587 * 1;
        const Int32 NOTE_5 = 660 * 1;
        const Int32 NOTE_6 = 733 * 1;
        const Int32 NOTE_7 = 825 * 1;

        static void Test3()
        {
            //长亭外  
            Beep(NOTE_5, ONE_BEEP);
            Beep(NOTE_3, HALF_BEEP);
            Beep(NOTE_5, HALF_BEEP);
            Beep(NOTE_1 * 2, ONE_BEEP * 2);

            //古道边  
            Beep(NOTE_6, ONE_BEEP);
            Beep(NOTE_1 * 2, ONE_BEEP);
            Beep(NOTE_5, ONE_BEEP * 2);

            //芳草碧连天  
            Beep(NOTE_5, ONE_BEEP);
            Beep(NOTE_1, HALF_BEEP);
            Beep(NOTE_2, HALF_BEEP);
            Beep(NOTE_3, ONE_BEEP);
            Beep(NOTE_2, HALF_BEEP);
            Beep(NOTE_1, HALF_BEEP);
            Beep(NOTE_2, ONE_BEEP * 4);

            //晚风扶柳笛声残  
            Beep(NOTE_5, ONE_BEEP);
            Beep(NOTE_3, HALF_BEEP);
            Beep(NOTE_5, HALF_BEEP);
            Beep(NOTE_1 * 2, HALF_BEEP * 3);
            Beep(NOTE_7, HALF_BEEP);
            Beep(NOTE_6, ONE_BEEP);
            Beep(NOTE_1 * 2, ONE_BEEP);
            Beep(NOTE_5, ONE_BEEP * 2);

            //夕阳山外山  
            Beep(NOTE_5, ONE_BEEP);
            Beep(NOTE_2, HALF_BEEP);
            Beep(NOTE_3, HALF_BEEP);
            Beep(NOTE_4, HALF_BEEP * 3);
            Beep((Int32)Math.Round((Double)NOTE_7 / 2), HALF_BEEP);
            Beep(NOTE_1, ONE_BEEP * 4);

            //天之涯  
            Beep(NOTE_6, ONE_BEEP);
            Beep(NOTE_1 * 2, ONE_BEEP);
            Beep(NOTE_1 * 2, ONE_BEEP * 2);

            //地之角  
            Beep(NOTE_7, ONE_BEEP);
            Beep(NOTE_6, HALF_BEEP);
            Beep(NOTE_7, HALF_BEEP);
            Beep(NOTE_1 * 2, ONE_BEEP * 2);

            //知交半零落  
            Beep(NOTE_6, HALF_BEEP);
            Beep(NOTE_7, HALF_BEEP);
            Beep(NOTE_1 * 2, HALF_BEEP);
            Beep(NOTE_6, HALF_BEEP);
            Beep(NOTE_6, HALF_BEEP);
            Beep(NOTE_5, HALF_BEEP);
            Beep(NOTE_3, HALF_BEEP);
            Beep(NOTE_1, HALF_BEEP);
            Beep(NOTE_2, ONE_BEEP * 4);

            //一壶浊酒尽余欢  
            Beep(NOTE_5, ONE_BEEP);
            Beep(NOTE_3, HALF_BEEP);
            Beep(NOTE_5, HALF_BEEP);
            Beep(NOTE_1 * 2, HALF_BEEP * 3);
            Beep(NOTE_7, HALF_BEEP);
            Beep(NOTE_6, ONE_BEEP);
            Beep(NOTE_1 * 2, ONE_BEEP);
            Beep(NOTE_5, ONE_BEEP * 2);

            //今宵别梦寒  
            Beep(NOTE_5, ONE_BEEP);
            Beep(NOTE_2, HALF_BEEP);
            Beep(NOTE_3, HALF_BEEP);
            Beep(NOTE_4, HALF_BEEP * 3);
            Beep((Int32)Math.Round((Double)NOTE_7 / 2), HALF_BEEP);
            Beep(NOTE_1, ONE_BEEP * 3);

            Thread.Sleep(2);
            // 播放  生日快乐
            Int32[] FREQUENCY = {392,392,440,392,523,494,  

              392,392,440,392,587,523,  

              392,392,784,659,523,494,440,  

              689,689,523,587,523};



            Int32[] DELAY = {375,125,500,500,500,1000,  

              375,125,500,500,500,1000,  

              375,125,500,500,500,500,1000,  

              375,125,500,500,500,1000};

            for (int i = 0; i < FREQUENCY.Length; i++)
            {
                Console.Beep(FREQUENCY[i], DELAY[i]);
            }
        }

        static void Beep(int frequency, int duration)
        {
            Console.Beep(frequency, duration);
        }
    }
}