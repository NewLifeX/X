namespace NewLife.Data;

/// <summary>环形缓冲区。用于协议组包设计</summary>
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
    public Int32 Length => Head >= Tail ? (Head - Tail) : (Head + _data.Length - Tail);

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
    /// <param name="capacity"></param>
    public void EnsureCapacity(Int32 capacity)
    {
        if (capacity <= Capacity) return;

        // 分配新空间，全量拷贝。比分块拷贝要低效一些，但是代码简单直接
        var data = new Byte[capacity];
        if (Length > 0)
            Buffer.BlockCopy(_data, 0, data, 0, _data.Length);
        _data = data;
    }

    private void CheckCapacity(Int32 capacity)
    {
        var len = _data.Length;

        // 两倍增长
        while (len < capacity) len *= 2;

        EnsureCapacity(len);
    }

    /// <summary>写入数据</summary>
    /// <param name="data">数据</param>
    /// <param name="offset">偏移量</param>
    /// <param name="count">个数</param>
    public void Write(Byte[] data, Int32 offset = 0, Int32 count = -1)
    {
        if (count < 0) count = data.Length - offset;

        CheckCapacity(Length + count);

        var len = _data.Length - Head;
        if (len > count) len = count;

        Buffer.BlockCopy(data, offset, _data, Head, len);

        count -= len;
        Head += len;
        if (Head == _data.Length) Head = 0;

        // 还有数据，移到开头
        if (count > 0)
        {
            Buffer.BlockCopy(data, offset, _data, Head, len);

            Head = count;
        }
    }

    /// <summary>读取数据</summary>
    /// <param name="data">数据</param>
    /// <param name="offset">偏移量</param>
    /// <param name="count">个数</param>
    /// <returns></returns>
    public Int32 Read(Byte[] data, Int32 offset = 0, Int32 count = -1)
    {
        if (count < 0) count = data.Length - offset;

        var len = Length;
        if (len > count) len = count;
        if (Tail + len > _data.Length) len = _data.Length - Tail;

        Buffer.BlockCopy(_data, Tail, data, offset, len);

        var rs = len;
        count -= len;
        Tail += len;
        if (Tail == _data.Length) Tail = 0;

        // 还有数据，移到开头
        if (count > 0)
        {
            offset += len;
            len = Length;
            if (len > count) len = count;

            Buffer.BlockCopy(_data, 0, data, offset, len);

            rs += len;
            count -= len;
            Tail += len;
        }

        return rs;
    }
    #endregion
}
