using System.Buffers;
using System.Runtime.CompilerServices;
#if NETFRAMEWORK || NETSTANDARD2_0
using ValueTask = System.Threading.Tasks.Task;
#endif

namespace NewLife.Buffers;

/// <summary>池化缓冲区写入器</summary>
/// <remarks>
/// - 面向需要动态扩容、避免频繁分配的大块连续写入场景。
/// - 基于 <see cref="ArrayPool{T}.Shared"/> 进行数组租借与归还；使用完毕后必须调用 <see cref="Dispose"/> 或 <see cref="ClearAndReturnBuffers"/>。
/// - 非线程安全：单实例请勿跨线程并发写入。
/// </remarks>
public sealed class PooledByteBufferWriter : IBufferWriter<Byte>, IDisposable
{
    // 与 corefx 内部 Array.MaxLength 对齐（约 2GB - 对象头）：0x7FFFFFC7 = 2,147,483,591
    private const Int32 MaxArrayLength = 0x7FFFFFC7;
    private const Int32 HalfOfMaxArrayLength = MaxArrayLength / 2;

    #region 字段/属性
    private Byte[] _rentedBuffer;
    private Int32 _index;

    /// <summary>已写入内存（只读）</summary>
    public ReadOnlyMemory<Byte> WrittenMemory => _rentedBuffer.AsMemory(0, _index);

    /// <summary>已写入数据段（Span）</summary>
    public ReadOnlySpan<Byte> WrittenSpan => _rentedBuffer.AsSpan(0, _index);

    /// <summary>已写入字节数</summary>
    public Int32 WrittenCount => _index;

    /// <summary>当前缓冲区总容量</summary>
    public Int32 Capacity => _rentedBuffer.Length;
    #endregion

    #region 构造
    /// <summary>指定初始容量并初始化写入器</summary>
    /// <param name="initialCapacity">初始容量（&gt;=1）。内部会从数组池租用该大小或更大数组。</param>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="initialCapacity"/> 小于等于 0</exception>
    public PooledByteBufferWriter(Int32 initialCapacity)
    {
        if (initialCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(initialCapacity));

        _rentedBuffer = ArrayPool<Byte>.Shared.Rent(initialCapacity);
        _index = 0;
    }

    /// <summary>销毁。释放内存并归还池中数组</summary>
    public void Dispose()
    {
        if (_rentedBuffer != null)
            ClearAndReturnBuffers();
    }
    #endregion

    #region 公共方法
    /// <summary>重新初始化为一个空实例（不自动释放原缓冲，需要确保当前实例已处于释放状态或刚构造）。</summary>
    /// <param name="initialCapacity">初始容量</param>
    public void InitializeEmptyInstance(Int32 initialCapacity)
    {
        _rentedBuffer = ArrayPool<Byte>.Shared.Rent(initialCapacity);
        _index = 0;
    }

    /// <summary>清空写入器：仅把已写入区域清零并重置位置，不归还数组。</summary>
    public void Clear()
    {
        _rentedBuffer.AsSpan(0, _index).Clear();
        _index = 0;
    }

    /// <summary>清空并归还到数组池。调用后本实例不可再使用。</summary>
    public void ClearAndReturnBuffers()
    {
        Clear();

        var rentedBuffer = _rentedBuffer;
        _rentedBuffer = null!; // 标记失效
        ArrayPool<Byte>.Shared.Return(rentedBuffer);
    }

    /// <summary>通知缓冲区已向前推进 <paramref name="count"/> 个字节。</summary>
    /// <param name="count">新增写入的字节数</param>
    /// <exception cref="ArgumentOutOfRangeException">越界</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(Int32 count)
    {
        var newIndex = _index + count;
        if (newIndex < 0 || (UInt32)newIndex > (UInt32)_rentedBuffer.Length) throw new ArgumentOutOfRangeException(nameof(count));
        _index = newIndex;
    }

    /// <summary>返回可写入的 <see cref="Memory{T}"/>，至少包含 <paramref name="sizeHint"/> 字节空闲空间。</summary>
    /// <param name="sizeHint">期望的最小可用空间（可为 0）。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Memory<Byte> GetMemory(Int32 sizeHint = 256)
    {
        EnsureCapacity(sizeHint);
        return _rentedBuffer.AsMemory(_index);
    }

    /// <summary>返回可写入的 <see cref="Span{T}"/>，至少包含 <paramref name="sizeHint"/> 字节空闲空间。</summary>
    /// <param name="sizeHint">期望的最小可用空间（可为 0）。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    #endregion

    #region 内部实现
    /// <summary>确保剩余容量至少满足 sizeHint。</summary>
    /// <param name="sizeHint">需要的空间</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureCapacity(Int32 sizeHint)
    {
        if (sizeHint <= 0) sizeHint = 0; // 允许0（与IBufferWriter 语义一致）

        var remaining = _rentedBuffer.Length - _index;
        if (sizeHint <= remaining) return;

        CheckAndResizeBuffer(sizeHint);
    }

    /// <summary>扩容策略：容量不足时增长为 当前长度 + max(请求, 当前长度)（近似 2 倍）；上限接近 Array 最大长度（MaxArrayLength）。</summary>
    /// <param name="sizeHint">最小需要空间</param>
    /// <exception cref="OutOfMemoryException">超过数组最大允许长度</exception>
    private void CheckAndResizeBuffer(Int32 sizeHint)
    {
        var currentLength = _rentedBuffer.Length;
        var remaining = currentLength - _index;

        // 如果接近上限，需要收缩增长幅度避免溢出
        if (_index >= HalfOfMaxArrayLength)
            sizeHint = Math.Max(sizeHint, MaxArrayLength - currentLength);

        if (sizeHint <= remaining) return; // 可能在上一步已满足

        // 目标大小 = 当前长度 + max(请求, 当前长度)（近似 2x 扩容）
        var grow = Math.Max(sizeHint, currentLength);
        var newSize = currentLength + grow;
        if ((UInt32)newSize > MaxArrayLength)
        {
            // 使用精确需求大小再尝试一次
            newSize = currentLength + sizeHint;
            if ((UInt32)newSize > MaxArrayLength)
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
