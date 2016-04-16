using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml.Serialization;
using NewLife.Common;
using NewLife.Compression;
using NewLife.Log;
using NewLife.MessageQueue;
using NewLife.Net;
using NewLife.Net.IO;
using NewLife.Net.Stress;
using NewLife.Reflection;
using NewLife.Security;
using NewLife.Serialization;
using NewLife.Threading;
using NewLife.Xml;
using XCode.DataAccessLayer;
using XCode.Membership;
using XCode.Transform;

namespace Test
{
    public class Program
    {
        private static void Main(string[] args)
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;

            //XTrace.Log = new NetworkLog();
            XTrace.UseConsole();
#if DEBUG
            XTrace.Debug = true;
#endif
            while (true)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
#if !DEBUG
                try
                {
#endif
                    Test4();
#if !DEBUG
                }
                catch (Exception ex)
                {
                    XTrace.WriteException(ex);
                }
#endif

                sw.Stop();
                Console.WriteLine("OK! 耗时 {0}", sw.Elapsed);
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key != ConsoleKey.C) break;
            }
        }

        static void Test1()
        {
            //using (var zip = new ZipFile(@"..\System.Data.SQLite.zip".GetFullPath()))
            //{
            //    foreach (var item in zip.Entries)
            //    {
            //        Console.WriteLine("{0}\t{1}\t{2}", item.Key, item.Value.FileName, item.Value.UncompressedSize);
            //    }
            //    zip.Extract("SQLite".GetFullPath());
            //}

            //ZipFile.CompressDirectory("SQLite".GetFullPath());

            var buf = Certificate.CreateSelfSignCertificatePfx("CN=新生命团队;C=China;OU=NewLife;O=开发团队;E=nnhy@vip.qq.com");
            File.WriteAllBytes("stone.pfx", buf);
        }

        static void Test2()
        {
            var ss = PinYin.GetMulti('石');
            Console.WriteLine(ss.Join());

            var count = UserX.Meta.Count;
            "共有{0}行数据".F(count).SpeakAsync();
        }

        static void Test3()
        {
            var uri = new NetUri("udp://x2:3389");

            Console.WriteLine(uri);
            Console.WriteLine(uri.ProtocolType);
            Console.WriteLine(uri.EndPoint);
            Console.WriteLine(uri.Address);
            Console.WriteLine(uri.Host);
            Console.WriteLine(uri.Port);

            var xml = uri.ToXml();
            Console.WriteLine(xml);

            uri = xml.ToXmlEntity<NetUri>();
            Console.WriteLine(uri);
        }

        static void Test4()
        {
            ushort v = 0xFD79;
            var n = v.GetBytes().ToInt();
            Console.WriteLine("{0:X4} {1:X4}", v, n);

            var pis = typeof(B).GetProperties();
            Console.WriteLine(pis);

            var user = UserX.FindAll()[0];
            //Console.WriteLine(user);
            user.RegisterTime = DateTime.Now;

            //var sb = new StringBuilder();

            //var json = new Json();
            //json.Log = XTrace.Log;
            //json.Write(sb, user);

            //Console.WriteLine(sb.ToString());

            //Console.WriteLine(user.ToJson(true));

            //var type = "JsonWriter".GetTypeEx();
            //var txt = (String)type.CreateInstance().Invoke("ToJson", user, true);
            //Console.WriteLine(txt);

            //JsonTest.Start();

            //// 为所有Json宿主创建实例
            //var hosts = typeof(IJsonHost).GetAllSubclasses().Select(e => e.CreateInstance() as IJsonHost).ToArray();

            //var json = hosts[2].Write(user);
            //Thread.Sleep(1000);
            //Console.Clear();

            //CodeTimer.ShowHeader("Json序列化性能测试");
            //foreach (var item in hosts)
            //{
            //    CodeTimer.TimeLine(item.GetType().Name, 100000, n => { item.Write(user); });
            //}

            //Console.WriteLine();
            //CodeTimer.ShowHeader("Json反序列化性能测试");
            //foreach (var item in hosts)
            //{
            //    CodeTimer.TimeLine(item.GetType().Name, 100000, n => { item.Read(json, user.GetType()); });
            //}
        }

        class A
        {
            public Int32 ID { get; set; }

            public virtual Guid Guid { get; set; }
        }

        class B : A
        {
            public String Name { get; set; }

            public override Guid Guid { get; set; }
        }

        static void Test5()
        {
            using (var zf = new ZipFile())
            {
                zf.AddFile("XCode.pdb");
                zf.AddFile("NewLife.Core.pdb");

                zf.Write("test.zip");
                zf.Write("test.7z");
            }
            //using (var zf = new ZipFile("Test.lzma.zip"))
            //{
            //    foreach (var item in zf.Entries.Values)
            //    {
            //        Console.WriteLine("{0} {1}", item.FileName, item.CompressionMethod);
            //    }

            //    zf.Extract("lzma");
            //}
        }

        static void Test7()
        {
            //Console.Write("请输入表达式：");
            //var code = Console.ReadLine();

            //var rs = ScriptEngine.Execute(code, new Dictionary<String, Object> { { "a", 222 }, { "b", 333 } });
            ////Console.WriteLine(rs);

            //var se = ScriptEngine.Create(code);
            //var fm = code.Replace("a", "{0}").Replace("b", "{1}");
            //for (int i = 1; i <= 9; i++)
            //{
            //    for (int j = 1; j <= i; j++)
            //    {
            //        Console.Write(fm + "={2}\t", j, i, se.Invoke(i, j));
            //    }
            //    Console.WriteLine();
            //}

            var se = ScriptEngine.Create("Test.Program.TestMath(k)");
            if (se.Method == null)
            {
                se.Parameters.Add("k", typeof(Double));
                se.Compile();
            }

            var fun = (DM)(Object)Delegate.CreateDelegate(typeof(DM), se.Method as MethodInfo);

            var timer = 1000000;
            var k = 123;
            CodeTimer.ShowHeader();
            CodeTimer.TimeLine("原生", timer, n => TestMath(k));
            CodeTimer.TimeLine("动态", timer, n => se.Invoke(k));
            CodeTimer.TimeLine("动态2", timer, n => fun(k));
        }
        public static Double TestMath(Double k)
        {
            //var bts = File.ReadAllBytes(Assembly.GetExecutingAssembly().Location);
            return Math.Sin(k) * Math.Log10(k) * Math.Exp(k);
        }
        delegate Object DM(Double k);

        static SysConfig Load()
        {
            var filename = SysConfig._.ConfigFile;
            if (filename.IsNullOrWhiteSpace()) return null;
            filename = filename.GetFullPath();
            if (!File.Exists(filename)) return null;

            try
            {
                var config = filename.ToXmlFileEntity<SysConfig>();
                if (config == null) return null;

                //config.OnLoaded();

                //// 第一次加载，建立定时重载定时器
                //if (timer == null && _.ReloadTime > 0) timer = new TimerX(s => Current = null, null, _.ReloadTime * 1000, _.ReloadTime * 1000);

                return config;
            }
            catch (Exception ex) { XTrace.WriteException(ex); return null; }
        }

        static void Test9()
        {
            var user = UserX.FindAllWithCache()[0];
            Console.WriteLine(user.RoleName);
            Console.Clear();

            //var bn = new Binary();
            //bn.EnableTrace();
            var bn = new Xml();
            bn.Write(user);

            var sw = new Stopwatch();
            sw.Start();

            var buf = bn.GetBytes();
            Console.WriteLine(buf.ToHex());
            Console.WriteLine(bn.GetString());

            var ms = new MemoryStream(buf);
            //bn = new Binary();
            bn.Stream = ms;
            //bn.EnableTrace();
            var u = bn.Read<UserX>();

            foreach (var item in UserX.Meta.AllFields)
            {
                if (user[item.Name] == u[item.Name])
                    Console.WriteLine("{0} {1} <=> {2} 通过", item.Name, user[item.Name], u[item.Name]);
                else
                    Console.WriteLine("{0} {1} <=> {2} 失败", item.Name, user[item.Name], u[item.Name]);
            }

            //var hi = HardInfo.Current;
            //sw.Stop();
            //Console.WriteLine(sw.Elapsed);
            //Console.WriteLine(hi);

            //var ci = new ComputerInfo();
            //Console.WriteLine(ci);
        }

        static void Test10()
        {
            NetHelper.ShowTcpParameters();
            Console.WriteLine("k键设置最优Tcp参数，其它键开始测试：");
            var key = Console.ReadKey();
            if (key.KeyChar == 'k') NetHelper.SetTcpMax();

            TcpStress.Main();
        }

        static void Test13()
        {
            var file = @"E:\BaiduYunDownload\xiaomi.db";
            var file2 = Path.ChangeExtension(file, "sqlite");
            DAL.AddConnStr("src", "Data Source=" + file, null, "sqlite");
            DAL.AddConnStr("des", "Data Source=" + file2, null, "sqlite");

            if (!File.Exists(file2))
            {
                var et = new EntityTransform();
                et.SrcConn = "src";
                et.DesConn = "des";
                //et.PartialTableNames.Add("xiaomi");
                //et.PartialCount = 1000000;

                et.Transform();
            }

            var sw = new Stopwatch();

            var dal = DAL.Create("src");
            var eop = dal.CreateOperate(dal.Tables[0].TableName);
            sw.Start();
            var count = eop.Count;
            sw.Stop();
            XTrace.WriteLine("{0} 耗时 {1}ms", count, sw.ElapsedMilliseconds);
            sw.Reset(); sw.Start();
            count = eop.FindCount();
            sw.Stop();
            XTrace.WriteLine("{0} 耗时 {1}ms", count, sw.ElapsedMilliseconds);

            var entity = eop.Create();
            entity["username"] = "Stone";
            entity.Save();
            count = eop.FindCount();
            Console.WriteLine(count);

            entity.Delete();
            count = eop.FindCount();
            Console.WriteLine(count);
        }

        static void Test14()
        {
            Console.Clear();
            //XTrace.Log.Level = LogLevel.Info;
            var server = new FileServer();
            server.Log = XTrace.Log;
            server.Start();

            var count = 0;
            WaitCallback func = s =>
            {
                count++;
                var client = new FileClient();
                try
                {
                    client.Log = XTrace.Log;
                    if (s + "" == "Test.exe")
                        client.Connect("127.0.0.1", server.Port);
                    else
                        client.Connect("::1", server.Port);
                    client.SendFile(s + "");
                }
                finally
                {
                    count--;
                    client.Dispose();
                }
            };

            ThreadPoolX.QueueUserWorkItem(func, "Test.exe");
            ThreadPoolX.QueueUserWorkItem(func, "NewLife.Core.dll");
            ThreadPoolX.QueueUserWorkItem(func, "NewLife.Net.dll");

            var file = @"F:\MS\cn_visual_studio_ultimate_2013_with_update_4_x86_dvd_5935081.iso";
            if (File.Exists(file))
                ThreadPoolX.QueueUserWorkItem(func, file);

            Thread.Sleep(500);
            while (count > 0) Thread.Sleep(200);
            server.Dispose();
        }
    }
}