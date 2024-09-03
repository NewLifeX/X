using System.Buffers;
#if NETFRAMEWORK || NETSTANDARD2_0
using ValueTask = System.Threading.Tasks.Task;
#endif

namespace NewLife.Buffers;

/// <summary>池化缓冲区写入器</summary>
public sealed class PooledByteBufferWriter : IBufferWriter<Byte>, IDisposable
{
    #region 属性
    private Byte[] _rentedBuffer;
    private Int32 _index;

    /// <summary>已写入内存</summary>
    public ReadOnlyMemory<Byte> WrittenMemory => _rentedBuffer.AsMemory(0, _index);

    /// <summary>容量</summary>
    public Int32 Capacity => _rentedBuffer.Length;
    #endregion

    #region 构造
    /// <summary>指定初始容量并初始化写入器</summary>
    /// <param name="initialCapacity"></param>
    public PooledByteBufferWriter(Int32 initialCapacity)
    {
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
    /// <summary>初始化空实例</summary>
    /// <param name="initialCapacity"></param>
    public void InitializeEmptyInstance(Int32 initialCapacity)
    {
        _rentedBuffer = ArrayPool<Byte>.Shared.Rent(initialCapacity);
        _index = 0;
    }

    /// <summary>清空写入器</summary>
    public void Clear()
    {
        _rentedBuffer.AsSpan(0, _index).Clear();
        _index = 0;
    }

    /// <summary>清空写入器并归还到对象池</summary>
    public void ClearAndReturnBuffers()
    {
        Clear();

        var rentedBuffer = _rentedBuffer;
        _rentedBuffer = null!;
        ArrayPool<Byte>.Shared.Return(rentedBuffer);
    }

    /// <summary>通知 IBufferWriter，已向输出写入 count 数据项。</summary>
    /// <param name="count"></param>
    public void Advance(Int32 count) => _index += count;

    /// <summary>返回要向其中写入数据的 Memory，且大小至少是 sizeHint 指定的请求大小。</summary>
    /// <param name="sizeHint"></param>
    /// <returns></returns>
    public Memory<Byte> GetMemory(Int32 sizeHint = 256)
    {
        CheckAndResizeBuffer(sizeHint);
        return _rentedBuffer.AsMemory(_index);
    }

    /// <summary>返回要向其中写入数据的 Span，且大小至少是 sizeHint 指定的请求大小。</summary>
    /// <param name="sizeHint"></param>
    /// <returns></returns>
    public Span<Byte> GetSpan(Int32 sizeHint = 256)
    {
        CheckAndResizeBuffer(sizeHint);
        return _rentedBuffer.AsSpan(_index);
    }

    /// <summary>写入到数据流</summary>
    /// <param name="destination"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public ValueTask WriteToStreamAsync(Stream destination, CancellationToken cancellationToken) => destination.WriteAsync(WrittenMemory, cancellationToken);

    /// <summary>写入到数据流</summary>
    /// <param name="destination"></param>
    public void WriteToStream(Stream destination) => destination.Write(_rentedBuffer, 0, _index);

    private void CheckAndResizeBuffer(Int32 sizeHint)
    {
        var num = _rentedBuffer.Length;
        var num2 = num - _index;
        if (_index >= 1073741795)
        {
            sizeHint = Math.Max(sizeHint, 2147483591 - num);
        }
        if (sizeHint <= num2) return;

        var num3 = Math.Max(sizeHint, num);
        var num4 = num + num3;
        if ((UInt32)num4 > 2147483591u)
        {
            num4 = num + sizeHint;
            if ((UInt32)num4 > 2147483591u)
            {
                throw new OutOfMemoryException($"BufferMaximumSizeExceeded({num4})");
            }
        }
        var rentedBuffer = _rentedBuffer;
        _rentedBuffer = ArrayPool<Byte>.Shared.Rent(num4);
        var span = rentedBuffer.AsSpan(0, _index);
        span.CopyTo(_rentedBuffer);
        span.Clear();
        ArrayPool<Byte>.Shared.Return(rentedBuffer);
    }
    #endregion
}
