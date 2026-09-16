#if !NETCOREAPP3_0_OR_GREATER && !NETSTANDARD2_1_OR_GREATER
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// 注：本文件移植自 dotnet/corefx release/3.1（System.Memory/src/System/Buffers/ 下的 SequenceReader.cs、SequenceReader.Search.cs 与 SequenceReaderExtensions.Binary.cs，合并为单文件）。
// 为 .NET Framework 4.5 / 4.6.1 / 4.6.2 与 .NET Standard 2.0 提供与 BCL 3.1 面一致的 System.Buffers.SequenceReader<T> 与有符号端序扩展；
// 高版本框架由 Stub/SequenceReaderForwards.cs 通过 TypeForwardedTo 转发到 BCL。
// 相比原版主要差异：
// - 移除对 System.Memory 内部实现（ReadOnlySequence.GetFirstSpan、ThrowHelper 等）的依赖，改为等价的公共 API 调用
// - ThrowHelper 调用改为直接 throw new 语句
// - 原三文件合并为单文件，采用文件级命名空间

using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Buffers;

/// <summary>基于 <see cref="ReadOnlySequence{T}"/> 的高性能顺序读取器。移植自 BCL 3.1，为 net45 / net461 / net462 / netstandard2.0 提供与高版本框架一致的类型</summary>
/// <typeparam name="T">序列元素类型</typeparam>
public ref partial struct SequenceReader<T> where T : unmanaged, IEquatable<T>
{
    private SequencePosition _currentPosition;
    private SequencePosition _nextPosition;
    private bool _moreData;
    private long _length;

    /// <summary>
    /// Create a <see cref="SequenceReader{T}"/> over the given <see cref="ReadOnlySequence{T}"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SequenceReader(ReadOnlySequence<T> sequence)
    {
        CurrentSpanIndex = 0;
        Consumed = 0;
        Sequence = sequence;
        _currentPosition = sequence.Start;
        _length = -1;

        // 等价替换 System.Memory 内部 GetFirstSpan：TryGet 返回首段内存并推进到下一段起始位置（末段/单段时推出默认位置）
        _nextPosition = _currentPosition;
        var hasFirst = sequence.TryGet(ref _nextPosition, out ReadOnlyMemory<T> firstMemory, advance: true);
        CurrentSpan = hasFirst ? firstMemory.Span : default;
        _moreData = CurrentSpan.Length > 0;

        if (!_moreData && !sequence.IsSingleSegment)
        {
            _moreData = true;
            GetNextSpan();
        }
    }

    /// <summary>
    /// True when there is no more data in the <see cref="Sequence"/>.
    /// </summary>
    public readonly bool End => !_moreData;

    /// <summary>
    /// The underlying <see cref="ReadOnlySequence{T}"/> for the reader.
    /// </summary>
    public readonly ReadOnlySequence<T> Sequence { get; }

    /// <summary>
    /// The current position in the <see cref="Sequence"/>.
    /// </summary>
    public readonly SequencePosition Position
        => Sequence.GetPosition(CurrentSpanIndex, _currentPosition);

    /// <summary>
    /// The current segment in the <see cref="Sequence"/> as a span.
    /// </summary>
    public ReadOnlySpan<T> CurrentSpan { readonly get; private set; }

    /// <summary>
    /// The index in the <see cref="CurrentSpan"/>.
    /// </summary>
    public int CurrentSpanIndex { readonly get; private set; }

    /// <summary>
    /// The unread portion of the <see cref="CurrentSpan"/>.
    /// </summary>
    public readonly ReadOnlySpan<T> UnreadSpan
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => CurrentSpan.Slice(CurrentSpanIndex);
    }

    /// <summary>
    /// The total number of <typeparamref name="T"/>'s processed by the reader.
    /// </summary>
    public long Consumed { readonly get; private set; }

    /// <summary>
    /// Remaining <typeparamref name="T"/>'s in the reader's <see cref="Sequence"/>.
    /// </summary>
    public readonly long Remaining => Length - Consumed;

    /// <summary>
    /// Count of <typeparamref name="T"/> in the reader's <see cref="Sequence"/>.
    /// </summary>
    public readonly long Length
    {
        get
        {
            if (_length < 0)
            {
                // Cast-away readonly to initialize lazy field
                Volatile.Write(ref Unsafe.AsRef(_length), Sequence.Length);
            }
            return _length;
        }
    }

    /// <summary>
    /// Peeks at the next value without advancing the reader.
    /// </summary>
    /// <param name="value">The next value or default if at the end.</param>
    /// <returns>False if at the end of the reader.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool TryPeek(out T value)
    {
        if (_moreData)
        {
            value = CurrentSpan[CurrentSpanIndex];
            return true;
        }
        else
        {
            value = default;
            return false;
        }
    }

    /// <summary>
    /// Read the next value and advance the reader.
    /// </summary>
    /// <param name="value">The next value or default if at the end.</param>
    /// <returns>False if at the end of the reader.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryRead(out T value)
    {
        if (End)
        {
            value = default;
            return false;
        }

        value = CurrentSpan[CurrentSpanIndex];
        CurrentSpanIndex++;
        Consumed++;

        if (CurrentSpanIndex >= CurrentSpan.Length)
        {
            GetNextSpan();
        }

        return true;
    }

    /// <summary>
    /// Move the reader back the specified number of items.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if trying to rewind a negative amount or more than <see cref="Consumed"/>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Rewind(long count)
    {
        if ((ulong)count > (ulong)Consumed)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        Consumed -= count;

        if (CurrentSpanIndex >= count)
        {
            CurrentSpanIndex -= (int)count;
            _moreData = true;
        }
        else
        {
            // Current segment doesn't have enough data, scan backward through segments
            RetreatToPreviousSpan(Consumed);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void RetreatToPreviousSpan(long consumed)
    {
        ResetReader();
        Advance(consumed);
    }

    private void ResetReader()
    {
        CurrentSpanIndex = 0;
        Consumed = 0;
        _currentPosition = Sequence.Start;
        _nextPosition = _currentPosition;

        if (Sequence.TryGet(ref _nextPosition, out ReadOnlyMemory<T> memory, advance: true))
        {
            _moreData = true;

            if (memory.Length == 0)
            {
                CurrentSpan = default;
                // No data in the first span, move to one with data
                GetNextSpan();
            }
            else
            {
                CurrentSpan = memory.Span;
            }
        }
        else
        {
            // No data in any spans and at end of sequence
            _moreData = false;
            CurrentSpan = default;
        }
    }

    /// <summary>
    /// Get the next segment with available data, if any.
    /// </summary>
    private void GetNextSpan()
    {
        if (!Sequence.IsSingleSegment)
        {
            SequencePosition previousNextPosition = _nextPosition;
            while (Sequence.TryGet(ref _nextPosition, out ReadOnlyMemory<T> memory, advance: true))
            {
                _currentPosition = previousNextPosition;
                if (memory.Length > 0)
                {
                    CurrentSpan = memory.Span;
                    CurrentSpanIndex = 0;
                    return;
                }
                else
                {
                    CurrentSpan = default;
                    CurrentSpanIndex = 0;
                    previousNextPosition = _nextPosition;
                }
            }
        }
        _moreData = false;
    }

    /// <summary>
    /// Move the reader ahead the specified number of items.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(long count)
    {
        const long TooBigOrNegative = unchecked((long)0xFFFFFFFF80000000);
        if ((count & TooBigOrNegative) == 0 && CurrentSpan.Length - CurrentSpanIndex > (int)count)
        {
            CurrentSpanIndex += (int)count;
            Consumed += count;
        }
        else
        {
            // Can't satisfy from the current span
            AdvanceToNextSpan(count);
        }
    }

    /// <summary>
    /// Unchecked helper to avoid unnecessary checks where you know count is valid.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AdvanceCurrentSpan(long count)
    {
        Debug.Assert(count >= 0);

        Consumed += count;
        CurrentSpanIndex += (int)count;
        if (CurrentSpanIndex >= CurrentSpan.Length)
            GetNextSpan();
    }

    /// <summary>
    /// Only call this helper if you know that you are advancing in the current span
    /// with valid count and there is no need to fetch the next one.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AdvanceWithinSpan(long count)
    {
        Debug.Assert(count >= 0);

        Consumed += count;
        CurrentSpanIndex += (int)count;

        Debug.Assert(CurrentSpanIndex < CurrentSpan.Length);
    }

    private void AdvanceToNextSpan(long count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        Consumed += count;
        while (_moreData)
        {
            int remaining = CurrentSpan.Length - CurrentSpanIndex;

            if (remaining > count)
            {
                CurrentSpanIndex += (int)count;
                count = 0;
                break;
            }

            // As there may not be any further segments we need to
            // push the current index to the end of the span.
            CurrentSpanIndex += remaining;
            count -= remaining;
            Debug.Assert(count >= 0);

            GetNextSpan();

            if (count == 0)
            {
                break;
            }
        }

        if (count != 0)
        {
            // Not enough data left- adjust for where we actually ended and throw
            Consumed -= count;
            throw new ArgumentOutOfRangeException(nameof(count));
        }
    }

    /// <summary>
    /// Copies data from the current <see cref="Position"/> to the given <paramref name="destination"/> span if there
    /// is enough data to fill it.
    /// </summary>
    /// <remarks>
    /// This API is used to copy a fixed amount of data out of the sequence if possible. It does not advance
    /// the reader. To look ahead for a specific stream of data <see cref="IsNext(ReadOnlySpan{T}, bool)"/> can be used.
    /// </remarks>
    /// <param name="destination">Destination span to copy to.</param>
    /// <returns>True if there is enough data to completely fill the <paramref name="destination"/> span.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool TryCopyTo(Span<T> destination)
    {
        // This API doesn't advance to facilitate conditional advancement based on the data returned.
        // We don't provide an advance option to allow easier utilizing of stack allocated destination spans.
        // (Because we can make this method readonly we can guarantee that we won't capture the span.)

        ReadOnlySpan<T> firstSpan = UnreadSpan;
        if (firstSpan.Length >= destination.Length)
        {
            firstSpan.Slice(0, destination.Length).CopyTo(destination);
            return true;
        }

        // Not enough in the current span to satisfy the request, fall through to the slow path
        return TryCopyMultisegment(destination);
    }

    internal readonly bool TryCopyMultisegment(Span<T> destination)
    {
        // If we don't have enough to fill the requested buffer, return false
        if (Remaining < destination.Length)
            return false;

        ReadOnlySpan<T> firstSpan = UnreadSpan;
        Debug.Assert(firstSpan.Length < destination.Length);
        firstSpan.CopyTo(destination);
        int copied = firstSpan.Length;

        SequencePosition next = _nextPosition;
        while (Sequence.TryGet(ref next, out ReadOnlyMemory<T> nextSegment, true))
        {
            if (nextSegment.Length > 0)
            {
                ReadOnlySpan<T> nextSpan = nextSegment.Span;
                int toCopy = Math.Min(nextSpan.Length, destination.Length - copied);
                nextSpan.Slice(0, toCopy).CopyTo(destination.Slice(copied));
                copied += toCopy;
                if (copied >= destination.Length)
                {
                    break;
                }
            }
        }

        return true;
    }
}

public ref partial struct SequenceReader<T> where T : unmanaged, IEquatable<T>
{
    /// <summary>
    /// Try to read everything up to the given <paramref name="delimiter"/>.
    /// </summary>
    /// <param name="span">The read data, if any.</param>
    /// <param name="delimiter">The delimiter to look for.</param>
    /// <param name="advancePastDelimiter">True to move past the <paramref name="delimiter"/> if found.</param>
    /// <returns>True if the <paramref name="delimiter"/> was found.</returns>
    public bool TryReadTo(out ReadOnlySpan<T> span, T delimiter, bool advancePastDelimiter = true)
    {
        ReadOnlySpan<T> remaining = UnreadSpan;
        int index = remaining.IndexOf(delimiter);

        if (index != -1)
        {
            span = index == 0 ? default : remaining.Slice(0, index);
            AdvanceCurrentSpan(index + (advancePastDelimiter ? 1 : 0));
            return true;
        }

        return TryReadToSlow(out span, delimiter, advancePastDelimiter);
    }

    private bool TryReadToSlow(out ReadOnlySpan<T> span, T delimiter, bool advancePastDelimiter)
    {
        if (!TryReadToInternal(out ReadOnlySequence<T> sequence, delimiter, advancePastDelimiter, CurrentSpan.Length - CurrentSpanIndex))
        {
            span = default;
            return false;
        }

        span = sequence.IsSingleSegment ? sequence.First.Span : sequence.ToArray();
        return true;
    }

    /// <summary>
    /// Try to read everything up to the given <paramref name="delimiter"/>, ignoring delimiters that are
    /// preceded by <paramref name="delimiterEscape"/>.
    /// </summary>
    /// <param name="span">The read data, if any.</param>
    /// <param name="delimiter">The delimiter to look for.</param>
    /// <param name="delimiterEscape">If found prior to <paramref name="delimiter"/> it will skip that occurrence.</param>
    /// <param name="advancePastDelimiter">True to move past the <paramref name="delimiter"/> if found.</param>
    /// <returns>True if the <paramref name="delimiter"/> was found.</returns>
    public bool TryReadTo(out ReadOnlySpan<T> span, T delimiter, T delimiterEscape, bool advancePastDelimiter = true)
    {
        ReadOnlySpan<T> remaining = UnreadSpan;
        int index = remaining.IndexOf(delimiter);

        if ((index > 0 && !remaining[index - 1].Equals(delimiterEscape)) || index == 0)
        {
            span = remaining.Slice(0, index);
            AdvanceCurrentSpan(index + (advancePastDelimiter ? 1 : 0));
            return true;
        }

        // This delimiter might be skipped, go down the slow path
        return TryReadToSlow(out span, delimiter, delimiterEscape, index, advancePastDelimiter);
    }

    private bool TryReadToSlow(out ReadOnlySpan<T> span, T delimiter, T delimiterEscape, int index, bool advancePastDelimiter)
    {
        if (!TryReadToSlow(out ReadOnlySequence<T> sequence, delimiter, delimiterEscape, index, advancePastDelimiter))
        {
            span = default;
            return false;
        }

        Debug.Assert(sequence.Length > 0);
        span = sequence.IsSingleSegment ? sequence.First.Span : sequence.ToArray();
        return true;
    }

    private bool TryReadToSlow(out ReadOnlySequence<T> sequence, T delimiter, T delimiterEscape, int index, bool advancePastDelimiter)
    {
        SequenceReader<T> copy = this;

        ReadOnlySpan<T> remaining = UnreadSpan;
        bool priorEscape = false;

        do
        {
            if (index >= 0)
            {
                if (index == 0 && priorEscape)
                {
                    // We were in the escaped state, so skip this delimiter
                    priorEscape = false;
                    Advance(index + 1);
                    remaining = UnreadSpan;
                    goto Continue;
                }
                else if (index > 0 && remaining[index - 1].Equals(delimiterEscape))
                {
                    // This delimiter might be skipped

                    // Count our escapes
                    int escapeCount = 1;
                    int i = index - 2;
                    for (; i >= 0; i--)
                    {
                        if (!remaining[i].Equals(delimiterEscape))
                            break;
                    }
                    if (i < 0 && priorEscape)
                    {
                        // Started and ended with escape, increment once more
                        escapeCount++;
                    }
                    escapeCount += index - 2 - i;

                    if ((escapeCount & 1) != 0)
                    {
                        // An odd escape count means we're currently escaped,
                        // skip the delimiter and reset escaped state.
                        Advance(index + 1);
                        priorEscape = false;
                        remaining = UnreadSpan;
                        goto Continue;
                    }
                }

                // Found the delimiter. Move to it, slice, then move past it.
                AdvanceCurrentSpan(index);

                sequence = Sequence.Slice(copy.Position, Position);
                if (advancePastDelimiter)
                {
                    Advance(1);
                }
                return true;
            }
            else
            {
                // No delimiter, need to check the end of the span for odd number of escapes then advance
                if (remaining.Length > 0 && remaining[remaining.Length - 1].Equals(delimiterEscape))
                {
                    int escapeCount = 1;
                    int i = remaining.Length - 2;
                    for (; i >= 0; i--)
                    {
                        if (!remaining[i].Equals(delimiterEscape))
                            break;
                    }

                    escapeCount += remaining.Length - 2 - i;
                    if (i < 0 && priorEscape)
                        priorEscape = (escapeCount & 1) == 0;   // equivalent to incrementing escapeCount before setting priorEscape
                    else
                        priorEscape = (escapeCount & 1) != 0;
                }
                else
                {
                    priorEscape = false;
                }
            }

            // Nothing in the current span, move to the end, checking for the skip delimiter
            AdvanceCurrentSpan(remaining.Length);
            remaining = CurrentSpan;

        Continue:
            index = remaining.IndexOf(delimiter);
        } while (!End);

        // Didn't find anything, reset our original state.
        this = copy;
        sequence = default;
        return false;
    }

    /// <summary>
    /// Try to read everything up to the given <paramref name="delimiter"/>.
    /// </summary>
    /// <param name="sequence">The read data, if any.</param>
    /// <param name="delimiter">The delimiter to look for.</param>
    /// <param name="advancePastDelimiter">True to move past the <paramref name="delimiter"/> if found.</param>
    /// <returns>True if the <paramref name="delimiter"/> was found.</returns>
    public bool TryReadTo(out ReadOnlySequence<T> sequence, T delimiter, bool advancePastDelimiter = true)
    {
        return TryReadToInternal(out sequence, delimiter, advancePastDelimiter);
    }

    private bool TryReadToInternal(out ReadOnlySequence<T> sequence, T delimiter, bool advancePastDelimiter, int skip = 0)
    {
        Debug.Assert(skip >= 0);
        SequenceReader<T> copy = this;
        if (skip > 0)
            Advance(skip);
        ReadOnlySpan<T> remaining = UnreadSpan;

        while (_moreData)
        {
            int index = remaining.IndexOf(delimiter);
            if (index != -1)
            {
                // Found the delimiter. Move to it, slice, then move past it.
                if (index > 0)
                {
                    AdvanceCurrentSpan(index);
                }

                sequence = Sequence.Slice(copy.Position, Position);
                if (advancePastDelimiter)
                {
                    Advance(1);
                }
                return true;
            }

            AdvanceCurrentSpan(remaining.Length);
            remaining = CurrentSpan;
        }

        // Didn't find anything, reset our original state.
        this = copy;
        sequence = default;
        return false;
    }

    /// <summary>
    /// Try to read everything up to the given <paramref name="delimiter"/>, ignoring delimiters that are
    /// preceded by <paramref name="delimiterEscape"/>.
    /// </summary>
    /// <param name="sequence">The read data, if any.</param>
    /// <param name="delimiter">The delimiter to look for.</param>
    /// <param name="delimiterEscape">If found prior to <paramref name="delimiter"/> it will skip that occurrence.</param>
    /// <param name="advancePastDelimiter">True to move past the <paramref name="delimiter"/> if found.</param>
    /// <returns>True if the <paramref name="delimiter"/> was found.</returns>
    public bool TryReadTo(out ReadOnlySequence<T> sequence, T delimiter, T delimiterEscape, bool advancePastDelimiter = true)
    {
        SequenceReader<T> copy = this;

        ReadOnlySpan<T> remaining = UnreadSpan;
        bool priorEscape = false;

        while (_moreData)
        {
            int index = remaining.IndexOf(delimiter);
            if (index != -1)
            {
                if (index == 0 && priorEscape)
                {
                    // We were in the escaped state, so skip this delimiter
                    priorEscape = false;
                    Advance(index + 1);
                    remaining = UnreadSpan;
                    continue;
                }
                else if (index > 0 && remaining[index - 1].Equals(delimiterEscape))
                {
                    // This delimiter might be skipped

                    // Count our escapes
                    int escapeCount = 0;
                    for (int i = index; i > 0 && remaining[i - 1].Equals(delimiterEscape); i--, escapeCount++)
                        ;
                    if (escapeCount == index && priorEscape)
                    {
                        // Started and ended with escape, increment once more
                        escapeCount++;
                    }

                    priorEscape = false;
                    if ((escapeCount & 1) != 0)
                    {
                        // Odd escape count means we're in the escaped state, so skip this delimiter
                        Advance(index + 1);
                        remaining = UnreadSpan;
                        continue;
                    }
                }

                // Found the delimiter. Move to it, slice, then move past it.
                if (index > 0)
                {
                    Advance(index);
                }

                sequence = Sequence.Slice(copy.Position, Position);
                if (advancePastDelimiter)
                {
                    Advance(1);
                }
                return true;
            }

            // No delimiter, need to check the end of the span for odd number of escapes then advance
            {
                int escapeCount = 0;
                for (int i = remaining.Length; i > 0 && remaining[i - 1].Equals(delimiterEscape); i--, escapeCount++)
                    ;
                if (priorEscape && escapeCount == remaining.Length)
                {
                    escapeCount++;
                }
                priorEscape = escapeCount % 2 != 0;
            }

            // Nothing in the current span, move to the end, checking for the skip delimiter
            Advance(remaining.Length);
            remaining = CurrentSpan;
        }

        // Didn't find anything, reset our original state.
        this = copy;
        sequence = default;
        return false;
    }

    /// <summary>
    /// Try to read everything up to the given <paramref name="delimiters"/>.
    /// </summary>
    /// <param name="span">The read data, if any.</param>
    /// <param name="delimiters">The delimiters to look for.</param>
    /// <param name="advancePastDelimiter">True to move past the first found instance of any of the given <paramref name="delimiters"/>.</param>
    /// <returns>True if any of the <paramref name="delimiters"/> were found.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadToAny(out ReadOnlySpan<T> span, ReadOnlySpan<T> delimiters, bool advancePastDelimiter = true)
    {
        ReadOnlySpan<T> remaining = UnreadSpan;
        int index = delimiters.Length == 2
            ? remaining.IndexOfAny(delimiters[0], delimiters[1])
            : remaining.IndexOfAny(delimiters);

        if (index != -1)
        {
            span = remaining.Slice(0, index);
            Advance(index + (advancePastDelimiter ? 1 : 0));
            return true;
        }

        return TryReadToAnySlow(out span, delimiters, advancePastDelimiter);
    }

    private bool TryReadToAnySlow(out ReadOnlySpan<T> span, ReadOnlySpan<T> delimiters, bool advancePastDelimiter)
    {
        if (!TryReadToAnyInternal(out ReadOnlySequence<T> sequence, delimiters, advancePastDelimiter, CurrentSpan.Length - CurrentSpanIndex))
        {
            span = default;
            return false;
        }

        span = sequence.IsSingleSegment ? sequence.First.Span : sequence.ToArray();
        return true;
    }

    /// <summary>
    /// Try to read everything up to the given <paramref name="delimiters"/>.
    /// </summary>
    /// <param name="sequence">The read data, if any.</param>
    /// <param name="delimiters">The delimiters to look for.</param>
    /// <param name="advancePastDelimiter">True to move past the first found instance of any of the given <paramref name="delimiters"/>.</param>
    /// <returns>True if any of the <paramref name="delimiters"/> were found.</returns>
    public bool TryReadToAny(out ReadOnlySequence<T> sequence, ReadOnlySpan<T> delimiters, bool advancePastDelimiter = true)
    {
        return TryReadToAnyInternal(out sequence, delimiters, advancePastDelimiter);
    }

    private bool TryReadToAnyInternal(out ReadOnlySequence<T> sequence, ReadOnlySpan<T> delimiters, bool advancePastDelimiter, int skip = 0)
    {
        SequenceReader<T> copy = this;
        if (skip > 0)
            Advance(skip);
        ReadOnlySpan<T> remaining = UnreadSpan;

        while (!End)
        {
            int index = delimiters.Length == 2
                ? remaining.IndexOfAny(delimiters[0], delimiters[1])
                : remaining.IndexOfAny(delimiters);

            if (index != -1)
            {
                // Found one of the delimiters. Move to it, slice, then move past it.
                if (index > 0)
                {
                    AdvanceCurrentSpan(index);
                }

                sequence = Sequence.Slice(copy.Position, Position);
                if (advancePastDelimiter)
                {
                    Advance(1);
                }
                return true;
            }

            Advance(remaining.Length);
            remaining = CurrentSpan;
        }

        // Didn't find anything, reset our original state.
        this = copy;
        sequence = default;
        return false;
    }

    /// <summary>
    /// Try to read data until the entire given <paramref name="delimiter"/> matches.
    /// </summary>
    /// <param name="sequence">The read data, if any.</param>
    /// <param name="delimiter">The multi (T) delimiter.</param>
    /// <param name="advancePastDelimiter">True to move past the <paramref name="delimiter"/> if found.</param>
    /// <returns>True if the <paramref name="delimiter"/> was found.</returns>
    public bool TryReadTo(out ReadOnlySequence<T> sequence, ReadOnlySpan<T> delimiter, bool advancePastDelimiter = true)
    {
        if (delimiter.Length == 0)
        {
            sequence = default;
            return true;
        }

        SequenceReader<T> copy = this;

        bool advanced = false;
        while (!End)
        {
            if (!TryReadTo(out sequence, delimiter[0], advancePastDelimiter: false))
            {
                this = copy;
                return false;
            }

            if (delimiter.Length == 1)
            {
                if (advancePastDelimiter)
                {
                    Advance(1);
                }
                return true;
            }

            if (IsNext(delimiter))
            {
                // Probably a faster way to do this, potentially by avoiding the Advance in the previous TryReadTo call
                if (advanced)
                {
                    sequence = copy.Sequence.Slice(copy.Consumed, Consumed - copy.Consumed);
                }

                if (advancePastDelimiter)
                {
                    Advance(delimiter.Length);
                }
                return true;
            }
            else
            {
                Advance(1);
                advanced = true;
            }
        }

        this = copy;
        sequence = default;
        return false;
    }

    /// <summary>
    /// Advance until the given <paramref name="delimiter"/>, if found.
    /// </summary>
    /// <param name="delimiter">The delimiter to search for.</param>
    /// <param name="advancePastDelimiter">True to move past the <paramref name="delimiter"/> if found.</param>
    /// <returns>True if the given <paramref name="delimiter"/> was found.</returns>
    public bool TryAdvanceTo(T delimiter, bool advancePastDelimiter = true)
    {
        ReadOnlySpan<T> remaining = UnreadSpan;
        int index = remaining.IndexOf(delimiter);
        if (index != -1)
        {
            Advance(advancePastDelimiter ? index + 1 : index);
            return true;
        }

        return TryReadToInternal(out _, delimiter, advancePastDelimiter);
    }

    /// <summary>
    /// Advance until any of the given <paramref name="delimiters"/>, if found.
    /// </summary>
    /// <param name="delimiters">The delimiters to search for.</param>
    /// <param name="advancePastDelimiter">True to move past the first found instance of any of the given <paramref name="delimiters"/>.</param>
    /// <returns>True if any of the given <paramref name="delimiters"/> were found.</returns>
    public bool TryAdvanceToAny(ReadOnlySpan<T> delimiters, bool advancePastDelimiter = true)
    {
        ReadOnlySpan<T> remaining = UnreadSpan;
        int index = remaining.IndexOfAny(delimiters);
        if (index != -1)
        {
            AdvanceCurrentSpan(index + (advancePastDelimiter ? 1 : 0));
            return true;
        }

        return TryReadToAnyInternal(out _, delimiters, advancePastDelimiter);
    }

    /// <summary>
    /// Advance past consecutive instances of the given <paramref name="value"/>.
    /// </summary>
    /// <returns>How many positions the reader has been advanced.</returns>
    public long AdvancePast(T value)
    {
        long start = Consumed;

        do
        {
            // Advance past all matches in the current span
            int i;
            for (i = CurrentSpanIndex; i < CurrentSpan.Length && CurrentSpan[i].Equals(value); i++)
            {
            }

            int advanced = i - CurrentSpanIndex;
            if (advanced == 0)
            {
                // Didn't advance at all in this span, exit.
                break;
            }

            AdvanceCurrentSpan(advanced);

            // If we're at postion 0 after advancing and not at the End,
            // we're in a new span and should continue the loop.
        } while (CurrentSpanIndex == 0 && !End);

        return Consumed - start;
    }

    /// <summary>
    /// Skip consecutive instances of any of the given <paramref name="values"/>.
    /// </summary>
    /// <returns>How many positions the reader has been advanced.</returns>
    public long AdvancePastAny(ReadOnlySpan<T> values)
    {
        long start = Consumed;

        do
        {
            // Advance past all matches in the current span
            int i;
            for (i = CurrentSpanIndex; i < CurrentSpan.Length && values.IndexOf(CurrentSpan[i]) != -1; i++)
            {
            }

            int advanced = i - CurrentSpanIndex;
            if (advanced == 0)
            {
                // Didn't advance at all in this span, exit.
                break;
            }

            AdvanceCurrentSpan(advanced);

            // If we're at postion 0 after advancing and not at the End,
            // we're in a new span and should continue the loop.
        } while (CurrentSpanIndex == 0 && !End);

        return Consumed - start;
    }

    /// <summary>
    /// Advance past consecutive instances of any of the given values.
    /// </summary>
    /// <returns>How many positions the reader has been advanced.</returns>
    public long AdvancePastAny(T value0, T value1, T value2, T value3)
    {
        long start = Consumed;

        do
        {
            // Advance past all matches in the current span
            int i;
            for (i = CurrentSpanIndex; i < CurrentSpan.Length; i++)
            {
                T value = CurrentSpan[i];
                if (!value.Equals(value0) && !value.Equals(value1) && !value.Equals(value2) && !value.Equals(value3))
                {
                    break;
                }
            }

            int advanced = i - CurrentSpanIndex;
            if (advanced == 0)
            {
                // Didn't advance at all in this span, exit.
                break;
            }

            AdvanceCurrentSpan(advanced);

            // If we're at postion 0 after advancing and not at the End,
            // we're in a new span and should continue the loop.
        } while (CurrentSpanIndex == 0 && !End);

        return Consumed - start;
    }

    /// <summary>
    /// Advance past consecutive instances of any of the given values.
    /// </summary>
    /// <returns>How many positions the reader has been advanced.</returns>
    public long AdvancePastAny(T value0, T value1, T value2)
    {
        long start = Consumed;

        do
        {
            // Advance past all matches in the current span
            int i;
            for (i = CurrentSpanIndex; i < CurrentSpan.Length; i++)
            {
                T value = CurrentSpan[i];
                if (!value.Equals(value0) && !value.Equals(value1) && !value.Equals(value2))
                {
                    break;
                }
            }

            int advanced = i - CurrentSpanIndex;
            if (advanced == 0)
            {
                // Didn't advance at all in this span, exit.
                break;
            }

            AdvanceCurrentSpan(advanced);

            // If we're at postion 0 after advancing and not at the End,
            // we're in a new span and should continue the loop.
        } while (CurrentSpanIndex == 0 && !End);

        return Consumed - start;
    }

    /// <summary>
    /// Advance past consecutive instances of any of the given values.
    /// </summary>
    /// <returns>How many positions the reader has been advanced.</returns>
    public long AdvancePastAny(T value0, T value1)
    {
        long start = Consumed;

        do
        {
            // Advance past all matches in the current span
            int i;
            for (i = CurrentSpanIndex; i < CurrentSpan.Length; i++)
            {
                T value = CurrentSpan[i];
                if (!value.Equals(value0) && !value.Equals(value1))
                {
                    break;
                }
            }

            int advanced = i - CurrentSpanIndex;
            if (advanced == 0)
            {
                // Didn't advance at all in this span, exit.
                break;
            }

            AdvanceCurrentSpan(advanced);

            // If we're at postion 0 after advancing and not at the End,
            // we're in a new span and should continue the loop.
        } while (CurrentSpanIndex == 0 && !End);

        return Consumed - start;
    }

    /// <summary>
    /// Check to see if the given <paramref name="next"/> value is next.
    /// </summary>
    /// <param name="next">The value to compare the next items to.</param>
    /// <param name="advancePast">Move past the <paramref name="next"/> value if found.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsNext(T next, bool advancePast = false)
    {
        if (End)
            return false;

        if (CurrentSpan[CurrentSpanIndex].Equals(next))
        {
            if (advancePast)
            {
                AdvanceCurrentSpan(1);
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// Check to see if the given <paramref name="next"/> values are next.
    /// </summary>
    /// <param name="next">The span to compare the next items to.</param>
    /// <param name="advancePast">Move past the <paramref name="next"/> values if found.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsNext(ReadOnlySpan<T> next, bool advancePast = false)
    {
        ReadOnlySpan<T> unread = UnreadSpan;
        if (unread.StartsWith(next))
        {
            if (advancePast)
            {
                AdvanceCurrentSpan(next.Length);
            }
            return true;
        }

        // Only check the slow path if there wasn't enough to satisfy next
        return unread.Length < next.Length && IsNextSlow(next, advancePast);
    }

    private unsafe bool IsNextSlow(ReadOnlySpan<T> next, bool advancePast)
    {
        ReadOnlySpan<T> currentSpan = UnreadSpan;

        // We should only come in here if we need more data than we have in our current span
        Debug.Assert(currentSpan.Length < next.Length);

        int fullLength = next.Length;
        SequencePosition nextPosition = _nextPosition;

        while (next.StartsWith(currentSpan))
        {
            if (next.Length == currentSpan.Length)
            {
                // Fully matched
                if (advancePast)
                {
                    Advance(fullLength);
                }
                return true;
            }

            // Need to check the next segment
            while (true)
            {
                if (!Sequence.TryGet(ref nextPosition, out ReadOnlyMemory<T> nextSegment, advance: true))
                {
                    // Nothing left
                    return false;
                }

                if (nextSegment.Length > 0)
                {
                    next = next.Slice(currentSpan.Length);
                    currentSpan = nextSegment.Span;
                    if (currentSpan.Length > next.Length)
                    {
                        currentSpan = currentSpan.Slice(0, next.Length);
                    }
                    break;
                }
            }
        }

        return false;
    }
}

/// <summary>有符号端序读取扩展（Int16 / Int32 / Int64），移植自 BCL 3.1。无符号端序见 <see cref="NewLife.Buffers.SequenceReaderHelper"/></summary>
public static partial class SequenceReaderExtensions
{
    /// <summary>
    /// Try to read the given type out of the buffer if possible. Warning: this is dangerous to use with arbitrary
    /// structs- see remarks for full details.
    /// </summary>
    /// <remarks>
    /// IMPORTANT: The read is a straight copy of bits. If a struct depends on specific state of it's members to
    /// behave correctly this can lead to exceptions, etc. If reading endian specific integers, use the explicit
    /// overloads such as <see cref="TryReadLittleEndian(ref SequenceReader{byte}, out short)"/>
    /// </remarks>
    /// <returns>
    /// True if successful. <paramref name="value"/> will be default if failed (due to lack of space).
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static unsafe bool TryRead<T>(ref this SequenceReader<byte> reader, out T value) where T : unmanaged
    {
        ReadOnlySpan<byte> span = reader.UnreadSpan;
        if (span.Length < sizeof(T))
            return TryReadMultisegment(ref reader, out value);

        value = Unsafe.ReadUnaligned<T>(ref MemoryMarshal.GetReference(span));
        reader.Advance(sizeof(T));
        return true;
    }

    private static unsafe bool TryReadMultisegment<T>(ref SequenceReader<byte> reader, out T value) where T : unmanaged
    {
        Debug.Assert(reader.UnreadSpan.Length < sizeof(T));

        // Not enough data in the current segment, try to peek for the data we need.
        T buffer = default;
        Span<byte> tempSpan = new Span<byte>(&buffer, sizeof(T));

        if (!reader.TryCopyTo(tempSpan))
        {
            value = default;
            return false;
        }

        value = Unsafe.ReadUnaligned<T>(ref MemoryMarshal.GetReference(tempSpan));
        reader.Advance(sizeof(T));
        return true;
    }

    /// <summary>
    /// Reads an <see cref="Int16"/> as little endian.
    /// </summary>
    /// <returns>False if there wasn't enough data for an <see cref="Int16"/>.</returns>
    public static bool TryReadLittleEndian(ref this SequenceReader<byte> reader, out short value)
    {
        if (BitConverter.IsLittleEndian)
        {
            return reader.TryRead(out value);
        }

        return TryReadReverseEndianness(ref reader, out value);
    }

    /// <summary>
    /// Reads an <see cref="Int16"/> as big endian.
    /// </summary>
    /// <returns>False if there wasn't enough data for an <see cref="Int16"/>.</returns>
    public static bool TryReadBigEndian(ref this SequenceReader<byte> reader, out short value)
    {
        if (!BitConverter.IsLittleEndian)
        {
            return reader.TryRead(out value);
        }

        return TryReadReverseEndianness(ref reader, out value);
    }

    private static bool TryReadReverseEndianness(ref SequenceReader<byte> reader, out short value)
    {
        if (reader.TryRead(out value))
        {
            value = BinaryPrimitives.ReverseEndianness(value);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Reads an <see cref="Int32"/> as little endian.
    /// </summary>
    /// <returns>False if there wasn't enough data for an <see cref="Int32"/>.</returns>
    public static bool TryReadLittleEndian(ref this SequenceReader<byte> reader, out int value)
    {
        if (BitConverter.IsLittleEndian)
        {
            return reader.TryRead(out value);
        }

        return TryReadReverseEndianness(ref reader, out value);
    }

    /// <summary>
    /// Reads an <see cref="Int32"/> as big endian.
    /// </summary>
    /// <returns>False if there wasn't enough data for an <see cref="Int32"/>.</returns>
    public static bool TryReadBigEndian(ref this SequenceReader<byte> reader, out int value)
    {
        if (!BitConverter.IsLittleEndian)
        {
            return reader.TryRead(out value);
        }

        return TryReadReverseEndianness(ref reader, out value);
    }

    private static bool TryReadReverseEndianness(ref SequenceReader<byte> reader, out int value)
    {
        if (reader.TryRead(out value))
        {
            value = BinaryPrimitives.ReverseEndianness(value);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Reads an <see cref="Int64"/> as little endian.
    /// </summary>
    /// <returns>False if there wasn't enough data for an <see cref="Int64"/>.</returns>
    public static bool TryReadLittleEndian(ref this SequenceReader<byte> reader, out long value)
    {
        if (BitConverter.IsLittleEndian)
        {
            return reader.TryRead(out value);
        }

        return TryReadReverseEndianness(ref reader, out value);
    }

    /// <summary>
    /// Reads an <see cref="Int64"/> as big endian.
    /// </summary>
    /// <returns>False if there wasn't enough data for an <see cref="Int64"/>.</returns>
    public static bool TryReadBigEndian(ref this SequenceReader<byte> reader, out long value)
    {
        if (!BitConverter.IsLittleEndian)
        {
            return reader.TryRead(out value);
        }

        return TryReadReverseEndianness(ref reader, out value);
    }

    private static bool TryReadReverseEndianness(ref SequenceReader<byte> reader, out long value)
    {
        if (reader.TryRead(out value))
        {
            value = BinaryPrimitives.ReverseEndianness(value);
            return true;
        }

        return false;
    }
}

#endif
