using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NewLife.Data;

/// <summary>字节数组包</summary>
public record struct ArrayPacket : IPacket
{
    #region 属性
    private readonly Byte[] _buffer;
    /// <summary>缓冲区</summary>
    public readonly Byte[] Buffer => _buffer;

    private readonly Int32 _offset;
    /// <summary>数据偏移</summary>
    public readonly Int32 Offset => _offset;

    /// <summary>头部可借位空间。等于视图起点偏移，供下游向前借位写入协议头</summary>
    public readonly Int32 FreeHeader => _offset;

    private readonly Int32 _length;
    /// <summary>数据长度</summary>
    public readonly Int32 Length => _length;

    /// <summary>下一个链式包</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public IPacket? Next { get; set; }

    /// <summary>总长度</summary>
    public readonly Int32 Total => Length + (Next?.Total ?? 0);

    /// <summary>空数组</summary>
    public static readonly ArrayPacket Empty = new([]);
    #endregion

    #region 索引
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

            return _buffer[_offset + index];
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
                _buffer[_offset + index] = value;
            }
        }
    }
    #endregion

    #region 构造
    /// <summary>通过指定字节数组来实例化数据包</summary>
    /// <param name="buf"></param>
    /// <param name="offset"></param>
    /// <param name="count"></param>
    public ArrayPacket(Byte[] buf, Int32 offset = 0, Int32 count = -1)
    {
        if (buf == null) throw new ArgumentNullException(nameof(buf));
        if (count < 0) count = buf.Length - offset;

        _buffer = buf;
        _offset = offset;
        _length = count;
    }

    /// <summary>从可扩展内存流实例化，尝试窃取内存流内部的字节数组，失败后拷贝</summary>
    /// <remarks>因数据包内数组窃取自内存流，需要特别小心，避免多线程共用。常用于内存流转数据包，而内存流不再使用</remarks>
    /// <param name="stream"></param>
    public ArrayPacket(Stream stream)
    {
        if (stream is MemoryStream ms)
        {
#if !NET45
            // 尝试抠了内部存储区，下面代码需要.Net 4.6支持
            if (ms.TryGetBuffer(out var seg))
            {
                if (seg.Array == null) throw new InvalidDataException();

                _buffer = seg.Array;
                _offset = seg.Offset + (Int32)ms.Position;
                _length = seg.Count - (Int32)ms.Position;
                return;
            }
            // GetBuffer窃盗内部缓冲区后，无法得知真正的起始位置index，可能导致错误取数
            // public MemoryStream(byte[] buffer, int index, int count, bool writable, bool publiclyVisible)

            //try
            //{
            //    Set(ms.GetBuffer(), (Int32)ms.Position, (Int32)(ms.Length - ms.Position));
            //}
            //catch (UnauthorizedAccessException) { }
#endif
        }

        var buf = new Byte[stream.Length - stream.Position];
        var count = stream.Read(buf, 0, buf.Length);
        _buffer = buf;
        _offset = 0;
        _length = count;

        // 必须确保数据流位置不变
        if (count > 0) stream.Seek(-count, SeekOrigin.Current);
    }

    /// <summary>从数据段实例化数据包</summary>
    /// <param name="segment"></param>
    public ArrayPacket(ArraySegment<Byte> segment) : this(segment.Array!, segment.Offset, segment.Count) { }
    #endregion

    /// <summary>获取分片包。在管理权生命周期内短暂使用</summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<Byte> GetSpan() => new(_buffer, _offset, _length);

    /// <summary>获取内存包。在管理权生命周期内短暂使用</summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Memory<Byte> GetMemory() => new(_buffer, _offset, _length);

    /// <summary>切片得到新数据包，共用缓冲区</summary>
    /// <param name="offset">偏移</param>
    /// <param name="count">个数。默认-1表示到末尾</param>
    IPacket IPacket.Slice(Int32 offset, Int32 count)
    {
        // 内联逻辑，避免 struct 通过 (this as IPacket) 装箱
        if (count == 0) return Empty;

        var remain = _length - offset;
        var next = Next;
        if (next != null && remain <= 0) return next.Slice(offset - _length, count);

        return Slice(offset, count);
    }

    /// <summary>扩展头部空间，共用缓冲区，无内存分配</summary>
    /// <param name="size">向前扩展的字节数</param>
    /// <returns>新的数据包实例（视图）</returns>
    /// <remarks>前置空间足够时向前借位；结构体为借阅视图，调用后应改用返回的新视图，不再使用原实例。</remarks>
    /// <exception cref="ArgumentOutOfRangeException">前置空间不足</exception>
    public readonly ArrayPacket ExpandHeader(Int32 size)
    {
        if (_offset < size) throw new ArgumentOutOfRangeException(nameof(size), $"Expand size {size} exceeds available front space {_offset}");

        return new ArrayPacket(_buffer, _offset - size, _length + size) { Next = Next };
    }

    /// <summary>切片得到新数据包，共用缓冲区，无内存分配</summary>
    /// <param name="offset">偏移</param>
    /// <param name="count">个数。默认-1表示到末尾</param>
    public ArrayPacket Slice(Int32 offset, Int32 count = -1)
    {
        if (count == 0) return Empty;

        // 负偏移会向前多取字节（越窗读到本包之前的活数据）且不报错，必须显式拒绝；向前借位请用 ExpandHeader
        if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset), "Offset cannot be negative.");

        var start = Offset + offset;
        var remain = _length - offset;

        var next = Next;
        if (next == null)
        {
            // count 是 offset 之后的个数
            if (count < 0 || count > remain) count = remain;
            return count <= 0 ? Empty : new ArrayPacket(_buffer, start, count);
        }

        // 如果当前段用完，则取下一段。强转ArrayPacket，如果不是则抛出异常
        if (remain <= 0)
            return (ArrayPacket)next.Slice(offset - _length, count);

        // 当前包用一截，剩下的全部
        if (count < 0)
            return new ArrayPacket(_buffer, start, remain) { Next = next };

        // 当前包可以读完
        if (count <= remain)
            return new ArrayPacket(_buffer, start, count);

        // 当前包用一截，剩下的再截取
        return new ArrayPacket(_buffer, start, remain) { Next = next.Slice(0, count - remain) };
    }

    /// <summary>尝试获取缓冲区（仅本段，不含 Next）</summary>
    /// <param name="segment"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Boolean TryGetArray(out ArraySegment<Byte> segment)
    {
        segment = new ArraySegment<Byte>(_buffer, _offset, _length);
        return true;
    }

    #region 重载运算符
    /// <summary>重载类型转换，字节数组直接转为 ArrayPacket 对象</summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static implicit operator ArrayPacket(Byte[] value) => new(value);

    /// <summary>重载类型转换，一维数组直接转为 ArrayPacket 对象</summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static implicit operator ArrayPacket(ArraySegment<Byte> value) => new(value.Array!, value.Offset, value.Count);

    /// <summary>重载类型转换，字符串直接转为 ArrayPacket 对象</summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static implicit operator ArrayPacket(String value) => new(value.GetBytes());

    /// <summary>已重载</summary>
    /// <returns></returns>
    public override readonly String ToString() => $"ArrayPacket[{_buffer.Length}]({_offset}, {_length})<{Total}>";
    #endregion
}
