using System.Net;
using System.Net.Sockets;
using NewLife.Data;
using NewLife.Messaging;
using NewLife.Net;
using NewLife.Net.Handlers;
using Xunit;

namespace XUnitTest.Net;

/// <summary>无等待方应答消息测试</summary>
/// <remarks>
/// 服务端主动推送 Reply=true 的应答，而客户端没有挂起请求（匹配队列为空）时：
/// 事件仍全量可见，匹配未命中路径必须安全归还拥有缓冲，不能出现二次归还或状态污染。
/// </remarks>
[Collection("Net")]
public class UnmatchedReplyTests
{
    [Theory(DisplayName = "无等待方应答：事件全量可见且缓冲安全归还")]
    [InlineData(256, false)]
    [InlineData(256, true)]
    [InlineData(60_000, false)]
    [InlineData(60_000, true)]
    public async Task UnsolicitedReply_NoWaiter(Int32 payloadSize, Boolean userPacket)
    {
        const Int32 pushCount = 8;

        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
        };
        server.Add(new StandardCodec { UserPacket = false });
        server.Received += (s, e) =>
        {
            if (s is INetSession session && e.Message is IMessage req)
            {
                // 1条配对应答（供客户端等待方匹配），随后为空队列推送多条无等待方应答
                for (var i = 0; i < pushCount + 1; i++)
                {
                    var reply = req.CreateReply();
                    reply.Payload = new ArrayPacket(new Byte[payloadSize]);
                    session.SendMessage(reply);
                }
            }
        };
        server.Start();

        using var client = new NetClient($"tcp://127.0.0.1:{server.Port}");
        client.Add(new StandardCodec { UserPacket = userPacket });
        client.Timeout = 15_000;

        var received = 0;
        var allDone = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        client.Received += (s, e) =>
        {
            if (Interlocked.Increment(ref received) >= pushCount + 1) allDone.TrySetResult();
        };
        client.Open();

        // 建立并清空匹配队列；随后到达的推送应答全部走"无等待方"路径
        var resp = await client.SendMessageAsync(new ArrayPacket(new Byte[16]));
        (resp as IDisposable)?.Dispose();

        await allDone.Task.WaitAsync(TimeSpan.FromSeconds(15));
        Assert.True(received >= pushCount + 1, $"只收到 {received} 条应答");
    }
}
