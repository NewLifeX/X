using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;
using NewLife.Data;
using NewLife.Log;
using NewLife.Messaging;
using NewLife.Net;
using NewLife.Net.Handlers;
using Xunit;

namespace XUnitTest.Integration;

/// <summary>TCP StandardCodec 集成测试固定装置</summary>
public class TcpCodecServerFixture : IDisposable
{
    public TcpCodecNetServer Server { get; }

    public TcpCodecServerFixture()
    {
        var server = new TcpCodecNetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
            Log = XTrace.Log,
#if DEBUG
            SessionLog = XTrace.Log,
#endif
        };
        Server = server;
        Server.Start();
    }

    public void Dispose() => Server?.Stop("IntegrationTestDone");
}

public class TcpCodecNetServer : NetServer<TcpCodecSession>
{
    /// <summary>服务端解码后收到的原始载荷，线程安全</summary>
    public ConcurrentQueue<Byte[]> ReceivedPayloads { get; } = new();

    public TcpCodecNetServer()
    {
        Add<StandardCodec>();
    }
}

public class TcpCodecSession : NetSession<TcpCodecNetServer>
{
    private Int32 _count;

    protected override void OnReceive(ReceivedEventArgs e)
    {
        var msg = e.Message;
        if (msg == null) return;

        // 提取解码后的原始载荷，记录到服务端，供测试断言
        var payload = ExtractSessionPayload(msg);
        Host.ReceivedPayloads.Enqueue(payload);
        WriteLog("收到解码数据：{0} 字节", payload.Length);

        var n = Interlocked.Increment(ref _count);
        if (n % 10 == 0) Thread.Sleep(Random.Shared.Next(10, 30));

        // 复用接收链路上下文，StandardCodec.Write 可通过 GetRequest 取到原始请求，自动构造 Reply=true+相同 Sequence 的回复
        SendReply(msg, e);
    }

    private static Byte[] ExtractSessionPayload(Object msg)
    {
        if (msg is IMessage imsg) return imsg.Payload?.ToArray() ?? [];
        if (msg is IPacket pk) return pk.ToArray();
        if (msg is Byte[] buf) return buf;
        return [];
    }
}

/// <summary>UDP StandardCodec 集成测试固定装置</summary>
public class UdpCodecServerFixture : IDisposable
{
    public UdpCodecNetServer Server { get; }

    public UdpCodecServerFixture()
    {
        var server = new UdpCodecNetServer
        {
            Port = 0,
            ProtocolType = NetType.Udp,
            AddressFamily = AddressFamily.InterNetwork,
            Log = XTrace.Log,
#if DEBUG
            SessionLog = XTrace.Log,
#endif
        };
        Server = server;
        Server.Start();
        // 增大 UDP 收发缓冲区，防止高并发吞吐测试中操作系统层面丢包
        foreach (var srv in Server.Servers)
        {
            if (srv.Client != null)
            {
                srv.Client.ReceiveBufferSize = 8 << 20;
                srv.Client.SendBufferSize = 8 << 20;
            }
        }
    }

    public void Dispose() => Server?.Stop("IntegrationTestDone");
}

public class UdpCodecNetServer : NetServer<UdpCodecSession>
{
    /// <summary>服务端解码后收到的原始载荷，线程安全</summary>
    public ConcurrentQueue<Byte[]> ReceivedPayloads { get; } = new();

    public UdpCodecNetServer()
    {
        Add<StandardCodec>();
    }
}

public class UdpCodecSession : NetSession<UdpCodecNetServer>
{
    protected override void OnReceive(ReceivedEventArgs e)
    {
        var msg = e.Message;
        if (msg == null) return;

        // 记录服务端解码后收到的原始载荷
        var payload = ExtractSessionPayload(msg);
        Host.ReceivedPayloads.Enqueue(payload);

        // 复用接收链路上下文，StandardCodec.Write 可通过 GetRequest 取到原始请求，自动构造 Reply=true+相同 Sequence 的回复
        SendReply(msg, e);
    }

    private static Byte[] ExtractSessionPayload(Object msg)
    {
        if (msg is IMessage imsg) return imsg.Payload?.ToArray() ?? [];
        if (msg is IPacket pk) return pk.ToArray();
        if (msg is Byte[] buf) return buf;
        return [];
    }
}

