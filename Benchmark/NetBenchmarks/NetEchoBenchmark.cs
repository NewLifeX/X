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
    private ISocketClient[] _clients = null!;
    private Byte[] _payload = null!;
    private Int64 _receivedBytes;
    private Int64 _expectedBytes;

    [Params(32)]
    public Int32 PacketSize { get; set; }

    [Params(1, 1_000, 10_000)]
    public Int32 Concurrency { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _payload = new Byte[PacketSize];
        Random.Shared.NextBytes(_payload);

        _server = new EchoNetServer { Port = Port };
        _server.Start();

        _clients = new ISocketClient[Concurrency];
        for (var i = 0; i < _clients.Length; i++)
        {
            var client = new NetUri($"tcp://127.0.0.1:{Port}").CreateRemote();
            client.Received += OnReceived;
            client.Open();
            _clients[i] = client;
        }
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
        if (_clients == null || _clients.Length == 0)
            throw new InvalidOperationException("未初始化客户端");

        var batch = PacketCount / Concurrency;
        var remain = PacketCount % Concurrency;
        var tasks = new Task[Concurrency];
        for (var i = 0; i < Concurrency; i++)
        {
            var index = i;
            tasks[i] = Task.Run(() =>
            {
                var count = batch + (index < remain ? 1 : 0);
                var client = _clients[index];
                for (var n = 0; n < count; n++)
                {
                    _ = client.Send(_payload);
                }
            });
        }
        Task.WaitAll(tasks);

        if (!_completed.Wait(30_000))
            throw new TimeoutException("等待回环数据超时");

        return Interlocked.Read(ref _receivedBytes);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (_clients != null)
        {
            foreach (var client in _clients)
            {
                if (client == null) continue;

                client.Received -= OnReceived;
                client.Dispose();
            }

            _clients = null!;
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
