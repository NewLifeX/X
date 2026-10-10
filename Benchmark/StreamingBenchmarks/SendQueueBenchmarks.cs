using System.Net;
using System.Net.Sockets;
using BenchmarkDotNet.Attributes;
using NewLife.Data;
using NewLife.Net;

namespace Benchmark.StreamingBenchmarks;

/// <summary>出站发送出口吞吐基准：默认直发 vs 可选发送队列（`TcpSession.SendQueue`），并覆盖队列水位档位</summary>
/// <remarks>
/// <para>命令：dotnet run --project Benchmark/Benchmark.csproj -c Release -- --filter "*SendQueueThroughputBenchmark*"</para>
/// <para>对端持续排空，水位不应成为瓶颈——测的是发送出口本身的开销与水位档位对吞吐的影响。</para>
/// <para>队列路径用可复用的非拥有数组包（`ArrayPacket(_chunk)`）入队，把"入队必须拥有数据"的拷贝成本排除在外，
/// 只暴露队列与发送泵自身开销；真实调用方需按所有权转移语义自行准备句柄。</para>
/// </remarks>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 2, iterationCount: 3)]
public class SendQueueThroughputBenchmark : IDisposable
{
    private Socket _listener = null!;
    private Socket _peer = null!;
    private TcpSession _client = null!;
    private Byte[] _chunk = null!;
    private CancellationTokenSource _cts = null!;
    private Task _drain = null!;

    /// <summary>单块字节数</summary>
    private const Int32 ChunkSize = 32 * 1024;

    /// <summary>单次基准调用发送的总字节数</summary>
    private const Int32 TotalSize = 8 * 1024 * 1024;

    /// <summary>出站队列暂停水位（恢复水位取一半）。直发路径不使用</summary>
    [Params(64 * 1024, 256 * 1024, 1024 * 1024)]
    public Int32 PauseThreshold { get; set; }

    /// <summary>全局初始化：启动裸监听并建立回环连接，对端起排空线程</summary>
    [GlobalSetup]
    public void Setup()
    {
        _chunk = new Byte[ChunkSize];

        _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        _listener.Listen(1);
        var port = ((IPEndPoint)_listener.LocalEndPoint!).Port;

        _client = new TcpSession { Remote = new NetUri($"tcp://127.0.0.1:{port}") };
        _client.Open();

        _peer = _listener.Accept();

        // 按参数量身定制出站水位（首次访问即创建队列与发送泵）
        var queue = _client.SendQueue;
        queue.PauseThreshold = PauseThreshold;
        queue.ResumeThreshold = PauseThreshold / 2;

        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        // 对端持续排空：水位不应成为瓶颈
        _drain = Task.Factory.StartNew(() =>
        {
            var buf = new Byte[256 * 1024];
            while (!token.IsCancellationRequested)
            {
                Int32 n;
                try { n = _peer.Receive(buf); }
                catch (SocketException) { break; }
                catch (ObjectDisposedException) { break; }

                if (n <= 0) break;
            }
        }, TaskCreationOptions.LongRunning);
    }

    /// <summary>基线：同步直发（0 拷贝，写完才返回）</summary>
    [Benchmark(Baseline = true)]
    public Int32 Send_Direct()
    {
        var total = 0;
        for (var i = 0; i < TotalSize; i += ChunkSize) total += _client.Send(_chunk, 0, ChunkSize);

        return total;
    }

    /// <summary>可选发送队列：逐块入队（所有权转移），积压达水位时异步等待</summary>
    [Benchmark]
    public async Task<Int32> Send_Queued()
    {
        var total = 0;
        for (var i = 0; i < TotalSize; i += ChunkSize)
        {
            await _client.SendQueuedAsync(new ArrayPacket(_chunk)).ConfigureAwait(false);

            total += ChunkSize;
        }

        return total;
    }

    /// <summary>清理：停止排空线程并释放连接</summary>
    [GlobalCleanup]
    public void Cleanup() => Dispose();

    /// <summary>释放资源</summary>
    public void Dispose()
    {
        _cts?.Cancel();

        try { _client?.Dispose(); } catch { }
        try { _peer?.Dispose(); } catch { }
        try { _listener?.Dispose(); } catch { }
        try { _drain?.Wait(3_000); } catch { }

        _cts?.Dispose();
    }
}
