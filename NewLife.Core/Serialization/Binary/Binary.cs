﻿using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Reflection;

namespace NewLife.Serialization;

/// <summary>二进制序列化</summary>
public class Binary : FormatterBase, IBinary
{
    #region 属性
    /// <summary>使用7位编码整数。默认false不使用</summary>
    public Boolean EncodeInt { get; set; }

    /// <summary>小端字节序。默认false大端</summary>
    public Boolean IsLittleEndian { get; set; }

    /// <summary>使用指定大小的FieldSizeAttribute特性，默认false</summary>
    public Boolean UseFieldSize { get; set; }

    /// <summary>使用对象引用，默认false</summary>
    public Boolean UseRef { get; set; } = false;

    /// <summary>大小宽度。可选0/1/2/4，默认0表示压缩编码整数</summary>
    public Int32 SizeWidth { get; set; }

    /// <summary>解析字符串时，是否清空两头的0字节，默认false</summary>
    public Boolean TrimZero { get; set; }

    /// <summary>协议版本。用于支持多版本协议序列化，配合FieldSize特性使用。例如JT/T808的2011/2019</summary>
    public String? Version { get; set; }

    /// <summary>使用完整的时间格式。完整格式使用8个字节保存毫秒数，默认false</summary>
    public Boolean FullTime { get; set; }

    /// <summary>要忽略的成员</summary>
    public ICollection<String> IgnoreMembers { get; set; } = [];

    /// <summary>处理器列表</summary>
    public IList<IBinaryHandler> Handlers { get; private set; }

    /// <summary>是否已达到数据流末尾</summary>
    /// <remarks>该类内部与外部均可设置，例如IAccessor中可设置EOF</remarks>
    public Boolean EndOfStream { get; set; }

    /// <summary>总的字节数。读取或写入</summary>
    public Int64 Total { get; set; }
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    public Binary()
    {
        // 遍历所有处理器实现
        var list = new List<IBinaryHandler>
        {
            new BinaryGeneral { Host = this },
            new BinaryNormal { Host = this },
            new BinaryComposite { Host = this },
            new BinaryList { Host = this },
            new BinaryDictionary { Host = this }
        };
        // 根据优先级排序
        Handlers = list.OrderBy(e => e.Priority).ToList();
    }

    /// <summary>实例化</summary>
    public Binary(Stream stream) : this()
    {
        Stream = stream ?? throw new ArgumentNullException(nameof(stream));
    }
    #endregion

    #region 处理器
    /// <summary>添加处理器</summary>
    /// <param name="handler"></param>
    /// <returns></returns>
    public Binary AddHandler(IBinaryHandler handler)
    {
        if (handler != null)
        {
            handler.Host = this;
            Handlers.Add(handler);
            // 根据优先级排序
            Handlers = Handlers.OrderBy(e => e.Priority).ToList();
        }

        return this;
    }

    /// <summary>添加处理器</summary>
    /// <typeparam name="THandler"></typeparam>
    /// <param name="priority"></param>
    /// <returns></returns>
    public Binary AddHandler<THandler>(Int32 priority = 0) where THandler : IBinaryHandler, new()
    {
        var handler = new THandler
        {
            Host = this
        };
        if (priority != 0) handler.Priority = priority;

        return AddHandler(handler);
    }

    /// <summary>获取处理器</summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T? GetHandler<T>() where T : class, IBinaryHandler
    {
        foreach (var item in Handlers)
        {
            if (item is T handler) return handler;
        }

        return default;
    }
    #endregion

    #region 写入
    /// <summary>写入一个对象</summary>
    /// <param name="value">目标对象</param>
    /// <param name="type">类型</param>
    /// <returns></returns>
    public virtual Boolean Write(Object? value, Type? type = null)
    {
        if (type == null)
        {
            if (value == null) return true;

            type = value.GetType();

            // 一般类型为空是顶级调用
            if (Hosts.Count == 0 && Log != null && Log.Enable) WriteLog("BinaryWrite {0} {1}", type.Name, value);
        }

        // 优先 IAccessor 接口
        if (value is IAccessor acc)
        {
            if (acc.Write(Stream, this)) return true;
        }

        foreach (var item in Handlers)
        {
            if (item.Write(value, type)) return true;
        }
        return false;
    }

