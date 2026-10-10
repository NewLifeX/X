using BenchmarkDotNet.Attributes;
using NewLife;
using NewLife.Net;
using System.Net.Sockets;

namespace Benchmark.NetBenchmarks;

/// <summary>网络库服务端接收吞吐量基准测试</summary>
/// <remarks>
/// 包含两个测试方法，同表对比：
/// 1. 逐包发送：每次 Send(32B)，衡量每次 recv() 回调的完整处理开销
/// 2. 批量发送：256 包合并为 Send(8KB)，衡量 TCP 流式吞吐（开销被 TCP 粘包分摊）
/// 命令：dotnet run --project Benchmark/Benchmark.csproj -c Release -- --filter "*NetServerThroughputBenchmark*"
/// </remarks>
[MemoryDiagnoser]
[GcServer(true)]
[SimpleJob(warmupCount: 2, iterationCount: 5)]
public class NetServerThroughputBenchmark : IDisposable
{
    /// <summary>逐包发送逻辑包总数（2^21），C=1 时迭代约 1 秒</summary>
    private const Int32 PerPacketTotal = 2_097_152;

    /// <summary>批量发送逻辑包总数（2^24），确保迭代 >100ms</summary>
    private const Int32 BatchTotal = 16_777_216;

    /// <summary>批量发送合并数：256 个 32B 包 = 8KB 一次 Send</summary>
    private const Int32 BatchSize = 256;

    private const Int32 Port = 7779;

    private ThroughputNetServer? _server;
    private ISocketClient[] _clients = null!;
    private Byte[] _singlePayload = null!;
    private Byte[] _batchPayload = null!;

    /// <summary>数据包大小（字节）</summary>
    [Params(32)]
    public Int32 PacketSize { get; set; }

    /// <summary>并发客户端数</summary>
    [Params(1, 4, 16, 64, 256, 1024)]
    public Int32 Concurrency { get; set; }

    /// <summary>全局初始化：启动服务端，建立所有客户端连接</summary>
    [GlobalSetup]
    public void Setup()
    {
        _singlePayload = new Byte[PacketSize];
        Random.Shared.NextBytes(_singlePayload);

        _batchPayload = new Byte[BatchSize * PacketSize];
        for (var i = 0; i < BatchSize; i++)
            Buffer.BlockCopy(_singlePayload, 0, _batchPayload, i * PacketSize, PacketSize);

        // 增大 IOCP 接收缓冲区
        SocketSetting.Current.BufferSize = 64 * 1024;

        _server = new ThroughputNetServer
        {
            Port = Port,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
            UseSession = false,
        };
        _server.Start();

        _clients = new ISocketClient[Concurrency];
        for (var i = 0; i < Concurrency; i++)
        {
            var client = new NetUri($"tcp://127.0.0.1:{Port}").CreateRemote();
            client.Open();
            _clients[i] = client;
        }
    }

    /// <summary>逐包发送：每次 Send(32B)，测量每次 recv() 回调的完整处理开销</summary>
    [Benchmark(Description = "逐包发送", OperationsPerInvoke = PerPacketTotal)]
    public Int64 PerPacketThroughput()
    {
        _server!.Reset((Int64)PacketSize * PerPacketTotal);

        var perClient = PerPacketTotal / Concurrency;
        var tasks = new Task[Concurrency];
        for (var i = 0; i < Concurrency; i++)
        {
            var idx = i;
            tasks[i] = Task.Run(() =>
            {
                var client = _clients[idx];
                var payload = _singlePayload;
                for (var n = 0; n < perClient; n++)
                    client.Send(payload);
            });
        }

        Task.WaitAll(tasks);
        if (!_server!.WaitComplete(120_000))
            throw new TimeoutException($"逐包发送超时（已收 {_server.ReceivedBytes:N0} / {(Int64)PacketSize * PerPacketTotal:N0}）");

        return _server.ReceivedBytes;
    }

    /// <summary>批量发送：256 包合并为 8KB 一次 Send，测量 TCP 流式吞吐上限</summary>
    [Benchmark(Description = "批量发送", OperationsPerInvoke = BatchTotal)]
    public Int64 BatchThroughput()
    {
        _server!.Reset((Int64)PacketSize * BatchTotal);

        var perClient = BatchTotal / Concurrency;
        var sendsPerClient = perClient / BatchSize;
        var tasks = new Task[Concurrency];
        for (var i = 0; i < Concurrency; i++)
        {
            var idx = i;
            tasks[i] = Task.Run(() =>
            {
                var client = _clients[idx];
                var payload = _batchPayload;
                for (var n = 0; n < sendsPerClient; n++)
                    client.Send(payload);
            });
        }

        Task.WaitAll(tasks);
        if (!_server!.WaitComplete(60_000))
            throw new TimeoutException($"批量发送超时（已收 {_server.ReceivedBytes:N0} / {(Int64)PacketSize * BatchTotal:N0}）");

        return _server.ReceivedBytes;
    }

