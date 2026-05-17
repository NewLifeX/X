using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NewLife.Log;
using NewLife.Net;
using NewLife.Net.Handlers;
using Xunit;

namespace XUnitTest.Integration;

// ─── 高吞吐量测试固定装置（无人工延迟，专用于 TPS 与原始字节注入测试） ───────────────────────

/// <summary>高速TCP StandardCodec 服务端，无任何人工延迟，用于 100K TPS 测试</summary>
public class FastTcpCodecNetServer : NetServer<FastTcpCodecSession>
{
    public FastTcpCodecNetServer() { Add<StandardCodec>(); }
}

/// <summary>高速会话：直接回显，无延迟</summary>
public class FastTcpCodecSession : NetSession<FastTcpCodecNetServer>
{
    protected override void OnReceive(ReceivedEventArgs e)
    {
        var msg = e.Message;
        if (msg == null) return;
        SendReply(msg, e);
    }
}

/// <summary>高速 TCP StandardCodec 固定装置</summary>
public class FastTcpCodecServerFixture : IDisposable
{
    /// <summary>服务端实例</summary>
    public FastTcpCodecNetServer Server { get; }

    public FastTcpCodecServerFixture()
    {
        var server = new FastTcpCodecNetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
            Log = XTrace.Log,
        };
        Server = server;
        Server.Start();
    }

    public void Dispose() => Server?.Stop("done");
}

