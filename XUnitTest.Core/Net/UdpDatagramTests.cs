using System.Net;
using System.Net.Sockets;
using System.Text;
using NewLife;
using NewLife.Log;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net;

/// <summary>UDP 报文完整性测试：大报文与 MTU 边界回环回显，逐字节校验</summary>
[Collection("Net")]
public class UdpDatagramTests
{
    [Fact(DisplayName = "UDP空数据报_不关闭服务端_后续收发正常")]
    public async Task UdpEmptyDatagram_ServerStaysAlive()
    {
        using var server = new UdpServer { Port = 0, BufferSize = 1 << 20 };
        server.Received += (s, e) =>
        {
            if (s is UdpSession session && e.Packet != null && e.Packet.Length > 0) session.Send(e.Packet);
        };
        server.Open();

        using var client = NetHelper.CreateUdpClient();
        var remote = new IPEndPoint(IPAddress.Loopback, server.Port);

        // 连续发空数据报：老实现会把“同步完成且0字节”当成对端关闭，进而关掉整个监听服务
        for (var i = 0; i < 10; i++) await client.SendAsync(new Byte[0], 0, remote);

        await Task.Delay(200);
        Assert.True(server.Active, "空数据报不得关闭监听服务");

        // 服务仍可正常收发（新客户端 = 新远端端点；空数据报按约定会结束原端点会话）
        using var client2 = NetHelper.CreateUdpClient();
        var payload = new Byte[] { 1, 2, 3, 4, 5 };
        await client2.SendAsync(payload, payload.Length, remote);
        var result = await client2.ReceiveAsync().WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(payload, result.Buffer);
    }

    [Fact(DisplayName = "UDP事件内释放本轮句柄_会话存活且状态跨报文保留")]
    public async Task UdpPacketReleasedInHandler_SessionStateKept()
    {
        using var server = new UdpServer { Port = 0, BufferSize = 1 << 20 };

        // 会话级事件（经 NewSession 订阅）：事件内消费本轮句柄，正是空数据报判定的触发点
        server.NewSession += (s, e) =>
        {
            if (e.Session is UdpSession session) session.Received += OnDatagram;
        };
        server.Open();

        using var client = NetHelper.CreateUdpClient();
        var remote = new IPEndPoint(IPAddress.Loopback, server.Port);

        for (var i = 1; i <= 3; i++)
        {
            var payload = "ping"u8.ToArray();
            await client.SendAsync(payload, payload.Length, remote);
            var result = await client.ReceiveAsync().WaitAsync(TimeSpan.FromSeconds(5));

            // 上一轮在事件内释放了句柄：判定若发生在事件之后，会话会被误停，本轮计数回到 1
            Assert.Equal($"echo:{i}", Encoding.UTF8.GetString(result.Buffer));
        }
    }

    /// <summary>会话级处理器：累加会话状态并回显，随后按契约消费本轮句柄</summary>
    private static void OnDatagram(Object? sender, ReceivedEventArgs e)
    {
        if (sender is not UdpSession session || e.Packet is not { Length: > 0 }) return;

        // 会话级状态：同一远端端点跨报文累加。会话若被销毁，计数会从 1 重新开始
        var count = session.Items.TryGetValue("count", out var v) ? (Int32)v! : 0;
        session.Items["count"] = count + 1;
        session.Send(Encoding.UTF8.GetBytes($"echo:{count + 1}"));

        // 按契约消费本轮句柄：释放后 Packet.Length 归零
        e.Packet.TryDispose();
    }

    /// <summary>回显服务端与客户端：指定大小的随机负载应原样返回</summary>
    /// <remarks>服务端应用层接收缓冲放大到 1MB，承载超过默认值的报文；否则大报文会触发 MessageSize 丢弃</remarks>
    [Theory(DisplayName = "UDP报文_回环回显_逐字节一致")]
    [InlineData(1024)]
    [InlineData(1472)]
    [InlineData(1473)]
    [InlineData(8192)]
    [InlineData(60 * 1024)]
    public async Task UdpDatagram_Echo_ByteExact(Int32 size)
    {
        using var server = new UdpServer { Port = 0, BufferSize = 1 << 20, Log = XTrace.Log };
        server.Received += (s, e) =>
        {
            // 原样回显
            if (s is UdpSession session && e.Packet != null && e.Packet.Length > 0) session.Send(e.Packet);
        };
        server.Open();
        if (server.Client is { } srvSock)
        {
            srvSock.ReceiveBufferSize = 8 << 20;
            srvSock.SendBufferSize = 8 << 20;
        }

        using var client = NetHelper.CreateUdpClient();
        client.Client.ReceiveBufferSize = 8 << 20;
        client.Client.SendBufferSize = 8 << 20;

        var payload = new Byte[size];
        Random.Shared.NextBytes(payload);

        await client.SendAsync(payload, payload.Length, new IPEndPoint(IPAddress.Loopback, server.Port));
        var result = await client.ReceiveAsync().WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(size, result.Buffer.Length);
        Assert.Equal(payload, result.Buffer);
    }
}
