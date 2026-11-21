using BenchmarkDotNet.Attributes;
using NewLife;

namespace Benchmark.StringBenchmarks;

/// <summary>字符串裁剪与路径合并的 params 重载基准</summary>
/// <remarks>
/// 每个基准内部执行 <see cref="Ops"/> 次调用，让数组分配点位于循环内，
/// 反映逐行、逐请求场景的真实分配行为。
/// </remarks>
[MemoryDiagnoser]
[SimpleJob(iterationCount: 3)]
public class StringManipulationBenchmark
{
    private const Int32 Ops = 1024;

    private readonly String _value = "catalog/product/list";
    private readonly String _prefix = "catalog/";
    private readonly String _suffix = "/list";
    private readonly String _path = "data";
    private readonly String _repeat = "aaaaaab";
    private readonly String _unit = "a";

    [Benchmark(Baseline = true, OperationsPerInvoke = Ops, Description = "TrimPrefix params")]
    public String TrimPrefix_Params()
    {
        var rs = _value;
        for (var i = 0; i < Ops; i++) rs = _value.TrimPrefix(new String[] { _prefix });
        return rs;
    }

    [Benchmark(OperationsPerInvoke = Ops, Description = "TrimPrefix overload")]
    public String TrimPrefix_Overload()
    {
        var rs = _value;
        for (var i = 0; i < Ops; i++) rs = _value.TrimPrefix(_prefix);
        return rs;
    }

    [Benchmark(OperationsPerInvoke = Ops, Description = "TrimSuffix params")]
    public String TrimSuffix_Params()
    {
        var rs = _value;
        for (var i = 0; i < Ops; i++) rs = _value.TrimSuffix(new String[] { _suffix });
        return rs;
    }

    [Benchmark(OperationsPerInvoke = Ops, Description = "TrimSuffix overload")]
    public String TrimSuffix_Overload()
    {
        var rs = _value;
        for (var i = 0; i < Ops; i++) rs = _value.TrimSuffix(_suffix);
        return rs;
    }

    [Benchmark(OperationsPerInvoke = Ops, Description = "CombinePath params")]
    public String CombinePath_Params()
    {
        var rs = _path;
        for (var i = 0; i < Ops; i++) rs = _path.CombinePath(new String[] { "users" });
        return rs;
    }

    [Benchmark(OperationsPerInvoke = Ops, Description = "CombinePath overload")]
    public String CombinePath_Overload()
    {
        var rs = _path;
        for (var i = 0; i < Ops; i++) rs = _path.CombinePath("users");
        return rs;
    }

    [Benchmark(OperationsPerInvoke = Ops, Description = "TrimPrefix repeat params")]
    public String TrimPrefix_Repeat_Params()
    {
        var rs = _repeat;
        for (var i = 0; i < Ops; i++) rs = _repeat.TrimPrefix(new String[] { _unit });
        return rs;
    }

    [Benchmark(OperationsPerInvoke = Ops, Description = "TrimPrefix repeat overload")]
    public String TrimPrefix_Repeat_Overload()
    {
        var rs = _repeat;
        for (var i = 0; i < Ops; i++) rs = _repeat.TrimPrefix(_unit);
        return rs;
    }
}
