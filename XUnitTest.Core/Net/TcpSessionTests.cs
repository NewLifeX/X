using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using NewLife;
using NewLife.Http;
using NewLife.Log;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net;

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
}