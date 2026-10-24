using BenchmarkDotNet.Attributes;
using NewLife;
using NewLife.Data;

namespace Benchmark.StreamingBenchmarks;

/// <summary>Pipe 写读回环基准：包级零拷贝路径 / 写入缓冲拷贝路径 / 背压提交成本</summary>
/// <remarks>
/// 命令：dotnet run --project Benchmark/Benchmark.csproj -c Release -- --filter "*PipeBenchmark*"
/// 三档大小覆盖：小帧（64B）、典型帧（4KB）、大帧（64KB）。
/// </remarks>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 2, iterationCount: 10)]
public class PipeBenchmark
{
    private Pipe _pipe = null!;
    private Byte[] _payload = null!;

    /// <summary>单帧大小（字节）</summary>
    [Params(64, 4096, 65536)]
    public Int32 Size { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _pipe = new Pipe();
        _payload = new Byte[Size];
        Random.Shared.NextBytes(_payload);
    }

    [GlobalCleanup]
    public void Cleanup() => _pipe.Dispose();

    /// <summary>包级零拷贝回环：Append 视图（所有权转移，不拷贝）→ ReadAsync 取窗 → AdvanceTo 消费</summary>
    [Benchmark]
    public void AppendReadRoundtrip()
    {
        _pipe.Writer.Append(new ArrayPacket(_payload));

        var rr = _pipe.Reader.ReadAsync().GetAwaiter().GetResult();
        _pipe.Reader.AdvanceTo(rr.Buffer.Length);
    }

    /// <summary>写入缓冲拷贝回环：WriteAsync 拷贝进管道缓冲（PipeWriter 形态）→ 读窗消费</summary>
    [Benchmark]
    public void WriteAsyncRoundtrip()
    {
        _pipe.Writer.WriteAsync(_payload).GetAwaiter().GetResult();

        var rr = _pipe.Reader.ReadAsync().GetAwaiter().GetResult();
        _pipe.Reader.AdvanceTo(rr.Buffer.Length);
    }

    /// <summary>背压提交快路径：FlushAsync(waitForResume:true) 在未暂停时立即完成的成本</summary>
    [Benchmark]
    public void FlushWaitForResume_NoPause() => _pipe.Writer.FlushAsync(true).GetAwaiter().GetResult();
}