    /// <summary>写入字节</summary>
    /// <param name="value"></param>
    public virtual void Write(Byte value)
    {
        Stream.WriteByte(value);

        Total++;
    }

    /// <summary>将字节数组部分写入当前流，不写入数组长度。</summary>
    /// <param name="buffer">包含要写入的数据的字节数组。</param>
    /// <param name="offset">buffer 中开始写入的起始点。</param>
    /// <param name="count">要写入的字节数。</param>
    public virtual void Write(Byte[] buffer, Int32 offset, Int32 count)
    {
        if (count < 0) count = buffer.Length - offset;
        Stream.Write(buffer, offset, count);

        Total += count;
    }

    /// <summary>写入数据</summary>
    /// <param name="buffer"></param>
    public virtual void Write(ReadOnlySpan<Byte> buffer)
    {
#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
        Stream.Write(buffer);
#else
        var array = ArrayPool<Byte>.Shared.Rent(buffer.Length);
        try
        {
            buffer.CopyTo(array);

            Stream.Write(array, 0, buffer.Length);
        }
        finally
        {
            ArrayPool<Byte>.Shared.Return(array);
        }
#endif
        Total += buffer.Length;
    }

    /// <summary>写入数据</summary>
    /// <param name="buffer"></param>
    public virtual void Write(ReadOnlyMemory<Byte> buffer)
    {
        if (MemoryMarshal.TryGetArray(buffer, out var segment))
        {
            Stream.Write(segment.Array!, segment.Offset, segment.Count);
            Total += segment.Count;

            return;
        }

        Write(buffer.Span);
    }

    /// <summary>写入大小，如果有FieldSize则返回，否则写入编码的大小并返回-1</summary>
    /// <param name="size"></param>
    /// <returns></returns>
    public virtual Int32 WriteSize(Int32 size)
    {
        var sizeWidth = -1;
        if (UseFieldSize && TryGetFieldSize(out var fieldsize, out sizeWidth)) return fieldsize;

        if (sizeWidth < 0) sizeWidth = SizeWidth;
        switch (sizeWidth)
        {
            case 1:
                Write((Byte)size);
                break;
            case 2:
                Write((Int16)size);
                break;
            case 4:
                Write(size);
                break;
            case 0:
            default:
                WriteEncoded(size);
                break;
        }

        return -1;
    }
    #endregion

    #region 读取
    /// <summary>读取指定类型对象</summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public virtual Object? Read(Type type)
    {
        Object? value = null;
        if (!TryRead(type, ref value)) throw new Exception($"Read failed, type {type} is not supported!");

        return value;
    }

    /// <summary>读取指定类型对象</summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T? Read<T>() => (T?)Read(typeof(T));

    /// <summary>尝试读取指定类型对象</summary>
    /// <param name="type"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public virtual Boolean TryRead(Type type, [NotNullWhen(true)] ref Object? value)
    {
        if (Hosts.Count == 0 && Log != null && Log.Enable) WriteLog("BinaryRead {0} {1}", type.Name, value);

        if (EndOfStream) return false;

        // 优先 IAccessor 接口
        if (value is IAccessor acc)
        {
            if (acc.Read(Stream, this)) return true;
        }
        if (value == null && type.As<IAccessor>())
        {
            value = type.CreateInstance();
            if (value is IAccessor acc2)
            {
                if (acc2.Read(Stream, this)) return true;
            }
        }

        foreach (var item in Handlers)
        {
            if (item.TryRead(type, ref value!)) return true;

            // TryRead 失败时，可能是数据流不足
            if (EndOfStream) return false;
        }
        return false;
    }

