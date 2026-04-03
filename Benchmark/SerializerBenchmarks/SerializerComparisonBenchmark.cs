using System.IO;
using BenchmarkDotNet.Attributes;
using NewLife.Buffers;
using NewLife.Serialization;

namespace Benchmark.SerializerBenchmarks;

// -------------------------------------------------------
// 测试用模型定义
// -------------------------------------------------------

/// <summary>普通 POCO：通过反射路径序列化</summary>
public class SimpleModel
{
    public Int32 Id { get; set; }
    public String Name { get; set; } = String.Empty;
    public Boolean IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public Double Score { get; set; }
}

/// <summary>嵌套 POCO：含引用类型成员</summary>
public class NestedModel
{
    public Int32 Id { get; set; }
    public String Title { get; set; } = String.Empty;
    public SimpleModel? Inner { get; set; }
    public Int64 Timestamp { get; set; }
}

/// <summary>ISpanSerializable 实现：零反射快速路径</summary>
public class FastModel : ISpanSerializable
{
    public Int32 Id { get; set; }
    public String Name { get; set; } = String.Empty;
    public Boolean IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public Double Score { get; set; }

    private static readonly DateTime _dt1970 = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>将对象成员序列化写入SpanWriter</summary>
    /// <param name="writer">Span写入器</param>
    public void Write(ref SpanWriter writer)
    {
        writer.Write(Id);
        writer.Write(Name, 0);
        writer.Write((Byte)(IsActive ? 1 : 0));
        writer.Write(CreatedAt > DateTime.MinValue ? (UInt32)(CreatedAt - _dt1970).TotalSeconds : 0u);
        writer.Write(Score);
    }

    /// <summary>从SpanReader反序列化读取对象成员</summary>
    /// <param name="reader">Span读取器</param>
    public void Read(ref SpanReader reader)
    {
        Id = reader.ReadInt32();
        Name = reader.ReadString() ?? String.Empty;
        IsActive = reader.ReadByte() != 0;
        var sec = reader.ReadUInt32();
        CreatedAt = sec == 0 ? DateTime.MinValue : _dt1970.AddSeconds(sec);
        Score = reader.ReadDouble();
    }
}

// -------------------------------------------------------
// 序列化基准测试
// -------------------------------------------------------

/// <summary>SpanSerializer vs Binary 序列化性能对比</summary>
/// <remarks>
/// 三条测试路径：
/// <list type="bullet">
/// <item>SpanSerializer(反射) — 普通 POCO，通过编译委托序列化</item>
/// <item>SpanSerializer(ISpanSerializable) — 实现接口，零反射快速路径</item>
/// <item>Binary — 基于 Stream 的传统序列化</item>
/// </list>
/// </remarks>
[MemoryDiagnoser]
[SimpleJob]
public class SerializerSerializeBenchmark
{
    private SimpleModel _simple = null!;
    private FastModel _fast = null!;
    private NestedModel _nested = null!;
    private Byte[] _buffer = null!;
    private MemoryStream _stream = null!;

    [GlobalSetup]
    public void Setup()
    {
        _simple = new SimpleModel
        {
            Id = 42,
            Name = "NewLife",
            IsActive = true,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Score = 99.5,
        };
        _fast = new FastModel
        {
            Id = 42,
            Name = "NewLife",
            IsActive = true,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Score = 99.5,
        };
        _nested = new NestedModel
        {
            Id = 1,
            Title = "Hello World",
            Inner = _simple,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        };
        _buffer = new Byte[512];
        _stream = new MemoryStream(_buffer);
    }

    // ── SpanSerializer(反射) ─────────────────────────────

    [Benchmark(Description = "Span_Simple序列化_到Span", Baseline = true)]
    public Int32 Span_Serialize_Simple_ToSpan()
    {
        var writer = new SpanWriter(_buffer);
        SpanSerializer.WriteObject(ref writer, _simple, _simple.GetType());
        return writer.WrittenCount;
    }

    [Benchmark(Description = "Span_Simple序列化_写缓冲")]
    public void Span_Serialize_Simple_Pool()
    {
        var writer = new SpanWriter(_buffer);
        SpanSerializer.WriteObject(ref writer, _simple, _simple.GetType());
    }

    [Benchmark(Description = "Span_Nested序列化_到Span")]
    public Int32 Span_Serialize_Nested_ToSpan()
    {
        var writer = new SpanWriter(_buffer);
        SpanSerializer.WriteObject(ref writer, _nested, _nested.GetType());
        return writer.WrittenCount;
    }

    // ── SpanSerializer(ISpanSerializable) ───────────────

    [Benchmark(Description = "Span_Fast序列化_到Span")]
    public Int32 Span_Serialize_Fast_ToSpan()
    {
        var writer = new SpanWriter(_buffer);
        SpanSerializer.WriteObject(ref writer, _fast, _fast.GetType());
        return writer.WrittenCount;
    }

