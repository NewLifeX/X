using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using NewLife;
using NewLife.Caching;
using NewLife.Log;
using NewLife.Net;
using NewLife.Reflection;
using NewLife.Remoting;
using NewLife.Security;
using NewLife.Serialization;
using XCode.DataAccessLayer;
using XCode.Membership;
using XCode.Service;
using XCode;
using System.Collections;
using XCode.Code;
using System.Reflection;
using System.Security.Cryptography;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Crypto.Parameters;

#if !NET4
using TaskEx = System.Threading.Tasks.Task;
#endif

namespace Test
{
    public class Program
    {
        private static void Main(String[] args)
        {
            //Environment.SetEnvironmentVariable("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", "1");

            MachineInfo.RegisterAsync();
            //XTrace.Log = new NetworkLog();
            XTrace.UseConsole();
#if DEBUG
            XTrace.Debug = true;
            XTrace.Log.Level = LogLevel.All;
#endif
            while (true)
            {
                var sw = Stopwatch.StartNew();
#if !DEBUG
                try
                {
#endif
                //Test1();
                //XMLConvertToPEM();
                ExportPublicKeyToPEMFormat();
#if !DEBUG
                }
                catch (Exception ex)
                {
                    XTrace.WriteException(ex?.GetTrue());
                }
#endif

                sw.Stop();
                Console.WriteLine("OK! 耗时 {0}", sw.Elapsed);
                //Thread.Sleep(5000);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                var key = Console.ReadKey(true);
                if (key.Key != ConsoleKey.C) break;
            }
        }

        private static void Test1()
        {
            //foreach (var item in Enum.GetValues(typeof(TypeCode)))
            //{
            //    var t = (item + "").GetTypeEx();
            //    Console.WriteLine("{0}\t{1}\t{2}", item, t, t?.IsPrimitive);
            //}

            //"你好".SpeakAsync();

            XTrace.WriteLine("FullPath:{0}", ".".GetFullPath());
            XTrace.WriteLine("BasePath:{0}", ".".GetBasePath());
            XTrace.WriteLine("TempPath:{0}", Path.GetTempPath());

            var mi = MachineInfo.Current ?? MachineInfo.RegisterAsync().Result;

            foreach (var pi in mi.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                XTrace.WriteLine("{0}:\t{1}", pi.Name, mi.GetValue(pi));
            }

            Console.WriteLine();

#if __CORE__
            foreach (var pi in typeof(RuntimeInformation).GetProperties())
            {
                XTrace.WriteLine("{0}:\t{1}", pi.Name, pi.GetValue(null));
            }
#endif

            //Console.WriteLine();

            //foreach (var pi in typeof(Environment).GetProperties())
            //{
            //    XTrace.WriteLine("{0}:\t{1}", pi.Name, pi.GetValue(null));
            //}

            mi = MachineInfo.Current;
            for (var i = 0; i < 100; i++)
            {
                XTrace.WriteLine("CPU={0:p2} Temp={1} Memory={2:n0} Disk={3}", mi.CpuRate, mi.Temperature, mi.AvailableMemory.ToGMK(), MachineInfo.GetFreeSpace().ToGMK());
                Thread.Sleep(1000);
                mi.Refresh();
            }

            Console.ReadKey();
        }

        private static void Test2()
        {
            XTrace.WriteLine("FullPath:{0}", ".".GetFullPath());
            XTrace.WriteLine("BasePath:{0}", ".".GetBasePath());
            XTrace.WriteLine("TempPath:{0}", Path.GetTempPath());
            Console.WriteLine();

            var set = NewLife.Setting.Current;
            for (var i = 0; i < 100; i++)
            {
                XTrace.WriteLine(set.DataPath);

                Thread.Sleep(1000);
            }
        }

