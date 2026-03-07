using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using NewLife.Collections;
using NewLife.Data;

namespace NewLife.Buffers;

/// <summary>Span写入器</summary>
/// <remarks>
/// 引用结构，面向高性能无 GC 写入。支持写入基础类型、结构体以及 7 位压缩整数、长度前缀字符串等。
/// 支持后备 <see cref="Stream"/>，当缓冲区不足时自动 Flush 到流中，适用于序列化未知大小的数据。
/// </remarks>
public ref struct SpanWriter
{
    #region 属性
    private Span<Byte> _span;
    /// <summary>数据片段</summary>
    public readonly Span<Byte> Span => _span;

    private Int32 _index;
    /// <summary>已写入字节数</summary>
    public Int32 Position { readonly get => _index; set => _index = value; }

    /// <summary>总容量</summary>
    public readonly Int32 Capacity => _span.Length;

    /// <summary>空闲容量</summary>
    public readonly Int32 FreeCapacity => _span.Length - _index;

    /// <summary>已写入数据</summary>
    public readonly ReadOnlySpan<Byte> WrittenSpan => _span[.._index];

    /// <summary>已写入长度</summary>
    public readonly Int32 WrittenCount => _index;

    /// <summary>是否小端字节序。默认true</summary>
    public Boolean IsLittleEndian { get; set; } = true;
    #endregion

    #region 构造
    /// <summary>实例化Span写入器</summary>
    /// <param name="buffer">目标缓冲区</param>
    public SpanWriter(Span<Byte> buffer) => _span = buffer;

    /// <summary>实例化Span写入器</summary>
    /// <param name="data">数据包</param>
    public SpanWriter(IPacket data) : this(data.GetSpan()) { }

    /// <summary>实例化Span写入器，从字节数组创建</summary>
    /// <param name="buffer">字节数组</param>
    /// <param name="offset">起始偏移量</param>
    /// <param name="count">长度，-1表示从offset到数组末尾</param>
    public SpanWriter(Byte[] buffer, Int32 offset = 0, Int32 count = -1) : this(new Span<Byte>(buffer, offset, count < 0 ? buffer.Length - offset : count)) { }
    #endregion

    #region 流模式
    private Stream? _stream;
    private Int32 _total;

    /// <summary>已写入的总字节数（含已 Flush 到流中的数据）。非流模式下等同于 <see cref="WrittenCount"/></summary>
    public readonly Int32 TotalWritten => _total + _index;

    /// <summary>实例化Span写入器，支持自动 Flush 到流</summary>
    /// <remarks>
    /// 调用方提供缓冲区和后备数据流。写入数据时先写入缓冲区，缓冲区满时自动 Flush 到流并重置写入位置。
    /// 若 <see cref="TotalWritten"/> 不超过缓冲区容量，说明数据全在缓冲区中，流中无数据。
    /// 缓冲区和流的生命周期均由调用方管理。
    /// </remarks>
    /// <param name="span">写入缓冲区</param>
    /// <param name="stream">后备数据流，缓冲区满时数据刷入此流</param>
    public SpanWriter(Span<Byte> span, Stream stream)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));

        _span = span;
        _stream = stream;
    }

    /// <summary>将缓冲区中已写入的数据刷入底层流，并重置写入位置。非流模式下无操作</summary>
    public void Flush()
    {
        if (_stream == null || _index <= 0) return;

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP
        _stream.Write(_span[.._index]);
#else
        _stream.Write(_span[.._index].ToArray(), 0, _index);
#endif
        _total += _index;
        _index = 0;
    }

    /// <summary>释放资源。Flush 剩余数据到流</summary>
    public void Dispose()
    {
        Flush();
        _stream = null;
    }
    #endregion

    #region 基础方法
    /// <summary>告知有多少数据已写入缓冲区</summary>
    /// <param name="count">要前进的字节数</param>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="count"/> 超出可写范围时</exception>
    public void Advance(Int32 count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");
        if (_index + count > _span.Length)
            throw new ArgumentOutOfRangeException(nameof(count), "Exceeds buffer capacity.");

        _index += count;
    }

    /// <summary>返回要写入到的 Span，其大小按 <paramref name="sizeHint"/> 指定至少为所请求的大小</summary>
    /// <param name="sizeHint">期望的最小大小提示。流模式下不足时自动 Flush；非流模式不足时抛异常</param>
    /// <returns>当前位置到末尾的可写字节片段</returns>
    /// <exception cref="InvalidOperationException">非流模式下剩余空间不足时</exception>
    public Span<Byte> GetSpan(Int32 sizeHint = 0)
    {
        if (sizeHint > 0) EnsureSpace(sizeHint);

        return _span[_index..];
    }
    #endregion

    #region 写入方法
    /// <summary>确保缓冲区中有足够的空间。流模式下空间不足时自动 Flush 并可能扩容</summary>
    /// <param name="size">需要的字节数</param>
    /// <exception cref="InvalidOperationException">空间不足且无法扩容时</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureSpace(Int32 size)
    {
        if (_index + size > _span.Length)
            FlushAndGrow(size);
    }

    /// <summary>Flush 当前数据到流</summary>
    /// <param name="size">本次写入需要的字节数</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void FlushAndGrow(Int32 size)
    {
        if (_stream == null)
            throw new InvalidOperationException($"Not enough space to write {size} bytes. Available: {FreeCapacity}");

        // 先把已写入数据刷入流
        Flush();

        // Flush 后 _index=0，若单次写入仍超过缓冲区则抛异常
        if (size > _span.Length)
            throw new InvalidOperationException($"Single write of {size} bytes exceeds buffer capacity {_span.Length}. Use Write(ReadOnlySpan<Byte>) for large data.");
    }

    /// <summary>写入字节</summary>
    /// <param name="value">要写入的字节值</param>
    /// <returns>写入的字节数（固定为1）</returns>
    public Int32 WriteByte(Int32 value) => Write((Byte)value);

    /// <summary>写入字节</summary>
    /// <param name="value">要写入的字节值</param>
    /// <returns>写入的字节数（固定为1）</returns>
    public Int32 Write(Byte value)
    {
        const Int32 size = sizeof(Byte);
        EnsureSpace(size);
        _span[_index] = value;
        _index += size;
        return size;
    }

    /// <summary>写入 16 位整数</summary>
    /// <param name="value">要写入的整数值</param>
    /// <returns>写入的字节数（固定为2）</returns>
    public Int32 Write(Int16 value)
    {
        const Int32 size = sizeof(Int16);
        EnsureSpace(size);
        if (IsLittleEndian)
            BinaryPrimitives.WriteInt16LittleEndian(_span[_index..], value);
        else
            BinaryPrimitives.WriteInt16BigEndian(_span[_index..], value);
        _index += size;
        return size;
    }

    /// <summary>写入无符号 16 位整数</summary>
    /// <param name="value">要写入的无符号整数值</param>
    /// <returns>写入的字节数（固定为2）</returns>
    public Int32 Write(UInt16 value)
    {
        const Int32 size = sizeof(UInt16);
        EnsureSpace(size);
        if (IsLittleEndian)
            BinaryPrimitives.WriteUInt16LittleEndian(_span[_index..], value);
        else
            BinaryPrimitives.WriteUInt16BigEndian(_span[_index..], value);
        _index += size;
        return size;
    }

    /// <summary>写入 32 位整数</summary>
    /// <param name="value">要写入的整数值</param>
    /// <returns>写入的字节数（固定为4）</returns>
    public Int32 Write(Int32 value)
    {
        const Int32 size = sizeof(Int32);
        EnsureSpace(size);
        if (IsLittleEndian)
            BinaryPrimitives.WriteInt32LittleEndian(_span[_index..], value);
        else
            BinaryPrimitives.WriteInt32BigEndian(_span[_index..], value);
        _index += size;
        return size;
    }

    /// <summary>写入无符号 32 位整数</summary>
    /// <param name="value">要写入的无符号整数值</param>
    /// <returns>写入的字节数（固定为4）</returns>
    public Int32 Write(UInt32 value)
    {
        const Int32 size = sizeof(UInt32);
        EnsureSpace(size);
        if (IsLittleEndian)
            BinaryPrimitives.WriteUInt32LittleEndian(_span[_index..], value);
        else
            BinaryPrimitives.WriteUInt32BigEndian(_span[_index..], value);
        _index += size;
        return size;
    }

    /// <summary>写入 64 位整数</summary>
    /// <param name="value">要写入的整数值</param>
    /// <returns>写入的字节数（固定为8）</returns>
    public Int32 Write(Int64 value)
    {
        const Int32 size = sizeof(Int64);
        EnsureSpace(size);
        if (IsLittleEndian)
            BinaryPrimitives.WriteInt64LittleEndian(_span[_index..], value);
        else
            BinaryPrimitives.WriteInt64BigEndian(_span[_index..], value);
        _index += size;
        return size;
    }

    /// <summary>写入无符号 64 位整数</summary>
    /// <param name="value">要写入的无符号整数值</param>
    /// <returns>写入的字节数（固定为8）</returns>
    public Int32 Write(UInt64 value)
    {
        const Int32 size = sizeof(UInt64);
        EnsureSpace(size);
        if (IsLittleEndian)
            BinaryPrimitives.WriteUInt64LittleEndian(_span[_index..], value);
        else
            BinaryPrimitives.WriteUInt64BigEndian(_span[_index..], value);
        _index += size;
        return size;
    }

    /// <summary>写入单精度浮点数</summary>
    /// <param name="value">要写入的浮点值</param>
    /// <returns>写入的字节数（固定为4）</returns>
    public unsafe Int32 Write(Single value)
    {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP
        return Write(BitConverter.SingleToInt32Bits(value));
#else
        return Write(*(Int32*)&value);
#endif
    }

    /// <summary>写入双精度浮点数</summary>
    /// <param name="value">要写入的浮点值</param>
    /// <returns>写入的字节数（固定为8）</returns>
    public unsafe Int32 Write(Double value)
    {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP
        return Write(BitConverter.DoubleToInt64Bits(value));
#else
        return Write(*(Int64*)&value);
#endif
    }

    /// <summary>写入字符串。支持定长、全部和长度前缀</summary>
    /// <param name="value">要写入的字符串</param>
    /// <param name="length">最大长度（字节）。-1: 全部；0: 先写入 7 位压缩长度；&gt;0: 固定长度（不足填0，超长截断）</param>
    /// <param name="encoding">编码，默认UTF8</param>
    /// <returns>写入字节数（含头部）</returns>
    public Int32 Write(String? value, Int32 length = 0, Encoding? encoding = null)
    {
        var p = _index;
        encoding ??= Encoding.UTF8;

        if (value.IsNullOrEmpty())
        {
            if (length == 0)
                WriteEncodedInt(0);
            else if (length > 0)
            {
                EnsureSpace(length);
                _span.Slice(_index, length).Clear();
                _index += length;
            }
            return _index - p;
        }

        return length switch
        {
            < 0 => WriteStringAll(value, encoding, p),
            0 => WriteStringWithLength(value, encoding, p),
            _ => WriteStringFixed(value, length, encoding, p)
        };
    }

    /// <summary>写入字符串全部内容</summary>
    /// <param name="value">非空字符串</param>
    /// <param name="encoding">编码</param>
    /// <param name="startPos">起始位置</param>
    /// <returns>写入的字节数</returns>
    private Int32 WriteStringAll(String value, Encoding encoding, Int32 startPos)
    {
        var byteCount = encoding.GetByteCount(value);
        EnsureSpace(byteCount);

        var count = encoding.GetBytes(value.AsSpan(), _span[_index..]);
        _index += count;

        return _index - startPos;
    }

    /// <summary>写入带长度前缀的字符串</summary>
    /// <param name="value">非空字符串</param>
    /// <param name="encoding">编码</param>
    /// <param name="startPos">起始位置</param>
    /// <returns>写入的字节数</returns>
    private Int32 WriteStringWithLength(String value, Encoding encoding, Int32 startPos)
    {
        var byteCount = encoding.GetByteCount(value);
        WriteEncodedInt(byteCount);
        EnsureSpace(byteCount);

        var count = encoding.GetBytes(value.AsSpan(), _span[_index..]);
        _index += count;

        return _index - startPos;
    }

    /// <summary>写入固定长度字符串</summary>
    /// <param name="value">非空字符串</param>
    /// <param name="length">固定长度</param>
    /// <param name="encoding">编码</param>
    /// <param name="startPos">起始位置</param>
    /// <returns>写入的字节数</returns>
    private Int32 WriteStringFixed(String value, Int32 length, Encoding encoding, Int32 startPos)
    {
        var span = GetSpan(length);
        if (span.Length > length) span = span[..length];

        var source = value.AsSpan();
        var need = encoding.GetByteCount(value);
        if (need <= length)
        {
            var count = encoding.GetBytes(source, span);
            if (count < length) span[count..length].Clear();
        }
        else
        {
            // 编码结果超过目标长度：先编码到临时缓冲，拷贝前 length 个字节
            var buf = Pool.Shared.Rent(need);
            try
            {
                var count = encoding.GetBytes(source, buf);
                new ReadOnlySpan<Byte>(buf, 0, length).CopyTo(span);
            }
            finally
            {
                Pool.Shared.Return(buf);
            }
        }

        _index += length;
        return length;
    }

    /// <summary>写入字节数组</summary>
    /// <param name="value">要写入的数据</param>
    /// <returns>写入的字节数</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="value"/> 为 null 时</exception>
    public Int32 Write(Byte[]? value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        return Write((ReadOnlySpan<Byte>)value);
    }

    /// <summary>写入Span。流模式下支持超过缓冲区的大数据分块写入</summary>
    /// <param name="span">要写入的数据</param>
    /// <returns>写入的字节数</returns>
    public Int32 Write(ReadOnlySpan<Byte> span)
    {
        // 流模式下，大数据分块通过缓冲区写入
        if (_stream != null && span.Length > FreeCapacity)
        {
            var remaining = span;
            while (remaining.Length > 0)
            {
                if (FreeCapacity <= 0) Flush();
                var n = Math.Min(remaining.Length, FreeCapacity);
                remaining[..n].CopyTo(_span[_index..]);
                _index += n;
                remaining = remaining[n..];
            }
            return span.Length;
        }

        EnsureSpace(span.Length);
        span.CopyTo(_span[_index..]);
        _index += span.Length;

        return span.Length;
    }

    /// <summary>写入Span</summary>
    /// <param name="span">要写入的数据</param>
    /// <returns>写入的字节数</returns>
    public Int32 Write(Span<Byte> span) => Write((ReadOnlySpan<Byte>)span);

    /// <summary>写入数据包。遍历链式包，逐段写入</summary>
    /// <param name="value">要写入的数据包</param>
    /// <returns>写入的字节数</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="value"/> 为 null 时</exception>
    public Int32 Write(IPacket value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));

        var total = 0;
        for (var p = value; p != null; p = p.Next)
        {
            total += Write(p.GetSpan());
        }

        return total;
    }

    /// <summary>写入结构体</summary>
    /// <typeparam name="T">结构体类型</typeparam>
    /// <param name="value">要写入的值</param>
    /// <returns>写入的字节数</returns>
    public Int32 Write<T>(T value) where T : struct
    {
        var size = Unsafe.SizeOf<T>();
        EnsureSpace(size);
#if NET8_0_OR_GREATER
        MemoryMarshal.Write(_span.Slice(_index, size), in value);
#else
        MemoryMarshal.Write(_span.Slice(_index, size), ref value);
#endif
        _index += size;
        return size;
    }
    #endregion

    #region 扩展写入
    /// <summary>写入 7 位压缩编码的 32 位整数</summary>
    /// <remarks>
    /// 以 7 位压缩格式写入 32 位整数，小于 7 位用 1 字节，小于 14 位用 2 字节。
    /// 每个字节高位表示后续是否还有数据，低 7 位存储数值。
    /// 兼容 <see cref="SpanReader.ReadEncodedInt"/>。
    /// </remarks>
    /// <param name="value">数值（可为负数，内部按无符号位模式编码）</param>
    /// <returns>实际写入字节数</returns>
    public Int32 WriteEncodedInt(Int32 value)
    {
        // 7 位压缩编码最多 5 字节
        EnsureSpace(5);
        var span = _span[_index..];
        var count = 0;
        var num = (UInt32)value; // 与 BinaryWriter.Write7BitEncodedInt 一致，允许负数（将占 5 字节）

        while (num >= 0x80)
        {
            span[count++] = (Byte)(num | 0x80);
            num >>= 7;
        }
        span[count++] = (Byte)num;

        _index += count;
        return count;
    }

    /// <summary>填充指定字节值</summary>
    /// <param name="value">要填充的字节值</param>
    /// <param name="count">填充次数</param>
    /// <returns>写入的字节数</returns>
    public Int32 Fill(Byte value, Int32 count)
    {
        if (count <= 0) return 0;

        EnsureSpace(count);
        _span.Slice(_index, count).Fill(value);
        _index += count;

        return count;
    }

    /// <summary>填充零字节</summary>
    /// <param name="count">填充次数</param>
    /// <returns>写入的字节数</returns>
    public Int32 FillZero(Int32 count)
    {
        if (count <= 0) return 0;

        EnsureSpace(count);
        _span.Slice(_index, count).Clear();
        _index += count;

        return count;
    }

    /// <summary>重复写入数据片段</summary>
    /// <param name="data">要重复写入的数据</param>
    /// <param name="repeat">重复次数</param>
    /// <returns>写入的总字节数</returns>
    public Int32 WriteRepeat(ReadOnlySpan<Byte> data, Int32 repeat)
    {
        if (repeat <= 0 || data.IsEmpty) return 0;

        var total = data.Length * repeat;
        EnsureSpace(total);

        for (var i = 0; i < repeat; i++)
        {
            data.CopyTo(_span.Slice(_index));
            _index += data.Length;
        }

        return total;
    }
    #endregion
}
