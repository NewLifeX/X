using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using NewLife.Data;

namespace NewLife.Buffers;

/// <summary>Span读取器</summary>
/// <remarks>
/// 引用结构的Span读取器确保高性能无GC读取。
/// 支持Stream扩展，当数据不足时，自动从数据流中读取，常用于解析Redis/MySql等协议。
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

    /// <summary>实例化Span读取器</summary>
    /// <param name="data"></param>
    public SpanReader(IPacket data)
    {
        _data = data;
        _span = data.GetSpan();
        _total = data.Total;
    }
    #endregion

    #region 扩容增强
    /// <summary>最大容量。多次从数据流读取数据时，受限于此最大值</summary>
    public Int32 MaxCapacity { get; set; }

    private readonly Stream? _stream;
    private readonly Int32 _bufferSize;
    private IPacket? _data;
    private Int32 _total;

    /// <summary>实例化Span读取器。支持从数据流中读取更多数据，突破大小限制</summary>
    /// <remarks>
    /// 解析网络协议时，有时候数据帧较大，超过特定缓冲区大小，导致无法一次性读取完整数据帧。
    /// 加入数据流参数后，在读取数据不足时，SpanReader会自动从数据流中读取一批数据。
    /// </remarks>
    /// <param name="stream">数据流。一般是网络流</param>
    /// <param name="data"></param>
    /// <param name="bufferSize"></param>
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
    public void EnsureSpace(Int32 size)
    {
        // 检查剩余空间大小，不足时，再从数据流中读取。此时需要注意，创建新的OwnerPacket后，需要先把之前剩余的一点数据拷贝过去，然后再读取Stream
        var remain = FreeCapacity;
        if (remain < size && _stream != null)
        {
            // 申请指定大小的数据包缓冲区，至少达到缓冲区大小，但不超过最大容量
            var idx = 0;
            var bsize = size;
            if (bsize < _bufferSize) bsize = _bufferSize;
            if (MaxCapacity > 0 && bsize > MaxCapacity - _total) bsize = MaxCapacity - _total;
            var pk = new OwnerPacket(bsize);
            if (_data != null && remain > 0)
            {
                if (!_data.TryGetArray(out var arr)) throw new NotSupportedException();

                arr.AsSpan(_index, remain).CopyTo(pk.Buffer);
                idx += remain;
            }

            _data.TryDispose();
            _data = pk;
            _index = 0;

            // 多次读取，直到满足需求
            //var n = _stream.ReadExactly(pk.Buffer, pk.Offset + idx, pk.Length - idx);
            while (idx < size)
            {
                // 实际缓冲区大小可能大于申请大小，充分利用缓冲区，避免多次读取
                var len = pk.Buffer.Length - pk.Offset;
                var n = _stream.Read(pk.Buffer, pk.Offset + idx, len - idx);
                if (n <= 0) break;

                idx += n;
            }
            if (idx < size)
                throw new InvalidOperationException("Not enough data to read.");
            pk.Resize(idx);

            _span = pk.GetSpan();
            _total += idx - remain;
        }

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
    public ReadOnlySpan<Byte> ReadBytes(Int32 length)
    {
        EnsureSpace(length);

        var result = _span.Slice(_index, length);
        _index += length;
        return result;
    }

    /// <summary>读取字节数组</summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public Int32 Read(Span<Byte> data)
    {
        var length = data.Length;
        EnsureSpace(length);

        var result = _span.Slice(_index, length);
        result.CopyTo(data);
        _index += length;
        return length;
    }

    /// <summary>读取数据包。直接对内部数据包进行切片</summary>
    /// <param name="length"></param>
    /// <returns></returns>
    public IPacket ReadPacket(Int32 length)
    {
        if (_data == null) throw new InvalidOperationException("No data stream to read!");

        //EnsureSpace(length);

        var result = _data.Slice(_index, length);
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
