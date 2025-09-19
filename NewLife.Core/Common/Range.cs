#if NETFRAMEWORK || NETSTANDARD2_0
using System.Runtime.CompilerServices;

namespace System;

/// <summary>
/// 表示一个范围，以起始 <see cref="Index"/> 与结束 <see cref="Index"/> 表示。
/// 等同于 .NET Core 的 System.Range。
/// </summary>
public readonly struct Range : IEquatable<Range>
{
    /// <summary>范围起始索引（包含）。</summary>
    public Index Start { get; }

    /// <summary>范围结束索引（不包含）。</summary>
    public Index End { get; }

    /// <summary>表示整个序列的范围 [Start..End]。</summary>
    public static Range All => new(Index.Start, Index.End);

    /// <summary>使用给定起止索引创建范围。</summary>
    /// <param name="start">起始索引（包含）。</param>
    /// <param name="end">结束索引（不包含）。</param>
    public Range(Index start, Index end)
    {
        Start = start;
        End = end;
    }

    /// <inheritdoc />
    public override Boolean Equals(Object value)
    {
        if (value is Range r)
        {
            if (r.Start.Equals(Start))
            {
                return r.End.Equals(End);
            }
        }
        return false;
    }

    /// <inheritdoc />
    public Boolean Equals(Range other) => other.Start.Equals(Start) && other.End.Equals(End);

    /// <inheritdoc />
    public override Int32 GetHashCode() => Start.GetHashCode() * 31 + End.GetHashCode();

    /// <inheritdoc />
    public override String ToString() => Start.ToString() + ".." + End;

    /// <summary>创建从指定起点到序列末尾的范围。</summary>
    public static Range StartAt(Index start) => new(start, Index.End);

    /// <summary>创建从序列开头到指定终点的范围。</summary>
    public static Range EndAt(Index end) => new(Index.Start, end);

    /// <summary>
    /// 结合指定序列总长度，计算该范围对应的偏移与长度。
    /// </summary>
    /// <param name="length">序列总长度。</param>
    /// <returns>二元组 (Offset, Length)。</returns>
    /// <exception cref="ArgumentOutOfRangeException">当范围越界或顺序不合法时抛出。</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //[CLSCompliant(false)]
    public (Int32 Offset, Int32 Length) GetOffsetAndLength(Int32 length)
    {
        var startIndex = Start;
        var start = ((!startIndex.IsFromEnd) ? startIndex.Value : (length - startIndex.Value));
        var endIndex = End;
        var end = ((!endIndex.IsFromEnd) ? endIndex.Value : (length - endIndex.Value));
        if ((UInt32)end > (UInt32)length || (UInt32)start > (UInt32)end)
        {
            throw new ArgumentOutOfRangeException("length");
        }
        return (start, end - start);
    }
}
#endif