using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Benchmark.NetBenchmarks;
using BenchmarkDotNet.Attributes;
using NewLife;
using NewLife.Data;
using NewLife.Messaging;
using NewLife.Net;

namespace Benchmark.StreamingBenchmarks;

/// <summary>发送管道水位档位基准：SendAsync(Stream) 在不同暂停水位下的回环吞吐（服务端仅接收计数）</summary>
/// <remarks>
/// 命令：dotnet run --project Benchmark/Benchmark.csproj -c Release -- --filter "*PipeWatermarkSendBenchmark*"
/// 唯一变量为客户端发送管道水位（恢复水位取暂停水位一半）；其余条件与方法同 SessionStreamBenchmark。
/// </remarks>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 2, iterationCount: 5)]
public class PipeWatermarkSendBenchmark : IDisposable
{
    private ThroughputNetServer _server = null!;
    private TcpSession _client = null!;
    private Byte[] _payload = null!;
    private MemoryStream _stream = null!;

    /// <summary>单次发送总字节数</summary>
    private const Int32 Size = 8 * 1024 * 1024;

    /// <summary>发送管道暂停水位（恢复水位取一半）</summary>
    [Params(64 * 1024, 128 * 1024, 256 * 1024, 1024 * 1024)]
    public Int32 PauseThreshold { get; set; }

    /// <summary>全局初始化：启动计数服务端并建立回环连接，设置发送管道水位</summary>
    [GlobalSetup]
    public void Setup()
    {
        _server = new ThroughputNetServer
        {
            Port = 0,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
            UseSession = false,
        };
        _server.Start();

        var client = new NetUri($"tcp://127.0.0.1:{_server.Port}").CreateRemote();
        client.Open();
        _client = (TcpSession)client;

        // 首次访问触建发送管道与发送泵，先设水位再发送（水位为唯一变量）
        var pipe = _client.SendPipe;
        pipe.PauseThreshold = PauseThreshold;
        pipe.ResumeThreshold = PauseThreshold / 2;

        _payload = new Byte[Size];
        Random.Shared.NextBytes(_payload);
        _stream = new MemoryStream(_payload);
    }

    /// <summary>全局清理：释放客户端与服务端</summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        _client?.Dispose();
        _server?.Dispose();
    }

    /// <summary>释放资源</summary>
    public void Dispose() => Cleanup();

    /// <summary>流式发送：SendAsync(Stream) 分块入管道 + 写侧回压</summary>
    [Benchmark]
    public async Task<Int64> SendAsyncStream()
    {
        _server.Reset(Size);
        _stream.Position = 0;

        await _client.SendAsync(_stream, Size);
        if (!_server.WaitComplete(120_000)) throw new TimeoutException("SendAsync 超时");

        return _server.ReceivedBytes;
    }
}

/// <summary>接收管道水位档位基准：不同暂停水位下的回环接收吞吐（服务端帧泵即时消费，水位不应触发）</summary>
/// <remarks>
/// 命令：dotnet run --project Benchmark/Benchmark.csproj -c Release -- --filter "*PipeWatermarkReceiveBenchmark*"
/// 客户端连续发送定长帧，服务端会话管道由帧泵即时消费；唯一变量为接收管道水位（恢复水位取一半）。
/// </remarks>
[SimpleJob(warmupCount: 2, iterationCount: 5)]
public class PipeWatermarkReceiveBenchmark : IDisposable
{
    private NetServer _server = null!;
    private ISocketClient _client = null!;
    private Byte[] _frame = null!;
    private Int64 _frameCount;
    private Int64 _total;

    private Pipe? _pipe;
    private Int64 _received;
    private Int64 _target;
    private Int32 _headerSize;
    private TaskCompletionSource<Boolean>? _completed;

    /// <summary>单轮发送总字节数</summary>
    private const Int32 Size = 32 * 1024 * 1024;

    /// <summary>帧负载大小</summary>
    private const Int32 FrameSize = 32 * 1024;

    /// <summary>接收管道暂停水位（恢复水位取一半）</summary>
    [Params(64 * 1024, 256 * 1024, 1024 * 1024)]
    public Int32 PauseThreshold { get; set; }

