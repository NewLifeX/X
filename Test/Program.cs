using System;
using System.Diagnostics;
using NewLife.Log;
using NewLife.PeerToPeer;
using System.Reflection;
using NewLife.PeerToPeer.Messages;
using NewLife.Reflection;
using NewLife.Net.Udp;
using NewLife.Net.Sockets;
using System.IO;
using NewLife.Messaging;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            XTrace.OnWriteLog += new EventHandler<WriteLogEventArgs>(XTrace_OnWriteLog);
            while (true)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
#if !DEBUG
                try
                {
#endif
                Test1();
                //ThreadPoolTest.Main2(args);
#if !DEBUG
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
#endif

                sw.Stop();
                Console.WriteLine("OK! 耗时 {0}", sw.Elapsed);
                ConsoleKeyInfo key = Console.ReadKey();
                if (key.Key != ConsoleKey.C) break;
            }
            //Console.ReadKey();
        }

        static void XTrace_OnWriteLog(object sender, WriteLogEventArgs e)
        {
            Console.WriteLine(e.ToString());
        }

        static void Test1()
        {
            PingMessage msg = new PingMessage();
            msg.Token = Guid.NewGuid();

            Type type = msg.GetType();

            PropertyInfo[] pis = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (PropertyInfo item in pis)
            {
                Console.WriteLine("{0} {1}", item.Name, item.GetValue(msg, null));

                //PropertyInfoX pi = PropertyInfoX.Create(item);
                //PropertyInfoX pi = PropertyInfoX.Create(type, "Token");
                //PropertyInfoX pi = item;
                //pi.GetValue(msg); // =msg.Token
                //pi.SetValue(msg, null);// msg.Token = null;
            }

            Console.WriteLine();
            FieldInfo[] fis = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (FieldInfo item in fis)
            {
                Console.WriteLine("{0} {1}", item.Name, item.GetValue(msg));
            }

            Console.WriteLine();
            MethodInfo[] methods = type.GetMethods();
            foreach (MethodInfo item in methods)
            {
                Console.WriteLine(item.Name);
            }
        }

        static void Test11()
        {
            //FastTest.Test();

            //UdpTest.Test();
            //TcpTest.Test();

            //ProtocolTest.Test();

            //P2PTest.TestTracker();
            P2PTest.TestClient();
            //P2PTest.TestMessage();
        }

        static void Test2()
        {
            UdpServer server = new UdpServer();
            server.Received += new EventHandler<NetEventArgs>(server_Received);
            server.Start();
        }

        static void server_Received(object sender, NetEventArgs e)
        {
            Stream stream = e.GetStream();
            Message msg = Message.Deserialize(stream);
        }
    }
}