    [Benchmark(Description = "Span_Fast序列化_写缓冲")]
    public void Span_Serialize_Fast_Pool()
    {
        var writer = new SpanWriter(_buffer);
        SpanSerializer.WriteObject(ref writer, _fast, _fast.GetType());
    }

    // ── Binary ──────────────────────────────────────────

    [Benchmark(Description = "Binary_Simple序列化")]
    public void Binary_Serialize_Simple()
    {
        _stream.Position = 0;
        Binary.FastWrite(_simple, _stream);
    }

    [Benchmark(Description = "Binary_Nested序列化")]
    public void Binary_Serialize_Nested()
    {
        _stream.Position = 0;
        Binary.FastWrite(_nested, _stream);
    }
}

// -------------------------------------------------------
// 反序列化基准测试
// -------------------------------------------------------

/// <summary>SpanSerializer vs Binary 反序列化性能对比</summary>
[MemoryDiagnoser]
[SimpleJob]
public class SerializerDeserializeBenchmark
{
    private Byte[] _spanSimpleBytes = null!;
    private Byte[] _spanFastBytes = null!;
    private Byte[] _spanNestedBytes = null!;
    private Byte[] _binarySimpleBytes = null!;
    private Byte[] _binaryNestedBytes = null!;

    [GlobalSetup]
    public void Setup()
    {
        var simple = new SimpleModel
        {
            Id = 42,
            Name = "NewLife",
            IsActive = true,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Score = 99.5,
        };
        var fast = new FastModel
        {
            Id = 42,
            Name = "NewLife",
            IsActive = true,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Score = 99.5,
        };
        var nested = new NestedModel
        {
            Id = 1,
            Title = "Hello World",
            Inner = simple,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        };

        // SpanSerializer — 序列化为字节
        var buf = new Byte[512];
        var w1 = new SpanWriter(buf);
        SpanSerializer.WriteObject(ref w1, simple, simple.GetType());
        _spanSimpleBytes = buf[..w1.WrittenCount];

        var w2 = new SpanWriter(buf);
        SpanSerializer.WriteObject(ref w2, fast, fast.GetType());
        _spanFastBytes = buf[..w2.WrittenCount];

        var w3 = new SpanWriter(buf);
        SpanSerializer.WriteObject(ref w3, nested, nested.GetType());
        _spanNestedBytes = buf[..w3.WrittenCount];

        // Binary — 序列化为字节
        using var ms1 = new MemoryStream();
        Binary.FastWrite(simple, ms1);
        _binarySimpleBytes = ms1.ToArray();

        using var ms2 = new MemoryStream();
        Binary.FastWrite(nested, ms2);
        _binaryNestedBytes = ms2.ToArray();
    }

    // ── SpanSerializer(反射) ─────────────────────────────

    [Benchmark(Description = "Span_Simple反序列化", Baseline = true)]
    public SimpleModel Span_Deserialize_Simple()
    {
        var reader = new SpanReader(_spanSimpleBytes);
        return (SimpleModel)SpanSerializer.ReadObject(ref reader, typeof(SimpleModel));
    }

    [Benchmark(Description = "Span_Nested反序列化")]
    public NestedModel Span_Deserialize_Nested()
    {
        var reader = new SpanReader(_spanNestedBytes);
        return (NestedModel)SpanSerializer.ReadObject(ref reader, typeof(NestedModel));
    }

    // ── SpanSerializer(ISpanSerializable) ───────────────

    [Benchmark(Description = "Span_Fast反序列化")]
    public FastModel Span_Deserialize_Fast()
    {
        var reader = new SpanReader(_spanFastBytes);
        return (FastModel)SpanSerializer.ReadObject(ref reader, typeof(FastModel));
    }

    // ── Binary ──────────────────────────────────────────

    [Benchmark(Description = "Binary_Simple反序列化")]
    public SimpleModel? Binary_Deserialize_Simple()
    {
        using var ms = new MemoryStream(_binarySimpleBytes);
        return Binary.FastRead<SimpleModel>(ms);
    }

    [Benchmark(Description = "Binary_Nested反序列化")]
    public NestedModel? Binary_Deserialize_Nested()
    {
        using var ms = new MemoryStream(_binaryNestedBytes);
        return Binary.FastRead<NestedModel>(ms);
    }
}

// -------------------------------------------------------
// 批量操作基准测试
// -------------------------------------------------------

/// <summary>SpanSerializer vs Binary 批量序列化/反序列化性能对比</summary>
[MemoryDiagnoser]
[SimpleJob]
public class SerializerBatchBenchmark
{
    private SimpleModel[] _models = null!;
    private Byte[] _buffer = null!;
    private Byte[] _spanBatchBytes = null!;
    private Byte[] _binaryBatchBytes = null!;

    [Params(100, 1000)]
    public Int32 Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _models = new SimpleModel[Count];
        for (var i = 0; i < Count; i++)
        {
            _models[i] = new SimpleModel
            {
                Id = i,
                Name = $"Item_{i}",
                IsActive = i % 2 == 0,
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(i),
                Score = i * 0.5,
            };
        }