        private static void Test3()
        {
            var tracer = DefaultTracer.Instance;
            tracer.MaxSamples = 100;
            tracer.MaxErrors = 100;

            if (Console.ReadLine() == "1")
            {
                var svr = new ApiServer(1234)
                //var svr = new ApiServer("http://*:1234")
                {
                    Log = XTrace.Log,
                    //EncoderLog = XTrace.Log,
                    StatPeriod = 10,
                    Tracer = DefaultTracer.Instance,
                };

                // http状态
                svr.UseHttpStatus = true;

                var ns = svr.EnsureCreate() as NetServer;
                ns.EnsureCreateServer();
                var ts = ns.Servers.FirstOrDefault(e => e is TcpServer);
                //ts.ProcessAsync = true;

                svr.Start();

                Console.ReadKey();
            }
            else
            {
                var client = new ApiClient("tcp://127.0.0.1:335,tcp://127.0.0.1:1234")
                {
                    Log = XTrace.Log,
                    //EncoderLog = XTrace.Log,
                    StatPeriod = 10,
                    Tracer = DefaultTracer.Instance,

                    UsePool = true,
                };
                client.Open();

                TaskEx.Run(() =>
                {
                    var sw = Stopwatch.StartNew();
                    try
                    {
                        for (var i = 0; i < 10; i++)
                        {
                            client.InvokeAsync<Object>("Api/All", new { state = 111 }).Wait();
                        }
                    }
                    catch (Exception ex)
                    {
                        XTrace.WriteException(ex.GetTrue());
                    }
                    sw.Stop();
                    XTrace.WriteLine("总耗时 {0:n0}ms", sw.ElapsedMilliseconds);
                });

                TaskEx.Run(() =>
                {
                    var sw = Stopwatch.StartNew();
                    try
                    {
                        for (var i = 0; i < 10; i++)
                        {
                            client.InvokeAsync<Object>("Api/All", new { state = 222 }).Wait();
                        }
                    }
                    catch (Exception ex)
                    {
                        XTrace.WriteException(ex.GetTrue());
                    }
                    sw.Stop();
                    XTrace.WriteLine("总耗时 {0:n0}ms", sw.ElapsedMilliseconds);
                });

                TaskEx.Run(() =>
                {
                    var sw = Stopwatch.StartNew();
                    try
                    {
                        for (var i = 0; i < 10; i++)
                        {
                            client.InvokeAsync<Object>("Api/Info", new { state = 333 }).Wait();
                        }
                    }
                    catch (Exception ex)
                    {
                        XTrace.WriteException(ex.GetTrue());
                    }
                    sw.Stop();
                    XTrace.WriteLine("总耗时 {0:n0}ms", sw.ElapsedMilliseconds);
                });

                TaskEx.Run(() =>
                {
                    var sw = Stopwatch.StartNew();
                    try
                    {
                        for (var i = 0; i < 10; i++)
                        {
                            client.InvokeAsync<Object>("Api/Info", new { state = 444 }).Wait();
                        }
                    }
                    catch (Exception ex)
                    {
                        XTrace.WriteException(ex.GetTrue());
                    }
                    sw.Stop();
                    XTrace.WriteLine("总耗时 {0:n0}ms", sw.ElapsedMilliseconds);
                });

                Console.ReadKey();
            }
        }

        private static void Test4()
        {
            var v = Rand.NextBytes(32);
            Console.WriteLine(v.ToBase64());

            ICache ch = null;
            //ICache ch = new DbCache();
            //ch.Set(key, v);
            //v = ch.Get<Byte[]>(key);
            //Console.WriteLine(v.ToBase64());
            //ch.Remove(key);

            Console.Clear();

            Console.Write("选择要测试的缓存：1，MemoryCache；2，DbCache；3，Redis ");
            var select = Console.ReadKey().KeyChar;
            switch (select)
            {
                case '1':
                    ch = new MemoryCache();
                    break;
                case '2':
                    ch = new DbCache();
                    break;
                case '3':
                    var rds = new Redis("127.0.0.1", null, 9)
                    {
                        Counter = new PerfCounter()
                    };
                    ch = rds;
                    break;
            }

            var mode = false;
            Console.WriteLine();
            Console.Write("选择测试模式：1，顺序；2，随机 ");
            if (Console.ReadKey().KeyChar != '1') mode = true;

            var batch = 0;
            Console.WriteLine();
            Console.Write("选择输入批大小[0]：");
            batch = Console.ReadLine().ToInt();

            Console.Clear();

            //var batch = 0;
            //if (mode) batch = 1000;

            var rs = ch.Bench(mode, batch);

            XTrace.WriteLine("总测试数据：{0:n0}", rs);
            if (ch is Redis rds2) XTrace.WriteLine(rds2.Counter + "");
        }

