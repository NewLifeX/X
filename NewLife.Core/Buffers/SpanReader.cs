using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace NewLife.Buffers;

/// <summary>Span读取器</summary>
public ref struct SpanReader
{
    #region 属性
    private readonly ReadOnlySpan<Byte> _span;

    private Int32 _index;
    /// <summary>已读取字节数</summary>
    public Int32 Position => _index;

    /// <summary>总容量</summary>
    public Int32 Capacity => _span.Length;

    /// <summary>空闲容量</summary>
    public Int32 FreeCapacity => _span.Length - _index;

    /// <summary>是否小端字节序。默认true</summary>
    public Boolean IsLittleEndian { get; set; } = true;
    #endregion

    #region 构造
    /// <summary>实例化。暂时兼容旧版，后面使用主构造函数</summary>
    /// <param name="span"></param>
    public SpanReader(ReadOnlySpan<Byte> span) => _span = span;

    /// <summary>实例化。暂时兼容旧版，后面删除</summary>
    /// <param name="span"></param>
    public SpanReader(Span<Byte> span) => _span = span;
    #endregion

    #region 基础方法
    /// <summary>告知有多少数据已从缓冲区读取</summary>
    /// <param name="count"></param>
    public void Advance(Int32 count)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        if (_index + count > _span.Length) throw new ArgumentOutOfRangeException(nameof(count));

        _index += count;
    }

    /// <summary>返回要写入到的Span，其大小按 sizeHint 参数指定至少为所请求的大小</summary>
    /// <param name="sizeHint"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public ReadOnlySpan<Byte> GetSpan(Int32 sizeHint = 0)
    {
        if (sizeHint > FreeCapacity) throw new ArgumentOutOfRangeException(nameof(sizeHint));

        return _span[_index..];
    }
    #endregion

    #region 读取方法
    /// <summary>确保缓冲区中有足够的空间。</summary>
    /// <param name="size">需要的字节数。</param>
    /// <exception cref="InvalidOperationException"></exception>
    private void EnsureSpace(Int32 size)
    {
        if (_index + size > _span.Length)
            throw new InvalidOperationException("Not enough data to read.");
    }

    /// <summary>读取单个字节</summary>
    /// <returns></returns>
    public Byte ReadByte()
    {
        var size = sizeof(Byte);
        EnsureSpace(size);
        var result = _span[_index];
        _index += size;
        return result;
    }

    /// <summary>读取Int16整数</summary>
    /// <returns></returns>
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
    /// <returns></returns>
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
    /// <returns></returns>
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
    /// <returns></returns>
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
    /// <returns></returns>
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
    /// <returns></returns>
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
    /// <returns></returns>
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
    /// <returns></returns>
    public Double ReadDouble()
    {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP
        return BitConverter.Int64BitsToDouble(ReadInt64());
#else
        var result = ReadInt64();
        return Unsafe.ReadUnaligned<Double>(ref Unsafe.As<Int64, Byte>(ref result));
#endif
    }

    /// <summary>读取字符串。支持定长、全部和长度前缀</summary>
    /// <param name="length">需要读取的长度。-1表示读取全部，默认0表示读取7位压缩编码整数长度</param>
    /// <param name="encoding">字符串编码，默认UTF8</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
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

    /// <summary>读取字节数组</summary>
    /// <param name="length"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public ReadOnlySpan<Byte> ReadBytes(Int32 length)
    {
        EnsureSpace(length);

        var result = _span.Slice(_index, length);
        _index += length;
        return result;
    }

    /// <summary>读取结构体</summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
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
    /// <summary>以压缩格式读取32位整数</summary>
    /// <returns></returns>
    public Int32 ReadEncodedInt()
    {
        Byte b;
        UInt32 rs = 0;
        Byte n = 0;
        while (true)
        {
            var bt = ReadByte();
            if (bt < 0) throw new Exception($"The data stream is out of range! The integer read is {rs: n0}");
            b = (Byte)bt;

            // 必须转为Int32，否则可能溢出
            rs |= (UInt32)((b & 0x7f) << n);
            if ((b & 0x80) == 0) break;

            n += 7;
            if (n >= 32) throw new FormatException("The number value is too large to read in compressed format!");
        }
        return (Int32)rs;
    }
    #endregion
}
