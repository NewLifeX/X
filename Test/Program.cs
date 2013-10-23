using System;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Reflection;
using System.Text;
using System.Threading;
using NewLife.Common;
using NewLife.CommonEntity;
using NewLife.IO;
using NewLife.Log;
using NewLife.Messaging;
using NewLife.Model;
using NewLife.Net;
using NewLife.Net.Common;
using NewLife.Net.Proxy;
using NewLife.Net.Sockets;
using NewLife.Reflection;
using NewLife.Serialization;
using NewLife.Threading;
using NewLife.Xml;
using XCode.DataAccessLayer;
using XCode.Sync;
using XCode.Transform;

#if NET4
using System.Linq;
#else
using NewLife.Linq;
using Microsoft.International.Converters.PinYinConverter;
using System.Web.Hosting;
using NewLife;
using NewLife.Security;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
#endif

namespace Test
{
    public class Program
    {
        private static void Main(string[] args)
        {
            XTrace.UseConsole();
            while (true)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
#if !DEBUG
                try
                {
#endif
                    Test11();
#if !DEBUG
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
#endif

                sw.Stop();
                Console.WriteLine("OK! 耗时 {0}", sw.Elapsed);
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key != ConsoleKey.C) break;
            }
        }

        static HttpProxy http = null;
        private static void Test1()
        {
            //var server = new HttpReverseProxy();
            //server.Port = 888;
            //server.ServerHost = "www.cnblogs.com";
            //server.ServerPort = 80;
            //server.Start();

            //var ns = Enum.GetNames(typeof(ConsoleColor));
            //var vs = Enum.GetValues(typeof(ConsoleColor));
            //for (int i = 0; i < ns.Length; i++)
            //{
            //    Console.ForegroundColor = (ConsoleColor)vs.GetValue(i);
            //    Console.WriteLine(ns[i]);
            //}

            //NewLife.Net.Application.AppTest.Start();

            http = new HttpProxy();
            http.Port = 8080;
            http.EnableCache = true;
            //http.OnResponse += new EventHandler<HttpProxyEventArgs>(http_OnResponse);
            http.Start();

            var old = HttpProxy.GetIEProxy();
            if (!old.IsNullOrWhiteSpace()) Console.WriteLine("旧代理：{0}", old);
            HttpProxy.SetIEProxy("127.0.0.1:" + http.Port);
            Console.WriteLine("已设置IE代理，任意键结束测试，关闭IE代理！");

            ThreadPoolX.QueueUserWorkItem(ShowStatus);

            Console.ReadKey(true);
            HttpProxy.SetIEProxy(old);

            //server.Dispose();
            http.Dispose();

            //var ds = new DNSServer();
            //ds.Start();

            //for (int i = 5; i < 6; i++)
            //{
            //    var buffer = File.ReadAllBytes("dns" + i + ".bin");
            //    var entity2 = DNSEntity.Read(buffer, false);
            //    Console.WriteLine(entity2);

            //    var buffer2 = entity2.GetStream().ReadBytes();

            //    var p = buffer.CompareTo(buffer2);
            //    if (p != 0)
            //    {
            //        Console.WriteLine("{0:X2} {1:X2} {2:X2}", p, buffer[p], buffer2[p]);
            //    }
            //}
        }

        static void ShowStatus()
        {
            //var pool = PropertyInfoX.GetValue<SocketBase, ObjectPool<NetEventArgs>>("Pool");
            var pool = NetEventArgs.Pool;

            while (true)
            {
                var asyncCount = 0; try
                {
                    foreach (var item in http.Servers)
                    {
                        asyncCount += item.AsyncCount;
                    }
                    foreach (var item in http.Sessions.Values.ToArray())
                    {
                        var remote = (item as IProxySession).Remote;
                        if (remote != null) asyncCount += remote.Host.AsyncCount;
                    }
                }
                catch (Exception ex) { Console.WriteLine(ex.ToString()); }

                Int32 wt = 0;
                Int32 cpt = 0;
                ThreadPool.GetAvailableThreads(out wt, out cpt);
                Int32 threads = Process.GetCurrentProcess().Threads.Count;

                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("异步:{0} 会话:{1} Thread:{2}/{3}/{4} Pool:{5}/{6}/{7}", asyncCount, http.Sessions.Count, threads, wt, cpt, pool.StockCount, pool.FreeCount, pool.CreateCount);
                Console.ForegroundColor = color;

                Thread.Sleep(3000);

                //GC.Collect();
            }
        }

