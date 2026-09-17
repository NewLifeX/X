using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net;

public class NetUriTests
{
    [Fact]
    public void TestIPv6()
    {
        var addr = IPAddress.IPv6Loopback;
        Assert.Equal("::1", addr + "");

        var ep = new IPEndPoint(addr, 80);
        Assert.Equal("[::1]:80", ep + "");

        var uri = new NetUri(NetType.Tcp, ep);
        Assert.Equal("tcp://[::1]:80", uri.ToString());
        Assert.Equal(ep, uri.EndPoint);
        Assert.True(uri.IsTcp);

        uri.Port = 0;
        Assert.Equal("tcp://::1", uri + "");

        var uri2 = new NetUri("http://[::1]:80");
        Assert.Equal(NetType.Http, uri2.Type);
        Assert.False(uri2.IsUdp);
        Assert.Null(uri2.Host);
        Assert.Equal(IPAddress.IPv6Loopback, uri2.Address);
        Assert.Equal(80, uri2.Port);
        Assert.Equal("http://[::1]:80", uri2.ToString());

        var uri3 = new NetUri("wss://::1");
        Assert.Equal(NetType.WebSocket, uri3.Type);
        Assert.Null(uri3.Host);
        Assert.Equal(IPAddress.IPv6Loopback, uri3.Address);
        Assert.Equal(443, uri3.Port);
        Assert.Equal("wss://[::1]:443", uri3.ToString());

        var uri4 = new NetUri();
        uri4.Parse("ws://[240e:e0:9930:2100:9914:b410:c7d8:c0a6]");
        Assert.Equal(NetType.WebSocket, uri4.Type);
        Assert.Null(uri4.Host);
        Assert.Equal("240e:e0:9930:2100:9914:b410:c7d8:c0a6", uri4.Address + "");
        Assert.Equal(80, uri4.Port);
        Assert.Equal("ws://[240e:e0:9930:2100:9914:b410:c7d8:c0a6]:80", uri4.ToString());
    }

    [Fact]
    public void ParseAddress()
    {
        var addrs = NetUri.ParseAddress("newlifex.com");
        Assert.NotNull(addrs);
        //Assert.Equal(3, addrs.Length);
        Assert.True(addrs.Length > 0);
        Assert.Contains(addrs, e => e.AddressFamily == AddressFamily.InterNetwork);

        // IPv6。暂时注释，github action不支持IPv6
        //if (addrs.Length > 1)
        //    Assert.Contains(addrs, e => e.AddressFamily == AddressFamily.InterNetworkV6);

        var addrs2 = NetUri.ParseAddress("240e:e0:9930:2100:9914:b410:c7d8:c0a6");
        Assert.NotNull(addrs2);
        Assert.Single(addrs2);
        Assert.Equal("240e:e0:9930:2100:9914:b410:c7d8:c0a6", addrs2[0] + "");

        var uri = new NetUri("https://newlifex.com");
        Assert.Equal("newlifex.com", uri.Host);
        var addrs3 = uri.GetAddresses();
        //Assert.Equal(3, addrs3.Length);
        Assert.True(addrs.Length > 0);
    }

    [Fact]
    public void TestUri()
    {
        var uri = new Uri("https://newlifex.com/cube/info?state=1234");
        Assert.Equal("/cube/info", uri.AbsolutePath);
        Assert.NotEmpty(uri.Segments);
        Assert.Equal("/", uri.Segments[0]);
        Assert.Equal("cube/", uri.Segments[1]);
        Assert.Equal("info", uri.Segments[2]);

        uri = new Uri("https://newlifex.com");
        Assert.Equal("/", uri.AbsolutePath);
        Assert.NotEmpty(uri.Segments);
        Assert.Equal("/", uri.Segments[0]);
    }

    [Theory]
    [DisplayName("Unix域套接字地址解析")]
    [InlineData("unix:///tmp/app.sock", "/tmp/app.sock", "unix:///tmp/app.sock")]
    [InlineData("uds:///var/run/app.sock", "/var/run/app.sock", "unix:///var/run/app.sock")]
    [InlineData("unix://tmp/app.sock", "tmp/app.sock", "unix://tmp/app.sock")]
    public void ParseUnix(String address, String path, String expected)
    {
        var uri = new NetUri(address);

        Assert.Equal(NetType.Unix, uri.Type);
        Assert.True(uri.IsUnix);
        Assert.False(uri.IsTcp);
        Assert.False(uri.IsUdp);
        Assert.Equal(path, uri.Path);

        // 序列化往返，uds 别名归一为 unix
        Assert.Equal(expected, uri.ToString());
    }

    [Fact]
    [DisplayName("Unix域套接字解析后再次解析其它协议不残留路径")]
    public void ParseUnixThenTcp()
    {
        var uri = new NetUri("unix:///tmp/a.sock");
        Assert.Equal("/tmp/a.sock", uri.Path);

        // 同一个实例再次解析其它协议，Path 应被重置
        uri.Parse("tcp://127.0.0.1:8080");
        Assert.Equal(NetType.Tcp, uri.Type);
        Assert.Null(uri.Path);
        Assert.Equal(8080, uri.Port);
    }

    [Fact]
    [DisplayName("Unix域套接字克隆保留路径")]
    public void CloneUnix()
    {
        var uri = new NetUri("unix:///tmp/a.sock");
        var clone = uri.Clone();

        Assert.Equal(NetType.Unix, clone.Type);
        Assert.Equal("/tmp/a.sock", clone.Path);
        Assert.Equal("unix:///tmp/a.sock", clone.ToString());
    }
}