    /// <summary>全局清理：释放所有客户端和服务端</summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        if (_clients != null)
        {
            foreach (var c in _clients)
                c?.Dispose();
            _clients = null!;
        }

        _server?.Dispose();
        _server = null;
    }

    /// <summary>释放资源</summary>
    public void Dispose() => Cleanup();
}

/// <summary>服务端仅接收计数，不回发，专用于吞吐量测试</summary>
class ThroughputNetServer : NetServer
{
    private Int64 _receivedBytes;
    private Int64 _expectedBytes;
    private readonly ManualResetEventSlim _completed = new(false);

    /// <summary>已接收总字节数</summary>
    public Int64 ReceivedBytes => Interlocked.Read(ref _receivedBytes);

    /// <summary>重置计数器并设置期望接收字节数</summary>
    /// <param name="expectedBytes">本轮期望接收的总字节数</param>
    public void Reset(Int64 expectedBytes)
    {
        Interlocked.Exchange(ref _receivedBytes, 0);
        Interlocked.Exchange(ref _expectedBytes, expectedBytes);
        _completed.Reset();
    }

    /// <summary>等待服务端接收完成</summary>
    /// <param name="millisecondsTimeout">超时毫秒数</param>
    /// <returns>是否在超时前接收完成</returns>
    public Boolean WaitComplete(Int32 millisecondsTimeout) => _completed.Wait(millisecondsTimeout);

    /// <summary>接收数据仅计数，不回发</summary>
    protected override void OnReceive(INetSession session, ReceivedEventArgs e)
    {
        var bytes = e.Packet?.Total ?? 0;
        if (bytes <= 0) return;

        var expected = Interlocked.Read(ref _expectedBytes);
        var total = Interlocked.Add(ref _receivedBytes, bytes);
        if (total >= expected)
            _completed.Set();
    }
}

/// <summary>裸 Socket 层 TCP 回声基准：逐包往返与流水线两种模式，覆盖包大小与并发维度</summary>
/// <remarks>
/// <para>对比 NetServerThroughputBenchmark（单向接收、32B 固定维度）：本类走完整回声链路（收→回发→收），
/// 维度为 包大小 × 并发，可用于评估不同报文规模下的往返链路成本与流水线吞吐上限。</para>
/// <para>换算说明：单迭代数据量按 <see cref="TargetBytes"/> 字节限流，小包档以往返次数/包数上限保护；
/// 不设 OperationsPerInvoke，报表 ns/op 即“每迭代”成本；
/// MB/s = 传输字节 ÷ 迭代耗时，msg/s = 包数 ÷ 迭代耗时。</para>
/// <para>命令：dotnet run --project Benchmark/Benchmark.csproj -c Release -- --filter "*NetEchoBenchmark*"</para>
/// </remarks>
[MemoryDiagnoser]
[GcServer(true)]
[SimpleJob(warmupCount: 2, iterationCount: 5)]
public class NetEchoBenchmark : IDisposable
{
    /// <summary>单迭代数据传输字节目标（大包档按字节限流）</summary>
    private const Int64 TargetBytes = 256L * 1024 * 1024;

    /// <summary>往返模式单迭代总往返次数上限（小包档防迭代过长）</summary>
    private const Int32 MaxRoundTrips = 50_000;

    /// <summary>流水线模式单迭代总包数上限（小包档防迭代过长）</summary>
    private const Int32 MaxPackets = 2_000_000;

    private const Int32 EchoPort = 7780;

    /// <summary>接收兜底超时，防基准迭代永久挂起</summary>
    private const Int32 EchoTimeoutMs = 120_000;

    private NetServer? _echoServer;
    private ISocketClient[] _echoClients = null!;
    private Byte[] _echoPayload = null!;

    /// <summary>数据包大小（字节）</summary>
    [Params(16, 256, 4096, 65536, 1048576)]
    public Int32 EchoPacketSize { get; set; }

    /// <summary>并发客户端数</summary>
    [Params(1, 4, 16, 64)]
    public Int32 EchoConcurrency { get; set; }

    /// <summary>每客户端单迭代往返数（字节目标与次数上限取小者）</summary>
    private Int32 RoundsPerClient => Math.Max(1, (Int32)Math.Min(TargetBytes / EchoPacketSize, MaxRoundTrips) / EchoConcurrency);

