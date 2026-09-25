using System.Buffers;
using System.Net;
using System.Net.Sockets;
using Benchmark.NetBenchmarks;
using BenchmarkDotNet.Attributes;
using NewLife;
using NewLife.Data;
using NewLife.Messaging;
using NewLife.Net;

namespace Benchmark.StreamingBenchmarks;

/// <summary>会话流式发送基准：SendAsync(Stream) 管道路径 / 手工分块 Send / 整段 Send（回环 TCP）</summary>
/// <remarks>
/// 命令：dotnet run --project Benchmark/Benchmark.csproj -c Release -- --filter "*SessionStreamBenchmark*"
/// 服务端仅接收计数不回发；客户端与服务器共享 CPU（loopback 口径）。
/// </remarks>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 2, iterationCount: 5)]
public class SessionStreamBenchmark : IDisposable
{
    private ThroughputNetServer _server = null!;
    private TcpSession _client = null!;
    private Byte[] _payload = null!;
    private MemoryStream _stream = null!;

    /// <summary>单次发送总字节数</summary>
    [Params(1024 * 1024, 8 * 1024 * 1024)]
    public Int32 Size { get; set; }

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

        var uri = new NetUri($"tcp://127.0.0.1:{_server.Port}");
        var client = uri.CreateRemote();
        client.Open();
        _client = (TcpSession)client;

        _payload = new Byte[Size];
        Random.Shared.NextBytes(_payload);
        _stream = new MemoryStream(_payload);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _client?.Dispose();
        _server?.Dispose();
    }

    /// <summary>清理</summary>
    public void Dispose() => Cleanup();

    /// <summary>流式发送：SendAsync(Stream) 分块零拷贝入管道 + 写侧回压</summary>
    [Benchmark]
    public async Task<Int64> SendAsyncStream()
    {
        _server.Reset(Size);
        _stream.Position = 0;

        await _client.SendAsync(_stream, Size);
        if (!_server.WaitComplete(120_000)) throw new TimeoutException("SendAsync 超时");

        return _server.ReceivedBytes;
    }

    /// <summary>手工分块：16KB 小块循环 Send(Byte[], offset, count)（传统写法，无管道）</summary>
    [Benchmark]
    public Int64 ManualChunks()
    {
        _server.Reset(Size);

        const Int32 chunk = 16 * 1024;
        for (var offset = 0; offset < Size; offset += chunk)
        {
            var count = Math.Min(chunk, Size - offset);
            _client.Send(_payload, offset, count);
        }

        if (!_server.WaitComplete(120_000)) throw new TimeoutException("手工分块超时");

        return _server.ReceivedBytes;
    }

    /// <summary>手工分块：64KB 大块循环（测分块粒度敏感性）</summary>
    [Benchmark]
    public Int64 ManualChunks64K()
    {
        _server.Reset(Size);

        const Int32 chunk = 64 * 1024;
        for (var offset = 0; offset < Size; offset += chunk)
        {
            var count = Math.Min(chunk, Size - offset);
            _client.Send(_payload, offset, count);
        }

        if (!_server.WaitComplete(120_000)) throw new TimeoutException("手工分块64K超时");

        return _server.ReceivedBytes;
    }

    /// <summary>整段发送：一次 Send(Byte[])（内核自行分段）</summary>
    [Benchmark]
    public Int64 WholeSend()
    {
        _server.Reset(Size);

        _client.Send(_payload);

        if (!_server.WaitComplete(120_000)) throw new TimeoutException("整段发送超时");

        return _server.ReceivedBytes;
    }
}