        static void Test2()
        {
            HttpClientMessageProvider client = new HttpClientMessageProvider();
            client.Uri = new Uri("http://localhost:8/Web/MessageHandler.ashx");

            var rm = MethodMessage.Create("Admin.Login", "admin", "admin");
            //rm.Header.Channel = 88;

            //Message.Debug = true;
            //var ms = rm.GetStream();
            //var m2 = Message.Read(ms);

            Message msg = client.SendAndReceive(rm, 0);
            var rs = msg as EntityMessage;
            Console.WriteLine("返回：" + rs.Value);

            msg = client.SendAndReceive(rm, 0);
            rs = msg as EntityMessage;
            Console.WriteLine("返回：" + rs.Value);
        }

        static void Test3()
        {
            using (var sp = new SerialPort("COM2"))
            {
                sp.Open();

                var b = 0;
                while (true)
                {
                    Console.WriteLine(b);
                    var bs = new Byte[] { (Byte)b };
                    sp.Write(bs, 0, bs.Length);
                    b = b == 0 ? 0xFF : 0;

                    Thread.Sleep(1000);
                }
            }
        }

        static NetServer server = null;
        static IMessageProvider smp = null;
        static IMessageProvider cmp = null;
        static void Test4()
        {
            Console.Clear();
            if (server == null)
            {
                server = new NetServer();
                server.Port = 1234;
                //server.Received += new EventHandler<NetEventArgs>(server_Received);

                var mp = new ServerMessageProvider(server);
                mp.OnReceived += new EventHandler<MessageEventArgs>(smp_OnReceived);
                //mp.MaxMessageSize = 1460;
                mp.AutoJoinGroup = true;
                smp = mp;

                server.Start();
            }

            if (cmp == null)
            {
                var client = NetService.CreateSession(new NetUri("udp://::1:1234"));
                client.ReceiveAsync();
                cmp = new ClientMessageProvider() { Session = client };
                cmp.OnReceived += new EventHandler<MessageEventArgs>(cmp_OnReceived);
            }

            //Message.Debug = true;
            var msg = new EntityMessage();
            var rnd = new Random((Int32)DateTime.Now.Ticks);
            var bts = new Byte[rnd.Next(1000000, 5000000)];
            //var bts = new Byte[1460 * 1 - rnd.Next(0, 20)];
            rnd.NextBytes(bts);
            msg.Value = bts;

            //var rs = cmp.SendAndReceive(msg, 5000);
            cmp.Send(msg);
        }

        static void smp_OnReceived(object sender, MessageEventArgs e)
        {
            var msg = e.Message;
            Console.WriteLine("服务端收到：{0}", msg);
            var rs = new EntityMessage();
            rs.Value = "收到" + msg;
            (sender as IMessageProvider).Send(rs);
        }

        static void cmp_OnReceived(object sender, MessageEventArgs e)
        {
            Console.WriteLine("客户端收到：{0}", e.Message);
        }

        static void Test5()
        {
            DAL.AddConnStr("xxgk", "Data Source=192.168.1.21;Initial Catalog=信息公开;user id=sa;password=Pass@word", null, "mssql");
            var dal = DAL.Create("xxgk");

            DAL.AddConnStr("xxgk2", "Data Source=XXGK.db;Version=3;", null, "sqlite");
            File.Delete("XXGK.db");

            //DAL.ShowSQL = false;

            var etf = new EntityTransform();
            etf.SrcConn = "xxgk";
            etf.DesConn = "xxgk2";
            etf.AllowInsertIdentity = true;
            //etf.TableNames.Remove("PubInfoLog");
            //etf.TableNames.Remove("PublicInformation");
            //etf.TableNames.Remove("SystemUserLog");
            etf.PartialTableNames.Add("PubInfoLog");
            etf.PartialTableNames.Add("PublicInformation");
            etf.PartialTableNames.Add("SystemUserLog");
            etf.PartialCount = 25;
            etf.OnTransformTable += (s, e) => { if (e.Arg.TableName == "")e.Arg = null; };
            var rs = etf.Transform();
            Console.WriteLine("共转移：{0}", rs);
        }

