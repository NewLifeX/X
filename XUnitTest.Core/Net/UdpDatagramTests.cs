using System.Net;
using System.Net.Sockets;
using NewLife;
using NewLife.Log;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net;

/// <summary>UDP 报文完整性测试：大报文与 MTU 边界回环回显，逐字节校验</summary>
[Collection("Net")]
public class UdpDatagramTests
{
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