    /// <summary>读取字节</summary>
    /// <returns></returns>
    public virtual Byte ReadByte()
    {
        if (EndOfStream) throw new EndOfStreamException("The data stream is out of range!");

        var b = Stream.ReadByte();
        if (b < 0)
        {
            EndOfStream = true;
            throw new EndOfStreamException("The data stream is out of range!");
        }

        // 探测数据流是否已经到达末尾
        var ms = Stream;
        if (ms.CanSeek && ms.Position >= ms.Length) EndOfStream = true;

        Total++;

        return (Byte)b;
    }

    /// <summary>尝试从当前流中读取一个字节</summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public virtual Boolean TryReadByte(out Byte value)
    {
        value = 0;
        if (EndOfStream) return false;

        var b = Stream.ReadByte();
        if (b < 0)
        {
            EndOfStream = true;
            return false;
        }

        // 探测数据流是否已经到达末尾
        var ms = Stream;
        if (ms.CanSeek && ms.Position >= ms.Length) EndOfStream = true;

        value = (Byte)b;
        Total++;

        return true;
    }

    /// <summary>从当前流中将 count 个字节读入字节数组</summary>
    /// <param name="count">要读取的字节数。-1表示读取到末尾</param>
    /// <returns>要读取的字节数组。数据到达末尾时返回空数据</returns>
    public virtual Byte[] ReadBytes(Int32 count)
    {
        if (count == 0) return [];

        if (EndOfStream) throw new EndOfStreamException("The data stream is out of range!");

        // 如果是-1，则读取全部剩余数据
        if (count < 0)
        {
            var buf = Stream.ReadBytes(-1);
            Total += buf.Length;
            return buf;
        }

        var buffer = new Byte[count];
        var totalRead = 0;
        while (totalRead < count)
        {
            var bytesRead = Stream.Read(buffer, totalRead, count - totalRead);
            if (bytesRead <= 0)
            {
                Total += totalRead;
                EndOfStream = true;
                //throw new EndOfStreamException("The data stream is out of range!");
                return totalRead > 0 ? buffer : [];
            }
            //if (bytesRead == 0) break;

            totalRead += bytesRead;
        }
        Total += totalRead;

        // 探测数据流是否已经到达末尾
        var ms = Stream;
        if (ms.CanSeek && ms.Position >= ms.Length) EndOfStream = true;

        //Stream.ReadExactly(buffer, 0, buffer.Length);
        //var buffer = Stream.ReadBytes(count);
        //if (n != count) throw new InvalidDataException($"数据不足，需要{count}，实际{n}");
        //Total += buffer.Length;

        return buffer;
    }

#if NETCOREAPP || NETSTANDARD2_1
    /// <summary>从当前流中读取字节数组</summary>
    /// <param name="span">字节数组</param>
    /// <returns></returns>
    public virtual Int32 ReadBytes(Span<Byte> span)
    {
        if (span.Length == 0) return 0;

        if (EndOfStream) throw new EndOfStreamException("The data stream is out of range!");

        var totalRead = 0;
        while (totalRead < span.Length)
        {
            var bytesRead = Stream.Read(span.Slice(totalRead));
            if (bytesRead <= 0)
            {
                Total += totalRead;
                EndOfStream = true;
                return totalRead;
            }

            totalRead += bytesRead;
        }
        Total += totalRead;

        // 探测数据流是否已经到达末尾
        var ms = Stream;
        if (ms.CanSeek && ms.Position >= ms.Length) EndOfStream = true;

        return totalRead;
    }
#endif