        static void Test6()
        {
            Message.DumpStreamWhenError = true;
            //var msg = new EntityMessage();
            //msg.Value = Guid.NewGuid();
            var msg = new MethodMessage();
            msg.TypeName = "Admin";
            msg.Name = "Login";
            msg.Parameters = new Object[] { "admin", "password" };

            var kind = RWKinds.Json;
            var ms = msg.GetStream(kind);
            //ms = new MemoryStream(ms.ReadBytes(ms.Length - 1));
            //Console.WriteLine(ms.ReadBytes().ToHex());
            Console.WriteLine(ms.ToStr());
            //ms = msg.GetStream(RWKinds.Xml);
            //Console.WriteLine(ms.ToStr());

            Message.Debug = true;
            ms.Position = 0;
            var rs = Message.Read(ms, kind);
            Console.WriteLine(rs);
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
            var tb = Administrator.Meta.Table.DataTable;
            var table = ObjectContainer.Current.Resolve<IDataTable>();
            table = table.CopyAllFrom(tb);

            // 添加两个字段
            var fi = table.CreateColumn();
            fi.ColumnName = "LastUpdate";
            fi.DataType = typeof(DateTime);
            table.Columns.Add(fi);

            fi = table.CreateColumn();
            fi.ColumnName = "LastSync";
            fi.DataType = typeof(DateTime);
            table.Columns.Add(fi);

            var dal = DAL.Create("Common99");
            // 检查架构
            dal.SetTables(table);

            var sl = new SyncSlave();
            sl.Factory = dal.CreateOperate(table.TableName);

            var mt = new SyncMaster();
            mt.Facotry = Administrator.Meta.Factory;
            //mt.LastUpdateName = Administrator._.LastLogin;

            var sm = new SyncManager();
            sm.Slave = sl;
            sm.Master = mt;

            sm.Start();
        }

        static void Test10()
        {
            var str = "我是超级大石头！";
            Console.WriteLine(str);
            var buf = Encoding.UTF8.GetBytes(str);
            Console.WriteLine("明文：{0}", BitConverter.ToString(buf));

            var keys = RSAHelper.GenerateKey();

            var mw = RSAHelper.EncryptWithDES(buf, keys[1]);
            Console.WriteLine("密文：{0}", BitConverter.ToString(mw));

            var jm = RSAHelper.DecryptWithDES(mw, keys[0]);
            Console.WriteLine("明文：{0}", BitConverter.ToString(jm));

            str = Encoding.UTF8.GetString(jm);
            Console.WriteLine(str);

            var sig = RSAHelper.Sign(buf, keys[0]);
            Console.WriteLine("签名：{0}", sig.ToHex());
            var rs = RSAHelper.Verify(buf, keys[1], sig);
            Console.WriteLine("验证：{0}", rs ? "没有被修改过" : "被修改过");

            buf[0] = 1;
            rs = RSAHelper.Verify(buf, keys[1], sig);
            Console.WriteLine("验证：{0}", rs ? "没有被修改过" : "被修改过");

            var ds = DSAHelper.GenerateKey();

            sig = DSAHelper.Sign(buf, ds[0]);
            Console.WriteLine("签名：{0}", sig.ToHex());
            rs = DSAHelper.Verify(buf, ds[1], sig);
            Console.WriteLine("验证：{0}", rs ? "没有被修改过" : "被修改过");

            buf[0] = 2;
            rs = DSAHelper.Verify(buf, ds[1], sig);
            Console.WriteLine("验证：{0}", rs ? "没有被修改过" : "被修改过");

            //CodeTimer.ShowHeader("RSA加解密测试");
            //var ct = new CodeTimer();
            //ct.ShowProgress = true;
            //ct.Times = 1000;
            //ct.Action = n =>
            //{
            //    var mw2 = RSAHelper.EncryptWithDES(buf, keys[1]);
            //    var jm2 = RSAHelper.DecryptWithDES(mw2, keys[0]);
            //};
            //ct.TimeOne();
            //ct.Time();
            //Console.WriteLine("平均每次时间：{0:n0}毫秒", ct.Elapsed.TotalMilliseconds / ct.Times);
        }

