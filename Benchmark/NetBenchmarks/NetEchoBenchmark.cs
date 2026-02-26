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
