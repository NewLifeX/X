using System;
using System.Buffers;

namespace XUnitTest.Compat;

/// <summary>多段序列构造工具：用于跨段读取行为测试</summary>
internal static class SequenceFactory
{
    /// <summary>按给定字节数组顺序构造多段 ReadOnlySequence</summary>
    /// <param name="segments">各段数据</param>
    /// <returns>多段序列；无段时返回空序列</returns>
    public static ReadOnlySequence<Byte> Build(params Byte[][] segments)
    {
        Segment? first = null;
        Segment? last = null;
        Int64 index = 0;
        foreach (var data in segments)
        {
            var seg = new Segment(data, index);
            index += data.Length;
            if (first == null)
                first = seg;
            else
                last!.SetNext(seg);
            last = seg;
        }

        if (first == null) return ReadOnlySequence<Byte>.Empty;

        return new ReadOnlySequence<Byte>(first, 0, last!, last!.Memory.Length);
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
