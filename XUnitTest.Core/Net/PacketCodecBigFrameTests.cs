using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using NewLife;
using NewLife.Data;
using NewLife.Messaging;
using NewLife.Net;
using NewLife.Net.Handlers;
using Xunit;

namespace XUnitTest.Net;

/// <summary>L1 段保留实网压力回归：超大帧跨多轮接收（残片窗口共享 + 段链累积 + 组链返回）数据完整性</summary>
[Collection("Net")]
public class PacketCodecBigFrameTests
{
    /// <summary>StandardCodec 大负载双向往返：默认接收缓冲 8KB 下多轮段保留（残片跨轮共享），验证逐字节一致</summary>
    [Theory]
    [InlineData(60_000)]
    [InlineData(100_000)]
    [InlineData(1024 * 1024)]
    [DisplayName("StandardCodec 大帧实网往返：多轮段保留+收敛")]
    public async Task BigFrame_RoundTrip(Int32 size)
    {
        var payload = new Byte[size];
        Random.Shared.NextBytes(payload);

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
        };
        server.Add(new StandardCodec());
        server.Received += (s, e) =>
        {
            // Echo：UserPacket=true 时 Message 为负载 IPacket；必须以 Reply 回显才能匹配客户端请求
            if (s is INetSession session && e.Message is IPacket pk)
                session.SendReply(pk, e);
        };
        server.Start();

        using var client = new NetClient($"tcp://127.0.0.1:{server.Port}");
        client.Add(new StandardCodec());
        client.Timeout = 30_000;
        client.Open();

        // 发送 1MB 请求，等待服务端回显；双向都经过 PacketCodec 段保留/组链
        var resp = await client.SendMessageAsync(new ArrayPacket(payload));
        Assert.NotNull(resp);

        // UserPacket=true 时返回负载拥有副本（IPacket）；消息模式直接取消息负载
        var echo = resp as IPacket;
        if (echo == null && resp is IMessage imsg) echo = imsg.Payload;
        Assert.NotNull(echo);
        Assert.Equal(payload.Length, echo!.Total);
        Assert.Equal(payload, echo.ToArray());
        echo.TryDispose();
    }
}
