using System.Net;
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

[Collection("Net")]
[TestCaseOrderer("NewLife.UnitTest.DefaultOrderer", "NewLife.UnitTest")]
public class TcpSessionTests
{
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

        // 启动NetServer
        var server4 = new HttpServer
        {
            Local = new NetUri(NetType.Https, ip4, 443),
            Certificate = cert,
            SslProtocol = SslProtocols.Tls12,
            Log = XTrace.Log,
        };
        server4.Start();

        var server6 = new HttpServer
        {
            Local = new NetUri(NetType.Https, ip6, 443),
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

        var uri = new NetUri(NetType.Udp, addr, 12345);
        var client = uri.CreateClient();
        client.Log = XTrace.Log;
        client.Open();
    }

    [Fact]
    public void BindTest2()
    {
        var addr = NetHelper.GetIPsWithCache().FirstOrDefault(e => e.IsIPv4() && !IPAddress.IsLoopback(e));
        Assert.NotNull(addr);

        var uri = new NetUri("https://test.newlifex.com");
        var client = uri.CreateRemote() as TcpSession;
        client.Local.Address = addr;

        Assert.Equal(0, client.Local.Port);

        client.Log = XTrace.Log;
        client.Open();

        Assert.Equal(client.Local.Address, addr);
        Assert.NotEqual(0, client.Local.Port);
        Assert.True(client.RemoteAddress.IsIPv4());
    }

    [Fact]
    public void BindTest3()
    {
        var addr = NetHelper.GetIPsWithCache().FirstOrDefault(e => e.IsIPv4() && !IPAddress.IsLoopback(e));
        Assert.NotNull(addr);

        var uri = new NetUri("https://test.newlifex.com");
        var client = uri.CreateRemote() as TcpSession;

        Assert.Equal(0, client.Local.Port);

        client.Log = XTrace.Log;
        client.Open();

        Assert.True(client.Local.Address.IsAny());
        Assert.NotEqual(0, client.Local.Port);
        //Assert.True(!client.RemoteAddress.IsIPv4());
    }

    [Fact]
    public void BindTest4()
    {
        Assert.True(Socket.OSSupportsIPv4);
        Assert.True(Socket.OSSupportsIPv6);

        var entry = Dns.GetHostEntry("newlifex.com");
        //var entry = Dns.GetHostEntry("newlifex.com.w.cdngslb.com");
        Assert.NotNull(entry);

        var addr = NetHelper.GetIPsWithCache().FirstOrDefault(e => !e.IsIPv4() && !IPAddress.IsLoopback(e));
        Assert.NotNull(addr);

        if (entry.AddressList.Any(_ => !_.IsIPv4()))
        {
            var uri = new NetUri("https://test.newlifex.com");
            var client = uri.CreateRemote();
            client.Local.Address = addr;
            client.Log = XTrace.Log;
            client.Open();
        }
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
}