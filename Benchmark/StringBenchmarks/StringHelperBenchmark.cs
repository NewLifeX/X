using BenchmarkDotNet.Attributes;
using NewLife;

namespace Benchmark.StringBenchmarks;

/// <summary>忽略大小写比较扩展的分配与耗时基准</summary>
/// <remarks>
/// 每个基准内部执行 <see cref="Ops"/> 次调用，让数组分配点位于循环内。
/// .NET 10 的逃逸分析只对"每次方法调用只分配一次"的数组做栈分配，
/// 放进循环才是逐行、逐包、逐属性场景的真实分配行为。
/// </remarks>
[MemoryDiagnoser]
[SimpleJob(iterationCount: 3)]
public class StringHelperBenchmark
{
    private const Int32 Ops = 1024;

    private readonly String _value = "Content-Type";
    private readonly String _candidate = "content-type";

    [Benchmark(Baseline = true, OperationsPerInvoke = Ops, Description = "params (old call site)")]
    public Boolean Params_SingleValue()
    {
        var n = 0;
        for (var i = 0; i < Ops; i++)
        {
            if (_value.EqualIgnoreCase(new String[] { _candidate })) n++;
        }
        return n == Ops;
    }

    [Benchmark(OperationsPerInvoke = Ops, Description = "single overload")]
    public Boolean Overload_SingleValue()
    {
        var n = 0;
        for (var i = 0; i < Ops; i++)
        {
            if (_value.EqualIgnoreCase(_candidate)) n++;
        }
        return n == Ops;
    }

    [Benchmark(OperationsPerInvoke = Ops, Description = "String.Equals")]
    public Boolean Bcl_Equals()
    {
        var n = 0;
        for (var i = 0; i < Ops; i++)
        {
            if (String.Equals(_value, _candidate, StringComparison.OrdinalIgnoreCase)) n++;
        }
        return n == Ops;
    }
}
