using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using NewLife.Log;
using NewLife.Messaging;
using NewLife.Net.Sockets;
using NewLife.Net.Udp;
using NewLife.PeerToPeer.Messages;
using NewLife.Reflection;
using NewLife.IO;

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
                Test5();
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
            //P2PTest.TestClient();
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

        static void Test3()
        {

            List<AssemblyX> list = AssemblyX.GetAssemblies();
            //AssemblyX.ReflectionOnlyLoad();
            //List<AssemblyX> list = AssemblyX.ReflectionOnlyGetAssemblies();
            ////Console.WriteLine(list[13].Title);
            ////Int32 m = Administrator.Meta.Count;

            //foreach (AssemblyX item in list)
            //{
            //    Console.WriteLine(item.ToString());

            //    ListX<TypeX> list2 = item.FindPlugins<IEntity>();
            //    Console.WriteLine(list2 == null);
            //    if (list2 != null) Console.WriteLine(list2[0].Description);

            //    //Console.WriteLine("类型：");
            //    //foreach (TypeX type in item.TypeXs)
            //    //{
            //    //    Console.WriteLine("{0} 方法：", type);
            //    //    if (type.Methods != null)
            //    //    {
            //    //        foreach (MethodInfo elm in type.Methods)
            //    //        {
            //    //            Console.WriteLine(elm.Name);
            //    //        }
            //    //    }
            //    //}
            //}
            //return;

            //list = AssemblyX.GetAssemblies();

            AssemblyX asm = list[9];

            //MemberInfoX member2 = MethodInfoX.Create(typeof(Console), "WriteLine");
            //member2.Invoke(null, new Object[] { 1, 2 });

            MemberInfoX member2 = MethodInfoX.Create(typeof(AssemblyX), "Change");
            Console.WriteLine(member2.Invoke(asm, new Object[] { "aaasss" }));

            //{
            //    TypeX type = TypeX.Create(typeof(ConsoleKeyInfo));
            //    Object obj = type.CreateInstance();

            //    type = TypeX.Create(typeof(Byte[]));
            //    Byte[] buffer = (Byte[])type.CreateInstance(567);

            //    type = TypeX.Create(typeof(MemoryStream));
            //    MemoryStream ms = type.CreateInstance(buffer) as MemoryStream;
            //    Console.WriteLine(ms.Length);
            //}

            //{
            //    MemberInfoX member = ConstructorInfoX.Create(typeof(MemoryStream));
            //    Object obj = member.CreateInstance();

            //    //member = ConstructorInfoX.Create(typeof(IPAddress[]));
            //    // obj = member.CreateInstance(null);

            //    member = ConstructorInfoX.Create(typeof(Byte[]));
            //    //Byte[] buffer = new Byte[1024];
            //    Byte[] buffer = (Byte[])member.CreateInstance(567);
            //    member = ConstructorInfoX.Create(typeof(MemoryStream), new Type[] { typeof(Byte[]) });
            //    MemoryStream ms = member.CreateInstance(buffer) as MemoryStream;
            //    Console.WriteLine(ms.Length);
            //}

            //{
            //    MemoryStream ms = new MemoryStream();
            //}
            //{
            //    MemberInfoX member = ConstructorInfoX.Create(typeof(MemoryStream));
            //    MemoryStream ms = member.CreateInstance(null) as MemoryStream;
            //}
            //{
            //    MemoryStream ms = Activator.CreateInstance(typeof(MemoryStream)) as MemoryStream;
            //}

            Int32 n = 1000000;

            Stopwatch sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < n; i++)
            {
                MemoryStream ms = new MemoryStream();
            }
            sw.Stop();
            Console.WriteLine(sw.Elapsed);

            sw.Reset();
            sw.Start();
            {
                for (int i = 0; i < n; i++)
                {
                    MemberInfoX member = ConstructorInfoX.Create(typeof(MemoryStream));
                    MemoryStream ms = member.CreateInstance(null) as MemoryStream;
                }
            }
            sw.Stop();
            Console.WriteLine(sw.Elapsed);

            //sw.Reset();
            //sw.Start();
            //for (int i = 0; i < n; i++)
            //{
            //    MemoryStream ms = Activator.CreateInstance(typeof(MemoryStream)) as MemoryStream;
            //}
            //sw.Stop();
            //Console.WriteLine(sw.Elapsed);
        }

        public static Stream Add(Object[] args)
        {
            return new MemoryStream();
        }

        public static Stream Add2(Object[] args)
        {
            return new MemoryStream((Byte[])args[0]);
        }

        public static Int32[] Add3(Object[] args)
        {
            return new Int32[(Int32)args[0]];
        }

        public static Object Add4(Object[] args)
        {
            return new ConsoleKeyInfo();
        }

        static void Test4()
        {
#if DEBUG
            FastTest.Test();
#endif
        }

        static void Test5()
        {
            //MemoryStream ms = new MemoryStream();
            //BinaryWriterX writer = new BinaryWriterX(ms);
            ////writer.Write((Int32)0);
            //writer.WriteEncoded(0);
            //Console.WriteLine(BitConverter.ToString(ms.ToArray()));

            Int32[] ns = new Int32[123];
            List<Int64?> list = new List<Int64?>();

            Console.WriteLine(ns.GetType().GetElementType());
            Console.WriteLine(list.GetType().GetElementType());

            TypeX type = ns.GetType();
            Console.WriteLine(type);

            list.Add(123);
            list.Add(null);
            list.Add(123);

            aa a = new aa();

            Test5_0(a);

#if DEBUG
            BinaryTest.Test();
#endif
        }

        static void Test5_0(Object value)
        {
            //ConsoleKeyInfo key = (ConsoleKeyInfo)value;
            //Console.WriteLine(key == null);

            //((aa)value).Key = new ConsoleKeyInfo('A', ConsoleKey.A, false, false, false);
        }

        struct aa
        {
            public ConsoleKeyInfo Key;
        }
    }
}