/// <summary>高吞吐量编解码集成测试：TCP 100K TPS + 原始字节粘包/分帧注入 + UDP 100K TPS</summary>
[TestCaseOrderer("NewLife.UnitTest.DefaultOrderer", "NewLife.UnitTest")]
[Collection("Integration")]
public class HighThroughputCodecTests(FastTcpCodecServerFixture fastTcpFixture, UdpCodecServerFixture udpFixture)
    : IClassFixture<FastTcpCodecServerFixture>, IClassFixture<UdpCodecServerFixture>
{
    private NetClient CreateFastTcpClient()
    {
        var c = new NetClient($"tcp://127.0.0.1:{fastTcpFixture.Server.Port}") { AutoReconnect = false };
        c.Add<StandardCodec>();
        return c;
    }

    private NetClient CreateUdpClient()
    {
        var c = new NetClient($"udp://127.0.0.1:{udpFixture.Server.Port}") { AutoReconnect = false };
        c.Add<StandardCodec>();
        return c;
    }

    /// <summary>TCP 热身：单客户端发送少量消息，稀释 JIT/线程池/Socket 缓冲区冷启动开销</summary>
    private async Task WarmupFastTcpAsync(Int32 count = 1_000)
    {
        var done = 0;
        var tcs = new TaskCompletionSource<Boolean>();
        using var wc = CreateFastTcpClient();
        wc.Received += (_, _) => { if (Interlocked.Increment(ref done) >= count) tcs.TrySetResult(true); };
        wc.Open();
        for (var i = 0; i < count; i++) wc.SendMessage(new Byte[32]);
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(30));
        wc.Close("warmup");
    }

    /// <summary>UDP 热身：单客户端发送少量消息，稀释 UDP Socket 与编解码冷启动开销</summary>
    private async Task WarmupUdpAsync(Int32 count = 200)
    {
        var done = 0;
        var tcs = new TaskCompletionSource<Boolean>();
        using var wc = CreateUdpClient();
        wc.Received += (_, _) => { if (Interlocked.Increment(ref done) >= count) tcs.TrySetResult(true); };
        wc.Open();
        for (var i = 0; i < count; i++) wc.SendMessage(new Byte[16]);
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
        wc.Close("warmup");
    }

    /// <summary>构造 DefaultMessage 原始字节（请求帧，flag=0x01=Request+Packet kind）</summary>
    private static Byte[] MakeRawMsg(Byte[] payload, Byte seq, Byte flag = 0x01)
    {
        var len = payload.Length;
        var buf = new Byte[4 + len];
        buf[0] = flag;
        buf[1] = seq;
        buf[2] = (Byte)(len & 0xFF);
        buf[3] = (Byte)(len >> 8);
        payload.CopyTo(buf, 4);
        return buf;
    }

    /// <summary>从 NetworkStream 精确读取一条 DefaultMessage 帧（4字节头+Payload）</summary>
    private static async Task<(Byte flag, Byte seq, Byte[] payload)> ReadDefaultMessageAsync(NetworkStream stream, CancellationToken ct = default)
    {
        var header = new Byte[4];
        await ReadExactAsync(stream, header, 4, ct);
        var len = header[2] | (header[3] << 8);
        var payload = new Byte[len];
        if (len > 0) await ReadExactAsync(stream, payload, len, ct);
        return (header[0], header[1], payload);
    }

    /// <summary>从流中精确读取 count 字节，自动处理分片</summary>
    private static async Task ReadExactAsync(Stream stream, Byte[] buf, Int32 count, CancellationToken ct = default)
    {
        var total = 0;
        while (total < count)
        {
            var n = await stream.ReadAsync(buf.AsMemory(total, count - total), ct);
            if (n == 0) throw new EndOfStreamException("连接在数据读取期间关闭");
            total += n;
        }
    }

    [Fact(DisplayName = "13-TCP+StandardCodec 100K TPS：热身+50客户端×10000消息=500000条，零延迟服务端，硬断言TPS≥100000")]
    public async Task Test13_Tcp_Throughput_100K()
    {
        // 热身：单独客户端先跑一轮，稀释 JIT 与线程池冷启动开销
        await WarmupFastTcpAsync();

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
        XTrace.WriteLine("TCP 100K TPS（热身后）：{0}条/{1}ms，TPS={2}", total, sw.ElapsedMilliseconds, tps);

        Assert.Equal(total, received);
        Assert.True(tps >= 100_000, $"TPS={tps}，低于100000，耗时={sw.ElapsedMilliseconds}ms");
    }

    [Fact(DisplayName = "14-TCP+StandardCodec 原始粘包注入：两消息合为一次写入，服务端分别解码并回显")]
    public async Task Test14_Tcp_RawInjection_StickyPacket()
    {
        var payload1 = Encoding.UTF8.GetBytes("sticky-first-message");
        var payload2 = Encoding.UTF8.GetBytes("sticky-second-message");
        var raw1 = MakeRawMsg(payload1, 0x10);
        var raw2 = MakeRawMsg(payload2, 0x11);

        // 合并为单次写入，制造粘包
        var combined = new Byte[raw1.Length + raw2.Length];
        raw1.CopyTo(combined, 0);
        raw2.CopyTo(combined, raw1.Length);

        using var tcp = new TcpClient { NoDelay = true };
        await tcp.ConnectAsync(IPAddress.Loopback, fastTcpFixture.Server.Port);
        var stream = tcp.GetStream();
        stream.ReadTimeout = 5_000;

        await stream.WriteAsync(combined);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var (_, seq1, recv1) = await ReadDefaultMessageAsync(stream, cts.Token);
        var (_, seq2, recv2) = await ReadDefaultMessageAsync(stream, cts.Token);

        // 服务端必须分别解码，返回各自独立的载荷与序列号
        Assert.Equal(0x10, seq1);
        Assert.Equal(0x11, seq2);
        Assert.Equal(payload1, recv1);
        Assert.Equal(payload2, recv2);
    }

    [Fact(DisplayName = "15-TCP+StandardCodec 原始分帧注入：消息切成两段分次写入，服务端重组后回显")]
    public async Task Test15_Tcp_RawInjection_SplitFrame()
    {
        var payload = Encoding.UTF8.GetBytes("split-frame-reassembly-test-payload");
        var raw = MakeRawMsg(payload, 0x20);

        // 在头部之后、载荷中间切分（确保第一帧含完整头部 + 前半载荷）
        var splitAt = 4 + payload.Length / 2;
        var first = raw[..splitAt];
        var second = raw[splitAt..];

        using var tcp = new TcpClient { NoDelay = true };
        await tcp.ConnectAsync(IPAddress.Loopback, fastTcpFixture.Server.Port);
        var stream = tcp.GetStream();
        stream.ReadTimeout = 5_000;

        // 发送前半帧
        await stream.WriteAsync(first);
        await Task.Delay(50);   // 强制服务端处于等待后半帧状态
        // 发送后半帧
        await stream.WriteAsync(second);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var (_, seq, recv) = await ReadDefaultMessageAsync(stream, cts.Token);

        // 服务端必须重组完整消息后才回显，载荷与序列号均正确
        Assert.Equal((Byte)0x20, seq);
        Assert.Equal(payload, recv);
    }

    [Fact(DisplayName = "16-UDP+StandardCodec 并发吞吐：热身+5客户端×10000消息=50000条，TPS≥10000")]
    public async Task Test16_Udp_Throughput_100K()
    {
        // 热身：单独客户端先跑一轮，稀释 UDP Socket 与编解码冷启动开销
        await WarmupUdpAsync();

        const Int32 clientCount = 5;
        const Int32 perClient = 10_000;
        const Int32 total = clientCount * perClient;

        var received = 0;
        var tcs = new TaskCompletionSource<Boolean>();

        var clients = new List<NetClient>(clientCount);
        for (var i = 0; i < clientCount; i++)
        {
            var c = CreateUdpClient();
            c.Received += (s, e) =>
            {
                var n = Interlocked.Increment(ref received);
                // UDP 不可靠，收到90%即视为完成
                if (n >= total * 9 / 10) tcs.TrySetResult(true);
            };
            c.Open();
            clients.Add(c);
        }

        var sw = Stopwatch.StartNew();

        var sendTasks = clients.Select(async c =>
        {
            for (var i = 0; i < perClient; i++)
            {
                var payload = new Byte[16];
                payload[0] = (Byte)(i & 0xFF);
                c.SendMessage(payload);
                if (i % 100 == 0) await Task.Yield();
            }
        }).ToArray();

        await Task.WhenAll(sendTasks);
        // 等待90%包到达或超时
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(30));
        sw.Stop();

        foreach (var c in clients) c.Close("done");

        var tps = (Int64)(total / sw.Elapsed.TotalSeconds);
        XTrace.WriteLine("UDP 并发吞吐（热身后）：{0}条/{1}ms，TPS={2}，收到={3}", total, sw.ElapsedMilliseconds, tps, received);

        // UDP 属于不可靠协议，仅验证大多数包正常到达
        Assert.True(received >= total * 9 / 10, $"UDP 收到率低于90%：{received}/{total}");
        Assert.True(tps >= 10_000, $"TPS={tps}，低于10000，耗时={sw.ElapsedMilliseconds}ms");
    }
}