        // 预分配足够大的缓冲区（每个对象约 40 字节，留余量）
        _buffer = new Byte[Count * 64];

        // 预序列化批量数据用于反序列化测试
        using var msSrc = new MemoryStream(_buffer);
        var bn = new Binary(msSrc) { EncodeInt = true };
        foreach (var m in _models)
            bn.Write(m);
        var spanBuf = new Byte[Count * 64];
        var writer = new NewLife.Buffers.SpanWriter(spanBuf);
        foreach (var m in _models)
            SpanSerializer.WriteObject(ref writer, m, typeof(SimpleModel));

        _binaryBatchBytes = msSrc.ToArray();
        _spanBatchBytes = spanBuf[..writer.WrittenCount];
    }

    // ── SpanSerializer 批量 ──────────────────────────────

    [Benchmark(Description = "Span_批量序列化", Baseline = true)]
    public Int32 Span_Serialize_Batch()
    {
        var writer = new NewLife.Buffers.SpanWriter(_buffer);
        foreach (var m in _models)
            SpanSerializer.WriteObject(ref writer, m, typeof(SimpleModel));
        return writer.WrittenCount;
    }

    [Benchmark(Description = "Span_批量反序列化")]
    public Int32 Span_Deserialize_Batch()
    {
        var reader = new NewLife.Buffers.SpanReader(_spanBatchBytes);
        var count = 0;
        while (reader.Available >= 24) // 最小字段保护
        {
            SpanSerializer.ReadObject(ref reader, typeof(SimpleModel));
            count++;
            if (count >= Count) break;
        }
        return count;
    }

    // ── Binary 批量 ─────────────────────────────────────

    [Benchmark(Description = "Binary_批量序列化")]
    public Int64 Binary_Serialize_Batch()
    {
        using var ms = new MemoryStream(_buffer);
        var bn = new Binary(ms) { EncodeInt = true };
        foreach (var m in _models)
            bn.Write(m);
        return bn.Total;
    }

    [Benchmark(Description = "Binary_批量反序列化")]
    public Int32 Binary_Deserialize_Batch()
    {
        using var ms = new MemoryStream(_binaryBatchBytes);
        var bn = new Binary(ms) { EncodeInt = true };
        var count = 0;
        while (!bn.EndOfStream && count < Count)
        {
            bn.Read<SimpleModel>();
            count++;
        }
        return count;
    }
}

// -------------------------------------------------------
// 并发基准测试
// -------------------------------------------------------

/// <summary>SpanSerializer vs Binary 多线程并发吞吐对比</summary>
[MemoryDiagnoser]
[SimpleJob]
public class SerializerConcurrencyBenchmark
{
    private SimpleModel _model = null!;
    private Byte[] _spanBytes = null!;
    private Byte[] _binaryBytes = null!;

    public static IEnumerable<Int32> ThreadCounts
    {
        get
        {
            var cores = Environment.ProcessorCount;
            var set = new SortedSet<Int32> { 1, 4, 8, 32 };
            set.Add(cores);
            return set;
        }
    }

    [ParamsSource(nameof(ThreadCounts))]
    public Int32 ThreadCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _model = new SimpleModel
        {
            Id = 42,
            Name = "NewLife",
            IsActive = true,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Score = 99.5,
        };

        var buf = new Byte[256];
        var wc = new SpanWriter(buf);
        SpanSerializer.WriteObject(ref wc, _model, _model.GetType());
        _spanBytes = buf[..wc.WrittenCount];

        using var ms = new MemoryStream();
        Binary.FastWrite(_model, ms);
        _binaryBytes = ms.ToArray();
    }

    [Benchmark(Description = "Span_并发序列化", Baseline = true)]
    public void Span_Serialize_Concurrent()
    {
        Parallel.For(0, ThreadCount, _ =>
        {
            Span<Byte> buf = stackalloc Byte[256];
            var writer = new SpanWriter(buf);
            SpanSerializer.WriteObject(ref writer, _model, _model.GetType());
        });
    }

    [Benchmark(Description = "Span_并发反序列化")]
    public void Span_Deserialize_Concurrent()
    {
        Parallel.For(0, ThreadCount, _ =>
        {
            var reader = new SpanReader(_spanBytes);
            SpanSerializer.ReadObject(ref reader, typeof(SimpleModel));
        });
    }

    [Benchmark(Description = "Binary_并发序列化")]
    public void Binary_Serialize_Concurrent()
    {
        Parallel.For(0, ThreadCount, _ =>
        {
            using var ms = new MemoryStream(256);
            Binary.FastWrite(_model, ms);
        });
    }

    [Benchmark(Description = "Binary_并发反序列化")]
    public void Binary_Deserialize_Concurrent()
    {
        Parallel.For(0, ThreadCount, _ =>
        {
            using var ms = new MemoryStream(_binaryBytes);
            Binary.FastRead<SimpleModel>(ms);
        });
    }
}
