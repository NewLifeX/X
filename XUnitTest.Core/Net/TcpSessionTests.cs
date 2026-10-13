using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using NewLife;
using NewLife.Data;
using NewLife.Http;
using NewLife.Log;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net;

[Collection("Net.B")]
[TestCaseOrderer("NewLife.UnitTest.DefaultOrderer", "NewLife.UnitTest")]
public class TcpSessionTests
{
    /// <summary>HTTPS 服务端口。动态分配：固定 443 一旦被其它进程占用，本类静态构造就会抛“Failed to listen to all ports”，整类用例全红</summary>
    private static readonly Int32 _httpsPort;

    static TcpSessionTests()
    {
        var ip4 = IPAddress.Parse("127.0.0.66");
        //var ip6 = IPAddress.Parse("::66");
        var ip6 = IPAddress.Parse("::1");

        var ips = NetHelper.GetIPsWithCache().Where(e => !IPAddress.IsLoopback(e)).ToArray();
        ip4 = ips.FirstOrDefault(e => e.IsIPv4()) ?? ip4;
        ip6 = ips.FirstOrDefault(e => !e.IsIPv4()) ?? ip6;

        // 修改DnsResolver解析，把域名 newlifex.com 指向本地
        if (DnsResolver.Instance is DnsResolver resolver)
        {
            resolver.Set("test.newlifex.com", [ip4, ip6]);
        }

        var asm = typeof(TcpSessionTests).Assembly;
        //var key = asm.GetManifestResourceStream("XUnitTest.certs.newlifex.com.pem").ToStr();
        //var pkey = asm.GetManifestResourceStream("XUnitTest.certs.newlifex.com.privatekey.pem").ToStr();
        var pfx = asm.GetManifestResourceStream("XUnitTest.certs.newlifex.com.pfx").ReadBytes(-1);
        //var cert = X509Certificate2.CreateFromPem(key, pkey);
        //var cert = X509Certificate2.CreateFromEncryptedPem(key, pkey, "123456");
#if NET9_0_OR_GREATER
        var cert = X509CertificateLoader.LoadPkcs12(pfx, "123456");
#else
        var cert = new X509Certificate2(pfx, "123456", X509KeyStorageFlags.DefaultKeySet);
#endif

        // 启动NetServer。先让 IPv4 随机取端口，再让 IPv6 复用同一端口（地址不同、互不冲突），
        // 这样 https://test.newlifex.com:端口 在两条协议栈上都能连上，且不再依赖固定 443
        var server4 = new HttpServer
        {
            Local = new NetUri(NetType.Https, ip4, 0),
            Certificate = cert,
            SslProtocol = SslProtocols.Tls12,
            Log = XTrace.Log,
        };
        server4.Start();

        _httpsPort = server4.Port;

        var server6 = new HttpServer
        {
            Local = new NetUri(NetType.Https, ip6, _httpsPort),
            Certificate = cert,
            SslProtocol = SslProtocols.Tls12,
            Log = XTrace.Log,
        };
        server6.Start();
    }

    [Fact(DisplayName = "发送_大数据包_内核发送缓冲按发送量调优")]
    public void DirectSend_TunesSendBufferSize()
    {
        using var server = new TcpServer { Port = 0 };
        server.Start();

        using var client = new TcpSession { Remote = new NetUri($"tcp://127.0.0.1:{server.Port}") };
        client.Open();

        // 超过默认内核发送缓冲的一次发送：调优在锁内执行后，SendBufferSize 应被上调到本次发送量
        var payload = new Byte[256 * 1024];
        Assert.Equal(payload.Length, client.Send(payload));

        Assert.True(client.Client!.SendBufferSize >= payload.Length);
    }

