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
using NewLife.Configuration;
using System.Text;
using NewLife.Http;
using System.Net.WebSockets;
using XCode;
using XCode.Cache;

#if !NET40
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

            //new LogEventListener(new[] {
            //    "System.Runtime",
            //    "System.Diagnostics.Eventing.FrameworkEventSource",
            //    "System.Transactions.TransactionsEventSource",
            //    "Microsoft-Windows-DotNETRuntime",
            //    //"Private.InternalDiagnostics.System.Net.Sockets",
            //    "System.Net.NameResolution",
            //    //"Private.InternalDiagnostics.System.Net.NameResolution",
            //    "System.Net.Sockets",
            //    //"Private.InternalDiagnostics.System.Net.Http",
            //    "System.Net.Http",
            //    //"System.Data.DataCommonEventSource",
            //    //"Microsoft-Diagnostics-DiagnosticSource",
            //});

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
                Test1();
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
            var td = DAL.Create("tdengine");
            var tables = td.Tables;
            XTrace.WriteLine(tables.ToJson(true));

            var dt = td.Query("select * from t;");
            XTrace.WriteLine(dt.Total + "");

            //var guid = new Guid("00ac7f06-4612-4791-9c84-e221a2d963ad");
            //var buf = guid.ToByteArray();
            //XTrace.WriteLine(buf.ToHex());

            //var dal = DAL.Create("test");

            //var rs = dal.RestoreAll($"../dbbak.zip", null);

            //var tables = DAL.Import(File.ReadAllText("../data/lawyer.xml".GetFullPath()));
            //var table = tables.FirstOrDefault(e => e.Name == "SpringSession");
            //var dc1 = table.Columns[0];
            //var dc2 = table.Columns[1];
            //dc1.DataType = typeof(Guid);
            //dc2.DataType = typeof(Guid);
            //dal.Restore($"../data/SpringSession.table", table);

            //var dt = dal.Query("select * from spring_session");
            //XTrace.WriteLine("字段[{0}]：{1}", dt.Columns.Length, dt.Columns.Join());
            //XTrace.WriteLine("类型[{0}]：{1}", dt.Types.Length, dt.Types.Join(",", e => e?.Name));
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
            using var tracer = new DefaultTracer { Log = XTrace.Log };
            tracer.MaxSamples = 100;
            tracer.MaxErrors = 100;

            if (Console.ReadLine() == "1")
            {
                var svr = new ApiServer(12345)
                //var svr = new ApiServer("http://*:1234")
                {
                    Log = XTrace.Log,
                    //EncoderLog = XTrace.Log,
                    StatPeriod = 10,
                    Tracer = tracer,
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
                var client = new ApiClient("tcp://127.0.0.1:335,tcp://127.0.0.1:12345")
                {
                    Log = XTrace.Log,
                    //EncoderLog = XTrace.Log,
                    StatPeriod = 10,
                    Tracer = tracer,

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

        private static NetServer _server;
        private static async void Test5()
        {
            var server = new HttpServer
            {
                Port = 8080,
                Log = XTrace.Log,
                //SessionLog = XTrace.Log,
            };
            server.Map("/", () => "<h1>Hello NewLife!</h1></br> " + DateTime.Now.ToFullString() + "</br><img src=\"logos/leaf.png\" />");
            server.Map("/user", (String act, Int32 uid) => new { code = 0, data = $"User.{act}({uid}) success!" });
            server.MapStaticFiles("/logos", "images/");
            server.MapStaticFiles("/", "./");
            server.MapController<ApiController>("/api");
            server.Map("/my", new MyHttpHandler());
            server.Map("/ws", new WebSocketHandler());
            server.Start();

            _server = server;

#if NET5_0_OR_GREATER
            var client = new ClientWebSocket();
            await client.ConnectAsync(new Uri("ws://127.0.0.1:8080/ws"), default);
            await client.SendAsync("Hello NewLife".GetBytes(), System.Net.WebSockets.WebSocketMessageType.Text, true, default);

            var buf = new Byte[1024];
            var rs = await client.ReceiveAsync(buf, default);
            XTrace.WriteLine(new Packet(buf, 0, rs.Count).ToStr());

            await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "通信完成", default);
            XTrace.WriteLine("Close [{0}] {1}", client.CloseStatus, client.CloseStatusDescription);
#endif
        }

        class MyHttpHandler : IHttpHandler
        {
            public void ProcessRequest(IHttpContext context)
            {
                var name = context.Parameters["name"];
                var html = $"<h2>你好，<span color=\"red\">{name}</span></h2>";
                var files = context.Request.Files;
                if (files != null && files.Length > 0)
                {
                    foreach (var file in files)
                    {
                        file.SaveToFile();
                        html += $"<br />文件：{file.FileName} 大小：{file.Length} 类型：{file.ContentType}";
                    }
                }
                context.Response.SetResult(html);
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
            var config = new HttpConfigProvider
            {
                Server = "http://star.newlifex.com:6600",
                AppId = "Test",
                Period = 5,
            };
            //config.LoadAll();
            DAL.SetConfig(config);
            //DAL.GetConfig = config.GetConfig;

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
            var di = "Plugins".AsDirectory();
            if (di.Exists) di.Delete(true);

            //var db = DbFactory.Create(DatabaseType.MySql);
            //var db = DbFactory.Create(DatabaseType.PostgreSQL);
            var db = DbFactory.Create(DatabaseType.SQLite);
            var factory = db.Factory;
        }

        private static void Test9()
        {
            var cache = new SingleEntityCache<Int32, User> { Expire = 1 };

            // 首次访问
            var user = cache[1];
            XTrace.WriteLine("cache.Success={0}", cache.Success);

            user = cache[1];
            XTrace.WriteLine("cache.Success={0}", cache.Success);

            user = cache[1];
            XTrace.WriteLine("cache.Success={0}", cache.Success);

            EntityFactory.InitAll();

            XTrace.WriteLine("TestRole");
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
            var sb = new StringBuilder();
            for (var i = 0; i < 26; i++)
            {
                sb.Append((Char)('a' + i));
            }
            for (var i = 0; i < 26; i++)
            {
                sb.Append((Char)('A' + i));
            }
            for (var i = 0; i < 10; i++)
            {
                sb.Append((Char)('0' + i));
            }
            Console.WriteLine(sb);
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
            var rds = new Redis("127.0.0.1", null, 3)
            {
                Log = XTrace.Log
            };
            var rs = rds.Execute<Object>(null, rc => rc.Execute("XREAD", "count", "3", "streams", "stream_empty_item", "0-0"));
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

        /// <summary>测试config文件的写入</summary>
        private static void TestWriteConfig()
        {
            ConfigTest.Current.Names = new List<String> { "1", "2" };
            ConfigTest.Current.Sex = "1";
            ConfigTest.Current.xyf = new List<XYF>() { new XYF() { name = "123" }, new XYF() { name = "321" } };
            ConfigTest.Current.Save();

            //Class1.Current.Names = "123";
            //Class1.Current.Save();

            //Class1.Provider = XmlConfig;


        }

        /// <summary>测试config文件的读取</summary>
        private static void TestReadConfig()
        {
            var z = ConfigTest.Current.Names;
            var x = ConfigTest.Current.Sex;
            var y = ConfigTest.Current.xyf;
        }
    }
}