        static void Test11()
        {
            XTrace.WriteLine("xxx");

            var log = XTrace.Log;
            log.Debug("111");
            log.Info("222");
            log.Warn("333");
            log.Error("444");
            log.Fatal("555");
        }

        static void Test12()
        {
            var str = "[b]www.NewLifeX.com[/b]";
            var pat = "\\[b(?:\\s*)\\]";
            var dst = "<b>";

            TestRegex("短字符串没有预热", 1000 * 10000, false, pat, str, dst);
            TestRegex("短字符串带有预热", 1000 * 10000, true, pat, str, dst);

            str = @"[color=Blue][b]1，开发板是必须的[/b][/color]。当然，如果我们的嵌入式社区足够强大，你也可以不需要开发板，只安装开发环境，然后在软件模拟器上测试程序，测试后直接生产。因为刚开始的时候学MF，我买的是红牛开发板二代，现在最新的是红牛三代。  前面有说到，MF默认不支持STM32，得自己Port，我当然不会了，只好从网上买现成的（包括已经Port好的MF）（458元）[url=http://item.taobao.com/item.htm?id=7117999726]http://item.taobao.com/item.htm?id=7117999726[/url]。  更先进的有红牛三代（786元）[url=http://item.taobao.com/item.htm?id=10919470266]http://item.taobao.com/item.htm?id=10919470266[/url]。  上面两个其实是开发板加MF STM32授权的价格，如果不用MF，可以在这里买到同样的开发板（上面店家就是从这里买的）  红牛二代（279元）[url=http://item.taobao.com/item.htm?id=2558683476]http://item.taobao.com/item.htm?id=2558683476[/url]  红牛三代（539元）[url=http://item.taobao.com/item.htm?id=9940198732]http://item.taobao.com/item.htm?id=9940198732[/url]  购买的时候，最好问问能不能加几块钱或者十多快钱让店家帮忙把排针焊好，我的忘了说，后悔死了。  如果想省钱的，考虑别买显示屏，那样估计便宜很多。我们是程序员，在电脑上看调试输出更棒，哈哈。    [color=Blue][b]2，开发板外设[/b][/color]。这些是可有可无的啦，建议根据自己情况选购。  我在上面那位老大那里买的东西（[url=http://netmf.taobao.com/]http://netmf.taobao.com/[/url]）  1）智能小车98元  2）直流电机驱动器48元  3）超声波模块49元  4）蓝牙模块48元  5）激光模块16元  6）LED数码管9元  7）电池盒2元  8）杜邦线10份5元  9）面包板20元    [color=Blue][b]3，万用表。[/b][/color]有时候呢，程序测试正常，但是控制的设备不正常，比如小灯不亮、电机不转、对方收不到信号等，这个时候，可以用万用表测试一下。  优利德万用表（38.89元）[url=http://detail.tmall.com/item.htm?id=9865787128&amp;prt=1332986794851]http://detail.tmall.com/item.htm?id=9865787128&amp;prt=1332986794851[/url]    [color=Blue][b]4，USB转串口。[/b][/color]笔记本上没有串口，虽然红牛板可以用USB线写入程序，但是刷固件的时候还是得用串口，最好买一个。win7可以从微软官网自动下驱动。  Z-TEK力特 USB2.0转串口头（牌子货42元）[url=http://item.taobao.com/item.htm?id=118787362]http://item.taobao.com/item.htm?id=118787362[/url]    [color=Blue][b]5，USB逻辑分析仪。[/b][/color]大学模电经常用示波器，那种高档货我们买不起，可以用这种USB逻辑分析仪，通过分析仪收集信号，然后通过USB先输入电脑，然后软件显示。对于我们程序员来说，这个方案最划算。大部分通许不需要看波形的就别买了。  逻辑分析仪 USB Saleae 24M 8CH（48元）[url=http://item.taobao.com/item.htm?id=8430104015]http://item.taobao.com/item.htm?id=8430104015[/url]  另外我买了10个夹子    [color=Blue][b]6，USB转UART串口模块。[/b][/color]我实在是懒，想直接通过电脑串口输出TTL电平（比如往串口写0x55，就会输出01010101），于是买了这个。它也有USB转串口的功能，但是它的输出口并不是RS232那种9针，是不能直接接串口线的。  USB转UART串口模块（38元）[url=http://item.taobao.com/item.htm?id=12562651145]http://item.taobao.com/item.htm?id=12562651145[/url]    [color=Blue][b]7，仿真器JLink。[/b][/color]一定意义上来讲，做嵌入式开发，不能没有仿真器。据说这个最好有，我还没用，不大懂。  JLINK V8仿真器（74元）[url=http://item.taobao.com/item.htm?id=14330639543]http://item.taobao.com/item.htm?id=14330639543[/url]    [color=Blue][b]8，STM32最小板。[/b][/color]我们只是软件工程师，不是硬件工程师，再去学怎么画板子，实在痛苦。不过我们可以购买核心板（最小板），然后加上需要的外部设备，来达到目的。开发板实际上等于核心板加上一大堆的各种外设，核心板一般只有MCU、晶振、看门狗，有些有外扩SRAM（内存）和NAND Flash（等同于硬盘）。注意：我买的这个最小板是STM32F101，在主频和内存上都比红牛二开发板的STM32F103要低，所以便宜。  STM32最小板STM32F101（38元）[url=http://item.taobao.com/item.htm?id=12473293081]http://item.taobao.com/item.htm?id=12473293081[/url]    [color=Blue][b]9，VisualStudio 2010。[/b][/color]身为软件工程师，别告诉我你没有这个。如果采用MF，那么就只需要用这个就足够啦，哈哈！    [color=Blue][b]10，Keil v4.5。[/b][/color]这是正牌的嵌入式开发工具，用C语言写代码。不多说，下载地址：  mdk450.part1.rar [url=http://www.kuaipan.cn/file/id_2378544298616973.html]http://www.kuaipan.cn/file/id_2378544298616973.html[/url]  mdk450.part2.rar [url=http://www.kuaipan.cn/file/id_2378544298616974.html]http://www.kuaipan.cn/file/id_2378544298616974.html[/url]  MDK4.12注册机 [url=http://www.kuaipan.cn/file/id_2378544298617545.html]http://www.kuaipan.cn/file/id_2378544298617545.html[/url]    最后，最低成本的方案就是什么都不买，其次就是买一块STM32F103最小板，最豪华的当然是啥都可以买啦。";
            pat = "\\[url=(([^\\[\"']+?))(?:\\s*)\\]([\\s\\S]+?)\\[/url(?:\\s*)\\]";
            dst = "<a href=\"$1\" target=\"_blank\">$3</a>";

            TestRegex("长字符串没有预热", 10 * 10000, false, pat, str, dst);
            TestRegex("长字符串带有预热", 10 * 10000, true, pat, str, dst);

        }

        static void TestRegex(String title, Int32 times, Boolean needTimeOne, String pat, String str, String dst)
        {
            var rs = "";

            GC.Collect(0, GCCollectionMode.Forced);
            Console.WriteLine();
            Console.WriteLine(title);
            CodeTimer.ShowHeader();
            var opt = RegexOptions.IgnoreCase;
            var reg = new Regex(pat, opt);
            CodeTimer.TimeLine("实例不编译", times, n => rs = reg.Replace(str, dst), true);
            CodeTimer.TimeLine("静态不编译", times, n => rs = Regex.Replace(str, pat, dst, opt), true);

            opt = RegexOptions.IgnoreCase | RegexOptions.Compiled;
            reg = new Regex(pat, opt);
            CodeTimer.TimeLine("实例有编译", times, n => rs = reg.Replace(str, dst), true);
            CodeTimer.TimeLine("静态有编译", times, n => rs = Regex.Replace(str, pat, dst, opt), true);
        }
    }
}