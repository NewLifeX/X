using System.Buffers;
using BenchmarkDotNet.Attributes;
using NewLife;
using NewLife.Data;
using NewLife.Messaging;

namespace Benchmark.StreamingBenchmarks;

/// <summary>消息帧层基准：自解析定界 / 头先行绑定 / 整帧切出 / 头部包与整帧构建</summary>
/// <remarks>
/// 命令：dotnet run --project Benchmark/Benchmark.csproj -c Release -- --filter "*MessageFrameBenchmark*"
/// SRMP 帧：4 字节头（&lt;64k）或 8 字节扩展头；负载长度覆盖三档。
/// </remarks>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 2, iterationCount: 10)]
public class MessageFrameBenchmark
{
    private static readonly SrmpCodec _codec = new();

    private Byte[] _frame = null!;
    private ReadOnlySequence<Byte> _seq;
    private ReadOnlySequence<Byte> _seqSplit;
    private Pipe _pipe = null!;
    private DefaultMessage _message = null!;
    private WebSocketCodec _wsCodec = null!;
    private Byte[] _wsFrame = null!;
    private ReadOnlySequence<Byte> _wsSeq;
    private ArrayPacket _view;

    /// <summary>负载大小（字节）</summary>
    [Params(64, 4096, 65536)]
    public Int32 Size { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // SRMP 帧：头部 + 负载
        var headerSize = Size < 0xFFFF ? 4 : 8;
        _frame = new Byte[headerSize + Size];
        _frame[0] = 0x01;
        _frame[1] = 0x66;
        if (headerSize == 4)
        {
            _frame[2] = (Byte)(Size & 0xFF);
            _frame[3] = (Byte)(Size >> 8);
        }
        else
        {
            _frame[2] = 0xFF;
            _frame[3] = 0xFF;
            _frame[4] = (Byte)(Size & 0xFF);
            _frame[5] = (Byte)((Size >> 8) & 0xFF);
            _frame[6] = (Byte)((Size >> 16) & 0xFF);
            _frame[7] = (Byte)((Size >> 24) & 0xFF);
        }
        Random.Shared.NextBytes(_frame.AsSpan(headerSize));

        _seq = new ArrayPacket(_frame).AsReadOnlySequence();

        // 跨段序列：头部最后一个字节与负载分离（头跨段最坏情形）
        IPacket chain = new ArrayPacket(_frame[..(headerSize - 1)]);
        chain.Append(new ArrayPacket(_frame[(headerSize - 1)..]));
        _seqSplit = chain.AsReadOnlySequence();

        _view = new ArrayPacket(_frame);

        _pipe = new Pipe();
        _message = new DefaultMessage();
        _message.SetBody(_view);

        // WebSocket 帧（无掩码，服务端方向）
        _wsCodec = new WebSocketCodec { IsServer = true };
        var wsHead = _wsCodec.BuildHeader(new WsMessage { Type = WebSocketMessageType.Binary }, Size);
        var headBytes = wsHead.ToArray();
        wsHead.TryDispose();

        _wsFrame = new Byte[headBytes.Length + Size];
        headBytes.CopyTo(_wsFrame, 0);
        _wsSeq = new ArrayPacket(_wsFrame).AsReadOnlySequence();
    }

    [GlobalCleanup]
    public void Cleanup() => _pipe.Dispose();

    /// <summary>SRMP 自解析定界：单段序列</summary>
    [Benchmark]
    public ParseResult? TryParse_SingleSegment() => _codec.TryParse(_seq);

    /// <summary>SRMP 自解析定界：头部跨段（最坏情形）</summary>
    [Benchmark]
    public ParseResult? TryParse_SplitSegment() => _codec.TryParse(_seqSplit);

    /// <summary>头先行绑定：头部到齐即绑定限长 Body（不切负载）</summary>
    [Benchmark]
    public Boolean HeaderFirstBind()
    {
        _pipe.Writer.Append(new ArrayPacket(_frame));

        var rs = _codec.TryParse(_pipe.Reader.Buffer);
        var ok = rs != null;
        if (rs != null)
        {
            _pipe.Reader.AdvanceTo(rs.Value.HeaderSize, rs.Value.HeaderSize);
            _message.BindBody(_pipe.Reader.Limit(rs.Value.BodyLength));
        }

        _pipe.Reader.AdvanceTo(_pipe.Reader.Buffer.Length);

        return ok;
    }

    /// <summary>整帧切出：等帧齐后 TakeFrame（拥有句柄）</summary>
    [Benchmark]
    public Boolean WholeFrameTake()
    {
        _pipe.Writer.Append(new ArrayPacket(_frame));

        var frame = _pipe.Reader.TakeFrame(_frame.Length);
        var ok = frame.Total > 0;
        frame.TryDispose();

        return ok;
    }

    /// <summary>头部包构建：BuildHeader（流式发送头部）</summary>
    [Benchmark]
    public Int32 BuildHeader()
    {
        var pk = _codec.BuildHeader(_message, Size);
        var total = pk.Total;
        pk.TryDispose();

        return total;
    }

    /// <summary>整帧构建：Build（头 + 负载链；构建转移体所有权，每轮重新注入负载）</summary>
    [Benchmark]
    public Int32 Build()
    {
        _message.SetBody(new ArrayPacket(_frame));

        var pk = _codec.Build(_message);
        var total = pk!.Total;
        pk.TryDispose();

        return total;
    }

    /// <summary>WS 帧自解析定界</summary>
    [Benchmark]
    public ParseResult? WebSocketTryParse() => _wsCodec.TryParse(_wsSeq);
}
