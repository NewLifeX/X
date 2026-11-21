using System.Globalization;
using System.Text;
using BenchmarkDotNet.Attributes;
using NewLife.Serialization;

namespace Benchmark;

/// <summary>JsonParser 解析性能与内存基准</summary>
[MemoryDiagnoser]
[MediumRunJob]
public class JsonParserBenchmarks
{
    private String _object = null!;
    private String _ints = null!;
    private String _rows = null!;
    private String _nested = null!;
    private String _escaped = null!;
    private String _decimals = null!;
    private String _chinese = null!;

    [GlobalSetup]
    public void Setup()
    {
        _object = """{"Name":"NewLife","Version":"12.0.2026.1119","Enabled":true,"Count":42,"Ratio":0.618,"Tags":["a","b","c"],"Nested":{"X":1,"Y":2.5},"Note":"hello","NullValue":null}""";

        var builder = new StringBuilder(8192);
        builder.Append('[');
        for (var i = 0; i < 1000; i++)
        {
            if (i > 0) builder.Append(',');
            builder.Append(i);
        }
        builder.Append(']');
        _ints = builder.ToString();

        builder.Clear();
        builder.Append('[');
        for (var i = 0; i < 1000; i++)
        {
            if (i > 0) builder.Append(',');
            builder.Append(CultureInfo.InvariantCulture, $"[\"2026-11-21T10:00:{i % 60:00}.000Z\",{i},1.25,\"value-{i}\",true]");
        }
        builder.Append(']');
        _rows = builder.ToString();

        builder.Clear();
        builder.Append('[');
        for (var i = 0; i < 200; i++)
        {
            if (i > 0) builder.Append(',');
            builder.Append(CultureInfo.InvariantCulture, $"{{\"Id\":{i},\"Name\":\"name-{i}\",\"Value\":1.5,\"Flag\":true,\"Child\":{{\"X\":{i},\"Y\":\"z\"}}}}");
        }
        builder.Append(']');
        _nested = builder.ToString();

        builder.Clear();
        builder.Append('[');
        for (var i = 0; i < 1000; i++)
        {
            if (i > 0) builder.Append(',');
            builder.Append("\"a\\\"b\\\\c\\nd\\u4e2d\"");
        }
        builder.Append(']');
        _escaped = builder.ToString();

        builder.Clear();
        builder.Append('[');
        for (var i = 0; i < 1000; i++)
        {
            if (i > 0) builder.Append(',');
            builder.Append((i / 1000.0).ToString("0.000", CultureInfo.InvariantCulture));
        }
        builder.Append(']');
        _decimals = builder.ToString();

        builder.Clear();
        builder.Append('[');
        for (var i = 0; i < 1000; i++)
        {
            if (i > 0) builder.Append(',');
            builder.Append("\"中文测试数据\"");
        }
        builder.Append(']');
        _chinese = builder.ToString();
    }

    [Benchmark(Description = "典型对象 10 字段")]
    public Object? Object() => new JsonParser(_object).Decode();

    [Benchmark(Description = "整数数组 1000 项")]
    public Object? IntArray() => new JsonParser(_ints).Decode();

    [Benchmark(Description = "行数组 1000×5")]
    public Object? Rows() => new JsonParser(_rows).Decode();

    [Benchmark(Description = "嵌套对象 200 项")]
    public Object? Nested() => new JsonParser(_nested).Decode();

    [Benchmark(Description = "转义字符串 1000 项")]
    public Object? Escaped() => new JsonParser(_escaped).Decode();

    [Benchmark(Description = "小数数组 1000 项")]
    public Object? Decimals() => new JsonParser(_decimals).Decode();

    [Benchmark(Description = "中文数组 1000 项")]
    public Object? Chinese() => new JsonParser(_chinese).Decode();
}