        private static void Test5()
        {
            var set = XCode.Setting.Current;
            set.Debug = true;
            set.ShowSQL = true;

            Console.WriteLine("1，服务端；2，客户端");
            if (Console.ReadKey().KeyChar == '1')
            {
                var n = UserOnline.Meta.Count;

                var svr = new DbServer
                {
                    Log = XTrace.Log,
                    StatPeriod = 5
                };
                svr.Start();
            }
            else
            {
                DAL.AddConnStr("net", "Server=tcp://admin:newlife@127.0.0.1:3305/Log", null, "network");
                var dal = DAL.Create("net");

                UserOnline.Meta.ConnName = "net";

                var count = UserOnline.Meta.Count;
                Console.WriteLine("count={0}", count);

                var entity = new UserOnline
                {
                    Name = "新生命",
                    OnlineTime = 12345
                };
                entity.Insert();

                Console.WriteLine("id={0}", entity.ID);

                var entity2 = UserOnline.FindByKey(entity.ID);
                Console.WriteLine("user={0}", entity2);

                entity2.Page = Rand.NextString(8);
                entity2.Update();

                entity2.Delete();

                for (var i = 0; i < 100; i++)
                {
                    entity2 = new UserOnline
                    {
                        Name = Rand.NextString(8),
                        Page = Rand.NextString(8)
                    };
                    entity2.Insert();

                    Thread.Sleep(5000);
                }
            }

            //var client = new DbClient();
            //client.Log = XTrace.Log;
            //client.EncoderLog = client.Log;
            //client.StatPeriod = 5;

            //client.Servers.Add("tcp://127.0.0.1:3305");
            //client.Open();

            //var db = "Membership";
            //var rs = client.LoginAsync(db, "admin", "newlife").Result;
            //Console.WriteLine((DatabaseType)rs["DbType"].ToInt());

            //var ds = client.QueryAsync("Select * from User").Result;
            //Console.WriteLine(ds);

            //var count = client.QueryCountAsync("User").Result;
            //Console.WriteLine("count={0}", count);

            //var ps = new Dictionary<String, Object>
            //{
            //    { "Logins", 3 },
            //    { "id", 1 }
            //};
            //var es = client.ExecuteAsync("update user set Logins=Logins+@Logins where id=@id", ps).Result;
            //Console.WriteLine("Execute={0}", es);
        }

        private static void Test6()
        {
            var pfx = new X509Certificate2("../newlife.pfx", "newlife");
            //Console.WriteLine(pfx);

            //using var svr = new ApiServer(1234);
            //svr.Log = XTrace.Log;
            //svr.EncoderLog = XTrace.Log;

            //var ns = svr.EnsureCreate() as NetServer;

            using var ns = new NetServer(1234)
            {
                Name = "Server",
                ProtocolType = NetType.Tcp,
                Log = XTrace.Log,
                SessionLog = XTrace.Log,
                SocketLog = XTrace.Log,
                LogReceive = true
            };

            ns.EnsureCreateServer();
            foreach (var item in ns.Servers)
            {
                if (item is TcpServer ts) ts.Certificate = pfx;
            }

            ns.Received += (s, e) =>
            {
                XTrace.WriteLine("收到：{0}", e.Packet.ToStr());
            };
            ns.Start();

            using var client = new TcpSession
            {
                Name = "Client",
                Remote = new NetUri("tcp://127.0.0.1:1234"),
                SslProtocol = SslProtocols.Tls,
                Log = XTrace.Log,
                LogSend = true
            };
            client.Open();

            client.Send("Stone");

            Console.ReadLine();
        }

