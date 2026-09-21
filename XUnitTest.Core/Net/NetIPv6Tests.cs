using System.Net;
using System.Net.Sockets;
using NewLife;
using NewLife.Log;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net;

/// <summary>IPv6 回环测试：TCP 大报文回显与 UDP 回显</summary>
[Collection("Net")]
public class NetIPv6Tests
{
    /// <summary>::1 上 1MB TCP 回显，逐字节一致</summary>
    [Fact(DisplayName = "IPv6_TCP回环_1MB逐字节回显")]
    public async Task IPv6_TcpEcho_1MB()
    {
        const Int32 size = 1 * 1024 * 1024;

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetworkV6,
            UseSession = true,
            Log = XTrace.Log,
        };
        server.NewSession += (s, e) =>
        {
            var session = e.Session;
            session.Session.Received += (s2, e2) =>
            {
                var pk = e2.Packet;
                if (pk != null && pk.Length > 0) session.Send(pk);
            };
        };
        server.Start();

        using var client = new NetClient($"tcp://[::1]:{server.Port}") { AutoReconnect = false, Log = XTrace.Log };

        var payload = new Byte[size];
        Random.Shared.NextBytes(payload);

        var buf = new Byte[size];
        var off = 0;
        var done = new TaskCompletionSource<Byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        client.Received += (s2, e2) =>
        {
            var data = e2.GetBytes();
            lock (buf)
            {
                var n = Math.Min(data.Length, size - off);
                System.Buffer.BlockCopy(data, 0, buf, off, n);
                off += n;
                if (off >= size) done.TrySetResult(buf);
            }
        };
        Assert.True(client.Open(), "IPv6 客户端应成功连接 ::1");
        client.Send(payload);

        var got = await done.Task.WaitAsync(TimeSpan.FromSeconds(15));
        Assert.Equal(payload, got);

        client.Close("done");
    }

    /// <summary>::1 上 UDP 小包回显</summary>
    [Fact(DisplayName = "IPv6_UDP回环_小包回显")]
    public async Task IPv6_UdpEcho()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Udp,
            AddressFamily = AddressFamily.InterNetworkV6,
            UseSession = true,
            Log = XTrace.Log,
        };
        server.NewSession += (s, e) =>
        {
            var session = e.Session;
            session.Session.Received += (s2, e2) =>
            {
                var pk = e2.Packet;
                if (pk != null && pk.Length > 0) session.Send(pk);
            };
        };
        server.Start();

        using var client = new NetClient($"udp://[::1]:{server.Port}") { AutoReconnect = false, Log = XTrace.Log };
        var done = new TaskCompletionSource<Byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        client.Received += (s2, e2) => done.TrySetResult(e2.GetBytes());
        Assert.True(client.Open());
        client.Send("ipv6-udp-ping");

        var got = await done.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("ipv6-udp-ping", got.ToStr());

        client.Close("done");
    }
}
