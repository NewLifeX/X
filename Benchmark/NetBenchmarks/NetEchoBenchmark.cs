using BenchmarkDotNet.Attributes;
using NewLife;
using NewLife.Net;

namespace Benchmark.NetBenchmarks;

/// <summary>网络库Echo回环性能测试</summary>
[MemoryDiagnoser]
[SimpleJob(iterationCount: 10)]
public class NetEchoBenchmark : IDisposable
{
    private const Int32 Port = 7777;
    private const Int32 PacketCount = 200_000;

    private readonly ManualResetEventSlim _completed = new(false);
    private EchoNetServer? _server;
    private ISocketClient? _client;
    private Byte[] _payload = null!;
    private Int64 _receivedBytes;
    private Int64 _expectedBytes;

    [Params(32)]
    public Int32 PacketSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _payload = new Byte[PacketSize];
        Random.Shared.NextBytes(_payload);

        _server = new EchoNetServer { Port = Port };
        _server.Start();

        _client = new NetUri($"tcp://127.0.0.1:{Port}").CreateRemote();
        _client.Received += OnReceived;
        _client.Open();
    }

    [IterationSetup]
    public void ResetCounter()
    {
        Interlocked.Exchange(ref _receivedBytes, 0);
        Interlocked.Exchange(ref _expectedBytes, (Int64)PacketSize * PacketCount);
        _completed.Reset();
    }

    [Benchmark(Description = "TCP回环收发", OperationsPerInvoke = PacketCount)]
    public Int64 EchoRoundTrip()
    {
        var client = _client ?? throw new InvalidOperationException("未初始化客户端");

        for (var i = 0; i < PacketCount; i++)
        {
            _ = client.Send(_payload);
        }

        if (!_completed.Wait(10_000))
            throw new TimeoutException("等待回环数据超时");

        return Interlocked.Read(ref _receivedBytes);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (_client != null)
        {
            _client.Received -= OnReceived;
            _client.Dispose();
            _client = null;
        }

        if (_server != null)
        {
            _server.Dispose();
            _server = null;
        }
    }

    public void Dispose() => Cleanup();

    private void OnReceived(Object? sender, ReceivedEventArgs e)
    {
        var packet = e.Packet;
        if (packet == null || packet.Length == 0) return;

        var total = Interlocked.Add(ref _receivedBytes, packet.Total);
        if (total >= Interlocked.Read(ref _expectedBytes))
            _completed.Set();
    }
}

/// <summary>定义服务端，用于管理所有网络会话</summary>
class EchoNetServer : NetServer<EchoNetSession>
{
}

/// <summary>定义会话。每一个远程连接唯一对应一个网络会话，再次重复收发信息</summary>
class EchoNetSession : NetSession<EchoNetServer>
{
    /// <summary>收到客户端数据</summary>
    /// <param name="e">事件参数</param>
    protected override void OnReceive(ReceivedEventArgs e)
    {
        var packet = e.Packet;
        if (packet == null || packet.Length == 0) return;

        Send(packet);
    }
}
