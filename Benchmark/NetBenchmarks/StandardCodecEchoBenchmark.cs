using BenchmarkDotNet.Attributes;
using NewLife;
using NewLife.Data;
using NewLife.Net;
using NewLife.Net.Handlers;
using System.Net.Sockets;

namespace Benchmark.NetBenchmarks;

/// <summary>StandardCodec Echo性能基准测试</summary>
/// <remarks>
/// 服务端和客户端均使用 StandardCodec（4字节协议头 = 1 Flag + 1 Seq + 2 Length），
/// 测量请求-响应回路的完整开销。
/// 包含两个场景：
/// 1. 逐包Echo：每个客户端串行发送一个请求并等待响应，测量单次RTT
/// 2. 批量Echo：每个客户端并发发送255个请求后统一等待响应，测量批量吞吐
///    利用 StandardCodec 的序列号匹配（1字节，最多255个并发请求），
///    并发 SendMessageAsync 导致 TCP 自然粘包合并
/// 命令：dotnet run --project Benchmark/Benchmark.csproj -c Release -- --filter "*StandardCodecEchoBenchmark*"
/// </remarks>
[MemoryDiagnoser]
[GcServer(true)]
[SimpleJob(warmupCount: 2, iterationCount: 5)]
public class StandardCodecEchoBenchmark : IDisposable
{
    /// <summary>StandardCodec头部大小（Flag+Seq+Length = 4字节）</summary>
    private const Int32 HeaderSize = 4;

    /// <summary>目标总包大小（含协议头）</summary>
    private const Int32 PacketSize = 32;

    /// <summary>有效负载大小 = PacketSize - HeaderSize</summary>
    private const Int32 PayloadSize = PacketSize - HeaderSize; // 28

    /// <summary>逐包Echo逻辑包总数（2^17 = 131072），需被所有并发数整除</summary>
    private const Int32 SingleTotal = 131_072;

    /// <summary>批量Echo逻辑包总数（255×1024 = 261120），需被255和所有并发数整除</summary>
    private const Int32 BatchTotal = 255 * 1024; // 261,120

    /// <summary>每轮批量并发数：StandardCodec序列号1字节，最多255个并发请求</summary>
    private const Int32 BatchSize = 255;

    private const Int32 Port = 7780;

    private NetServer? _server;
    private ISocketClient[] _clients = null!;
    private Byte[] _payloadTemplate = null!;

    /// <summary>并发客户端数</summary>
    [Params(1, 4, 16, 64, 256, 1024)]
    public Int32 Concurrency { get; set; }

    /// <summary>全局初始化：启动带StandardCodec的Echo服务端和客户端</summary>
    [GlobalSetup]
    public void Setup()
    {
        // 负载模板（28字节有效数据）
        _payloadTemplate = new Byte[PayloadSize];
        Random.Shared.NextBytes(_payloadTemplate);

        SocketSetting.Current.BufferSize = 64 * 1024;

        _server = new NetServer
        {
            Port = Port,
            ProtocolType = NetType.Tcp,
            AddressFamily = AddressFamily.InterNetwork,
            UseSession = false,
        };
        _server.Add<StandardCodec>();

        // Echo：收到请求后将负载原样返回
        _server.Received += (sender, e) =>
        {
            if (sender is INetSession session && e.Message is IPacket pk)
                session.SendReply(pk, e);
        };
        _server.Start();

        _clients = new ISocketClient[Concurrency];
        for (var i = 0; i < Concurrency; i++)
        {
            var client = new NetUri($"tcp://127.0.0.1:{Port}").CreateRemote();
            client.Add<StandardCodec>();
            client.Open();
            _clients[i] = client;
        }
    }

    /// <summary>创建带预留头部空间的负载包，ExpandHeader时直接复用缓冲区避免分配</summary>
    private ArrayPacket CreatePayload()
    {
        var buf = new Byte[PacketSize];
        Buffer.BlockCopy(_payloadTemplate, 0, buf, HeaderSize, PayloadSize);
        return new ArrayPacket(buf, HeaderSize, PayloadSize);
    }

    /// <summary>逐包Echo：每个客户端串行 send→recv，测量单次RTT开销</summary>
    [Benchmark(Description = "逐包Echo(StandardCodec)", OperationsPerInvoke = SingleTotal)]
    public void SingleEcho()
    {
        var perClient = SingleTotal / Concurrency;
        var tasks = new Task[Concurrency];
        for (var c = 0; c < Concurrency; c++)
        {
            var idx = c;
            tasks[c] = Task.Run(async () =>
            {
                var client = _clients[idx];
                for (var n = 0; n < perClient; n++)
                {
                    var payload = CreatePayload();
                    await client.SendMessageAsync(payload).ConfigureAwait(false);
                }
            });
        }

        Task.WaitAll(tasks);
    }

    /// <summary>批量Echo：每个客户端并发发送255请求后统一等待响应，利用TCP粘包提升吞吐</summary>
    /// <remarks>
    /// 每轮循环中，255个 SendMessageAsync 在同一线程内依次调用（Pipeline.Write 同步完成），
    /// 快速连续的 Send 调用配合 Nagle 算法使 TCP 内核自然合并小包。
    /// 服务端 PacketCodec 拆包后逐一回复，客户端通过序列号匹配响应。
    /// </remarks>
    [Benchmark(Description = "批量Echo(StandardCodec)", OperationsPerInvoke = BatchTotal)]
    public void BatchEcho()
    {
        var perClient = BatchTotal / Concurrency;
        var rounds = perClient / BatchSize;
        var tasks = new Task[Concurrency];
        for (var c = 0; c < Concurrency; c++)
        {
            var idx = c;
            tasks[c] = Task.Run(async () =>
            {
                var client = _clients[idx];
                for (var r = 0; r < rounds; r++)
                {
                    var batch = new Task<Object>[BatchSize];
                    for (var i = 0; i < BatchSize; i++)
                    {
                        var payload = CreatePayload();
                        batch[i] = client.SendMessageAsync(payload);
                    }
                    await Task.WhenAll(batch).ConfigureAwait(false);
                }
            });
        }

        Task.WaitAll(tasks);
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