    [Fact]
    public void BindTest()
    {
        var addr = NetHelper.GetIPsWithCache().FirstOrDefault(e => e.IsIPv4() && !IPAddress.IsLoopback(e));
        Assert.NotNull(addr);

        // 端口 0 由系统分配：固定端口 12345 在多测试宿主并发（多目标框架/VS 并行运行）时互相抢占，
        // 且原用例不释放客户端，端口会被一直占住，导致后续运行的同类用例报“地址已在使用”
        var uri = new NetUri(NetType.Udp, addr, 0);
        using var client = uri.CreateClient();
        client.Log = XTrace.Log;
        client.Open();

        Assert.Equal(addr, client.Local.Address);
        Assert.NotEqual(0, client.Local.Port);
    }

    [Fact]
    public void BindTest2()
    {
        var addr = NetHelper.GetIPsWithCache().FirstOrDefault(e => e.IsIPv4() && !IPAddress.IsLoopback(e));
        Assert.NotNull(addr);

        var uri = new NetUri($"https://test.newlifex.com:{_httpsPort}");

        // 服务端 TLS 握手超时沿用产品默认 3 秒，满载并行跑测下偶发超时（服务端关闭连接，客户端读到 EOF）。
        // 用例校验的是客户端绑定本地地址，与握手时长无关，故重试建连
        TcpSession? client = null;
        for (var i = 0; ; i++)
        {
            client?.Dispose();
            client = uri.CreateRemote() as TcpSession;
            client.Local.Address = addr;

            Assert.Equal(0, client.Local.Port);

            // 默认 3 秒握手超时在满载并行跑测下不足，放宽到 10 秒
            client.Timeout = 10_000;
            client.Log = XTrace.Log;

            try
            {
                client.Open();
                break;
            }
            catch (Exception) when (i < 2) { }
        }

        using var _ = client;
        Assert.Equal(client.Local.Address, addr);
        Assert.NotEqual(0, client.Local.Port);
        Assert.True(client.RemoteAddress.IsIPv4());
    }

    [Fact]
    public void BindTest3()
    {
        var addr = NetHelper.GetIPsWithCache().FirstOrDefault(e => e.IsIPv4() && !IPAddress.IsLoopback(e));
        Assert.NotNull(addr);

        var uri = new NetUri($"https://test.newlifex.com:{_httpsPort}");

        // 服务端 TLS 握手超时沿用产品默认 3 秒，满载并行跑测下偶发超时，故重试建连（用例校验的是本地地址绑定）
        TcpSession? client = null;
        for (var i = 0; ; i++)
        {
            client?.Dispose();
            client = uri.CreateRemote() as TcpSession;

            Assert.Equal(0, client.Local.Port);

            client.Timeout = 10_000;
            client.Log = XTrace.Log;

            try
            {
                client.Open();
                break;
            }
            catch (Exception) when (i < 2) { }
        }

        using var _ = client;
        Assert.True(client.Local.Address.IsAny());
        Assert.NotEqual(0, client.Local.Port);
        //Assert.True(!client.RemoteAddress.IsIPv4());
    }

    [Fact]
    public void BindTest4()
    {
        Assert.True(Socket.OSSupportsIPv4);
        Assert.True(Socket.OSSupportsIPv6);

        // 本地化：不再向真实 DNS 查询公网域名；test.newlifex.com 已由静态构造器映射到本机（含 IPv6）
        var addr = NetHelper.GetIPsWithCache().FirstOrDefault(e => !e.IsIPv4() && !IPAddress.IsLoopback(e));
        Assert.NotNull(addr);

        var uri = new NetUri($"https://test.newlifex.com:{_httpsPort}");
        var client = uri.CreateRemote();
        client.Local.Address = addr;
        client.Log = XTrace.Log;
        client.Open();
    }

    /// <summary>加载测试自签名证书（内嵌 pfx）</summary>
    private static X509Certificate2 LoadTestCert()
    {
        var pfx = typeof(TcpSessionTests).Assembly.GetManifestResourceStream("XUnitTest.certs.newlifex.com.pfx")!.ReadBytes(-1);
#if NET9_0_OR_GREATER
        return X509CertificateLoader.LoadPkcs12(pfx, "123456");
#else
        return new X509Certificate2(pfx, "123456", X509KeyStorageFlags.DefaultKeySet);
#endif
    }

