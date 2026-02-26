using BenchmarkDotNet.Attributes;
using NewLife;
using NewLife.Net;
using System.Net.Sockets;

namespace Benchmark.NetBenchmarks;

/// <summary>网络库服务端接收吞吐量基准测试</summary>
/// <remarks>
/// 测量服务端单纯接收数据的吞吐能力，服务端仅计数不回发，隔离纯接收性能。
/// 目标：超过 22,600,000 包/秒（32 字节包）。
/// 命令：EnableWindowsTargeting=true dotnet run --project Benchmark/Benchmark.csproj -c Release -- --filter "*NetServerThroughputBenchmark*"
/// </remarks>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 2, iterationCount: 5)]
public class NetServerThroughputBenchmark : IDisposable
{
    // 每次迭代总包数（固定，分配给所有并发客户端）
    private const Int32 TotalPackets = 200_000;
    private const Int32 Port = 7779;

    private ThroughputNetServer? _server;
    private ISocketClient[] _clients = null!;
    private Byte[] _payload = null!;

    /// <summary>数据包大小（字节）</summary>
    [Params(32)]
    public Int32 PacketSize { get; set; }

    /// <summary>并发客户端数</summary>
    [Params(1, 100, 1_000, 10_000)]
    public Int32 Concurrency { get; set; }

    /// <summary>全局初始化：启动服务端，建立所有客户端连接</summary>
    [GlobalSetup]
    public void Setup()
    {
        _payload = new Byte[PacketSize];
        Random.Shared.NextBytes(_payload);

        _server = new ThroughputNetServer
        {
            Port = Port,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
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

    /// <summary>每次迭代前重置服务端计数器</summary>
    [IterationSetup]
    public void ResetCounter() => _server!.Reset((Int64)PacketSize * TotalPackets);

    /// <summary>服务端接收吞吐：所有客户端并发发送，服务端仅接收计数，不回发</summary>
    [Benchmark(Description = "服务端接收吞吐", OperationsPerInvoke = TotalPackets)]
    public Int64 ServerReceiveThroughput()
    {
        var batch = TotalPackets / Concurrency;
        var remain = TotalPackets % Concurrency;
        var tasks = new Task[Concurrency];

        for (var i = 0; i < Concurrency; i++)
        {
            var idx = i;
            tasks[i] = Task.Run(() =>
            {
                var count = batch + (idx < remain ? 1 : 0);
                var client = _clients[idx];
                for (var n = 0; n < count; n++)
                    client.Send(_payload);
            });
        }

        Task.WaitAll(tasks);

        if (!_server!.WaitComplete(30_000))
            throw new TimeoutException($"等待服务端接收超时（已收 {_server.ReceivedBytes:N0} / {(Int64)PacketSize * TotalPackets:N0} 字节）");

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

        var total = Interlocked.Add(ref _receivedBytes, bytes);
        if (total >= Interlocked.Read(ref _expectedBytes))
            _completed.Set();
    }
}
