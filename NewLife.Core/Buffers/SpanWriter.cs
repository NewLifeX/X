using System.Buffers;
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
/// </remarks>
/// <param name="buffer">目标缓冲区</param>
public ref struct SpanWriter(Span<Byte> buffer)
{
    #region 属性
    private readonly Span<Byte> _span = buffer;
    /// <summary>数据片段</summary>
    public Span<Byte> Span => _span;

    private Int32 _index;
    /// <summary>已写入字节数</summary>
    public Int32 Position { get => _index; set => _index = value; }

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
    public SpanWriter(IPacket data) : this(data.GetSpan()) { }
    #endregion

    #region 基础方法
    /// <summary>告知有多少数据已写入缓冲区</summary>
    /// <param name="count">要前进的字节数</param>
    public void Advance(Int32 count)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        if (_index + count > _span.Length) throw new ArgumentOutOfRangeException(nameof(count));

        _index += count;
    }

    /// <summary>返回要写入到的 Span，其大小按 <paramref name="sizeHint"/> 指定至少为所请求的大小</summary>
    /// <param name="sizeHint">期望的最小大小提示。如果剩余空间小于该值则抛出异常</param>
    /// <returns>当前位置到末尾的可写字节片段</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="sizeHint"/> 大于剩余可写字节数时抛出</exception>
    public readonly Span<Byte> GetSpan(Int32 sizeHint = 0)
    {
        if (sizeHint > FreeCapacity) throw new ArgumentOutOfRangeException(nameof(sizeHint));

        return _span[_index..];
    }
    #endregion

    #region 写入方法
    /// <summary>确保缓冲区中有足够的空间。</summary>
    /// <param name="size">需要的字节数。</param>
    private readonly void EnsureSpace(Int32 size)
    {
        if (_index + size > _span.Length) throw new InvalidOperationException("Not enough space to write.");
    }

    /// <summary>写入字节</summary>
    public Int32 WriteByte(Int32 value) => Write((Byte)value);

    /// <summary>写入字节</summary>
    /// <param name="value">要写入的字节值。</param>
    public Int32 Write(Byte value)
    {
        var size = sizeof(Byte);
        EnsureSpace(size);
        _span[_index] = value;
        _index += size;
        return size;
    }

    /// <summary>写入 16 位整数。</summary>
    /// <param name="value">要写入的整数值。</param>
    public Int32 Write(Int16 value)
    {
        var size = sizeof(Int16);
        EnsureSpace(size);
        if (IsLittleEndian)
            BinaryPrimitives.WriteInt16LittleEndian(_span[_index..], value);
        else
            BinaryPrimitives.WriteInt16BigEndian(_span[_index..], value);
        _index += size;
        return size;
    }

    /// <summary>写入无符号 16 位整数。</summary>
    /// <param name="value">要写入的无符号整数值。</param>
    public Int32 Write(UInt16 value)
    {
        var size = sizeof(UInt16);
        EnsureSpace(size);
        if (IsLittleEndian)
            BinaryPrimitives.WriteUInt16LittleEndian(_span[_index..], value);
        else
            BinaryPrimitives.WriteUInt16BigEndian(_span[_index..], value);
        _index += size;
        return size;
    }

    /// <summary>写入 32 位整数。</summary>
    /// <param name="value">要写入的整数值。</param>
    public Int32 Write(Int32 value)
    {
        var size = sizeof(Int32);
        EnsureSpace(size);
        if (IsLittleEndian)
            BinaryPrimitives.WriteInt32LittleEndian(_span[_index..], value);
        else
            BinaryPrimitives.WriteInt32BigEndian(_span[_index..], value);
        _index += size;
        return size;
    }

    /// <summary>写入无符号 32 位整数。</summary>
    /// <param name="value">要写入的无符号整数值。</param>
    public Int32 Write(UInt32 value)
    {
        var size = sizeof(UInt32);
        EnsureSpace(size);
        if (IsLittleEndian)
            BinaryPrimitives.WriteUInt32LittleEndian(_span[_index..], value);
        else
            BinaryPrimitives.WriteUInt32BigEndian(_span[_index..], value);
        _index += size;
        return size;
    }

    /// <summary>写入 64 位整数。</summary>
    /// <param name="value">要写入的整数值。</param>
    public Int32 Write(Int64 value)
    {
        var size = sizeof(Int64);
        EnsureSpace(size);
        if (IsLittleEndian)
            BinaryPrimitives.WriteInt64LittleEndian(_span[_index..], value);
        else
            BinaryPrimitives.WriteInt64BigEndian(_span[_index..], value);
        _index += size;
        return size;
    }

    /// <summary>写入无符号 64 位整数。</summary>
    /// <param name="value">要写入的无符号整数值。</param>
    public Int32 Write(UInt64 value)
    {
        var size = sizeof(UInt64);
        EnsureSpace(size);
        if (IsLittleEndian)
            BinaryPrimitives.WriteUInt64LittleEndian(_span[_index..], value);
        else
            BinaryPrimitives.WriteUInt64BigEndian(_span[_index..], value);
        _index += size;
        return size;
    }

    /// <summary>写入单精度浮点数。</summary>
    /// <param name="value">要写入的浮点值。</param>
    public unsafe Int32 Write(Single value)
    {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP
        return Write(BitConverter.SingleToInt32Bits(value));
#else
        return Write(*(Int32*)&value);
#endif
    }

    /// <summary>写入双精度浮点数。</summary>
    /// <param name="value">要写入的浮点值。</param>
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
    public Int32 Write(String value, Int32 length = 0, Encoding? encoding = null)
    {
        var p = _index;
        encoding ??= Encoding.UTF8;
        if (length < 0)
        {
            // 写入字符串全部内容：必须确保容量充足，避免静默截断
            var byteCount = encoding.GetByteCount(value);
            EnsureSpace(byteCount);

            var count = encoding.GetBytes(value.AsSpan(), _span[_index..]);
            _index += count;

            return _index - p;
        }
        else if (length == 0)
        {
            // 先写入长度，再写入内容
            if (value.IsNullOrEmpty())
            {
                WriteEncodedInt(0);
                return _index - p;
            }

            length = encoding.GetByteCount(value);
            WriteEncodedInt(length);
            EnsureSpace(length);

            var count = encoding.GetBytes(value.AsSpan(), _span[_index..]);
            _index += count;

            return _index - p;
        }
        else
        {
            // 写入指定长度：不足填充字节 0，超长时截取（按字节截断）
            var span = GetSpan(length);
            if (span.Length > length) span = span[..length];

            var source = value.AsSpan();
            var need = encoding.GetByteCount(value); // 使用 string 重载以兼容旧 TFMs
            if (need <= length)
            {
                var count = encoding.GetBytes(source, span);
                if (count < length) span[count..length].Clear();
            }
            else
            {
                // 编码结果超过目标长度：先编码到临时缓冲，拷贝前 length 个字节
                var buf = Pool.Shared.Rent(need);
                var count = encoding.GetBytes(source, buf);
                new ReadOnlySpan<Byte>(buf, 0, length).CopyTo(span);
                Pool.Shared.Return(buf);
            }

            _index += length;

            return length;
        }
    }

    /// <summary>写入字节数组</summary>
    /// <param name="value">要写入的数据</param>
    /// <returns>写入的字节数</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public Int32 Write(Byte[] value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        EnsureSpace(value.Length);
        value.CopyTo(_span[_index..]);
        _index += value.Length;

        return value.Length;
    }

    /// <summary>写入Span</summary>
    /// <param name="span">要写入的数据</param>
    /// <returns>写入的字节数</returns>
    public Int32 Write(ReadOnlySpan<Byte> span)
    {
        EnsureSpace(span.Length);
        span.CopyTo(_span[_index..]);
        _index += span.Length;

        return span.Length;
    }

    /// <summary>写入Span</summary>
    /// <param name="span">要写入的数据</param>
    /// <returns>写入的字节数</returns>
    public Int32 Write(Span<Byte> span)
    {
        EnsureSpace(span.Length);
        span.CopyTo(_span[_index..]);
        _index += span.Length;

        return span.Length;
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
    /// <summary>写入 7 位压缩编码的 32 位整数。</summary>
    /// <remarks>
    /// 以 7 位压缩格式写入 32 位整数，小于 7 位用 1 字节，小于 14 位用 2 字节。
    /// 每个字节高位表示后续是否还有数据，低 7 位存储数值。
    /// 兼容 <see cref="SpanReader.ReadEncodedInt"/>。
    /// </remarks>
    /// <param name="value">数值（可为负数，内部按无符号位模式编码）</param>
    /// <returns>实际写入字节数</returns>
    public Int32 WriteEncodedInt(Int32 value)
    {
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
    #endregion
}
