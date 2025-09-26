namespace NewLife.Data;

/// <summary>环形缓冲区。用于协议组包设计</summary>
/// <remarks>
/// 环形缓冲区是一种固定大小的缓冲区，当缓冲区满时新数据会覆盖旧数据。
/// 本实现支持动态扩容，确保数据不丢失。
/// 使用 Head 指针标记写入位置，Tail 指针标记读取位置。
/// </remarks>
public class RingBuffer
{
    #region 属性
    /// <summary>容量</summary>
    public Int32 Capacity => _data.Length;

    /// <summary>头指针。写入位置</summary>
    public Int32 Head { get; set; }

    /// <summary>尾指针。读取位置</summary>
    public Int32 Tail { get; set; }

    /// <summary>数据长度</summary>
    /// <remarks>环形缓冲区中实际存储的数据长度</remarks>
    public Int32 Length { get; private set; }

    /// <summary>内部数据缓冲区</summary>
    private Byte[] _data;
    #endregion

    #region 构造
    /// <summary>使用默认容量1024来初始化</summary>
    public RingBuffer() : this(1024) { }

    /// <summary>实例化环形缓冲区</summary>
    /// <param name="capacity">容量。合理的容量能够减少扩容</param>
    public RingBuffer(Int32 capacity) => _data = new Byte[capacity];
    #endregion

    #region 方法
    /// <summary>扩容，确保容量</summary>
    /// <param name="capacity">目标容量</param>
    /// <remarks>
    /// 当需要更大容量时，会分配新的缓冲区并正确复制环形数据。
    /// 复制后会重置 Head 和 Tail 指针为线性布局。
    /// </remarks>
    public void EnsureCapacity(Int32 capacity)
    {
        if (capacity <= Capacity) return;

        var newData = new Byte[capacity];
        var length = Length;

        if (length > 0)
        {
            // 正确处理环形数据的拷贝
            if (Head > Tail)
            {
                // 数据是连续的，直接拷贝
                Buffer.BlockCopy(_data, Tail, newData, 0, length);
            }
            else
            {
                // 数据是分段的，分两次拷贝
                var tailToEnd = _data.Length - Tail;
                Buffer.BlockCopy(_data, Tail, newData, 0, tailToEnd);
                Buffer.BlockCopy(_data, 0, newData, tailToEnd, Head);
            }

            // 重置指针，让数据变成线性排列
            Tail = 0;
            Head = length;
        }

        _data = newData;
    }

    /// <summary>检查容量并在需要时扩容</summary>
    /// <param name="capacity">所需容量</param>
    private void CheckCapacity(Int32 capacity)
    {
        var len = _data.Length;

        // 两倍增长策略
        while (len < capacity)
            len *= 2;

        EnsureCapacity(len);
    }

    /// <summary>写入数据</summary>
    /// <param name="data">数据</param>
    /// <param name="offset">偏移量</param>
    /// <param name="count">个数</param>
    /// <remarks>
    /// 当缓冲区空间不足时会自动扩容。
    /// 写入时会正确处理环形边界情况。
    /// </remarks>
    public void Write(Byte[] data, Int32 offset = 0, Int32 count = -1)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (data.Length == 0) return;
        if (offset < 0 || offset >= data.Length) throw new ArgumentOutOfRangeException(nameof(offset));

        if (count < 0) count = data.Length - offset;
        if (count == 0) return;
        if (offset + count > data.Length) throw new ArgumentOutOfRangeException(nameof(count));

        // 检查并扩容
        CheckCapacity(Length + count);

        var remaining = count;
        var srcOffset = offset;

        if (Head >= Tail)
        {
            // 场景1：数据连续分布，Head >= Tail
            // 布局：[空间][有效数据][空间]
            // 可写区域：从 Head 到缓冲区末尾，然后从 0 到 Tail

            // 第一段：从 Head 写到缓冲区末尾
            var firstChunkSize = Math.Min(remaining, _data.Length - Head);
            if (firstChunkSize > 0)
            {
                Buffer.BlockCopy(data, srcOffset, _data, Head, firstChunkSize);
                Head += firstChunkSize;
                srcOffset += firstChunkSize;
                remaining -= firstChunkSize;

                // Head 到达缓冲区末尾时回绕到开头
                if (Head == _data.Length) Head = 0;
            }

            // 第二段：如果还有剩余数据，从缓冲区开头继续写入
            if (remaining > 0)
            {
                Buffer.BlockCopy(data, srcOffset, _data, Head, remaining);
                Head += remaining;
            }
        }
        else
        {
            // 场景2：数据分段分布，Head < Tail  
            // 布局：[有效数据][可写空间][有效数据]
            // 可写区域：从 Head 到 Tail 之间的空隙

            // Head < Tail 时，只能写入 Head 到 Tail 之间的空隙
            // 由于已经检查过容量，这里的空间一定足够
            var availableSpace = Tail - Head;
            if (count > availableSpace)
            {
                // 理论上不应该到达这里，因为 CheckCapacity 已经确保了足够空间
                throw new InvalidOperationException("缓冲区空间不足，容量检查失败");
            }

            // 直接写入 Head 到 Tail 之间的连续空间
            Buffer.BlockCopy(data, offset, _data, Head, count);
            Head += count;
        }

        // 更新数据长度
        Length += count;
    }

    /// <summary>读取数据</summary>
    /// <param name="data">目标数据缓冲区</param>
    /// <param name="offset">目标缓冲区偏移量</param>
    /// <param name="count">期望读取的字节数</param>
    /// <returns>实际读取的字节数</returns>
    /// <remarks>
    /// 读取时会正确处理环形边界情况。
    /// 实际读取的字节数可能小于期望值，取决于缓冲区中的可用数据量。
    /// </remarks>
    public Int32 Read(Byte[] data, Int32 offset = 0, Int32 count = -1)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (offset < 0 || offset >= data.Length) throw new ArgumentOutOfRangeException(nameof(offset));

        if (count < 0) count = data.Length - offset;
        if (count == 0) return 0;
        if (offset + count > data.Length) throw new ArgumentOutOfRangeException(nameof(count));

        var availableLength = Length;
        if (availableLength == 0) return 0;

        var toRead = Math.Min(count, availableLength);
        var totalRead = 0;

        if (Head > Tail)
        {
            // 场景1：数据连续分布，Head > Tail
            // 布局：[空间][有效数据][空间]
            // 有效数据范围：从 Tail 到 Head
            
            var readSize = Math.Min(toRead, Head - Tail);
            Buffer.BlockCopy(_data, Tail, data, offset, readSize);
            Tail += readSize;
            totalRead = readSize;
        }
        else
        {
            // 场景2：数据分段分布，Head <= Tail
            // 布局：[有效数据][空间][有效数据]
            // 有效数据范围：从 Tail 到缓冲区末尾，然后从 0 到 Head
            
            // 第一段：从 Tail 到缓冲区末尾
            var firstChunkSize = Math.Min(toRead, _data.Length - Tail);
            Buffer.BlockCopy(_data, Tail, data, offset, firstChunkSize);
            Tail = (Tail + firstChunkSize) % _data.Length;
            totalRead += firstChunkSize;
            toRead -= firstChunkSize;

            // 第二段：如果还需要读取更多数据，从缓冲区开头到 Head
            if (toRead > 0)
            {
                var secondChunkSize = Math.Min(toRead, Head);
                Buffer.BlockCopy(_data, 0, data, offset + totalRead, secondChunkSize);
                Tail = secondChunkSize;
                totalRead += secondChunkSize;
            }
        }

        // 更新数据长度
        Length -= totalRead;
        
        return totalRead;
    }
    #endregion
}
