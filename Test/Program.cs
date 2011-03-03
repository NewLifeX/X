using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using NewLife;
using NewLife.CommonEntity;
using NewLife.Log;
using NewLife.Messaging;
using NewLife.Net.Sockets;
using NewLife.Net.Udp;
using NewLife.Net.UPnP;
using NewLife.PeerToPeer.Messages;
using NewLife.Reflection;
using XCode;
using XCode.DataAccessLayer;
using NewLife.IO;
using NewLife.Remoting;
using System.Net;
using NewLife.Net.Common;
using NewLife.Exceptions;
using System.Threading;
using NewLife.Threading;

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
                    Test10();
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

        static void Test7()
        {
            //DAL dal = Administrator.Meta.DBO;
            //DatabaseSchema.Create(dal).CheckTable("Area");

            //XCodeTest.MulThread(1);

            Uri uri = new Uri("http://192.168.0.8:8080/aaa.ashx?a=b");
            ShowObj(uri);

            uri = new Uri("tcp://192.168.0.8:8080/aaa.ashx?a=b");
            ShowObj(uri);

            uri = new Uri("udp://192.168.0.8:8080/aaa.ashx?a=b");
            ShowObj(uri);

            Console.WriteLine(Path.GetFileNameWithoutExtension(uri.AbsolutePath));
        }

        static void ShowObj(Object obj)
        {
            Console.WriteLine();
            Console.WriteLine("显示对象成员{0}：", obj.GetType().FullName);
            //TypeX tx = TypeX.Create(obj.GetType());
            //foreach (PropertyInfoX pi in tx.Properties)
            PropertyInfo[] pis = obj.GetType().GetProperties();
            foreach (PropertyInfo pi in pis)
            {
                Console.Write("{0,15}=", pi.Name);
                ConsoleColor color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(pi.GetValue(obj, null));
                Console.ForegroundColor = color;
                Console.WriteLine();
            }
        }

        static Dictionary<PermissionFlags, String> flagCache;
        static Dictionary<PermissionFlags, String> GetDescriptions()
        {
            if (flagCache != null) return flagCache;

            flagCache = new Dictionary<PermissionFlags, string>();

            TypeX type = typeof(PermissionFlags);
            foreach (FieldInfoX item in type.Fields)
            {
                if (!item.Field.IsStatic) continue;

                PermissionFlags value = (PermissionFlags)item.GetValue();

                String des = item.Field.Name;
                DescriptionAttribute att = AttributeX.GetCustomAttribute<DescriptionAttribute>(item.Member, false);
                if (att != null && !String.IsNullOrEmpty(att.Description)) des = att.Description;
                flagCache.Add(value, des);
            }

            return flagCache;
        }

        static void Test8()
        {
            DAL dal = DAL.Create("Common");
            DataSet ds = dal.Select("select * from Test", "");
            Console.WriteLine(ds.Tables[0].Columns[2].DataType);

            Object data = ds.Tables[0].Rows[0][2];
            Console.WriteLine(data.GetType());
            Byte[] buffer = (Byte[])data;
            Console.WriteLine(BitConverter.ToString(buffer));

            Random rnd = new Random((Int32)DateTime.Now.Ticks);
            buffer = new Byte[16];
            rnd.NextBytes(buffer);

            String sql = String.Format("update Test set des=0x{0}", BitConverter.ToString(buffer).Replace("-", null));
            Console.WriteLine(dal.Execute(sql, ""));
        }

        static void Test9()
        {
            //Test02 entity = Test02.Find(Test02._.ID, 1);

            //String str = "ws.config";
            //str = FormatKeyWord(str);
            //Console.WriteLine(str);

            PortMappingEntryRequest entry = new PortMappingEntryRequest();
            entry.NewPortMappingIndex = 0;

            //UPnPCommand cp = new UPnPCommand();
            //cp.Url = "http://192.168.1.1:1900/ipc";
            //MethodInfoX.Invoke<String>(cp, "Invoke", new Object[] { "GetGenericPortMappingEntry", cp.Url, new Object[] { entry } });
            //cp.Discover();
            //PortMappingEntry2 obj = cp.GetGenericPortMappingEntry(entry);

            //String Command = UPnPClient.SerialRequest(entry, "u", "Xmlns");
            //Console.WriteLine(Command);

            String ip = NetHelper.GetIPV4()[0].ToString();

            UPnPClient client = new UPnPClient();
            client.OnNewDevice += delegate(object sender, EventArgs<InternetGatewayDevice, bool> e)
            {
                InternetGatewayDevice device = e.Arg1;
                Console.WriteLine("{0}网关：{1} {2}", e.Arg2 ? "[缓存]" : "", device.device.friendlyName, device.ServerHost);

                PortMappingEntry entity = null;
                for (int i = 0; i < 100; i++)
                {
                    try
                    {
                        entity = UPnPClient.GetMapByIndex(e.Arg1, i);
                    }
                    catch { break; }
                    if (entity == null) break;

                    Console.WriteLine("[{6}] {0} {1} {2} {3} {4} {5}", entity.Description, entity.ExternalPort, entity.Protocol, entity.InternalClient, entity.InternalPort, entity.Enabled, device.ServerHost);

                    if (entity.InternalClient == ip) UPnPClient.Delete(device, entity.RemoteHost, entity.ExternalPort, entity.Protocol);
                }
            };
            client.StartDiscover();

            Thread.Sleep(1000);

            InternetGatewayDevice device2 = client.Gateways.Values[0];
            PortMappingEntry entity2 = UPnPClient.GetMapByPortAndProtocol(device2, "", 20019, "TCP");
            Console.WriteLine(entity2.Description);

            Random rnd = new Random((Int32)DateTime.Now.Ticks);
            //Boolean b = UPnPClient.Add(device2, ip, rnd.Next(1000, 9999), "TCP", "UPNP测试");

            client.Dispose();
        }

        /// <summary>
        /// 格式化关键字
        /// </summary>
        /// <param name="keyWord">表名</param>
        /// <returns></returns>
        public static String FormatKeyWord(String keyWord)
        {
            //return String.Format("\"{0}\"", keyWord);

            if (String.IsNullOrEmpty(keyWord)) throw new ArgumentNullException("keyWord");

            Int32 pos = keyWord.LastIndexOf(".");

            if (pos < 0) return "\"" + keyWord + "\"";

            String tn = keyWord.Substring(pos + 1);
            if (tn.StartsWith("\"")) return keyWord;

            return keyWord.Substring(0, pos + 1) + "\"" + tn + "\"";
        }

        static void Test10()
        {
            Console.WriteLine(Administrator.FindCount());
            //Console.WriteLine(Administrator.Meta.Count);

            //XException ex = new XException("DT {0}", DateTime.Now);
            //XException ex2 = new XException("aaa", ex);
            //Console.WriteLine(ex2.Message);

            String url = "http://localhost:36716/WebSite1/Message2.ashx";
            HttpStreamClient client = new HttpStreamClient(url);
            Stream stream = client.GetStream();

            //IPAddress address = NetHelper.ParseAddress("www.qq.com");
            Administrator admin = new Administrator();
            admin.Name = "nnhy";

            EntityMessage message = new EntityMessage(admin);

            MemoryStream ms = new MemoryStream();
            message.WritePacket(ms);
            ms.Position = 0;

            EntityMessage msg = new EntityMessage();
            BinaryReaderX reader2 = new BinaryReaderX(ms);
            Console.WriteLine(reader2.ReadByte());
            msg.Read(reader2);
            Console.WriteLine(msg.Entity);

            //String name = "System.Data.SQLite.SQLiteFactory";
            //Type type = TypeX.GetType(name);
            //Console.WriteLine(type.FullName);

            //message.WritePacket(stream);

            //BinaryReader reader = new BinaryReader(stream);
            //String str = reader.ReadString();
            //Console.WriteLine(str);
        }

        static void Test11()
        {
            ThreadPool.QueueUserWorkItem(Test11_1);
            ThreadPool.QueueUserWorkItem(Test11_2);
        }

        static void Test11_1(Object state)
        {
            ReadWriteLock rw = ReadWriteLock.Create("Test");

            for (int i = 0; i < 10; i++)
            {
                rw.AcquireRead();

                Console.WriteLine("Test1: {0}", i);

                Thread.Sleep(2000);

                rw.ReleaseRead();
            }
        }

        static void Test11_2(Object state)
        {
            ReadWriteLock rw = ReadWriteLock.Create("Test");

            for (int i = 0; i < 10; i++)
            {
                rw.AcquireWrite();

                Console.WriteLine("Test2: {0}", i);

                Thread.Sleep(3000);

                rw.ReleaseWrite();
            }
        }
    }
}
