using System.Net;
using System.Net.Sockets;
using NewLife;
using NewLife.Data;
using NewLife.Messaging;
using NewLife.Net;
using NewLife.Net.Handlers;
using Xunit;

namespace XUnitTest.Net;

/// <summary>WebSocketCodec 会话状态隔离测试</summary>
/// <remarks>
/// 处理器实例在 NetServer 的多会话间共享（会话直接引用服务器的 Pipeline）。
/// 因此粘包编码器（PacketCodec，含跨轮段链状态）必须挂在会话上（IExtend["Codec"]），
/// 不能用处理器实例字段，否则多客户端并发时跨会话串包。
/// </remarks>
[Collection("Net")]
public class WebSocketCodecSessionTests
{
    [Fact(DisplayName = "多会话并发：大帧跨轮组链不串会话")]
    public async Task MultiSession_LargeFrames_NoCrossTalk()
    {
        using var server = new NetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
        };
        server.Add(new WebSocketCodec { UserPacket = true });
        server.Received += (s, e) =>
        {
            // UserPacket=true：Message 为负载 IPacket；原样回显
            if (s is INetSession session && e.Message is IPacket pk && pk.Total > 0)
                session.SendReply(pk, e);
        };
        server.Start();

        static async Task<String?> RunClientAsync(Int32 port, String tag)
        {
            using var client = new NetClient($"tcp://127.0.0.1:{port}");
            client.Add(new WebSocketCodec { UserPacket = true });
            client.Timeout = 30_000;

            var pending = new TaskCompletionSource<Byte[]?>(TaskCreationOptions.RunContinuationsAsynchronously);
            client.Received += (s, e) =>
            {
                if (e.Message is IPacket pk)
                {
                    var data = pk.ToArray();
                    pk.TryDispose();
                    pending.TrySetResult(data);
                }
                else
                {
                    pending.TrySetResult(null);
                }
            };
            client.Open();

            for (var round = 0; round < 6; round++)
            {
                var payload = new Byte[60_000];
                Random.Shared.NextBytes(payload);

                var wait = pending = new TaskCompletionSource<Byte[]?>(TaskCreationOptions.RunContinuationsAsynchronously);
                client.SendMessage(new ArrayPacket(payload));

                Byte[]? echo;
                try
                {
                    echo = await wait.Task.WaitAsync(TimeSpan.FromSeconds(10));
                }
                catch (TimeoutException)
                {
                    return $"{tag}: 第{round}轮等待回显超时";
                }

                if (echo == null) return $"{tag}: 第{round}轮回显为空";
                if (!echo.AsSpan().SequenceEqual(payload))
                    return $"{tag}: 第{round}轮内容不一致（回显 {echo.Length} 字节，期望 {payload.Length} 字节）";
            }

            return null;
        }

        var tasks = new[] { "A", "B", "C" }.Select(tag => Task.Run(() => RunClientAsync(server.Port, tag)));
        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.Null(r));
    }
}