    /// <summary>从当前流中读取字节数组</summary>
    /// <param name="buffer">字节数组</param>
    /// <param name="offset">偏移量</param>
    /// <param name="count">个数</param>
    /// <returns></returns>
    public virtual Int32 ReadBytes(Byte[] buffer, Int32 offset, Int32 count)
    {
        if (count == 0) return 0;

        if (EndOfStream) throw new EndOfStreamException("The data stream is out of range!");

        var totalRead = 0;
        while (totalRead < count)
        {
            var bytesRead = Stream.Read(buffer, offset + totalRead, count - totalRead);
            if (bytesRead <= 0)
            {
                Total += totalRead;
                EndOfStream = true;
                return totalRead;
            }

            totalRead += bytesRead;
        }
        Total += totalRead;

        // 探测数据流是否已经到达末尾
        var ms = Stream;
        if (ms.CanSeek && ms.Position >= ms.Length) EndOfStream = true;

        return totalRead;
    }

    /// <summary>读取大小</summary>
    /// <returns></returns>
    public virtual Int32 ReadSize()
    {
        var sizeWidth = -1;
        if (UseFieldSize && TryGetFieldSize(out var size, out sizeWidth)) return size;

        if (sizeWidth < 0) sizeWidth = SizeWidth;
        return sizeWidth switch
        {
            1 => ReadByte(),
            2 => (Int16)(Read(typeof(Int16)) ?? 0),
            4 => (Int32)(Read(typeof(Int32)) ?? 0),
            0 => ReadEncodedInt32(),
            _ => -1,
        };
    }

    /// <summary>读取大小</summary>
    /// <returns></returns>
    public virtual Boolean TryReadSize(out Int32 value)
    {
        value = 0;
        var sizeWidth = -1;
        if (UseFieldSize && TryGetFieldSize(out value, out sizeWidth)) return true;

        if (sizeWidth < 0) sizeWidth = SizeWidth;
        switch (sizeWidth)
        {
            case 1:
                if (!TryReadByte(out var b)) return false;
                value = b;
                break;
            case 2:
                Object? n16 = null;
                if (!TryRead(typeof(Int16), ref n16)) return false;
                value = (Int16)n16;
                break;
            case 4:
                Object? n32 = null;
                if (!TryRead(typeof(Int32), ref n32)) return false;
                value = (Int32)n32;
                break;
            case 0:
                if (!TryReadEncodedInt32(out value)) return false;
                break;
            default:
                value = -1;
                break;
        }
        return true;
    }

    private Boolean TryGetFieldSize(out Int32 size, out Int32 sizeWidth)
    {
        sizeWidth = -1;
        if (Member is MemberInfo member)
        {
            // 获取FieldSizeAttribute特性
            var atts = member.GetCustomAttributes<FieldSizeAttribute>();
            if (atts != null)
            {
                foreach (var att in atts)
                {
                    // 检查版本是否匹配
                    if (att.Version.IsNullOrEmpty() || att.Version == Version)
                    {
                        // 如果指定了引用字段，则找引用字段所表示的长度
                        var target = Hosts.Peek();
                        if (!att.ReferenceName.IsNullOrEmpty() && target != null && att.TryGetReferenceSize(target, member, out size))
                            return true;

                        // 如果指定了固定大小，直接返回
                        size = att.Size;
                        if (size > 0) return true;

                        // 指定了大小位宽
                        if (att.SizeWidth >= 0)
                        {
                            sizeWidth = att.SizeWidth;
                            return false;
                        }
                    }
                }
            }
        }

        size = -1;
        return false;
    }
    #endregion

    #region 7位压缩编码整数
    [ThreadStatic]
    private static Byte[]? _encodes;
    /// <summary>写7位压缩编码整数</summary>
    /// <remarks>
    /// 以7位压缩格式写入32位整数，小于7位用1个字节，小于14位用2个字节。
    /// 由每次写入的一个字节的第一位标记后面的字节是否还是当前数据，所以每个字节实际可利用存储空间只有后7位。
    /// </remarks>
    /// <param name="value">数值</param>
    /// <returns>实际写入字节数</returns>
    public Int32 WriteEncoded(Int16 value)
    {
        _encodes ??= new Byte[16];

        var count = 0;
        var num = (UInt16)value;
        while (num >= 0x80)
        {
            _encodes[count++] = (Byte)(num | 0x80);
            num >>= 7;
        }
        _encodes[count++] = (Byte)num;

        Write(_encodes, 0, count);

        return count;
    }