    /// <summary>全局初始化：启动服务端并建立回环连接，等待会话管道与消费泵就绪</summary>
    [GlobalSetup]
    public void Setup()
    {
        var server = new NetServer { Port = 0 };
        server.NewSession += (s, e) =>
        {
            if (e.Session?.Session is not TcpSession session) return;

            var pipe = session.Pipe;
            pipe.PauseThreshold = PauseThreshold;
            pipe.ResumeThreshold = PauseThreshold / 2;
            _pipe = pipe;

            _ = Task.Run(() => ConsumeAsync(pipe));
        };
        server.Start();
        _server = server;

        _frame = BuildFrame(new Byte[FrameSize]);
        _headerSize = _frame.Length - FrameSize;
        _frameCount = Size / _frame.Length;
        _total = _frameCount * _frame.Length;

        _client = new NetUri($"tcp://127.0.0.1:{_server.Port}").CreateRemote();
        _client.Open();

        // 等待服务端会话管道就绪
        var sw = Stopwatch.StartNew();
        while (_pipe == null && sw.ElapsedMilliseconds < 5_000) Thread.Sleep(10);
        if (_pipe == null) throw new TimeoutException("等待服务端会话超时");
    }

    /// <summary>会话管道消费泵：帧泵整帧消费并计数</summary>
    private async Task ConsumeAsync(Pipe pipe)
    {
        var pump = new MessagePump(new SrmpCodec());
        while (true)
        {
            var msg = await pump.ReadAsync(pipe.Reader).ConfigureAwait(false);
            if (msg == null) return;

            // 帧总字节 = 头 + 负载（MessagePump 整帧切出后体为内存视图）
            var n = Interlocked.Add(ref _received, _headerSize + (msg.Body?.Remaining ?? 0));
            await MessagePump.DiscardAsync(msg).ConfigureAwait(false);
            msg.TryDispose();

            var target = Interlocked.Read(ref _target);
            if (target > 0 && n >= target) _completed?.TrySetResult(true);
        }
    }

    /// <summary>重置本轮计数与完成信号</summary>
    private void Reset()
    {
        Interlocked.Exchange(ref _target, 0);
        Interlocked.Exchange(ref _received, 0);
        _completed = new TaskCompletionSource<Boolean>(TaskCreationOptions.RunContinuationsAsynchronously);
        Interlocked.Exchange(ref _target, _total);
    }

    /// <summary>全局清理：释放客户端与服务端</summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        _client?.Dispose();
        _server?.Dispose();
    }

    /// <summary>释放资源</summary>
    public void Dispose() => Cleanup();

    /// <summary>整轮接收：客户端连续发帧，服务端帧泵即时消费至收满</summary>
    [Benchmark]
    public async Task<Int64> Receive()
    {
        Reset();

        var sender = Task.Run(() =>
        {
            for (var i = 0L; i < _frameCount; i++) _client.Send(_frame);
        });

        try
        {
            await _completed!.Task.WaitAsync(TimeSpan.FromSeconds(120)).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            throw new TimeoutException($"接收超时：已收 {Interlocked.Read(ref _received):n0} / {_total:n0}，管道暂停={_pipe?.IsPaused}，未消费={_pipe?.UnconsumedLength:n0}");
        }

        await sender.ConfigureAwait(false);

        return Interlocked.Read(ref _received);
    }

    /// <summary>构造标准消息帧（4/8字节头 + 负载）</summary>
    private static Byte[] BuildFrame(Byte[] payload)
    {
        var headerSize = payload.Length < 0xFFFF ? 4 : 8;
        var buf = new Byte[headerSize + payload.Length];
        buf[0] = 0x01;
        buf[1] = 0x02;
        if (headerSize == 4)
        {
            buf[2] = (Byte)(payload.Length & 0xFF);
            buf[3] = (Byte)(payload.Length >> 8);
        }
        else
        {
            buf[2] = 0xFF;
            buf[3] = 0xFF;
            buf[4] = (Byte)(payload.Length & 0xFF);
            buf[5] = (Byte)((payload.Length >> 8) & 0xFF);
            buf[6] = (Byte)((payload.Length >> 16) & 0xFF);
            buf[7] = (Byte)((payload.Length >> 24) & 0xFF);
        }
        payload.CopyTo(buf, headerSize);

        return buf;
    }
}
