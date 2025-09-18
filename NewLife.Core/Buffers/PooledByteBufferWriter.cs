using System.Buffers;
#if NETFRAMEWORK || NETSTANDARD2_0
using ValueTask = System.Threading.Tasks.Task;
#endif

namespace NewLife.Buffers;

/// <summary>池化缓冲区写入器</summary>
/// <remarks>
/// 适用于需要动态扩容、避免频繁分配的大块连续写入场景。
/// 内部使用 <see cref="ArrayPool{T}.Shared"/>，请在使用完毕后调用 <see cref="Dispose"/> 或 <see cref="ClearAndReturnBuffers"/> 归还缓冲，避免内存泄漏。
/// </remarks>
public sealed class PooledByteBufferWriter : IBufferWriter<Byte>, IDisposable
{
    #region 属性
    private Byte[] _rentedBuffer;
    private Int32 _index;

    /// <summary>已写入内存（只读）</summary>
    public ReadOnlyMemory<Byte> WrittenMemory => _rentedBuffer.AsMemory(0, _index);

    /// <summary>已写入数据段（Span）</summary>
    public ReadOnlySpan<Byte> WrittenSpan => _rentedBuffer.AsSpan(0, _index);

    /// <summary>已写入字节数</summary>
    public Int32 WrittenCount => _index;

    /// <summary>容量</summary>
    public Int32 Capacity => _rentedBuffer.Length;
    #endregion

    #region 构造
    /// <summary>指定初始容量并初始化写入器</summary>
    /// <param name="initialCapacity">初始容量（>=1）。内部会从数组池租用该大小或更大数组。</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public PooledByteBufferWriter(Int32 initialCapacity)
    {
        if (initialCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(initialCapacity));

        _rentedBuffer = ArrayPool<Byte>.Shared.Rent(initialCapacity);
        _index = 0;
    }

    /// <summary>销毁。释放内存并返回池里</summary>
    public void Dispose()
    {
        if (_rentedBuffer != null)
            ClearAndReturnBuffers();
    }
    #endregion

    #region 方法
    /// <summary>重新初始化为一个空实例（不自动释放原缓冲，需要确保当前实例已处于释放状态或刚构造）。</summary>
    /// <param name="initialCapacity">初始容量</param>
    public void InitializeEmptyInstance(Int32 initialCapacity)
    {
        _rentedBuffer = ArrayPool<Byte>.Shared.Rent(initialCapacity);
        _index = 0;
    }

    /// <summary>清空写入器。仅把已写入区域清零并重置位置，不归还数组。</summary>
    public void Clear()
    {
        _rentedBuffer.AsSpan(0, _index).Clear();
        _index = 0;
    }

    /// <summary>清空写入器并归还到对象池。调用后本实例不可再使用。</summary>
    public void ClearAndReturnBuffers()
    {
        Clear();

        var rentedBuffer = _rentedBuffer;
        _rentedBuffer = null!; // 标记失效
        ArrayPool<Byte>.Shared.Return(rentedBuffer);
    }

    /// <summary>通知缓冲区已向输出前移 <paramref name="count"/> 个字节。</summary>
    /// <param name="count">新增写入的字节数</param>
    /// <exception cref="ArgumentOutOfRangeException">范围越界</exception>
    public void Advance(Int32 count)
    {
        var newIndex = _index + count;
        if (newIndex < 0 || (UInt32)newIndex > (UInt32)_rentedBuffer.Length) throw new ArgumentOutOfRangeException(nameof(count));
        _index = newIndex;
    }

    /// <summary>返回可写入的 <see cref="Memory{T}"/>，至少包含 <paramref name="sizeHint"/> 字节空闲空间。</summary>
    /// <param name="sizeHint">期望的最小可用空间（可为0）。</param>
    public Memory<Byte> GetMemory(Int32 sizeHint = 256)
    {
        EnsureCapacity(sizeHint);
        return _rentedBuffer.AsMemory(_index);
    }

    /// <summary>返回可写入的 <see cref="Span{T}"/>，至少包含 <paramref name="sizeHint"/> 字节空闲空间。</summary>
    /// <param name="sizeHint">期望的最小可用空间（可为0）。</param>
    public Span<Byte> GetSpan(Int32 sizeHint = 256)
    {
        EnsureCapacity(sizeHint);
        return _rentedBuffer.AsSpan(_index);
    }

    /// <summary>写入到数据流（同步）。</summary>
    /// <param name="destination">目标流</param>
    public void WriteToStream(Stream destination) => destination.Write(_rentedBuffer, 0, _index);

    /// <summary>写入到数据流（异步）。</summary>
    /// <param name="destination">目标流</param>
    /// <param name="cancellationToken">取消令牌</param>
    public ValueTask WriteToStreamAsync(Stream destination, CancellationToken cancellationToken) => destination.WriteAsync(WrittenMemory, cancellationToken);

    /// <summary>确保剩余容量至少满足 sizeHint。</summary>
    /// <param name="sizeHint">需要的空间</param>
    private void EnsureCapacity(Int32 sizeHint)
    {
        if (sizeHint <= 0) sizeHint = 0; // 允许0（与IBufferWriter 语义一致）
        var remaining = _rentedBuffer.Length - _index;
        if (sizeHint <= remaining) return;
        CheckAndResizeBuffer(sizeHint);
    }

    /// <summary>扩容策略：当前容量不足时，按 [当前容量 + max(请求, 当前容量)] 方式增长；上限接近 Array 最大值。</summary>
    /// <param name="sizeHint">最小需要空间</param>
    /// <exception cref="OutOfMemoryException">超过数组最大允许长度</exception>
    private void CheckAndResizeBuffer(Int32 sizeHint)
    {
        var currentLength = _rentedBuffer.Length;
        var remaining = currentLength - _index;

        // 如果接近 int.MaxValue，需要收缩增长幅度避免溢出。2147483591 与 corefx 内部常量 Array.MaxLength 对齐（减去对象头等）。
        if (_index >= 1073741795) sizeHint = Math.Max(sizeHint, 2147483591 - currentLength);
        if (sizeHint <= remaining) return; // 可能上一步调整后已满足

        // 目标大小 = 当前长度 + max(请求, 当前长度) => 近似 2 倍扩容策略。
        var grow = Math.Max(sizeHint, currentLength);
        var newSize = currentLength + grow;
        if ((UInt32)newSize > 2147483591u)
        {
            // 尝试使用精确需求大小再判断一次
            newSize = currentLength + sizeHint;
            if ((UInt32)newSize > 2147483591u)
                throw new OutOfMemoryException($"BufferMaximumSizeExceeded({newSize})");
        }

        var old = _rentedBuffer;
        _rentedBuffer = ArrayPool<Byte>.Shared.Rent(newSize);

        // 拷贝已写入数据后清理旧数据（防止潜在敏感信息残留）。
        var written = old.AsSpan(0, _index);
        written.CopyTo(_rentedBuffer);
        written.Clear();
        ArrayPool<Byte>.Shared.Return(old);
    }
    #endregion
}