    /// <summary>写7位压缩编码整数</summary>
    /// <remarks>
    /// 以7位压缩格式写入32位整数，小于7位用1个字节，小于14位用2个字节。
    /// 由每次写入的一个字节的第一位标记后面的字节是否还是当前数据，所以每个字节实际可利用存储空间只有后7位。
    /// </remarks>
    /// <param name="value">数值</param>
    /// <returns>实际写入字节数</returns>
    public Int32 WriteEncoded(Int32 value)
    {
        _encodes ??= new Byte[16];

        var count = 0;
        var num = (UInt32)value;
        while (num >= 0x80)
        {
            _encodes[count++] = (Byte)(num | 0x80);
            num >>= 7;
        }
        _encodes[count++] = (Byte)num;

        Write(_encodes, 0, count);

        return count;
    }

    /// <summary>写7位压缩编码整数</summary>
    /// <remarks>
    /// 以7位压缩格式写入32位整数，小于7位用1个字节，小于14位用2个字节。
    /// 由每次写入的一个字节的第一位标记后面的字节是否还是当前数据，所以每个字节实际可利用存储空间只有后7位。
    /// </remarks>
    /// <param name="value">数值</param>
    /// <returns>实际写入字节数</returns>
    public Int32 WriteEncoded(Int64 value)
    {
        _encodes ??= new Byte[16];

        var count = 0;
        var num = (UInt64)value;
        while (num >= 0x80)
        {
            _encodes[count++] = (Byte)(num | 0x80);
            num >>= 7;
        }
        _encodes[count++] = (Byte)num;

        Write(_encodes, 0, count);

        return count;
    }

    /// <summary>以压缩格式读取16位整数</summary>
    /// <returns></returns>
    public Int16 ReadEncodedInt16()
    {
        Byte b;
        Int16 rs = 0;
        Byte n = 0;
        while (true)
        {
            //b = ReadByte();
            if (!TryReadByte(out b)) break;
            // 必须转为Int16，否则可能溢出
            rs += (Int16)((b & 0x7f) << n);
            if ((b & 0x80) == 0) break;

            n += 7;
            if (n >= 16) throw new FormatException("The number value is too large to read in compressed format!");
        }
        return rs;
    }

    /// <summary>以压缩格式读取32位整数</summary>
    /// <returns></returns>
    public Int32 ReadEncodedInt32()
    {
        Byte b;
        var rs = 0;
        Byte n = 0;
        while (true)
        {
            //b = ReadByte();
            if (!TryReadByte(out b)) break;
            // 必须转为Int32，否则可能溢出
            rs += (b & 0x7f) << n;
            if ((b & 0x80) == 0) break;

            n += 7;
            if (n >= 32) throw new FormatException("The number value is too large to read in compressed format!");
        }
        return rs;
    }

    /// <summary>以压缩格式读取64位整数</summary>
    /// <returns></returns>
    public Int64 ReadEncodedInt64()
    {
        Byte b;
        Int64 rs = 0;
        Byte n = 0;
        while (true)
        {
            //b = ReadByte();
            if (!TryReadByte(out b)) break;
            // 必须转为Int64，否则可能溢出
            rs += (Int64)(b & 0x7f) << n;
            if ((b & 0x80) == 0) break;

            n += 7;
            if (n >= 64) throw new FormatException("The number value is too large to read in compressed format!");
        }
        return rs;
    }

    /// <summary>以压缩格式读取16位整数</summary>
    /// <returns></returns>
    public Boolean TryReadEncodedInt16(out Int16 value)
    {
        Byte b;
        Byte n = 0;
        value = 0;
        while (true)
        {
            if (!TryReadByte(out b)) return false;

            // 必须转为Int16，否则可能溢出
            value += (Int16)((b & 0x7f) << n);
            if ((b & 0x80) == 0) break;

            n += 7;
            if (n >= 16) throw new FormatException("The number value is too large to read in compressed format!");
        }
        return true;
    }

