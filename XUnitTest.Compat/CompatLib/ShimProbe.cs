using System;
using System.Buffers;
using NewLife.Buffers;

namespace CompatLib;

/// <summary>跨资产类型转发验证。本类库以 netstandard2.0 编译，引用的是 NewLife.Core 垫片中的 System.Buffers.SequenceReader&lt;T&gt;；
/// 在现代运行时（net5+ 应用加载 NewLife.Core 高版本资产）由 Stub/SequenceReaderForwards.cs 的 TypeForwardedTo 转发到 BCL</summary>
public static class ShimProbe
{
    /// <summary>跨段读取：大端 Int16 + 小端 Int32，返回组合值。用于验证垫片类型的跨资产类型标识统一</summary>
    /// <param name="first">第一段（Int16 高字节）</param>
    /// <param name="second">第二段（Int16 低字节 + Int32）</param>
    /// <returns>(code &lt;&lt; 32) ^ value；数据不足返回 -1</returns>
    public static Int64 ReadHeader(Byte[] first, Byte[] second)
    {
        var seq = Build(first, second);
        var reader = new SequenceReader<Byte>(seq);
        if (!reader.TryReadBigEndian(out Int16 code)) return -1;
        if (!reader.TryReadLittleEndian(out Int32 value)) return -1;

        return ((Int64)code << 32) ^ value;
    }

    /// <summary>用 NewLife.Buffers 无符号端序扩展读取 UInt16（该类型在所有资产中都存在，不依赖转发）</summary>
    /// <param name="data">双字节数据</param>
    /// <returns>大端解析值；数据不足返回 0</returns>
    public static UInt16 ReadUInt16(Byte[] data)
    {
        var reader = new SequenceReader<Byte>(new ReadOnlySequence<Byte>(data));
        return reader.TryReadBigEndian(out UInt16 value) ? value : (UInt16)0;
    }

    private static ReadOnlySequence<Byte> Build(Byte[] first, Byte[] second)
    {
        var segment1 = new Segment(first, 0);
        var segment2 = new Segment(second, first.Length);
        segment1.SetNext(segment2);
        return new ReadOnlySequence<Byte>(segment1, 0, segment2, segment2.Memory.Length);
    }

    private sealed class Segment : ReadOnlySequenceSegment<Byte>
    {
        public Segment(ReadOnlyMemory<Byte> memory, Int64 runningIndex)
        {
            Memory = memory;
            RunningIndex = runningIndex;
        }

        public void SetNext(Segment next) => Next = next;
    }
}