        private static void Test7()
        {
#if __CORE__
            XTrace.WriteLine(RuntimeInformation.OSDescription);
#endif

            //DAL.AddConnStr("membership", "Server=10.0.0.3;Port=3306;Database=Membership;Uid=root;Pwd=Pass@word;", null, "mysql");

            Role.Meta.Session.Dal.Db.ShowSQL = true;
            Role.Meta.Session.Dal.Expire = 10;
            //Role.Meta.Session.Dal.Db.Readonly = true;

            var list = Role.FindAll();
            Console.WriteLine(list.Count);

            list = Role.FindAll(Role._.Name.NotContains("abc"));
            Console.WriteLine(list.Count);

            Thread.Sleep(1000);

            list = Role.FindAll();
            Console.WriteLine(list.Count);

            Thread.Sleep(1000);

            var r = list.Last();
            r.IsSystem = !r.IsSystem;
            r.Update();

            Thread.Sleep(5000);

            list = Role.FindAll();
            Console.WriteLine(list.Count);
        }

        private static async void Test8()
        {
            Area.Meta.Session.Dal.Db.ShowSQL = false;

            var url = "http://www.mca.gov.cn/article/sj/xzqh/2019/2019/201912251506.html";
            //var file = "area.html".GetFullPath();
            //if (!File.Exists(file))
            //{
            //    var http = new HttpClient();
            //    await http.DownloadFileAsync(url, file);
            //}

            //var txt = File.ReadAllText(file);
            //foreach (var item in Area.Parse(txt))
            //{
            //    XTrace.WriteLine("{0} {1}", item.ID, item.Name);
            //}

            //#if __CORE__
            //            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            //#endif
            Area.FetchAndSave(url);

            //            var list = Area.FindAll();
            //            foreach (var item in list)
            //            {
            //                if (item.ParentID > 0 && item.Level - 1 != item.Parent.Level)
            //                {
            //                    XTrace.WriteLine("{0} {1} {2}", item.ID, item.Level, item.Name);
            //                }
            //            }

            //var file = "../2020年02月四级行政区划库.csv";
            var file = "Area.csv";
            var list = new List<Area>();
            list.LoadCsv(file);

            foreach (var r in list)
            {
                r.UpdateTime = DateTime.Now;
                if (r.ID > 70_00_00)
                {
                    r.Enable = true;
                    r.SaveAsync();
                }
                else
                {
                    var r2 = Area.FindByID(r.ID);
                    //if (r.ParentID != r2.ParentID || r.FullName != r2.FullName || r.Name != r2.Name) XTrace.WriteLine("{0} {1} {2}", r.ID, r.Name, r.FullName);
                    if (r2 == null)
                    {
                        XTrace.WriteLine("找不到 {0} {1} {2}", r.ID, r.Name, r.FullName);
                        r.Enable = false;
                        r.SaveAsync();
                    }
                    else
                    {
                        if (r.FullName != r2.FullName || r.Name != r2.Name) XTrace.WriteLine("{0} {1} {2} => {3} {4}", r.ID, r.Name, r.FullName, r2.Name, r2.FullName);

                        //r2.Longitude = r.Longitude;
                        //r2.Latitude = r.Latitude;
                        //r2.SaveAsync();
                        r.Enable = true;
                        r.SaveAsync();
                    }
                }
            }

            //using var csv = new CsvFile(file);
            //csv.ReadLine();

            //while (true)
            //{
            //    var ss = csv.ReadLine();
            //    if (ss == null) break;

            //    var r = new Area
            //    {
            //        ID = ss[0].ToInt(),
            //        ParentID = ss[1].ToInt(),
            //        FullName = ss[2].Trim(),
            //        Name = ss[3].Trim(),
            //        Longitude = ss[4].ToDouble(),
            //        Latitude = ss[5].ToDouble(),
            //        Enable = true,
            //    };
            //    if (r.ID > 70_00_00)
            //    {
            //        r.SaveAsync();
            //    }
            //    else
            //    {
            //        var r2 = Area.FindByID(r.ID);
            //        //if (r.ParentID != r2.ParentID || r.FullName != r2.FullName || r.Name != r2.Name) XTrace.WriteLine("{0} {1} {2}", r.ID, r.Name, r.FullName);
            //        if (r2 == null)
            //        {
            //            XTrace.WriteLine("找不到 {0} {1} {2}", r.ID, r.Name, r.FullName);
            //            r.Enable = false;
            //            r.SaveAsync();
            //        }
            //        else
            //        {
            //            if (r.FullName != r2.FullName) XTrace.WriteLine("{0} {1} {2} => {3} {4}", r.ID, r.Name, r.FullName, r2.Name, r2.FullName);

            //            r2.Longitude = r.Longitude;
            //            r2.Latitude = r.Latitude;
            //            r2.SaveAsync();
            //        }
            //    }
            //}
        }