    /// <summary>以压缩格式读取32位整数</summary>
    /// <returns></returns>
    public Boolean TryReadEncodedInt32(out Int32 value)
    {
        Byte b;
        Byte n = 0;
        value = 0;
        while (true)
        {
            if (!TryReadByte(out b)) return false;

            // 必须转为Int32，否则可能溢出
            value += (b & 0x7f) << n;
            if ((b & 0x80) == 0) break;

            n += 7;
            if (n >= 32) throw new FormatException("The number value is too large to read in compressed format!");
        }
        return true;
    }

    /// <summary>以压缩格式读取64位整数</summary>
    /// <returns></returns>
    public Boolean TryReadEncodedInt64(out Int64 value)
    {
        Byte b;
        Byte n = 0;
        value = 0;
        while (true)
        {
            if (!TryReadByte(out b)) return false;

            // 必须转为Int64，否则可能溢出
            value += (Int64)(b & 0x7f) << n;
            if ((b & 0x80) == 0) break;

            n += 7;
            if (n >= 64) throw new FormatException("The number value is too large to read in compressed format!");
        }
        return true;
    }
    #endregion

    #region 专用扩展
    /// <summary>读取无符号短整数</summary>
    /// <returns></returns>
    public UInt16 ReadUInt16() => Read<UInt16>();

    /// <summary>读取短整数</summary>
    /// <returns></returns>
    public Int16 ReadInt16() => Read<Int16>();

    /// <summary>读取无符号整数</summary>
    /// <returns></returns>
    public UInt32 ReadUInt32() => Read<UInt32>();

    /// <summary>读取整数</summary>
    /// <returns></returns>
    public Int32 ReadInt32() => Read<Int32>();

    /// <summary>写入字节</summary>
    /// <param name="value"></param>
    public void WriteByte(Byte value) => Write(value);

    /// <summary>写入无符号短整数</summary>
    /// <param name="value"></param>
    public void WriteUInt16(UInt16 value) => Write(value);

    /// <summary>写入短整数</summary>
    /// <param name="value"></param>
    public void WriteInt16(Int16 value) => Write(value);

    /// <summary>写入无符号整数</summary>
    /// <param name="value"></param>
    public void WriteUInt32(UInt32 value) => Write(value);

    /// <summary>写入整数</summary>
    /// <param name="value"></param>
    public void WriteInt32(Int32 value) => Write(value);

    /// <summary>BCD字节转十进制数字</summary>
    /// <param name="b"></param>
    /// <returns></returns>
    public static Int32 FromBCD(Byte b) => (b >> 4) * 10 + (b & 0x0F);

    /// <summary>十进制数字转BCD字节</summary>
    /// <param name="n"></param>
    /// <returns></returns>
    public static Byte ToBCD(Int32 n) => (Byte)(((n / 10) << 4) | (n % 10));

    /// <summary>读取指定长度的BCD字符串。BCD每个字节存放两个数字</summary>
    /// <param name="len"></param>
    /// <returns></returns>
    public String ReadBCD(Int32 len)
    {
        var buf = ReadBytes(len);
        if (buf.Length == 0) return String.Empty;

        var cs = new Char[len * 2];
        for (var i = 0; i < len; i++)
        {
            cs[i * 2] = (Char)('0' + (buf[i] >> 4));
            cs[i * 2 + 1] = (Char)('0' + (buf[i] & 0x0F));
        }

        return new String(cs).Trim('\0');
    }

