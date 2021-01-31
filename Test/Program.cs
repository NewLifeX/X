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
using XCode.Code;
using System.Reflection;
using System.Security.Cryptography;
using NewLife.Data;
using System.Threading.Tasks;

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

            XTrace.UseConsole();
#if DEBUG
            XTrace.Debug = true;
            XTrace.Log.Level = LogLevel.All;

            var set = NewLife.Setting.Current;
            set.Debug = true;
            set.LogLevel = LogLevel.All;

            new LogEventListener(new[] {
                "System.Runtime",
                "System.Diagnostics.Eventing.FrameworkEventSource",
                "System.Transactions.TransactionsEventSource",
                "Microsoft-Windows-DotNETRuntime",
                //"Private.InternalDiagnostics.System.Net.Sockets",
                "System.Net.NameResolution",
                //"Private.InternalDiagnostics.System.Net.NameResolution",
                "System.Net.Sockets",
                //"Private.InternalDiagnostics.System.Net.Http",
                "System.Net.Http",
                //"System.Data.DataCommonEventSource",
                //"Microsoft-Diagnostics-DiagnosticSource",
            });

            var set2 = XCode.Setting.Current;
            set2.Debug = true;
#endif
            while (true)
            {
                var sw = Stopwatch.StartNew();
#if !DEBUG
                try
                {
#endif
                    Test4();
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
            var b = (Byte)0x0F;
            XTrace.WriteLine("{0} {0:X} {0:X2}", b);
            // 15 F 0F

            //var keys = ECDsaHelper.GenerateKey();
            //XTrace.WriteLine("prvKey:{0}", keys[0]);
            //XTrace.WriteLine("pubKey:{0}", keys[1]);

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
            var sw = Stopwatch.StartNew();

            var count = 100_000_000L;

            var ts = new List<Task>();
            for (var i = 0; i < Environment.ProcessorCount; i++)
            {
                ts.Add(Task.Run(() =>
                {
                    var f = new Snowflake();

                    for (var i = 0; i < count; i++)
                    {
                        var id = f.NewId();
                    }
                }));
            }

            Task.WaitAll(ts.ToArray());

            sw.Stop();

            count *= ts.Count;

            XTrace.WriteLine("生成 {0:n0}，耗时 {1}，速度 {2:n0}tps", count, sw.Elapsed, count * 1000 / sw.ElapsedMilliseconds);
        }

        private static void Test3()
        {
            var tracer = DefaultTracer.Instance ?? new DefaultTracer();
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
                        Counter = new PerfCounter(),
                        Tracer = new DefaultTracer { Log = XTrace.Log },
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
            var type = typeof(DateTime?);
            var tc = Type.GetTypeCode(type);
            Console.WriteLine(tc);

            var set = XCode.Setting.Current;
            set.EntityCacheExpire = 5;

            Log.Meta.Session.Dal.Db.ShowSQL = true;

            for (var i = 0; i < 10; i++)
            {
                LogProvider.Provider.WriteLog("test" + i, "test", true, "xxx");
            }

            for (var i = 0; i < 1000; i++)
            {
                var names = Log.FindAllCategoryName();
                XTrace.WriteLine("names: {0}", names.Count);

                Thread.Sleep(1000);
            }
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

            XCode.Setting.Current.Migration = Migration.Full;
            //Role.Meta.Session.Dal.Db.Migration = Migration.Full;
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

            //var url = "http://www.mca.gov.cn/article/sj/xzqh/2020/2020/2020092500801.html";
            //Area.FetchAndSave(url);

            //var file = "../Area20200929.csv";
            //var file = "Area.csv.gz";
            var file = "http://x.newlifex.com/Area.csv.gz";
            //var list = new List<Area>();
            //list.LoadCsv(file);

            //Area.MergeLevel3(list, true);
            //Area.MergeLevel4(list, true);

            Area.Import(file, true);

            Area.Export($"Area_{DateTime.Now:yyyyMMddHHmmss}.csv.gz");
        }

        private static void Test9()
        {
            var r0 = Role.FindByName("Stone");
            r0?.Delete();

            var r = new Role
            {
                Name = "Stone"
            };
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
            var args = Environment.GetCommandLineArgs();
            if (args == null || args.Length < 2) return;

            XTrace.WriteLine(args[1]);

            var count = 10 * 1024 * 1024;
#if DEBUG
            count = 1024;
#endif
            var fi = args[1].AsFile();
            if (!fi.Exists || fi.Length < count) return;

            // 取最后1M
            using var fs = fi.OpenRead();
            var count2 = count;
            if (count2 > fs.Length) count2 = (Int32)fs.Length;
            //fs.Seek(count2, SeekOrigin.End);
            fs.Position = fs.Length - count2;

            var buf = fs.ReadBytes();
            File.WriteAllBytes($"{DateTime.Now:yyyyMMddHHmmss}.log".GetFullPath(), buf);
        }

        private static void Test11()
        {
        }

        /// <summary>测试序列化</summary>
        private static void Test12()
        {
            var option = new BuilderOption();
            var tables = ClassBuilder.LoadModels("../../NewLife.Cube/CubeDemoNC/Areas/School/Models/Model.xml", option, out var atts);
            EntityBuilder.BuildTables(tables, option);
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

        private static void Test14()
        {
            var str = "E59E4316-7E81-4A43-94D6-32480C83ACE7@fa6ad071-6f0a-498f-8875-b9fb65625e15@70-8B-CD-0B-4D-D5,74-C6-3B-87-3F-8D";
            var result = str.GetBytes().RC4("设备".GetBytes()).Crc().GetBytes().ToHex();
            Console.WriteLine(result);
        }

        ///// <summary>
        ///// 私钥XML2PEM
        ///// </summary>
        //private static void XMLConvertToPEM()//XML格式密钥转PEM
        //{
        //    var rsa2 = new RSACryptoServiceProvider();
        //    using (var sr = new StreamReader("D:\\keys\\private.key"))
        //    {
        //        rsa2.FromXmlString(sr.ReadToEnd());
        //    }
        //    var p = rsa2.ExportParameters(true);

        //    var key = new RsaPrivateCrtKeyParameters(
        //        new Org.BouncyCastle.Math.BigInteger(1, p.Modulus), new Org.BouncyCastle.Math.BigInteger(1, p.Exponent), new Org.BouncyCastle.Math.BigInteger(1, p.D),
        //        new Org.BouncyCastle.Math.BigInteger(1, p.P), new Org.BouncyCastle.Math.BigInteger(1, p.Q), new Org.BouncyCastle.Math.BigInteger(1, p.DP), new Org.BouncyCastle.Math.BigInteger(1, p.DQ),
        //        new Org.BouncyCastle.Math.BigInteger(1, p.InverseQ));

        //    using (var sw = new StreamWriter("D:\\keys\\PrivateKey.pem"))
        //    {
        //        var pemWriter = new PemWriter(sw);
        //        pemWriter.WriteObject(key);
        //    }
        //}

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
                writer.Write((Byte)0x30); // SEQUENCE
                using (var innerStream = new MemoryStream())
                {
                    var innerWriter = new BinaryWriter(innerStream);
                    EncodeIntegerBigEndian(innerWriter, new Byte[] { 0x00 }); // Version
                    EncodeIntegerBigEndian(innerWriter, parameters.Modulus);
                    EncodeIntegerBigEndian(innerWriter, parameters.Exponent);

                    //All Parameter Must Have Value so Set Other Parameter Value Whit Invalid Data  (for keeping Key Structure  use "parameters.Exponent" value for invalid data)
                    EncodeIntegerBigEndian(innerWriter, parameters.Exponent); // instead of parameters.D
                    EncodeIntegerBigEndian(innerWriter, parameters.Exponent); // instead of parameters.P
                    EncodeIntegerBigEndian(innerWriter, parameters.Exponent); // instead of parameters.Q
                    EncodeIntegerBigEndian(innerWriter, parameters.Exponent); // instead of parameters.DP
                    EncodeIntegerBigEndian(innerWriter, parameters.Exponent); // instead of parameters.DQ
                    EncodeIntegerBigEndian(innerWriter, parameters.Exponent); // instead of parameters.InverseQ

                    var length = (Int32)innerStream.Length;
                    EncodeLength(writer, length);
                    writer.Write(innerStream.GetBuffer(), 0, length);
                }

                var base64 = Convert.ToBase64String(stream.GetBuffer(), 0, (Int32)stream.Length).ToCharArray();
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

        private static void EncodeIntegerBigEndian(BinaryWriter stream, Byte[] value, Boolean forceUnsigned = true)
        {
            stream.Write((Byte)0x02); // INTEGER
            var prefixZeros = 0;
            for (var i = 0; i < value.Length; i++)
            {
                if (value[i] != 0) break;
                prefixZeros++;
            }
            if (value.Length - prefixZeros == 0)
            {
                EncodeLength(stream, 1);
                stream.Write((Byte)0);
            }
            else
            {
                if (forceUnsigned && value[prefixZeros] > 0x7f)
                {
                    // Add a prefix zero to force unsigned if the MSB is 1
                    EncodeLength(stream, value.Length - prefixZeros + 1);
                    stream.Write((Byte)0);
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

        private static void EncodeLength(BinaryWriter stream, Int32 length)
        {
            if (length < 0) throw new ArgumentOutOfRangeException("length", "Length must be non-negative");
            if (length < 0x80)
            {
                // Short form
                stream.Write((Byte)length);
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
                stream.Write((Byte)(bytesRequired | 0x80));
                for (var i = bytesRequired - 1; i >= 0; i--)
                {
                    stream.Write((Byte)(length >> (8 * i) & 0xff));
                }
            }
        }

        // dsa xml 转 pem
        private static void DSAXML2PEM()
        {
            // 私钥转换
            var dsa = new DSACryptoServiceProvider();
            using (var fs = new FileStream("D:\\token.prvkey", FileMode.Open, FileAccess.Read))
            {
                var sr = new StreamReader(fs);
                dsa.FromXmlStringX(sr.ReadToEnd());
            }

            //// 私钥
            //var dsaKey = DotNetUtilities.GetDsaKeyPair(dsa);
            //using (var sw = new StreamWriter("D:\\dsaprv.pem"))
            //{
            //    var pw = new PemWriter(sw);
            //    pw.WriteObject(dsaKey.Private);
            //}
            //// 公钥
            //using (var sw = new StreamWriter("D:\\dsapub.pem"))
            //{
            //    var pw = new PemWriter(sw);
            //    pw.WriteObject(dsaKey.Public);
            //}


            //// 公钥转换
            //var pubdsa = new DSACryptoServiceProvider();
            //using (var fs = new FileStream("D:\\token.pubkey", FileMode.Open, FileAccess.Read))
            //{
            //    var sr = new StreamReader(fs);
            //    pubdsa.FromXmlStringX(sr.ReadToEnd());
            //}

            //var dsapub = DotNetUtilities.GetDsaPublicKey(pubdsa);
            //using (var sw = new StreamWriter("D:\\dsapub1.pem"))
            //{
            //    var pw = new PemWriter(sw);
            //    pw.WriteObject(dsapub);
            //}
        }

        //// dsa public pem 转 xml
        //private static void DSAPublicPEM2XML()
        //{
        //    DSA dsa;
        //    using (var rdr = new StreamReader("D:\\dsapub.pem"))
        //    {
        //        var pr = new PemReader(rdr);
        //        var o = pr.ReadObject() as DsaPublicKeyParameters;
        //        var prm = new CspParameters(13);
        //        prm.Flags = CspProviderFlags.UseMachineKeyStore;

        //        dsa = new DSACryptoServiceProvider(prm);
        //        var dp = new DSAParameters
        //        {
        //            G = o.Parameters.G.ToByteArrayUnsigned(),
        //            P = o.Parameters.P.ToByteArrayUnsigned(),
        //            Q = o.Parameters.Q.ToByteArrayUnsigned(),
        //            Y = o.Y.ToByteArrayUnsigned()
        //        };

        //        if (o.Parameters.ValidationParameters != null)
        //        {
        //            dp.Counter = o.Parameters.ValidationParameters.Counter;
        //            dp.Seed = o.Parameters.ValidationParameters.GetSeed();
        //        }

        //        dsa.ImportParameters(dp);
        //    }

        //    // 写入xml文件
        //    using (var fs = new FileStream("D:\\xtoken.pubkey", FileMode.Create, FileAccess.Write))
        //    {
        //        var sw = new StreamWriter(fs);

        //        var xml = dsa.ToXmlString(false);
        //        sw.Write(xml);
        //        sw.Flush();
        //        sw.Dispose();
        //    }
        //}

        //// dsa private pem 转 xml
        //private static void DSAPrivatePEM2XML()
        //{
        //    DSA prvDsa;
        //    DSA pubDsa;

        //    using (var rdr = new StreamReader("D:\\dsaprv.pem"))
        //    {
        //        var pr = new PemReader(rdr);
        //        var opair = pr.ReadObject() as AsymmetricCipherKeyPair;

        //        var prm = new CspParameters(13);
        //        prm.Flags = CspProviderFlags.UseMachineKeyStore;

        //        //var prm1 = new CspParameters(13);
        //        //prm1.Flags = CspProviderFlags.UseMachineKeyStore;

        //        prvDsa = new DSACryptoServiceProvider(prm);
        //        pubDsa = new DSACryptoServiceProvider(prm);

        //        // 私钥
        //        var prvpara = opair.Private as DsaPrivateKeyParameters;
        //        var prvdp = new DSAParameters
        //        {
        //            G = prvpara.Parameters.G.ToByteArrayUnsigned(),
        //            P = prvpara.Parameters.P.ToByteArrayUnsigned(),
        //            Q = prvpara.Parameters.Q.ToByteArrayUnsigned(),
        //            X = prvpara.X.ToByteArrayUnsigned()
        //        };
        //        if (prvpara.Parameters.ValidationParameters != null)
        //        {
        //            prvdp.Counter = prvpara.Parameters.ValidationParameters.Counter;
        //            prvdp.Seed = prvpara.Parameters.ValidationParameters.GetSeed();
        //        }
        //        prvDsa.ImportParameters(prvdp);

        //        // 公钥
        //        var pubpara = opair.Public as DsaPublicKeyParameters;
        //        var pubdp = new DSAParameters
        //        {
        //            G = pubpara.Parameters.G.ToByteArrayUnsigned(),
        //            P = pubpara.Parameters.P.ToByteArrayUnsigned(),
        //            Q = pubpara.Parameters.Q.ToByteArrayUnsigned(),
        //            Y = pubpara.Y.ToByteArrayUnsigned()
        //        };
        //        if (pubpara.Parameters.ValidationParameters != null)
        //        {
        //            pubdp.Counter = pubpara.Parameters.ValidationParameters.Counter;
        //            pubdp.Seed = pubpara.Parameters.ValidationParameters.GetSeed();
        //        }
        //        pubDsa.ImportParameters(pubdp);
        //    }

        //    // 写入xml文件 private
        //    using (var sw = new StreamWriter("D:\\xtoken.prvkey"))
        //    {
        //        //var sw = new StreamWriter(fs);

        //        var xml = prvDsa.ToXmlString(true);
        //        sw.Write(xml);
        //        sw.Flush();
        //        //sw.Dispose();
        //    }
        //    // 写入xml文件 public
        //    using (var fs = new FileStream("D:\\xtoken.pubkey", FileMode.Create, FileAccess.Write))
        //    {
        //        var sw = new StreamWriter(fs);
        //        var xml = pubDsa.ToXmlString(false);
        //        sw.Write(xml);
        //        sw.Flush();
        //        sw.Dispose();
        //    }
        //}

        // 测试加密
        private static void Test15()
        {
            Byte[] signStr;

            using (var prvfs = new FileStream("D:\\xtoken.prvkey", FileMode.Open, FileAccess.Read))
            {
                var sr = new StreamReader(prvfs);
                var prvdsa = new DSACryptoServiceProvider();
                prvdsa.FromXmlStringX(sr.ReadToEnd());

                signStr = prvdsa.SignData("123".GetBytes());
                Console.WriteLine("签名结果：" + signStr.ToBase64());
            }

            using (var pubfs = new FileStream("D:\\xtoken.pubkey", FileMode.Open, FileAccess.Read))
            {
                var sr = new StreamReader(pubfs);
                var pubdsa = new DSACryptoServiceProvider();
                pubdsa.FromXmlStringX(sr.ReadToEnd());

                var result = pubdsa.VerifyData("123".GetBytes(), signStr);
                Console.WriteLine("验证结果:" + result);
            }
        }

        private static void TestReadAppSettings()
        {
            var str = DAL.ConnStrs["MySQL.AppSettings"];
            Console.WriteLine(str);
            Console.WriteLine(DAL.ConnStrs["MySQL.AppSettings.default"]);
        }
    }
}