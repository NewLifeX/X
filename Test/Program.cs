using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using NewLife.Http;
using NewLife.Log;
using NewLife.Net;
using NewLife.Security;
using NewLife.Serialization;
using NewLife;
using System.Threading.Tasks;
using NewLife.Data;
using NewLife.Remoting;

namespace Test;

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
            var sw = new Stopwatch();
            sw.Start();
            try
            {
                Test1();
            }
            catch (Exception ex)
            {
                ex = ex.GetTrue();
                XTrace.WriteException(ex);
            }

            sw.Stop();
            Console.WriteLine("OK! 耗时 {0}", sw.Elapsed);
            var key = Console.ReadKey(true);
            if (key.Key != ConsoleKey.C) break;
        }
    }

    static void Test1()
    {
        var file = "D:\\ZTO\\Simulink\\Bin\\Backup\\RouteDispBaseInfo_20240729190159.gz";
        file = file.GetFullPath();

        var buf = File.ReadAllBytes(file);
        buf = buf.DecompressGZip();

        var dt = new DbTable();
        //var rs = dt.LoadFile(file, false);
        var rs = dt.Read(buf);

        {
            using var apiServer = new ApiServer(19000)
            {
                Log = XTrace.Log,
                EncoderLog = XTrace.Log,
            };
            apiServer.Start();
            //apiServer.Server.TryDispose();
        }
        GC.Collect();
        Thread.Sleep(3000);
        Console.WriteLine();
        {
            var netUri = new NetUri("tcp://0.0.0.0:19000");
            using var apiServer = new ApiServer(netUri)
            {
                Log = XTrace.Log,
                EncoderLog = XTrace.Log,
            };
            apiServer.Start();
        }
        Thread.Sleep(3000);
        Console.WriteLine();
        {
            var netUri = new NetUri("tcp://[::]:19000");
            using var apiServer = new ApiServer(netUri)
            {
                Log = XTrace.Log,
                EncoderLog = XTrace.Log,
            };
            apiServer.Start();
        }
        Thread.Sleep(3000);
        Console.WriteLine();
        {
            var netUri = new NetUri("tcp://*:19000");
            using var apiServer = new ApiServer(netUri)
            {
                Log = XTrace.Log,
                EncoderLog = XTrace.Log,
            };
            apiServer.Start();
        }
        //Thread.Sleep(3000);
    }

    static async Task TestTask()
    {
        var client = new TinyHttpClient("http://star.newlifex.com:6600");

        var html = client.GetString("http://newlifex.com");
        XTrace.WriteLine(html);

        var rs = await client.GetAsync<Object>("api", new { state = 1234 });
        XTrace.WriteLine(rs.ToJson(true));

        var rs2 = await client.PostAsync<Object>("node/ping", new { state = 1234 });
        //var rs2 = await client.InvokeAsync<Object>("option", "api", new { state = 1234 });
        XTrace.WriteLine(rs2.ToJson(true));
    }

    static void Test2()
    {
        var server = new NetServer();
        server.Port = 88;
        server.NewSession += server_NewSession;
        //server.Received += server_Received;
        server.SocketLog = null;
        server.SessionLog = null;
        server.Start();

        var html = "新生命开发团队";

        var sb = new StringBuilder();
        sb.AppendLine("HTTP/1.1 200 OK");
        sb.AppendLine("Server: NewLife.WebServer");
        sb.AppendLine("Connection: keep-alive");
        sb.AppendLine("Content-Type: text/html; charset=UTF-8");
        sb.AppendFormat("Content-Length: {0}", Encoding.UTF8.GetByteCount(html));
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine();
        sb.Append(html);

        response = sb.ToString().GetBytes();

        while (true)
        {
            Console.Title = String.Format("会话：{0:n0} 请求：{1:n0} 错误：{2:n0}", server.SessionCount, Request, Error);
            Thread.Sleep(500);
        }
    }

    static void server_NewSession(object sender, NetSessionEventArgs e)
    {
        var session = e.Session;
        session.Received += session_Received;
        session.Session.Error += (s, e2) => Error++;
    }

    static Int32 Request;
    static Int32 Error;

    static Byte[] response;
    static void session_Received(object sender, ReceivedEventArgs e)
    {
        Request++;

        var session = sender as INetSession;
        //XTrace.WriteLine("客户端 {0} 收到：{1}", session, e.Stream.ToStr());

        //XTrace.WriteLine(response.ToStr());
        session.Send(response);

        //session.Dispose();
    }

    static void Test3()
    {
        var svr = new NetServer(3388);
        svr.Log = XTrace.Log;
        svr.SessionLog = XTrace.Log;
        svr.SocketLog = XTrace.Log;

        svr.Received += (s, e) =>
        {
            XTrace.WriteLine(e.Packet.ToStr());

            if (s is INetSession ss) ss.Send(e.Packet);
        };

        svr.Start();

        Console.ReadLine();
    }

    static async Task Test4()
    {
        var test = new ApiHttpClientTests();
        await test.BasicTest();
        await test.InfoTest();
        await test.ErrorTest();

        await test.TokenTest("12345678", "ABCDEFG");
        await test.TokenTest("ABCDEFG", "12345678");

        test.SlaveTest();
        await test.SlaveAsyncTest();
        await test.RoundRobinTest();
        await test.FilterTest();
    }

    static void Test5()
    {
        var uri = new NetUri("http://sso.newlifex.com");
        var client = uri.CreateRemote();
        client.Log = XTrace.Log;
        client.LogSend = true;
        client.LogReceive = true;
        if (client is TcpSession tcp) tcp.MaxAsync = 0;
        client.Open();

        client.Send("GET /cube/info HTTP/1.1\r\nHost: sso.newlifex.com\r\n\r\n");

        var rs = client.ReceiveString();
        XTrace.WriteLine(rs);
    }

    private static void Test8()
    {
        XTrace.WriteLine("启动两个服务端");

        // 不管是哪一种服务器用法，都具有相同的数据接收处理事件
        var onReceive = new EventHandler<ReceivedEventArgs>((s, e) =>
        {
            // ReceivedEventArgs中标准使用Data+Length或Stream表示收到的数据，测试时使用ToStr/ToHex直接输出
            // UserState表示来源地址IPEndPoint
            XTrace.WriteLine("收到 {0}：{1}", e.UserState, e.Packet.ToStr());

            // 拿到会话，原样发回去。
            // 不管是TCP/UDP，都会有一个唯一的ISocketSession对象表示一个客户端会话
            var session = s as ISocketSession;
            session.Send(e.Packet);
        });

        // 入门级UDP服务器，直接收数据
        var udp = new UdpServer(3388);
        udp.Received += onReceive;
        udp.Log = XTrace.Log;
        udp.LogSend = true;
        udp.LogReceive = true;
        udp.Open();

        // 入门级TCP服务器，先接收会话连接，然后每个连接再分开接收数据
        var tcp = new TcpServer(3388);
        tcp.NewSession += (s, e) =>
        {
            XTrace.WriteLine("新连接 {0}", e.Session);
            e.Session.Received += onReceive;
        };
        tcp.Log = XTrace.Log;
        tcp.LogSend = true;
        tcp.LogReceive = true;
        tcp.Start();

        // 轻量级应用服务器（不建议作为产品级使用），同时在TCP/TCPv6/UDP/UDPv6监听指定端口，统一事件接收数据
        var svr = new NetServer();
        svr.Port = 3377;
        svr.Received += onReceive;
        svr.Log = XTrace.Log;
        svr.SessionLog = XTrace.Log;
        svr.SocketLog = XTrace.Log;
        svr.LogSend = true;
        svr.LogReceive = true;
        svr.Start();

        Console.WriteLine();

        // 构造多个客户端连接上面的服务端
        var uri1 = new NetUri(NetType.Udp, IPAddress.Loopback, 3388);
        var uri2 = new NetUri(NetType.Tcp, IPAddress.Loopback, 3388);
        var uri3 = new NetUri(NetType.Tcp, IPAddress.IPv6Loopback, 3377);
        var clients = new ISocketClient[] { uri1.CreateRemote(), uri2.CreateRemote(), uri3.CreateRemote() };

        // 打开每个客户端，如果是TCP，此时连接服务器。
        // 这一步也可以省略，首次收发数据时也会自动打开连接
        // TCP客户端设置AutoReconnect指定断线自动重连次数，默认3次。
        foreach (var item in clients)
        {
            item.Log = XTrace.Log;
            item.LogSend = true;
            item.LogReceive = true;
            item.Open();
        }

        Thread.Sleep(1000);
        Console.WriteLine();
        XTrace.WriteLine("以下灰色日志为客户端日志，其它颜色为服务端日志，可通过线程ID区分");

        // 循环发送几次数据
        for (var i = 0; i < 3; i++)
        {
            foreach (var item in clients)
            {
                item.Send($"第{i + 1}次{item.Remote.Type}发送");
                var str = item.ReceiveString();
                Trace.Assert(!str.IsNullOrEmpty());
            }
            Thread.Sleep(500);
        }

        XTrace.WriteLine("不用担心断开连接等日志，因为离开当前函数后，客户端连接将会被销毁");

        // 为了统一TCP/UDP架构，网络库底层（UdpServer/TcpServer）是重量级封装为ISocketServer
        // 实际产品级项目不关心底层，而是继承中间层（位于NewLife.Net）的NetServer/NetSession，直接操作ISocketSession
        // 平台级项目一般在中间层之上封装消息序列化，转化为消息收发或者RPC调用，无视网络层的存在
        // 以太网接口之上还有一层传输接口ITransport，它定义包括以太网和其它工业网络接口的基本数据收发能力
    }
}