    /// <summary>写入指定长度的BCD字符串。BCD每个字节存放两个数字</summary>
    /// <param name="value"></param>
    /// <param name="max"></param>
    public void WriteBCD(String value, Int32 max)
    {
        var buf = Pool.Shared.Rent(max);
        for (Int32 i = 0, j = 0; i < max && j + 1 < value.Length; i++, j += 2)
        {
            var a = (Byte)(value[j] - '0');
            var b = (Byte)(value[j + 1] - '0');
            buf[i] = (Byte)((a << 4) | (b & 0x0F));
        }

        Write(buf, 0, max);
        Pool.Shared.Return(buf);
    }

    /// <summary>写入定长字符串。多余截取，少则补零</summary>
    /// <param name="value"></param>
    /// <param name="max"></param>
    public void WriteFixedString(String? value, Int32 max)
    {
        var len = 0;
        var buf = Pool.Shared.Rent(max);
        if (!value.IsNullOrEmpty()) len = Encoding.GetBytes(value, 0, value.Length, buf, 0);

        // 清空空白部分，避免出现脏数据
        if (len < max) Array.Clear(buf, len, max - len);

        Write(buf, 0, max);

        Pool.Shared.Return(buf);
    }

    /// <summary>读取定长字符串。多余截取，少则补零</summary>
    /// <param name="len"></param>
    /// <returns></returns>
    public String ReadFixedString(Int32 len)
    {
        var buf = ReadBytes(len);
        if (buf.Length == 0) return String.Empty;

        // 得到实际长度，在读取-1全部字符串时也能剔除首尾的0x00和0xFF
        if (len < 0) len = buf.Length;

        // 剔除头尾非法字符
        Int32 s, e;
        for (s = 0; s < len && (buf[s] == 0x00 || buf[s] == 0xFF); s++) ;
        for (e = len - 1; e >= 0 && (buf[e] == 0x00 || buf[e] == 0xFF); e--) ;

        if (s >= len || e < 0) return String.Empty;

        var str = Encoding.GetString(buf, s, e - s + 1);
        if (TrimZero && str != null) str = str.Trim('\0');

        return str ?? String.Empty;
    }
    #endregion

    #region 辅助
    ///// <summary>是否已达到末尾</summary>
    ///// <returns></returns>
    //public Boolean EndOfStream() => Stream.Position >= Stream.Length;

    /// <summary>检查剩余量是否足够</summary>
    /// <param name="size"></param>
    /// <returns></returns>
    public Boolean CheckRemain(Int32 size) => Stream.Position + size <= Stream.Length;
    #endregion

    #region 快捷方法
    /// <summary>快速读取</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="stream">数据流</param>
    /// <param name="encodeInt">使用7位编码整数</param>
    /// <returns></returns>
    public static T? FastRead<T>(Stream stream, Boolean encodeInt = true)
    {
        var bn = new Binary(stream) { EncodeInt = encodeInt };
        return bn.Read<T>();
    }

    /// <summary>快速写入</summary>
    /// <param name="value">对象</param>
    /// <param name="encodeInt">使用7位编码整数</param>
    /// <returns></returns>
    public static IPacket FastWrite(Object value, Boolean encodeInt = true)
    {
        // 头部预留8字节，方便加协议头
        var bn = new Binary { EncodeInt = encodeInt };
        bn.Stream.Seek(8, SeekOrigin.Current);
        bn.Write(value);

        //var buf = bn.GetBytes();
        //return new ArrayPacket(buf, 8, buf.Length - 8);

        bn.Stream.Position = 8;

        // 包装为数据包，直接窃取内存流内部的缓冲区
        return new ArrayPacket(bn.Stream);
    }

    /// <summary>快速写入</summary>
    /// <param name="value">对象</param>
    /// <param name="stream">目标数据流</param>
    /// <param name="encodeInt">使用7位编码整数</param>
    /// <returns></returns>
    public static Int64 FastWrite(Object value, Stream stream, Boolean encodeInt = true)
    {
        var bn = new Binary(stream)
        {
            EncodeInt = encodeInt,
        };
        bn.Write(value);

        return bn.Total;
    }
    #endregion
}