    /// <summary>SSL 回环 echo：256KB 逐字节一致（流式多轮读取）</summary>
    [Fact(DisplayName = "SSL_回环echo_256KB逐字节一致")]
    public async Task SslEcho_256KB()
    {
        using var cert = LoadTestCert();
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            SslProtocol = SslProtocols.Tls12,
            Certificate = cert,
            Log = XTrace.Log,
        };
        server.Received += (s, e) =>
        {
            if (s is INetSession session && e.Packet != null && e.Packet.Length > 0) session.Send(e.Packet);
        };
        server.Start();

        using var client = new TcpSession
        {
            Remote = new NetUri($"tcp://127.0.0.1:{server.Port}"),
            SslProtocol = SslProtocols.Tls12,
            AutoReceive = false,
            // 本地 TLS 握手，默认 3 秒超时在满载并行跑测下会超时，放宽到 10 秒
            Timeout = 10_000,
            Log = XTrace.Log,
        };
        client.Open();

        var payload = new Byte[256 * 1024];
        Random.Shared.NextBytes(payload);
        _ = client.Send(payload);

        // 拉取读满（SSL 流按字节读，不依赖包边界）
        var offset = 0;
        while (offset < payload.Length)
        {
            using var pk = await client.ReceiveAsync(default).WaitAsync(TimeSpan.FromSeconds(15));
            Assert.NotNull(pk);
            Assert.True(pk!.Length > 0, "SSL回显中断");

            var bytes = pk.ToArray();
            Assert.True(payload.AsSpan(offset, bytes.Length).SequenceEqual(bytes), $"偏移 {offset} 数据不一致");
            offset += bytes.Length;
        }
    }

    /// <summary>SSL 会话发文件：SslStream 不能零拷贝，降级为分块发送，内容逐字节一致</summary>
    [Fact(DisplayName = "文件发送_SSL会话_降级分块发送_内容一致")]
    public async Task SendFile_SslSession_FallbackChunked()
    {
        const Int32 size = 200 * 1024;
        var payload = new Byte[size];
        Random.Shared.NextBytes(payload);

        var file = Path.Combine(Path.GetTempPath(), "nl_sendfile_ssl_" + Guid.NewGuid().ToString("N") + ".bin");
        await File.WriteAllBytesAsync(file, payload);

        try
        {
            using var cert = LoadTestCert();
            using var server = new NetServer
            {
                Port = 0,
                ProtocolType = NetType.Tcp,
                SslProtocol = SslProtocols.Tls12,
                Certificate = cert,
                Log = XTrace.Log,
            };

            var received = new List<Byte>();
            var done = new ManualResetEventSlim(false);
            server.Received += (s, e) =>
            {
                if (e.Packet == null) return;

                lock (received)
                {
                    received.AddRange(e.Packet.ToArray());
                    if (received.Count >= size) done.Set();
                }
            };
            server.Start();

            using var client = new TcpSession
            {
                Remote = new NetUri($"tcp://127.0.0.1:{server.Port}"),
                SslProtocol = SslProtocols.Tls12,
                Log = XTrace.Log,
            };
            client.Open();

            var rs = await client.SendFileAsync(file);
            Assert.Equal(size, rs);

            Assert.True(done.Wait(15_000), "服务端未收齐文件内容");
            lock (received) Assert.Equal(payload, received.ToArray());
        }
        finally
        {
            File.Delete(file);
        }
    }

    /// <summary>SSL 客户端强制 RST：服务端应感知流异常并关闭会话，不悬挂</summary>
    [Fact(DisplayName = "SSL_客户端强制RST_服务端感知并关闭会话")]
    public void SslClientRst_ServerDetects()
    {
        var sessionReady = new ManualResetEventSlim(false);
        INetSession? serverSession = null;

        using var cert = LoadTestCert();
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            SslProtocol = SslProtocols.Tls12,
            Certificate = cert,
            Log = XTrace.Log,
        };
        server.NewSession += (s, e) => { serverSession = e.Session; sessionReady.Set(); };
        server.Start();

        using var client = new TcpSession
        {
            Remote = new NetUri($"tcp://127.0.0.1:{server.Port}"),
            SslProtocol = SslProtocols.Tls12,
            Log = XTrace.Log,
        };
        client.Open();

        Assert.True(sessionReady.Wait(5000));
        Assert.NotNull(serverSession);

        // 先发一点数据确保服务端会话进入接收
        _ = client.Send("hello");
        Thread.Sleep(200);

        // 强制 RST：以 Linger0 直接关闭底层套接字，不发 close_notify、不走四次挥手
        var sock = client.Client;
        Assert.NotNull(sock);
        sock!.LingerState = new LingerOption(true, 0);
        sock.Close();

        // 服务端应在数秒内感知流异常并关闭会话（未修复时 SSL 读回调异常被吞，会话悬挂）
        var ss = (SessionBase)serverSession!.Session;
        for (var i = 0; i < 160 && ss.Active; i++) Thread.Sleep(50);
        Assert.False(ss.Active, "服务端应感知SSL客户端强制断开并关闭会话");
    }

    /// <summary>
    /// 慢握手不占受理槽位：撑满全部受理槽位（MaxAsync=2）的客户端只建连、不完成TLS握手，
    /// 新连接仍应被受理并完成握手、收发数据。
    /// 旧实现在受理线程上同步 AuthenticateAsServer，槽位被慢握手占满后新连接不再被受理。
    /// </summary>
    [Fact(DisplayName = "TLS受理_慢握手不阻塞新连接")]
    public async Task SslSlowHandshake_DoesNotBlockAccept()
    {
        using var cert = LoadTestCert();

        using var server = new TcpServer
        {
            Local = new NetUri(NetType.Tcp, IPAddress.Loopback, 0),
            SslProtocol = SslProtocols.Tls12,
            Certificate = cert,
            MaxAsync = 2,
            Log = XTrace.Log,
        };
        server.NewSession += (s, e) =>
        {
            var session = (SessionBase)e.Session;
            // 慢握手会话不设超时：默认3秒超时会解开受理槽位，掩盖“槽位被长期占住”的缺陷
            session.Timeout = 0;
            session.Received += (ss, ee) =>
            {
                if (ee.Packet != null && ee.Packet.Length > 0) session.Send(ee.Packet);
            };
        };
        server.Start();

        // 只建连、不发送数据的客户端：会话建立后卡在等待 ClientHello 上，永不完成握手
        var stalled = new List<TcpClient>();
        try
        {
            for (var i = 0; i < 2; i++)
            {
                var tcp = new TcpClient();
                tcp.Connect(IPAddress.Loopback, server.Port);
                stalled.Add(tcp);
            }

            // 等两个连接都被受理（会话入集合）；此即受理槽位被慢握手占满的时刻
            for (var i = 0; i < 200 && server.Sessions.Count < 2; i++) await Task.Delay(25);
            Assert.Equal(2, server.Sessions.Count);

            // 新连接仍应被受理：握手成功 + 回显正常
            using var client = new TcpSession
            {
                Remote = new NetUri($"tcp://127.0.0.1:{server.Port}"),
                SslProtocol = SslProtocols.Tls12,
                AutoReceive = false,
                Timeout = 5_000,
                Log = XTrace.Log,
            };
            Assert.True(await client.OpenAsync().WaitAsync(TimeSpan.FromSeconds(15)), "慢握手占满受理槽位时，新连接仍应完成握手");

            client.Send("hello");
            using var pk = await client.ReceiveAsync(default).WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal("hello", pk!.ToStr());

            Assert.Equal(3, server.Sessions.Count);
        }
        finally
        {
            foreach (var tcp in stalled) tcp.Dispose();
        }
    }

    /// <summary>握手失败（明文请求打到TLS端口）后会话必须出集合，不能留在集合里直到会话超时</summary>
    [Fact(DisplayName = "TLS受理_握手失败会话出集合")]
    public async Task SslHandshakeFailure_SessionRemoved()
    {
        using var cert = LoadTestCert();
        using var server = new TcpServer
        {
            Local = new NetUri(NetType.Tcp, IPAddress.Loopback, 0),
            SslProtocol = SslProtocols.Tls12,
            Certificate = cert,
            Log = XTrace.Log,
        };
        ISocketSession? session = null;
        server.NewSession += (s, e) => session = e.Session;
        server.Start();

        using (var tcp = new TcpClient())
        {
            tcp.Connect(IPAddress.Loopback, server.Port);

            // 明文 HTTP 请求打到 TLS 端口：握手必然失败
            var req = "GET / HTTP/1.1\r\nHost: x\r\n\r\n".GetBytes();
            var ns = tcp.GetStream();
            ns.Write(req, 0, req.Length);
            ns.Flush();

            // 等会话建立（NewSession 触发）并因握手失败出集合
            for (var i = 0; i < 200 && (session == null || server.Sessions.Count > 0); i++) await Task.Delay(25);

            Assert.NotNull(session);
            Assert.Empty(server.Sessions);
        }

        Assert.NotNull(session);
        var ss = (SessionBase)session!;
        // 会话出集合（Disposed）后关闭流程仍在收尾：停消息泵、交付在途帧后才置 Active=false，
        // 满载调度停顿下瞬时断言会误报，改为条件等待（真的不清除活动标志时仍会超时失败）
        Assert.True(TestWait.Until(() => !ss.Active, 10_000), $"握手失败的会话不应保持活动：Active={ss.Active} Disposed={ss.Disposed} CloseReason={ss.CloseReason}");
    }

    /// <summary>
    /// 握手期间发送：服务端握手写持有写锁，业务发送会等待握手完成后再加密写出，不会把明文写进TLS流。
    /// 若明文写进TLS流，客户端握手会先读到非TLS数据而失败。
    /// </summary>
    [Fact(DisplayName = "TLS受理_握手期间发送_等锁后加密写出")]
    public async Task SslSendDuringHandshake_WaitsForHandshake()
    {
        using var cert = LoadTestCert();
        using var server = new TcpServer
        {
            Local = new NetUri(NetType.Tcp, IPAddress.Loopback, 0),
            SslProtocol = SslProtocols.Tls12,
            Certificate = cert,
            Log = XTrace.Log,
        };
        server.NewSession += (s, e) =>
        {
            // 握手会话不设超时：默认3秒超时会让“握手期间”窗口过短，用例不稳定
            ((SessionBase)e.Session).Timeout = 0;
        };
        server.Start();

        // 慢客户端先建连不发数据，把服务端握手卡在等待 ClientHello 上
        using var tcp = new TcpClient();
        tcp.Connect(IPAddress.Loopback, server.Port);

        for (var i = 0; i < 200 && server.Sessions.Count == 0; i++) await Task.Delay(25);
        Assert.Single(server.Sessions);
        var session = (SessionBase)server.Sessions.Values.First();

        // 握手期间发送：应阻塞在写锁上等待握手完成
        var send = Task.Run(() => session.Send("greeting".GetBytes()));
        await Task.Delay(300);
        Assert.False(send.IsCompleted, "握手期间发送必须等待握手完成，不得直接写套接字");

        // 客户端开始TLS握手：服务端握手完成，挂起的发送立即加密写出
        using var ssl = new SslStream(tcp.GetStream(), false, (a, b, c, d) => true);
        await ssl.AuthenticateAsClientAsync("127.0.0.1").WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal("greeting".Length, await send.WaitAsync(TimeSpan.FromSeconds(5)));

        // 客户端读到的是握手后加密写出的完整数据
        var buf = new Byte[64];
        var n = await ssl.ReadAsync(buf).AsTask().WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("greeting", buf.AsSpan(0, n).ToArray().ToStr());
    }
}