using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NewLife.Data;

/// <summary>内存包</summary>
/// <remarks>内存包可能来自内存池，失去所有权时已被释放，因此不应该长期持有。</remarks>
public struct MemoryPacket : IPacket
{
    #region 属性
    private readonly Memory<Byte> _memory;
    /// <summary>内存</summary>
    public readonly Memory<Byte> Memory => _memory;

    private readonly Int32 _length;
    /// <summary>数据长度</summary>
    public readonly Int32 Length => _length;

    // 缓存底层数组引用，Indexer 直接数组访问避免 Memory.Span 间接开销（1.66ns → ~0.24ns）
    private readonly Byte[]? _cachedArray;
    private readonly Int32 _cachedOffset;

    /// <summary>获取/设置 指定位置的字节</summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public Byte this[Int32 index]
    {
        get
        {
            var p = index - _length;
            if (p >= 0)
            {
                if (Next == null) throw new IndexOutOfRangeException(nameof(index));

                return Next[p];
            }

            var arr = _cachedArray;
            return arr != null ? arr[_cachedOffset + index] : _memory.Span[index];
        }
        set
        {
            var p = index - _length;
            if (p >= 0)
            {
                if (Next == null) throw new IndexOutOfRangeException(nameof(index));

                Next[p] = value;
            }
            else
            {
                var arr = _cachedArray;
                if (arr != null)
                    arr[_cachedOffset + index] = value;
                else
                    _memory.Span[index] = value;
            }
        }
    }

    /// <summary>下一个链式包</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public IPacket? Next { get; set; }

    /// <summary>总长度</summary>
    public readonly Int32 Total => Length + (Next?.Total ?? 0);
    #endregion

    /// <summary>实例化内存包，指定内存和长度</summary>
    /// <param name="memory">内存</param>
    /// <param name="length">长度</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public MemoryPacket(Memory<Byte> memory, Int32 length)
    {
        if (length < 0 || length > memory.Length)
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be non-negative and less than or equal to the memory owner's length.");

        _memory = memory;
        _length = length;

        // 缓存底层数组引用，加速 Indexer 直接数组访问
        if (MemoryMarshal.TryGetArray((ReadOnlyMemory<Byte>)memory, out var seg))
        {
            _cachedArray = seg.Array;
            _cachedOffset = seg.Offset;
        }
        else
        {
            _cachedArray = null;
            _cachedOffset = 0;
        }
    }

    /// <summary>获取分片包。在管理权生命周期内短暂使用</summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<Byte> GetSpan() => _memory.Span[.._length];

    /// <summary>获取内存包。在管理权生命周期内短暂使用</summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Memory<Byte> GetMemory() => _memory[.._length];

    /// <summary>切片得到新数据包，共用内存块</summary>
    /// <param name="offset">偏移</param>
    /// <param name="count">个数。默认-1表示到末尾</param>
    IPacket IPacket.Slice(Int32 offset, Int32 count) => Slice(offset, count);

    /// <summary>切片得到新数据包（兼容重载），共用内存块。无所有权，忽略转移参数</summary>
    /// <param name="offset">偏移</param>
    /// <param name="count">个数。默认-1表示到末尾</param>
    /// <param name="transferOwner">转移所有权。无所有权结构体忽略该参数</param>
    [Obsolete("引用计数共享模型下切片自动共享所有权，请改用 Slice(Int32 offset, Int32 count)。")]
    IPacket IPacket.Slice(Int32 offset, Int32 count, Boolean transferOwner) => Slice(offset, count);

    /// <summary>切片得到新数据包，共用内存块，无内存分配</summary>
    /// <param name="offset">偏移</param>
    /// <param name="count">个数。默认-1表示到末尾</param>
    public MemoryPacket Slice(Int32 offset, Int32 count = -1)
    {
        // 带有Next时，不支持Slice
        if (Next != null) throw new NotSupportedException("Slice with Next");

        var remain = _length - offset;
        if (count < 0 || count > remain) count = remain;
        if (offset == 0 && count == _length) return this;

        return offset == 0
            ? new MemoryPacket(_memory, count)
            : new MemoryPacket(_memory[offset..], count);
    }

    /// <summary>切片得到新数据包（兼容重载），共用内存块。无所有权，忽略转移参数</summary>
    /// <param name="offset">偏移</param>
    /// <param name="count">个数。默认-1表示到末尾</param>
    /// <param name="transferOwner">转移所有权。无所有权结构体忽略该参数</param>
    /// <returns>新的数据包实例</returns>
    [Obsolete("引用计数共享模型下切片自动共享所有权，请改用 Slice(Int32 offset, Int32 count)。")]
    public MemoryPacket Slice(Int32 offset, Int32 count, Boolean transferOwner) => Slice(offset, count);

    /// <summary>尝试获取缓冲区（仅本段，不含 Next）</summary>
    /// <param name="segment"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Boolean TryGetArray(out ArraySegment<Byte> segment) => MemoryMarshal.TryGetArray(GetMemory(), out segment);

    /// <summary>已重载</summary>
    /// <returns></returns>
    public override readonly String ToString() => $"MemoryPacket[{_memory.Length}](0, {_length})<{Total}>";
}
