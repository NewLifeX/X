using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using NewLife.Data;

namespace NewLife.Buffers;

/// <summary>Span读取器</summary>
/// <remarks>
/// 引用结构，零分配读取二进制数据，支持自动从底层 <see cref="Stream"/> 追加读取。
/// 典型用于解析 Redis/MySql/自定义协议帧；支持 7 位压缩整数、结构体直接反序列化等。
/// </remarks>
public ref struct SpanReader
{
    #region 属性
    private ReadOnlySpan<Byte> _span;
    /// <summary>数据片段</summary>
    public ReadOnlySpan<Byte> Span => _span;

    private Int32 _index;
    /// <summary>已读取字节数</summary>
    public Int32 Position { get => _index; set => _index = value; }

    /// <summary>总容量</summary>
    public Int32 Capacity => _span.Length;

    /// <summary>空闲容量（尚未读取的剩余字节数）</summary>
    public Int32 FreeCapacity => _span.Length - _index;

    /// <summary>是否小端字节序。默认 true</summary>
    public Boolean IsLittleEndian { get; set; } = true;
    #endregion

    #region 构造
    /// <summary>实例化。暂时兼容旧版，后面使用主构造函数</summary>
    /// <param name="span">数据</param>
    public SpanReader(ReadOnlySpan<Byte> span) => _span = span;

    /// <summary>实例化。暂时兼容旧版，后面删除</summary>
    /// <param name="span">数据</param>
    public SpanReader(Span<Byte> span) => _span = span;

    /// <summary>实例化Span读取器</summary>
    /// <param name="data">初始数据包</param>
    public SpanReader(IPacket data)
    {
        _data = data;
        _span = data.GetSpan();
        _total = data.Total;
    }
    #endregion

    #region 扩容增强
    /// <summary>最大容量。多次从数据流读取数据时，受限于此最大值（0 表示不限制）。</summary>
    public Int32 MaxCapacity { get; set; }

    private readonly Stream? _stream;
    private readonly Int32 _bufferSize;
    private IPacket? _data;
    private Int32 _total;

    /// <summary>支持从数据流中读取更多数据，突破初始大小限制。</summary>
    /// <remarks>
    /// 解析网络协议时，数据帧可能超过初始缓冲区大小。提供 <paramref name="stream"/> 后，
    /// 当剩余可读字节不足时，会自动从流中读取一批数据并扩充内部缓冲区。
    /// </remarks>
    /// <param name="stream">数据流，一般为网络流</param>
    /// <param name="data">初始数据包，可为空</param>
    /// <param name="bufferSize">追加读取缓冲区建议大小</param>
    public SpanReader(Stream stream, IPacket? data = null, Int32 bufferSize = 8192)
    {
        _stream = stream;
        _bufferSize = bufferSize;

        if (data != null)
        {
            _data = data;
            _span = data.GetSpan();
            _total = data.Total;
        }
    }
    #endregion

    #region 基础方法
    /// <summary>告知已消耗 <paramref name="count"/> 字节数据。</summary>
    public void Advance(Int32 count)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        if (count > 0) EnsureSpace(count);
        if (_index + count > _span.Length) throw new ArgumentOutOfRangeException(nameof(count));
        _index += count;
    }

    /// <summary>返回剩余可读数据片段（只读）。</summary>
    /// <param name="sizeHint">期望的最小大小提示。如果剩余空间小于该值则抛出异常</param>
    /// <returns>当前位置到末尾的只读字节片段</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="sizeHint"/> 大于剩余可读字节数时抛出</exception>
    public ReadOnlySpan<Byte> GetSpan(Int32 sizeHint = 0)
    {
        if (sizeHint > FreeCapacity) throw new ArgumentOutOfRangeException(nameof(sizeHint));
        return _span[_index..];
    }
    #endregion

    #region 读取方法
    /// <summary>确保缓冲区中有足够的空间，必要时从底层流追加读取。</summary>
    /// <param name="size">需要的字节数</param>
    /// <exception cref="InvalidOperationException">数据不足，且无法从流中补齐</exception>
    public void EnsureSpace(Int32 size)
    {
        // 检查剩余空间大小，不足时再从数据流中读取。创建新的 OwnerPacket 后，
        // 先把之前剩余的未读数据拷贝到新缓冲的前部，避免丢失，再从流中读取补齐。
        var remain = FreeCapacity;
        if (remain < size && _stream != null)
        {
            // 申请新的数据块：至少满足 size，且考虑 bufferSize / MaxCapacity。
            var idx = 0;
            var bsize = size;
            if (MaxCapacity > 0)
            {
                if (bsize < _bufferSize) bsize = _bufferSize;
                if (bsize > MaxCapacity - _total) bsize = MaxCapacity - _total;
            }
            var pk = new OwnerPacket(bsize);

            // 把剩余未读数据拷贝到新数据块前部，避免丢失
            if (_data != null && remain > 0)
            {
                if (!_data.TryGetArray(out var arr)) throw new NotSupportedException();

                arr.AsSpan(_index, remain).CopyTo(pk.Buffer);
                idx += remain;
            }

            _data.TryDispose();
            _data = pk;
            _index = 0; // 重置索引，后续直接从新缓冲读取

            // 直接读取指定大小，必要时抛异常，防止阻塞等待不确定长度数据
            _stream.ReadExactly(pk.Buffer, pk.Offset + idx, pk.Length - idx);
            idx = pk.Length;
            if (idx < size) throw new InvalidOperationException("Not enough data to read.");
            pk.Resize(idx);

            _span = pk.GetSpan();
            _total += idx - remain;
        }

        if (_index + size > _span.Length) throw new InvalidOperationException("Not enough data to read.");
    }

    /// <summary>读取单个字节</summary>
    public Byte ReadByte()
    {
        var size = sizeof(Byte);
        EnsureSpace(size);
        var result = _span[_index];
        _index += size;
        return result;
    }

    /// <summary>读取Int16整数</summary>
    public Int16 ReadInt16()
    {
        var size = sizeof(Int16);
        EnsureSpace(size);
        var result = IsLittleEndian ?
            BinaryPrimitives.ReadInt16LittleEndian(_span.Slice(_index, size)) :
            BinaryPrimitives.ReadInt16BigEndian(_span.Slice(_index, size));
        _index += size;
        return result;
    }

    /// <summary>读取UInt16整数</summary>
    public UInt16 ReadUInt16()
    {
        var size = sizeof(UInt16);
        EnsureSpace(size);
        var result = IsLittleEndian ?
            BinaryPrimitives.ReadUInt16LittleEndian(_span.Slice(_index, size)) :
            BinaryPrimitives.ReadUInt16BigEndian(_span.Slice(_index, size));
        _index += size;
        return result;
    }

    /// <summary>读取Int32整数</summary>
    public Int32 ReadInt32()
    {
        var size = sizeof(Int32);
        EnsureSpace(size);
        var result = IsLittleEndian ?
            BinaryPrimitives.ReadInt32LittleEndian(_span.Slice(_index, size)) :
            BinaryPrimitives.ReadInt32BigEndian(_span.Slice(_index, size));
        _index += size;
        return result;
    }

    /// <summary>读取UInt32整数</summary>
    public UInt32 ReadUInt32()
    {
        var size = sizeof(UInt32);
        EnsureSpace(size);
        var result = IsLittleEndian ?
            BinaryPrimitives.ReadUInt32LittleEndian(_span.Slice(_index, size)) :
            BinaryPrimitives.ReadUInt32BigEndian(_span.Slice(_index, size));
        _index += size;
        return result;
    }

    /// <summary>读取Int64整数</summary>
    public Int64 ReadInt64()
    {
        var size = sizeof(Int64);
        EnsureSpace(size);
        var result = IsLittleEndian ?
            BinaryPrimitives.ReadInt64LittleEndian(_span.Slice(_index, size)) :
            BinaryPrimitives.ReadInt64BigEndian(_span.Slice(_index, size));
        _index += size;
        return result;
    }

    /// <summary>读取UInt64整数</summary>
    public UInt64 ReadUInt64()
    {
        var size = sizeof(UInt64);
        EnsureSpace(size);
        var result = IsLittleEndian ?
            BinaryPrimitives.ReadUInt64LittleEndian(_span.Slice(_index, size)) :
            BinaryPrimitives.ReadUInt64BigEndian(_span.Slice(_index, size));
        _index += size;
        return result;
    }

    /// <summary>读取单精度浮点数</summary>
    public unsafe Single ReadSingle()
    {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP
        return BitConverter.Int32BitsToSingle(ReadInt32());
#else
        var result = ReadInt32();
        return Unsafe.ReadUnaligned<Single>(ref Unsafe.As<Int32, Byte>(ref result));
#endif
    }

    /// <summary>读取双精度浮点数</summary>
    public Double ReadDouble()
    {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP
        return BitConverter.Int64BitsToDouble(ReadInt64());
#else
        var result = ReadInt64();
        return Unsafe.ReadUnaligned<Double>(ref Unsafe.As<Int64, Byte>(ref result));
#endif
    }

    /// <summary>读取字符串。支持定长、读取全部与 7 位压缩长度前缀。</summary>
    /// <param name="length">需要读取的长度。-1 读取剩余全部；0 读取 7 位压缩长度前缀；&gt;0 定长</param>
    /// <param name="encoding">编码，默认 UTF8</param>
    public String ReadString(Int32 length = 0, Encoding? encoding = null)
    {
        if (length < 0)
            length = _span.Length - _index;
        else if (length == 0)
            length = ReadEncodedInt();
        if (length == 0) return String.Empty;

        EnsureSpace(length);

        encoding ??= Encoding.UTF8;

        var result = encoding.GetString(_span.Slice(_index, length));
        _index += length;
        return result;
    }

    /// <summary>读取字节数组片段</summary>
    public ReadOnlySpan<Byte> ReadBytes(Int32 length)
    {
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
        EnsureSpace(length);

        var result = _span.Slice(_index, length);
        _index += length;
        return result;
    }

    /// <summary>读取到目标 Span。</summary>
    public Int32 Read(Span<Byte> data)
    {
        var length = data.Length;
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(data));
        EnsureSpace(length);

        var result = _span.Slice(_index, length);
        result.CopyTo(data);
        _index += length;
        return length;
    }

    /// <summary>
    /// 读取数据包（对内部数据切片，不复制）。
    /// </summary>
    /// <remarks>
    /// 本方法直接在底层 <see cref="IPacket"/> 上进行切片并返回新数据包，避免任何数据拷贝；
    /// 为了支持跨段（链式）数据包的零拷贝读取，此处不调用 <see cref="EnsureSpace(int)"/>，
    /// 否则当当前 <see cref="Span"/> 不足而总长度足够时也会错误抛出异常（例如 WebSocket 头部在首段、负载在 Next 段）。
    /// 注意：该方法不触发从流追加读取，不能与基于流扩容的模式混用；若 <paramref name="length"/> 超出剩余总长度，将由底层 <c>IPacket.Slice</c> 抛出异常。
    /// </remarks>
    public IPacket ReadPacket(Int32 length)
    {
        if (_data == null) throw new InvalidOperationException("No data stream to read!");
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));

        // 不要在这里调用 EnsureSpace(length)。
        // 这里需要支持跨段 Packet 的切片，如果仅依据当前 _span 校验，
        // 在首段仅包含头部、负载在 Next 段的场景（如 WebSocket 数据包）会被误判为数据不足。
        // 由 IPacket.Slice 自行根据总长度进行边界检查并抛出异常。
        //EnsureSpace(length);

        var result = _data.Slice(_index, length);
        _index += length;
        return result;
    }

    /// <summary>读取结构体（按内存布局直接反序列化）。</summary>
    public T Read<T>() where T : struct
    {
        var size = Unsafe.SizeOf<T>();
        EnsureSpace(size);

        var result = MemoryMarshal.Read<T>(_span.Slice(_index));
        _index += size;
        return result;
    }
    #endregion

    #region 扩展读取
    /// <summary>以 7 位压缩格式读取 32 位整数。</summary>
    public Int32 ReadEncodedInt()
    {
        UInt32 rs = 0;
        Byte n = 0;
        while (true)
        {
            var b = ReadByte();

            // 必须转为 UInt32，否则可能溢出
            rs |= (UInt32)((b & 0x7f) << n);
            if ((b & 0x80) == 0) break;

            n += 7;
            if (n >= 32) throw new FormatException("The number value is too large to read in compressed format!");
        }
        return (Int32)rs;
    }
    #endregion
}