    /// <summary>每客户端单迭代包数（字节目标与包数上限取小者）</summary>
    private Int32 PacketsPerClient => Math.Max(1, (Int32)Math.Min(TargetBytes / EchoPacketSize, MaxPackets) / EchoConcurrency);

    /// <summary>全局初始化：启动回声服务端并建立全部客户端</summary>
    [GlobalSetup(Targets = [nameof(RoundTrip), nameof(Pipeline)])]
    public void EchoSetup()
    {
        // 高并发同步往返会触发线程池注入限速（~1线程/秒），预热最小线程避免秒级毛刺污染测量
        ThreadPool.SetMinThreads(512, 512);

        _echoPayload = new Byte[EchoPacketSize];
        Random.Shared.NextBytes(_echoPayload);

        // 增大接收缓冲区，降低大包分段的系统调用开销
        SocketSetting.Current.BufferSize = 256 * 1024;

        _echoServer = new NetServer
        {
            Port = EchoPort,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
        };
        _echoServer.Received += (s, e) =>
        {
            if (e.Packet != null && s is INetSession session) session.Send(e.Packet);
        };
        _echoServer.Start();

        _echoClients = new ISocketClient[EchoConcurrency];
        for (var i = 0; i < EchoConcurrency; i++)
        {
            // 拉取模式：每客户端独享直读回显，避免事件分发干扰基准
            var client = new TcpSession
            {
                Remote = new NetUri($"tcp://127.0.0.1:{EchoPort}"),
                AutoReceive = false,
                BufferSize = Math.Max(64 * 1024, EchoPacketSize),
                Timeout = EchoTimeoutMs,
            };
            client.Open();
            _echoClients[i] = client;
        }
    }

    /// <summary>逐包往返：每客户端发一包读满一包回显，测最小往返链路开销</summary>
    [Benchmark(Description = "逐包往返")]
    public Int64 RoundTrip()
    {
        var rounds = RoundsPerClient;
        var tasks = new Task[EchoConcurrency];
        for (var i = 0; i < EchoConcurrency; i++)
        {
            var idx = i;
            tasks[i] = Task.Run(() => EchoLoop(_echoClients[idx], rounds));
        }

        Task.WaitAll(tasks);

        return (Int64)EchoPacketSize * rounds * EchoConcurrency;
    }

    /// <summary>流水线：各客户端持续发送不等待回显，服务端边收边回，客户端按字节读满</summary>
    [Benchmark(Description = "流水线")]
    public Int64 Pipeline()
    {
        var count = PacketsPerClient;
        var tasks = new Task[EchoConcurrency];
        for (var i = 0; i < EchoConcurrency; i++)
        {
            var idx = i;
            tasks[i] = Task.Run(() => SendAndReceive(_echoClients[idx], count));
        }

        Task.WaitAll(tasks);

        return (Int64)EchoPacketSize * count * EchoConcurrency;
    }

    /// <summary>单客户端逐包往返：发一包后读满同字节回显，再发下一包</summary>
    private void EchoLoop(ISocketClient client, Int32 rounds)
    {
        for (var n = 0; n < rounds; n++)
        {
            client.Send(_echoPayload);

            var received = 0;
            while (received < EchoPacketSize)
            {
                using var pk = client.Receive();
                if (pk == null || pk.Length <= 0) throw new TimeoutException("回显接收中断");

                received += pk.Length;
            }
        }
    }

    /// <summary>单客户端流水线：发送与回显读取并行，按字节计数读满目标流量</summary>
    private void SendAndReceive(ISocketClient client, Int32 count)
    {
        var expected = (Int64)EchoPacketSize * count;
        var received = 0L;

        var sendTask = Task.Run(() =>
        {
            for (var n = 0; n < count; n++)
                client.Send(_echoPayload);
        });

        while (received < expected)
        {
            using var pk = client.Receive();
            if (pk == null || pk.Length <= 0) throw new TimeoutException("回显接收中断");

            received += pk.Length;
        }

        sendTask.Wait();
    }

    /// <summary>回声基准清理：释放所有客户端和服务端</summary>
    [GlobalCleanup(Targets = [nameof(RoundTrip), nameof(Pipeline)])]
    public void EchoCleanup()
    {
        if (_echoClients != null)
        {
            foreach (var c in _echoClients)
                c?.Dispose();
            _echoClients = null!;
        }

        _echoServer?.Dispose();
        _echoServer = null;
    }

    /// <summary>释放资源</summary>
    public void Dispose() => EchoCleanup();
}
