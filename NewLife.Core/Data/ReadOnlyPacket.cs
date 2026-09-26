namespace NewLife.Data;

/// <summary>只读数据包。禁止修改数据，适合多线程共享场景</summary>
/// <remarks>
/// <para>与 <see cref="ArrayPacket"/> 的区别：</para>
/// <list type="bullet">
/// <item>索引器为只读，禁止修改数据</item>
/// <item>不支持 Next 链式结构（始终为 null）</item>
/// <item>仅索引器受只读约束：GetSpan/GetMemory/Buffer 仍返回可写视图（接口所限），调用方须自律</item>
/// </list>
/// <para>适用场景：配置数据、协议模板、缓存数据等需要防止意外修改的场合</para>
/// </remarks>
public readonly record struct ReadOnlyPacket : IPacket
{
    #region 属性
    private readonly Byte[] _buffer;
    /// <summary>缓冲区</summary>
    public Byte[] Buffer => _buffer;

    private readonly Int32 _offset;
    /// <summary>数据偏移</summary>
    public Int32 Offset => _offset;

    private readonly Int32 _length;
    /// <summary>数据长度</summary>
    public Int32 Length => _length;

    /// <summary>头部可借位空间。本类型不预留头部，恒为 0</summary>
    public Int32 FreeHeader => 0;

    /// <summary>下一个链式包。只读包不支持链式结构，始终返回 null</summary>
    IPacket? IPacket.Next { get => null; set { } }

    /// <summary>总长度。只读包不支持链式，等于 Length</summary>
    public Int32 Total => _length;

    /// <summary>空数据包</summary>
    public static ReadOnlyPacket Empty { get; } = new([]);
    #endregion

    #region 索引
    /// <summary>获取指定位置的字节（只读）</summary>
    /// <param name="index">索引位置</param>
    /// <returns>字节值</returns>
    /// <exception cref="IndexOutOfRangeException">索引超出范围</exception>
    public Byte this[Int32 index]
    {
        get
        {
            if (index < 0 || index >= _length)
                throw new IndexOutOfRangeException($"Index {index} is out of range [0, {_length})");
            return _buffer[_offset + index];
        }
        set => throw new NotSupportedException("ReadOnlyPacket does not support modification");
    }
    #endregion

    #region 构造
    /// <summary>通过字节数组实例化只读数据包</summary>
    /// <param name="buffer">字节数组</param>
    /// <param name="offset">起始偏移</param>
    /// <param name="count">数据长度，-1 表示到数组末尾</param>
    public ReadOnlyPacket(Byte[] buffer, Int32 offset = 0, Int32 count = -1)
    {
        if (buffer == null) throw new ArgumentNullException(nameof(buffer));
        if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
        if (count < 0) count = buffer.Length - offset;
        if (offset + count > buffer.Length) throw new ArgumentOutOfRangeException(nameof(count));

        _buffer = buffer;
        _offset = offset;
        _length = count;
    }

    /// <summary>从数组段实例化只读数据包</summary>
    /// <param name="segment">数组段</param>
    public ReadOnlyPacket(ArraySegment<Byte> segment) : this(segment.Array!, segment.Offset, segment.Count) { }

    /// <summary>从 IPacket 创建只读副本</summary>
    /// <param name="packet">源数据包</param>
    /// <remarks>会复制数据到新的缓冲区，确保完全独立</remarks>
    public ReadOnlyPacket(IPacket packet) : this(packet.ToArray()) { }
    #endregion

    #region 方法
    /// <summary>获取分片视图（仅本段，只读包不支持 Next）</summary>
    /// <returns>只读字节片段</returns>
    public Span<Byte> GetSpan() => new(_buffer, _offset, _length);

    /// <summary>获取内存块（仅本段，只读包不支持 Next）</summary>
    /// <returns>只读内存块</returns>
    public Memory<Byte> GetMemory() => new(_buffer, _offset, _length);

    /// <summary>切片得到新的只读数据包</summary>
    /// <param name="offset">相对偏移</param>
    /// <param name="count">数据长度，-1 表示到末尾</param>
    /// <returns>新的只读数据包</returns>
    IPacket IPacket.Slice(Int32 offset, Int32 count) => Slice(offset, count);

    /// <summary>切片得到新的只读数据包，无内存分配</summary>
    /// <param name="offset">相对偏移</param>
    /// <param name="count">数据长度，-1 表示到末尾</param>
    /// <returns>新的只读数据包</returns>
    public ReadOnlyPacket Slice(Int32 offset, Int32 count = -1)
    {
        if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));

        var newOffset = _offset + offset;
        var remain = _length - offset;
        if (count < 0 || count > remain) count = remain;
        if (count < 0) count = 0;

        return new ReadOnlyPacket(_buffer, newOffset, count);
    }

    /// <summary>尝试获取数组段（仅本段，只读包不支持 Next）</summary>
    /// <param name="segment">输出的数组段</param>
    /// <returns>始终返回 true</returns>
    public Boolean TryGetArray(out ArraySegment<Byte> segment)
    {
        segment = new ArraySegment<Byte>(_buffer, _offset, _length);
        return true;
    }
    #endregion

    #region 转换
    /// <summary>转换为字节数组</summary>
    /// <returns>字节数组副本</returns>
    /// <remarks>总是复制：整段时直接返回内部缓冲区会让调用方绕过只读约束，改写共享的模板/协议常量块</remarks>
    public Byte[] ToArray() => GetSpan().ToArray();

    /// <summary>从字节数组隐式转换</summary>
    /// <param name="buffer">字节数组</param>
    public static implicit operator ReadOnlyPacket(Byte[] buffer) => new(buffer);

    /// <summary>从数组段隐式转换</summary>
    /// <param name="segment">数组段</param>
    public static implicit operator ReadOnlyPacket(ArraySegment<Byte> segment) => new(segment);

    /// <summary>已重载</summary>
    public override String ToString() => $"ReadOnlyPacket[{_buffer.Length}]({_offset}, {_length})";
    #endregion
}
