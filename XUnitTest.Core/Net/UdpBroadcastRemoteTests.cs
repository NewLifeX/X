using System.ComponentModel;
using System.Net;
using NewLife.Messaging;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net;

/// <summary>UDP 广播会话接收事件参数测试</summary>
/// <remarks>
/// 广播/任意地址会话在 <see cref="UdpServer"/> 里按端口兜底承接来自任意 IP 的回应，
/// 会话远端只是“发送目标”（广播地址），并非本包来源。
/// 事件参数的 <see cref="ReceivedEventArgs.Remote"/> 必须给出数据报真实来源，否则调用方无法区分是哪台设备回应。
/// </remarks>
public class UdpBroadcastRemoteTests
{
    [Fact]
    [DisplayName("广播会话_收到回应_事件远程地址取数据报真实来源")]
    public async Task BroadcastSession_RemoteIsRealSource()
    {
        // 协议模式才会走 ProcessDatagram 分支（本次修复点）
        using var client = new UdpServer { Port = 0, Protocol = new LengthFieldCodec { Size = 1 } };
        client.Open();

        // 服务端：稍后用它的端口充当广播端口，同时作为数据报的来源端口
        using var server = new UdpServer { Port = 0 };
        server.Open();

        // 客户端以“广播地址 + 服务端端口”建立会话，命中 UdpServer 的广播端口兜底
        var broadcast = Assert.IsType<UdpSession>(client.CreateSession(null, new IPEndPoint(IPAddress.Broadcast, server.Port)));
        Assert.Equal(IPAddress.Broadcast, broadcast.Remote.EndPoint!.Address);

        // 事件参数的 Remote 是池化对象的属性，必须在事件内取值
        IPEndPoint? remote = null;
        var hit = new TaskCompletionSource<Boolean>();
        client.Received += (s, e) =>
        {
            if (e.Packet is { Length: > 0 })
            {
                remote = e.Remote;
                hit.TrySetResult(true);
            }
        };

        // 从服务端端口（即广播端口）发一个“长度字段 + 负载”帧给客户端
        var payload = "broadcast-reply"u8.ToArray();
        var frame = new Byte[payload.Length + 1];
        frame[0] = (Byte)payload.Length;
        payload.CopyTo(frame, 1);

        var target = server.CreateSession(null, new IPEndPoint(IPAddress.Loopback, client.Port));

        // 回环 UDP 不可靠：重发轮询触达（断言口径不变）
        var end = Environment.TickCount64 + 15_000;
        while (!hit.Task.IsCompleted && Environment.TickCount64 < end)
        {
            target.Send(frame);
            try
            {
                await hit.Task.WaitAsync(TimeSpan.FromMilliseconds(500));
            }
            catch (TimeoutException) { }
        }

        await hit.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.NotNull(remote);
        Assert.Equal(IPAddress.Loopback, remote!.Address);
        Assert.NotEqual(IPAddress.Broadcast, remote.Address);
        Assert.Equal(server.Port, remote.Port);
    }
}
