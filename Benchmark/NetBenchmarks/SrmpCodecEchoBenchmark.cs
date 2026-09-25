using BenchmarkDotNet.Attributes;
using NewLife;
using NewLife.Data;
using NewLife.Messaging;
using NewLife.Net;
using System.Net.Sockets;

namespace Benchmark.NetBenchmarks;

/// <summary>SRMP编解码器Echo性能基准测试</summary>
/// <remarks>
/// 服务端和客户端均使用 SrmpCodec（标准消息 SRMP：1 Flag + 1 Sequence + 2 Length），
/// 测量请求-响应回路的完整开销。
/// 包含两个场景：
/// 1. 逐包Echo：每个客户端串行发送一个请求并等待响应，测量单次RTT
/// 2. 滑动窗口Echo：每个客户端始终保持255个在途请求，任一完成立即补发下一个，
///    保持匹配队列接近满载，充分利用 TCP 流水线和 Nagle 合包。
///    SRMP 序列号仅低8位有效（至多255个唯一号），窗口不得超过255，在途请求序列号重复会导致配对错乱。
/// 命令：dotnet run --project Benchmark/Benchmark.csproj -c Release -- --filter "*SrmpCodecEchoBenchmark*"
/// </remarks>
[MemoryDiagnoser]
[GcServer(true)]
[SimpleJob(warmupCount: 2, iterationCount: 5)]
public class SrmpCodecEchoBenchmark : IDisposable
{
    /// <summary>SRMP头部大小（Flag+Seq+Length = 4字节）</summary>
    private const Int32 HeaderSize = 4;

    /// <summary>目标总包大小（含协议头）</summary>
    private const Int32 PacketSize = 32;

    /// <summary>有效负载大小 = PacketSize - HeaderSize</summary>
    private const Int32 PayloadSize = PacketSize - HeaderSize; // 28

    /// <summary>逐包Echo逻辑包总数（2^17 = 131072），需被所有并发数整除</summary>
    private const Int32 SingleTotal = 131_072;

    /// <summary>批量Echo逻辑包总数（255×1024 = 261120），需被所有并发数整除</summary>
    private const Int32 BatchTotal = 255 * 1024; // 261,120

    /// <summary>滑动窗口大小：SRMP序列号仅低8位有效，至多255个在途请求且序列号不得重复</summary>
    private const Int32 WindowSize = 255;

    private const Int32 Port = 7780;

    private NetServer? _server;
    private ISocketClient[] _clients = null!;
    private Byte[] _payloadTemplate = null!;

    /// <summary>并发客户端数</summary>
    [Params(1, 4, 16, 64, 256, 1024)]
    public Int32 Concurrency { get; set; }

    /// <summary>全局初始化：启动带SrmpCodec的Echo服务端和客户端</summary>
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
            Protocol = new SrmpCodec(),
        };

        // Echo：收到请求后将负载原样返回（同步读满——处理器返回后消息收尾，未读体随消息丢弃）
        _server.Received += (sender, e) =>
        {
            if (sender is not INetSession session || e.Message is not DefaultMessage req) return;

            var reply = req.CreateReply();
            if (req.Body != null) reply.SetBody(req.Body.ReadAllAsync().AsTask().GetAwaiter().GetResult());
            session.SendReply(reply, e);
        };
        _server.Start();

        _clients = new ISocketClient[Concurrency];
        for (var i = 0; i < Concurrency; i++)
        {
            var client = new NetUri($"tcp://127.0.0.1:{Port}").CreateRemote();
            ((SessionBase)client).Protocol = new SrmpCodec();
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

    /// <summary>创建请求消息：序列号1..255轮流分配（SRMP仅低8位有效，在途请求不得重复）</summary>
    private DefaultMessage CreateRequest(Int32 sequence)
    {
        var msg = new DefaultMessage { Sequence = sequence };
        msg.SetBody(CreatePayload());
        return msg;
    }

    /// <summary>逐包Echo：每个客户端串行 send→recv，测量单次RTT开销</summary>
    [Benchmark(Description = "逐包Echo(SrmpCodec)", OperationsPerInvoke = SingleTotal)]
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
                    // 串行单请求在途，固定序列号安全
                    var rs = await client.SendMessageAsync(CreateRequest(1)).ConfigureAwait(false);

                    // 响应消息由等待方持有并负责释放；不释放会吊住接收缓冲，导致池失血
                    rs.TryDispose();
                }
            });
        }

        Task.WaitAll(tasks);
    }

    /// <summary>滑动窗口Echo：始终保持WindowSize个请求在途，任一完成立即补发下一个</summary>
    /// <remarks>
    /// 滑动窗口模式保持匹配队列始终接近满载（255），避免批量等待全部完成后再发的锯齿效应。
    /// 每个槽位固定使用同一序列号（slot+1），旧请求完成出窗后才在原槽位复用，保证在途序列号唯一。
    /// </remarks>
    [Benchmark(Description = "滑动窗口Echo(SrmpCodec)", OperationsPerInvoke = BatchTotal)]
    public void SlidingWindowEcho()
    {
        var perClient = BatchTotal / Concurrency;
        var tasks = new Task[Concurrency];
        for (var c = 0; c < Concurrency; c++)
        {
            var idx = c;
            tasks[c] = Task.Run(async () =>
            {
                var client = _clients[idx];
                var fill = Math.Min(WindowSize, perClient);
                var window = new ValueTask<Object>[fill];
                var sent = 0;

                // 填满初始窗口：序列号 1..fill
                for (var i = 0; i < fill; i++)
                {
                    window[i] = client.SendMessageAsync(CreateRequest(i + 1));
                    sent++;
                }

                // 滑动：await 最旧的请求，原槽位立即补发（序列号随槽位复用）
                var slot = 0;
                while (sent < perClient)
                {
                    var rs = await window[slot].ConfigureAwait(false);
                    rs.TryDispose();    // 响应消息由等待方负责释放

                    window[slot] = client.SendMessageAsync(CreateRequest(slot + 1));
                    sent++;
                    slot = (slot + 1) % fill;
                }

                // 排空剩余窗口
                for (var i = 0; i < fill; i++)
                {
                    var rs = await window[(slot + i) % fill].ConfigureAwait(false);
                    rs.TryDispose();
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
