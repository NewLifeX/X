using BenchmarkDotNet.Attributes;
using NewLife;
using NewLife.Data;
using NewLife.Messaging;

namespace Benchmark.PacketBenchmarks;

/// <summary>PacketCodec 链式缓存模型性能测试：单轮借用/两轮组链/多轮段链（视图与拥有句柄两态）</summary>
/// <remarks>
/// 帧布局与 DefaultMessage 一致：4 字节头（含 2 字节小端负载长度）+ 负载。
/// 借用路径为单轮完整帧（零拷贝视图）；视图输入路径的残片需转自有拷贝（直调语义）；
/// 拥有句柄路径（接收链路语义）残片以零拷贝切片跨轮累积，轮缓冲拷贝不计入测量（IterationSetup 预建）。
/// </remarks>
[MemoryDiagnoser]
[SimpleJob(iterationCount: 20)]
public class PacketCodecBenchmark
{
    private Byte[] _frame = null!;
    private PacketCodec _codec = null!;

    /// <summary>帧长度（含 4 字节头）</summary>
    [Params(8 * 1024, 64 * 1024, 1024 * 1024)]
    public Int32 Size { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _frame = new Byte[Size];
        Random.Shared.NextBytes(_frame);

        // 头部：负载长度（2字节小端；超过 64k 用 0xFFFF 扩展头 4 字节）
        var payload = Size - 4;
        if (payload < 0xFFFF)
        {
            _frame[2] = (Byte)(payload & 0xFF);
            _frame[3] = (Byte)(payload >> 8);
        }
        else
        {
            payload = Size - 8;
            _frame[2] = 0xFF;
            _frame[3] = 0xFF;
            _frame[4] = (Byte)(payload & 0xFF);
            _frame[5] = (Byte)((payload >> 8) & 0xFF);
            _frame[6] = (Byte)((payload >> 16) & 0xFF);
            _frame[7] = (Byte)((payload >> 24) & 0xFF);
        }

        _codec = new PacketCodec { GetLength = DefaultMessage.GetLength };
    }

    [GlobalCleanup]
    public void Cleanup() => _codec.Dispose();

    [Benchmark(Description = "单轮单帧：借用零拷贝")]
    public void Borrow_SingleRound()
    {
        var frames = _codec.Parse(new ArrayPacket(_frame));
        foreach (var f in frames) f.TryDispose();
    }

    [Benchmark(Description = "两轮一帧（视图输入）：残片转拷贝后组链")]
    public void Merge_TwoRounds()
    {
        var half = Size / 2;
        _codec.Parse(new ArrayPacket(_frame, 0, half));

        var frames = _codec.Parse(new ArrayPacket(_frame, half, Size - half));
        foreach (var f in frames) f.TryDispose();
    }

    [Benchmark(Description = "多轮一帧（视图输入）：残片逐轮转拷贝")]
    public void Accumulate_MultiRounds()
    {
        const Int32 Chunk = 8 * 1024;

        IList<IPacket>? frames = null;
        for (var pos = 0; pos < Size; pos += Chunk)
        {
            var count = Math.Min(Chunk, Size - pos);
            frames = _codec.Parse(new ArrayPacket(_frame, pos, count));
        }

        if (frames != null)
        {
            foreach (var f in frames) f.TryDispose();
        }
    }

    /// <summary>两轮轮缓冲预备（拷贝不计入测量）</summary>
    [IterationSetup(Targets = new[] { nameof(Merge_TwoRounds_Owned) })]
    public void SetupTwoRounds()
    {
        var half = Size / 2;
        _rounds = new[] { CreateRound(0, half), CreateRound(half, Size - half) };
    }

    /// <summary>多轮轮缓冲预备（按 8KB，拷贝不计入测量）</summary>
    [IterationSetup(Targets = new[] { nameof(Accumulate_MultiRounds_Owned) })]
    public void SetupMultiRounds()
    {
        const Int32 Chunk = 8 * 1024;

        var list = new List<OwnerPacket>();
        for (var pos = 0; pos < Size; pos += Chunk)
        {
            var count = Math.Min(Chunk, Size - pos);
            list.Add(CreateRound(pos, count));
        }
        _rounds = list.ToArray();
    }

    private OwnerPacket[]? _rounds;

    private OwnerPacket CreateRound(Int32 offset, Int32 count)
    {
        var pk = new OwnerPacket(count);
        _frame.AsSpan(offset, count).CopyTo(pk.GetSpan());
        return pk;
    }

    [Benchmark(Description = "两轮一帧（拥有句柄）：零拷贝组链")]
    public void Merge_TwoRounds_Owned()
    {
        var rounds = _rounds!;
        var codec = _codec;
        codec.Parse(rounds[0]);
        rounds[0].TryDispose();

        var frames = codec.Parse(rounds[1]);
        rounds[1].TryDispose();

        foreach (var f in frames) f.TryDispose();
    }

    [Benchmark(Description = "多轮一帧（拥有句柄）：零拷贝段链累积")]
    public void Accumulate_MultiRounds_Owned()
    {
        var codec = _codec;

        IList<IPacket>? frames = null;
        foreach (var round in _rounds!)
        {
            frames = codec.Parse(round);
            round.TryDispose();
        }

        if (frames != null)
        {
            foreach (var f in frames) f.TryDispose();
        }
    }
}