        private static void Test9()
        {
            var r0 = Role.FindByName("Stone");
            r0?.Delete();

            var r = new Role();
            r.Name = "Stone";
            r.Insert();

            var r2 = Role.FindByName("Stone");
            XTrace.WriteLine("FindByName: {0}", r2.ToJson());

            r.Enable = true;
            r.Update();

            var r3 = Role.Find(Role._.Name == "STONE");
            XTrace.WriteLine("Find: {0}", r3.ToJson());

            r.Delete();

            var n = Role.FindCount();
            XTrace.WriteLine("count={0}", n);
        }

        private static void Test10()
        {
            var dt1 = new DateTime(1970, 1, 1);
            //var x = dt1.ToFileTimeUtc();

            var yy = Int64.Parse("-1540795502468");

            //var yy = "1540795502468".ToInt();
            Console.WriteLine(yy);

            var dt = 1540795502468.ToDateTime();
            var y = dt.ToUniversalTime();
            Console.WriteLine(dt1.ToLong());
        }

        private static void Test11()
        {
        }

        /// <summary>测试序列化</summary>
        private static void Test12()
        {
            EntityBuilder.Build("../../Src/XCode/model.xml");
        }

        private static void Test13()
        {
            //DSACryptoServiceProvider dsa = new DSACryptoServiceProvider(1024);

            ////var x = dsa.ExportCspBlob(true);

            //using (var fs = new FileStream("D:\\keys\\private.key", FileMode.Open, FileAccess.Read))
            //{
            //    var rs = new StreamReader(fs);
            //    var keystr = rs.ReadToEnd();
            //    DSAHelper.FromXmlStringX(dsa, keystr);

            //    DsaPublicKeyParameters dsaKey = DotNetUtilities.GetDsaPublicKey(dsa);
            //    using (StreamWriter sw = new StreamWriter("D:\\keys\\dsa.pem"))
            //    {
            //        PemWriter pw = new PemWriter(sw);
            //        pw.WriteObject(dsaKey);
            //    }
            //}
        }

        /// <summary>
        /// 私钥XML2PEM
        /// </summary>
        private static void XMLConvertToPEM()//XML格式密钥转PEM
        {
            var rsa2 = new RSACryptoServiceProvider();
            using (var sr = new StreamReader("D:\\keys\\private.key"))
            {
                rsa2.FromXmlString(sr.ReadToEnd());
            }
            var p = rsa2.ExportParameters(true);

            var key = new RsaPrivateCrtKeyParameters(
                new Org.BouncyCastle.Math.BigInteger(1, p.Modulus), new Org.BouncyCastle.Math.BigInteger(1, p.Exponent), new Org.BouncyCastle.Math.BigInteger(1, p.D),
                new Org.BouncyCastle.Math.BigInteger(1, p.P), new Org.BouncyCastle.Math.BigInteger(1, p.Q), new Org.BouncyCastle.Math.BigInteger(1, p.DP), new Org.BouncyCastle.Math.BigInteger(1, p.DQ),
                new Org.BouncyCastle.Math.BigInteger(1, p.InverseQ));

            using (var sw = new StreamWriter("D:\\keys\\PrivateKey.pem"))
            {
                var pemWriter = new PemWriter(sw);
                pemWriter.WriteObject(key);
            }
        }


        private static void ExportPublicKeyToPEMFormat()
        {

            var rsa2 = new RSACryptoServiceProvider();
            using (var sr = new StreamReader("D:\\keys\\private.key"))
            {
                rsa2.FromXmlString(sr.ReadToEnd());
            }

            var str = ExportPublicKeyToPEMFormat(rsa2);

            using (var sw = new StreamWriter("D:\\keys\\PublicKey.pem"))
            {
                //var pemWriter = new PemWriter(sw);
                //pemWriter.WriteObject(str);
                sw.Write(str);
            }

        }