/// <summary>NetServer+NetClient 挂 StandardCodec：同步异步收发、大包小包、粘包、并发与吞吐</summary>
[Collection("Integration")]
[TestCaseOrderer("NewLife.UnitTest.DefaultOrderer", "NewLife.UnitTest")]
public class CodecIntegrationTests(TcpCodecServerFixture tcpFixture, UdpCodecServerFixture udpFixture, FastTcpCodecServerFixture fastTcpFixture) : IClassFixture<TcpCodecServerFixture>, IClassFixture<UdpCodecServerFixture>, IClassFixture<FastTcpCodecServerFixture>
{
    private NetClient CreateTcpClient()
    {
        var client = new NetClient($"tcp://127.0.0.1:{tcpFixture.Server.Port}") { AutoReconnect = false };
        client.Add<StandardCodec>();
        return client;
    }

    private NetClient CreateUdpClient()
    {
        var client = new NetClient($"udp://127.0.0.1:{udpFixture.Server.Port}") { AutoReconnect = false };
        client.Add<StandardCodec>();
        return client;
    }

    private NetClient CreateFastTcpClient()
    {
        var client = new NetClient($"tcp://127.0.0.1:{fastTcpFixture.Server.Port}") { AutoReconnect = false };
        client.Add<StandardCodec>();
        return client;
    }

    private static Byte[] ExtractPayload(Object? message, Byte[] fallback)
    {
        if (message is IMessage imsg) return imsg.Payload?.ToArray() ?? [];
        if (message is IPacket pk) return pk.ToArray();
        if (message is Byte[] buf) return buf;
        return fallback;
    }

    [Fact(DisplayName = "06-TCP+StandardCodec 完整流程：SendMessageAsync→精确回显→服务端确认解码内容")]
    public async Task Test06_Tcp_StandardCodec_Full()
    {
        var payload = new Byte[64];
        Random.Shared.NextBytes(payload);
        var serverBefore = tcpFixture.Server.ReceivedPayloads.Count;

        using var client = CreateTcpClient();
        client.Open();

        // SendMessageAsync：StandardCodec 通过 Sequence 号匹配请求响应，返回解码前的原始载荷
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var response = await client.SendMessageAsync(payload, cts.Token);
        var received = ExtractPayload(response, []);
        client.Close("done");

        // 客户端回显精确等于发送内容
        Assert.Equal(payload, received);

        // 服务端确认收到了解码后的原始载荷
        await Task.Delay(50);
        Assert.True(tcpFixture.Server.ReceivedPayloads.Count > serverBefore, "服务端应已记录解码后的载荷");
        Assert.True(tcpFixture.Server.ReceivedPayloads.Any(p => p.SequenceEqual(payload)),
            "服务端收到的载荷应与客户端发送的完全一致");
    }

    [Fact(DisplayName = "07-TCP+StandardCodec 64KB 大包：SendMessageAsync→精确回显→服务端确认")]
    public async Task Test07_Tcp_StandardCodec_Large()
    {
        var payload = new Byte[64 * 1024];
        Random.Shared.NextBytes(payload);
        var serverBefore = tcpFixture.Server.ReceivedPayloads.Count;

        using var client = CreateTcpClient();
        client.Open();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var response = await client.SendMessageAsync(payload, cts.Token);
        var received = ExtractPayload(response, []);
        client.Close("done");

        Assert.Equal(payload, received);

        await Task.Delay(100);
        Assert.True(tcpFixture.Server.ReceivedPayloads.Count > serverBefore, "服务端应已记录大包载荷");
        Assert.True(tcpFixture.Server.ReceivedPayloads.Any(p => p.SequenceEqual(payload)),
            "服务端收到的大包应与客户端发送的完全一致");
    }

    [Fact(DisplayName = "08-TCP+StandardCodec 粘包测试：快速连发100包全部收回")]
    public async Task Test08_Tcp_StandardCodec_Sticky()
    {
        const Int32 total = 100;
        var received = 0;
        var tcs = new TaskCompletionSource<Boolean>();

        using var client = CreateTcpClient();
        client.Received += (s, e) =>
        {
            var n = Interlocked.Increment(ref received);
            if (n >= total) tcs.TrySetResult(true);
        };

        client.Open();
        for (var i = 0; i < total; i++)
        {
            var payload = new Byte[16];
            payload[0] = (Byte)i;
            client.SendMessage(payload);
        }

        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(30));
        client.Close("done");

