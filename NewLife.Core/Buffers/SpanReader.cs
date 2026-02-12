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
/// 设计目标：在已有 <see cref="ReadOnlySpan{T}"/> / <see cref="IPacket"/> 基础上提供统一顺序读取 API，必要时按需增量拉取后续字节。
/// </remarks>
public ref struct SpanReader
{
    #region 属性
    private ReadOnlySpan<Byte> _span;
    /// <summary>数据片段</summary>
    public readonly ReadOnlySpan<Byte> Span => _span;

    private Int32 _index;
    /// <summary>已读取字节数（相对当前 <see cref="Span"/> 起始）</summary>
    public Int32 Position { readonly get => _index; set => _index = value; }

    /// <summary>当前缓冲总容量（不代表完整数据总长度，若基于流扩容仅表示当前已缓存区大小）</summary>
    public readonly Int32 Capacity => _span.Length;

    /// <summary>空闲容量（尚未读取的剩余字节数）</summary>
    [Obsolete("=>Available")]
    public readonly Int32 FreeCapacity => _span.Length - _index;

    /// <summary>空闲容量（尚未读取的剩余字节数）</summary>
    public readonly Int32 Available => _span.Length - _index;

    /// <summary>是否小端字节序。默认 true</summary>
    public Boolean IsLittleEndian { get; set; } = true;
    #endregion

    #region 构造
    /// <summary>实例化读取器，直接包裹只读跨度，不会拷贝</summary>
    /// <param name="span">数据</param>
    public SpanReader(ReadOnlySpan<Byte> span) => _span = span;

    /// <summary>实例化读取器，直接包裹可写跨度（按只读处理）</summary>
    /// <param name="span">数据</param>
    public SpanReader(Span<Byte> span) => _span = span;

    /// <summary>实例化读取器，基于数据包。链式数据包同时保留 <see cref="IPacket"/> 以支持 <see cref="ReadPacket"/> 零拷贝；必要时退化为流增量模式。</summary>
    /// <param name="data">初始数据包</param>
    public SpanReader(IPacket data)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));

        // 如果有后续数据包，说明是链式数据包，必须通过流读取
        // 链式数据包：为了兼容跨段后续读取（读取原始 span 内部结构体/整数等可能需要更多字节），
        // 这里仍然提供统一的流式补齐能力——通过把链式包聚合为内存流。
        // 注意：仅当后续调用 EnsureSpace 时才会真正触发复制；仅做一次性“可选”降级。
        if (data.Next != null)
        {
            _stream = data.GetStream(false);
            _bufferSize = 8192;
        }
        else
        {
            _data = data;
            _span = data.GetSpan();
            _total = data.Total;
        }
    }

    /// <summary>实例化读取器，从字节数组创建</summary>
    /// <param name="buffer">字节数组</param>
    /// <param name="offset">起始偏移量</param>
    /// <param name="count">长度，-1表示从offset到数组末尾</param>
    public SpanReader(Byte[] buffer, Int32 offset = 0, Int32 count = -1) : this(new ReadOnlySpan<Byte>(buffer, offset, count < 0 ? buffer.Length - offset : count)) { }
    #endregion

    #region 扩容增强
    /// <summary>最大容量。多次从数据流读取数据时，受限于此最大值（0 表示不限制）</summary>
    public Int32 MaxCapacity { get; set; }

    private Stream? _stream;
    private readonly Int32 _bufferSize;
    // 当前缓存（或原始）数据包，仅用于 ReadPacket 以及流扩容缓存承载
    private IPacket? _data;
    // 已成功读取/缓存的总字节数（用于 MaxCapacity 计算）
    private Int32 _total;

    /// <summary>实例化读取器，支持后续从流追加读取（突破初始大小限制）</summary>
    /// <remarks>
    /// 解析网络协议时，数据帧可能超过初始缓冲区大小。提供 <paramref name="stream"/> 后，
    /// 当剩余可读字节不足时，会自动从流中读取一批数据并扩充内部缓冲区。
    /// </remarks>
    /// <param name="stream">底层数据流，一般为网络流</param>
    /// <param name="data">初始数据包，可为空（例如已经到达的响应头）</param>
    /// <param name="bufferSize">每次追加读取建议大小（最小分块）</param>
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
    /// <summary>告知已消耗指定字节</summary>
    /// <param name="count">要消耗的字节数</param>
    /// <exception cref="ArgumentOutOfRangeException">count &lt; 0 或超出当前剩余</exception>
    public void Advance(Int32 count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");
        if (count > 0) EnsureSpace(count);
        if (_index + count > _span.Length)
            throw new ArgumentOutOfRangeException(nameof(count), "Exceeds available data.");
        _index += count;
    }

    /// <summary>返回剩余可读数据片段（只读）</summary>
    /// <param name="sizeHint">期望的最小大小提示。如果剩余空间小于该值则抛出异常</param>
    /// <returns>当前位置到末尾的只读字节片段</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="sizeHint"/> 大于剩余可读字节数时</exception>
    public readonly ReadOnlySpan<Byte> GetSpan(Int32 sizeHint = 0)
    {
        if (_index + sizeHint > _span.Length)
            throw new ArgumentOutOfRangeException(nameof(sizeHint), "Size hint exceeds free capacity.");
        return _span[_index..];
    }
    #endregion

    #region 读取方法
    /// <summary>确保缓冲区中有足够的可读取字节。若不足：
    /// <list type="number">
    /// <item>存在底层流 → 追加读取并重组内部缓冲</item>
    /// <item>无流但为单段数据 → 抛出异常</item>
    /// <item>无流且链式数据包（多段）→ 当前版本仍抛出（仅 <see cref="ReadPacket"/> 支持跨段零拷贝）</item>
    /// </list>
    /// </summary>
    /// <param name="size">需要的字节数</param>
    /// <exception cref="InvalidOperationException">数据不足且无法补齐</exception>
    public void EnsureSpace(Int32 size)
    {
        // 检查剩余空间大小，不足时再从数据流中读取。创建新的 OwnerPacket 后，
        // 先把之前剩余的未读数据拷贝到新缓冲的前部，避免丢失，再从流中读取补齐。
        if (size <= 0) return;

        var remain = Available;
        if (remain >= size) return;

        if (_stream != null)
        {
            // 申请新的数据块：至少满足 size，且考虑 bufferSize / MaxCapacity
            var bsize = size;
            if (bsize < _bufferSize) bsize = _bufferSize;
            if (MaxCapacity > 0 && bsize > MaxCapacity - _total) bsize = MaxCapacity - _total;
            if (remain + bsize < size) throw new InvalidOperationException();

            var pk = new OwnerPacket(bsize);

            // 把剩余未读数据拷贝到新数据块前部，避免丢失
            var available = 0;
            var old = _data;
            if (old != null && remain > 0)
            {
                if (!old.TryGetArray(out var arr))
                    throw new NotSupportedException("Data packet does not support array access.");

                arr.AsSpan(_index, remain).CopyTo(pk.Buffer);
                available += remain;
            }

            old.TryDispose();
            _data = pk;
            _index = 0; // 重置索引，后续直接从新缓冲读取

            // 直接读取指定大小，必要时抛异常，防止阻塞等待不确定长度数据
            //_stream.ReadExactly(pk.Buffer, pk.Offset + available, pk.Length - available);
            available = _stream.ReadAtLeast(pk.Buffer, pk.Offset + available, pk.Length - available, size - remain, false);
            if (remain + available < size)
                throw new InvalidOperationException($"Not enough data to read. Required: {size}, Available: {available}");
            pk.Resize(remain + available);

            _span = pk.GetSpan();
            _total += pk.Length - remain;
        }

        if (_index + size > _span.Length)
            throw new InvalidOperationException($"Not enough data to read. Required: {size}, Available: {Available}");
    }

    /// <summary>读取单个字节</summary>
    /// <returns>读取的字节值</returns>
    public Byte ReadByte()
    {
        const Int32 size = sizeof(Byte);
        EnsureSpace(size);
        var result = _span[_index];
        _index += size;
        return result;
    }

    /// <summary>读取Int16整数</summary>
    /// <returns>读取的整数值</returns>
    public Int16 ReadInt16()
    {
        const Int32 size = sizeof(Int16);
        EnsureSpace(size);
        var result = IsLittleEndian
            ? BinaryPrimitives.ReadInt16LittleEndian(_span.Slice(_index, size))
            : BinaryPrimitives.ReadInt16BigEndian(_span.Slice(_index, size));
        _index += size;
        return result;
    }

    /// <summary>读取UInt16整数</summary>
    /// <returns>读取的无符号整数值</returns>
    public UInt16 ReadUInt16()
    {
        const Int32 size = sizeof(UInt16);
        EnsureSpace(size);
        var result = IsLittleEndian
            ? BinaryPrimitives.ReadUInt16LittleEndian(_span.Slice(_index, size))
            : BinaryPrimitives.ReadUInt16BigEndian(_span.Slice(_index, size));
        _index += size;
        return result;
    }

    /// <summary>读取Int32整数</summary>
    /// <returns>读取的整数值</returns>
    public Int32 ReadInt32()
    {
        const Int32 size = sizeof(Int32);
        EnsureSpace(size);
        var result = IsLittleEndian
            ? BinaryPrimitives.ReadInt32LittleEndian(_span.Slice(_index, size))
            : BinaryPrimitives.ReadInt32BigEndian(_span.Slice(_index, size));
        _index += size;
        return result;
    }

    /// <summary>读取UInt32整数</summary>
    /// <returns>读取的无符号整数值</returns>
    public UInt32 ReadUInt32()
    {
        const Int32 size = sizeof(UInt32);
        EnsureSpace(size);
        var result = IsLittleEndian
            ? BinaryPrimitives.ReadUInt32LittleEndian(_span.Slice(_index, size))
            : BinaryPrimitives.ReadUInt32BigEndian(_span.Slice(_index, size));
        _index += size;
        return result;
    }

    /// <summary>读取Int64整数</summary>
    /// <returns>读取的长整数值</returns>
    public Int64 ReadInt64()
    {
        const Int32 size = sizeof(Int64);
        EnsureSpace(size);
        var result = IsLittleEndian
            ? BinaryPrimitives.ReadInt64LittleEndian(_span.Slice(_index, size))
            : BinaryPrimitives.ReadInt64BigEndian(_span.Slice(_index, size));
        _index += size;
        return result;
    }

    /// <summary>读取UInt64整数</summary>
    /// <returns>读取的无符号长整数值</returns>
    public UInt64 ReadUInt64()
    {
        const Int32 size = sizeof(UInt64);
        EnsureSpace(size);
        var result = IsLittleEndian
            ? BinaryPrimitives.ReadUInt64LittleEndian(_span.Slice(_index, size))
            : BinaryPrimitives.ReadUInt64BigEndian(_span.Slice(_index, size));
        _index += size;
        return result;
    }

    /// <summary>读取单精度浮点数</summary>
    /// <returns>读取的浮点值</returns>
    public Single ReadSingle()
    {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP
        return BitConverter.Int32BitsToSingle(ReadInt32());
#else
        var result = ReadInt32();
        return Unsafe.ReadUnaligned<Single>(ref Unsafe.As<Int32, Byte>(ref result));
#endif
    }

    /// <summary>读取双精度浮点数</summary>
    /// <returns>读取的双精度浮点值</returns>
    public Double ReadDouble()
    {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP
        return BitConverter.Int64BitsToDouble(ReadInt64());
#else
        var result = ReadInt64();
        return Unsafe.ReadUnaligned<Double>(ref Unsafe.As<Int64, Byte>(ref result));
#endif
    }

    /// <summary>读取字符串。支持定长、读取全部与 7 位压缩长度前缀</summary>
    /// <param name="length">需要读取的长度。-1 读取剩余全部；0 读取 7 位压缩长度前缀；&gt;0 定长</param>
    /// <param name="encoding">编码，默认 UTF8</param>
    /// <returns>解码的字符串</returns>
    public String ReadString(Int32 length = 0, Encoding? encoding = null)
    {
        var actualLength = length switch
        {
            < 0 => _span.Length - _index,
            0 => ReadEncodedInt(),
            _ => length
        };

        if (actualLength == 0) return String.Empty;

        EnsureSpace(actualLength);

        encoding ??= Encoding.UTF8;
        var result = encoding.GetString(_span.Slice(_index, actualLength));
        _index += actualLength;
        return result;
    }

    /// <summary>读取字节数组片段</summary>
    /// <param name="length">要读取的字节数</param>
    /// <returns>只读字节片段</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="length"/> 为负数时</exception>
    public ReadOnlySpan<Byte> ReadBytes(Int32 length)
    {
        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length), "Length cannot be negative.");
        EnsureSpace(length);

        var result = _span.Slice(_index, length);
        _index += length;
        return result;
    }

    /// <summary>读取到目标 Span</summary>
    /// <param name="data">目标缓冲区</param>
    /// <returns>实际读取的字节数</returns>
    public Int32 Read(Span<Byte> data)
    {
        var length = data.Length;
        EnsureSpace(length);

        var result = _span.Slice(_index, length);
        result.CopyTo(data);
        _index += length;
        return length;
    }

    /// <summary>读取数据包（底层切片，不复制）。不触发流扩容。</summary>
    /// <remarks>
    /// 本方法直接在底层 <see cref="IPacket"/> 上进行切片并返回新数据包，避免任何数据拷贝；
    /// </remarks>
    /// <param name="length">要读取的字节数</param>
    /// <returns>数据包切片</returns>
    /// <exception cref="InvalidOperationException">无数据流时</exception>
    /// <exception cref="ArgumentOutOfRangeException">长度为负数时</exception>
    public IPacket ReadPacket(Int32 length)
    {
        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length), "Length cannot be negative.");

        EnsureSpace(length);
        if (_data == null)
            throw new InvalidOperationException("No data packet available for reading.");

        var result = _data.Slice(_index, length);
        _index += length;
        return result;
    }

    /// <summary>读取结构体（按内存布局直接反序列化）</summary>
    /// <typeparam name="T">结构体类型</typeparam>
    /// <returns>反序列化的结构体实例</returns>
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
    /// <summary>以 7 位压缩格式读取 32 位整数</summary>
    /// <returns>解压后的整数值</returns>
    /// <exception cref="FormatException">压缩格式数值过大时</exception>
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
            if (n >= 32)
                throw new FormatException("The number value is too large to read in compressed format!");
        }
        return (Int32)rs;
    }
    #endregion

    #region 预览方法
    /// <summary>预览单个字节，不移动位置</summary>
    /// <returns>当前位置的字节值</returns>
    /// <exception cref="InvalidOperationException">无数据可预览时</exception>
    public readonly Byte PeekByte()
    {
        if (_index >= _span.Length)
            throw new InvalidOperationException("No data available to peek.");
        return _span[_index];
    }

    /// <summary>尝试预览单个字节，不移动位置</summary>
    /// <param name="value">输出的字节值</param>
    /// <returns>是否成功预览</returns>
    public readonly Boolean TryPeekByte(out Byte value)
    {
        if (_index >= _span.Length)
        {
            value = 0;
            return false;
        }
        value = _span[_index];
        return true;
    }

    /// <summary>预览指定长度的字节，不移动位置</summary>
    /// <param name="length">要预览的字节数</param>
    /// <returns>只读字节片段</returns>
    /// <exception cref="InvalidOperationException">剩余数据不足时</exception>
    public readonly ReadOnlySpan<Byte> Peek(Int32 length)
    {
        if (_index + length > _span.Length)
            throw new InvalidOperationException($"Not enough data to peek. Required: {length}, Available: {Available}");
        return _span.Slice(_index, length);
    }

    /// <summary>尝试预览指定长度的字节，不移动位置</summary>
    /// <param name="length">要预览的字节数</param>
    /// <param name="data">输出的字节片段</param>
    /// <returns>是否成功预览</returns>
    public readonly Boolean TryPeek(Int32 length, out ReadOnlySpan<Byte> data)
    {
        if (_index + length > _span.Length)
        {
            data = default;
            return false;
        }
        data = _span.Slice(_index, length);
        return true;
    }
    #endregion
}