        public static String ExportPublicKeyToPEMFormat(RSACryptoServiceProvider csp)
        {
            TextWriter outputStream = new StringWriter();

            var parameters = csp.ExportParameters(false);
            using (var stream = new MemoryStream())
            {
                var writer = new BinaryWriter(stream);
                writer.Write((byte)0x30); // SEQUENCE
                using (var innerStream = new MemoryStream())
                {
                    var innerWriter = new BinaryWriter(innerStream);
                    EncodeIntegerBigEndian(innerWriter, new byte[] { 0x00 }); // Version
                    EncodeIntegerBigEndian(innerWriter, parameters.Modulus);
                    EncodeIntegerBigEndian(innerWriter, parameters.Exponent);

                    //All Parameter Must Have Value so Set Other Parameter Value Whit Invalid Data  (for keeping Key Structure  use "parameters.Exponent" value for invalid data)
                    EncodeIntegerBigEndian(innerWriter, parameters.Exponent); // instead of parameters.D
                    EncodeIntegerBigEndian(innerWriter, parameters.Exponent); // instead of parameters.P
                    EncodeIntegerBigEndian(innerWriter, parameters.Exponent); // instead of parameters.Q
                    EncodeIntegerBigEndian(innerWriter, parameters.Exponent); // instead of parameters.DP
                    EncodeIntegerBigEndian(innerWriter, parameters.Exponent); // instead of parameters.DQ
                    EncodeIntegerBigEndian(innerWriter, parameters.Exponent); // instead of parameters.InverseQ

                    var length = (int)innerStream.Length;
                    EncodeLength(writer, length);
                    writer.Write(innerStream.GetBuffer(), 0, length);
                }

                var base64 = Convert.ToBase64String(stream.GetBuffer(), 0, (int)stream.Length).ToCharArray();
                outputStream.WriteLine("-----BEGIN PUBLIC KEY-----");
                // Output as Base64 with lines chopped at 64 characters
                for (var i = 0; i < base64.Length; i += 64)
                {
                    outputStream.WriteLine(base64, i, Math.Min(64, base64.Length - i));
                }
                outputStream.WriteLine("-----END PUBLIC KEY-----");

                return outputStream.ToString();

            }
        }

        private static void EncodeIntegerBigEndian(BinaryWriter stream, byte[] value, bool forceUnsigned = true)
        {
            stream.Write((byte)0x02); // INTEGER
            var prefixZeros = 0;
            for (var i = 0; i < value.Length; i++)
            {
                if (value[i] != 0) break;
                prefixZeros++;
            }
            if (value.Length - prefixZeros == 0)
            {
                EncodeLength(stream, 1);
                stream.Write((byte)0);
            }
            else
            {
                if (forceUnsigned && value[prefixZeros] > 0x7f)
                {
                    // Add a prefix zero to force unsigned if the MSB is 1
                    EncodeLength(stream, value.Length - prefixZeros + 1);
                    stream.Write((byte)0);
                }
                else
                {
                    EncodeLength(stream, value.Length - prefixZeros);
                }
                for (var i = prefixZeros; i < value.Length; i++)
                {
                    stream.Write(value[i]);
                }
            }
        }

        private static void EncodeLength(BinaryWriter stream, int length)
        {
            if (length < 0) throw new ArgumentOutOfRangeException("length", "Length must be non-negative");
            if (length < 0x80)
            {
                // Short form
                stream.Write((byte)length);
            }
            else
            {
                // Long form
                var temp = length;
                var bytesRequired = 0;
                while (temp > 0)
                {
                    temp >>= 8;
                    bytesRequired++;
                }
                stream.Write((byte)(bytesRequired | 0x80));
                for (var i = bytesRequired - 1; i >= 0; i--)
                {
                    stream.Write((byte)(length >> (8 * i) & 0xff));
                }
            }
        }


        

        private static void Test14()
        {
            var str = "E59E4316-7E81-4A43-94D6-32480C83ACE7@fa6ad071-6f0a-498f-8875-b9fb65625e15@70-8B-CD-0B-4D-D5,74-C6-3B-87-3F-8D";
            var result = str.GetBytes().RC4("设备".GetBytes()).Crc().GetBytes().ToHex();
            Console.WriteLine(result);
        }
    }
}