        Assert.Equal(total, received);
    }

    [Fact(DisplayName = "09-UDP+StandardCodec 小包：SendMessageAsync→精确回显→服务端确认")]
    public async Task Test09_Udp_StandardCodec_Small()
    {
        var payload = new Byte[32];
        Random.Shared.NextBytes(payload);
        var serverBefore = udpFixture.Server.ReceivedPayloads.Count;

        using var client = CreateUdpClient();
        client.Open();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var response = await client.SendMessageAsync(payload, cts.Token);
        var received = ExtractPayload(response, []);
        client.Close("done");

        Assert.Equal(payload, received);

        await Task.Delay(50);
        Assert.True(udpFixture.Server.ReceivedPayloads.Count > serverBefore, "服务端应已记录UDP解码载荷");
        Assert.True(udpFixture.Server.ReceivedPayloads.Any(p => p.SequenceEqual(payload)),
            "服务端收到的UDP载荷应与客户端发送的完全一致");
    }

    [Fact(DisplayName = "10-UDP+StandardCodec 多包收发")]
    public async Task Test10_Udp_StandardCodec_Multi()
    {
        const Int32 total = 10;
        var bag = new ConcurrentBag<Byte[]>();
        var tcs = new TaskCompletionSource<Boolean>();

        using var client = CreateUdpClient();
        client.Received += (s, e) =>
        {
            bag.Add(ExtractPayload(e.Message, e.GetBytes()));
            if (bag.Count >= total) tcs.TrySetResult(true);
        };

        client.Open();

        var payloads = new Byte[total][];
        for (var i = 0; i < total; i++)
        {
            payloads[i] = new Byte[16];
            payloads[i][0] = (Byte)i;
            Random.Shared.NextBytes(payloads[i].AsSpan(1));
            client.SendMessage(payloads[i]);
            await Task.Delay(5);
        }

        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
        client.Close("done");

        foreach (var p in payloads)
            Assert.Contains(bag, x => x.SequenceEqual(p));
    }

    [Fact(DisplayName = "11-TCP 并发吞吐：热身+50客户端×10000消息=500000条，零丢包且TPS≥100000")]
    public async Task Test11_Tcp_Throughput_100K()
    {
        // 热身：单独客户端先跑一轮，稀释 JIT 与线程池冷启动开销
        {
            var warmupDone = 0;
            var warmupTcs = new TaskCompletionSource<Boolean>();
            const Int32 warmupCount = 1_000;
            using var wc = CreateFastTcpClient();
            wc.Received += (_, _) => { if (Interlocked.Increment(ref warmupDone) >= warmupCount) warmupTcs.TrySetResult(true); };
            wc.Open();
            for (var i = 0; i < warmupCount; i++) wc.SendMessage(new Byte[32]);
            await warmupTcs.Task.WaitAsync(TimeSpan.FromSeconds(30));
            wc.Close("warmup");
        }

        const Int32 clientCount = 50;
        const Int32 perClient = 10_000;
        const Int32 total = clientCount * perClient;

        var received = 0;
        var tcs = new TaskCompletionSource<Boolean>();

        var clients = new List<NetClient>(clientCount);
        for (var i = 0; i < clientCount; i++)
        {
            var c = CreateFastTcpClient();
            c.Received += (s, e) =>
            {
                var n = Interlocked.Increment(ref received);
                if (n >= total) tcs.TrySetResult(true);
            };
            c.Open();
            clients.Add(c);
        }

        var sw = Stopwatch.StartNew();
        var sendTasks = clients.Select(async c =>
        {
            for (var i = 0; i < perClient; i++)
            {
                var payload = new Byte[32];
                payload[0] = (Byte)(i & 0xFF);
                c.SendMessage(payload);
                if (i % 100 == 0) await Task.Yield();
            }
        }).ToArray();

        await Task.WhenAll(sendTasks);
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(60));
        sw.Stop();

        foreach (var c in clients) c.Close("done");

        var tps = (Int64)(total / sw.Elapsed.TotalSeconds);
        XTrace.WriteLine("TCP 吞吐（热身后）：{0}条/{1}ms，TPS={2}", total, sw.ElapsedMilliseconds, tps);

        Assert.Equal(total, received);
        Assert.True(tps >= 100_000, $"TPS={tps}，低于100000，耗时={sw.ElapsedMilliseconds}ms");
    }
}
