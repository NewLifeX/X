using System.Linq;
using System.Net;
using System.Net.Sockets;
using NewLife;
using NewLife.Log;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net;

public class TcpSessionTests
{
    [Fact]
    public void BindTest()
    {
        var addr = NetHelper.GetIPsWithCache().FirstOrDefault(e => e.IsIPv4() && !IPAddress.IsLoopback(e));
        Assert.NotNull(addr);

        var uri = new NetUri(NetType.Udp, addr, 12345);
        var client = uri.CreateClient();
        client.Open();
    }

    [Fact]
    public void BindTest2()
    {
        var addr = NetHelper.GetIPsWithCache().FirstOrDefault(e => e.IsIPv4() && !IPAddress.IsLoopback(e));
        Assert.NotNull(addr);

        var uri = new NetUri("https://newlifex.com");
        var client = uri.CreateRemote() as TcpSession;
        client.Local.Address = addr;

        Assert.Equal(0, client.Local.Port);

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

        var uri = new NetUri("https://newlifex.com");
        var client = uri.CreateRemote() as TcpSession;

        Assert.Equal(0, client.Local.Port);

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
            var uri = new NetUri("https://newlifex.com");
            var client = uri.CreateRemote();
            client.Local.Address = addr;
            client.Open();
        }
    }
}