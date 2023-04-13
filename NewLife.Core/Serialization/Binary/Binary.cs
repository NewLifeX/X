using System.Reflection;
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
    public String Version { get; set; }

    /// <summary>要忽略的成员</summary>
    public ICollection<String> IgnoreMembers { get; set; } = new HashSet<String>();

    /// <summary>处理器列表</summary>
    public IList<IBinaryHandler> Handlers { get; private set; }
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
    public T GetHandler<T>() where T : class, IBinaryHandler
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
    public virtual Boolean Write(Object value, Type type = null)
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
    public virtual void Write(Byte value) => Stream.WriteByte(value);

    /// <summary>将字节数组部分写入当前流，不写入数组长度。</summary>
    /// <param name="buffer">包含要写入的数据的字节数组。</param>
    /// <param name="offset">buffer 中开始写入的起始点。</param>
    /// <param name="count">要写入的字节数。</param>
    public virtual void Write(Byte[] buffer, Int32 offset, Int32 count)
    {
        if (count < 0) count = buffer.Length - offset;
        Stream.Write(buffer, offset, count);
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

    [ThreadStatic]
    private static Byte[] _encodes;
    #endregion

    #region 读取
    /// <summary>读取指定类型对象</summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public virtual Object Read(Type type)
    {
        Object value = null;
        if (!TryRead(type, ref value)) throw new Exception($"读取失败，不支持类型{type}！");

        return value;
    }

    /// <summary>读取指定类型对象</summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T Read<T>() => (T)Read(typeof(T));

    /// <summary>尝试读取指定类型对象</summary>
    /// <param name="type"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public virtual Boolean TryRead(Type type, ref Object value)
    {
        if (Hosts.Count == 0 && Log != null && Log.Enable) WriteLog("BinaryRead {0} {1}", type.Name, value);

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
            if (item.TryRead(type, ref value)) return true;
        }
        return false;
    }

    /// <summary>读取字节</summary>
    /// <returns></returns>
    public virtual Byte ReadByte()
    {
        var b = Stream.ReadByte();
        if (b < 0) throw new Exception("数据流超出范围！");
        return (Byte)b;
    }

    /// <summary>从当前流中将 count 个字节读入字节数组</summary>
    /// <param name="count">要读取的字节数。</param>
    /// <returns></returns>
    public virtual Byte[] ReadBytes(Int32 count)
    {
        var buffer = Stream.ReadBytes(count);
        //if (n != count) throw new InvalidDataException($"数据不足，需要{count}，实际{n}");

        return buffer;
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
            2 => (Int16)Read(typeof(Int16)),
            4 => (Int32)Read(typeof(Int32)),
            0 => ReadEncodedInt32(),
            _ => -1,
        };
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
                        if (!att.ReferenceName.IsNullOrEmpty() && att.TryGetReferenceSize(Hosts.Peek(), member, out size))
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
    /// <summary>写7位压缩编码整数</summary>
    /// <remarks>
    /// 以7位压缩格式写入32位整数，小于7位用1个字节，小于14位用2个字节。
    /// 由每次写入的一个字节的第一位标记后面的字节是否还是当前数据，所以每个字节实际可利用存储空间只有后7位。
    /// </remarks>
    /// <param name="value">数值</param>
    /// <returns>实际写入字节数</returns>
    public Int32 WriteEncoded(Int32 value)
    {
        if (_encodes == null) _encodes = new Byte[16];

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

    /// <summary>以压缩格式读取16位整数</summary>
    /// <returns></returns>
    public Int16 ReadEncodedInt16()
    {
        Byte b;
        Int16 rs = 0;
        Byte n = 0;
        while (true)
        {
            b = ReadByte();
            // 必须转为Int16，否则可能溢出
            rs += (Int16)((b & 0x7f) << n);
            if ((b & 0x80) == 0) break;

            n += 7;
            if (n >= 16) throw new FormatException("数字值过大，无法使用压缩格式读取！");
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
            b = ReadByte();
            // 必须转为Int32，否则可能溢出
            rs += (b & 0x7f) << n;
            if ((b & 0x80) == 0) break;

            n += 7;
            if (n >= 32) throw new FormatException("数字值过大，无法使用压缩格式读取！");
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
            b = ReadByte();
            // 必须转为Int64，否则可能溢出
            rs += (Int64)(b & 0x7f) << n;
            if ((b & 0x80) == 0) break;

            n += 7;
            if (n >= 64) throw new FormatException("数字值过大，无法使用压缩格式读取！");
        }
        return rs;
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
        var buf = new Byte[max];
        for (Int32 i = 0, j = 0; i < max && j + 1 < value.Length; i++, j += 2)
        {
            var a = (Byte)(value[j] - '0');
            var b = (Byte)(value[j + 1] - '0');
            buf[i] = (Byte)((a << 4) | (b & 0x0F));
        }

        Write(buf, 0, buf.Length);
    }

    /// <summary>写入定长字符串。多余截取，少则补零</summary>
    /// <param name="value"></param>
    /// <param name="max"></param>
    public void WriteFixedString(String value, Int32 max)
    {
        var buf = new Byte[max];
        if (!value.IsNullOrEmpty()) Encoding.GetBytes(value, 0, value.Length, buf, 0);

        Write(buf, 0, buf.Length);
    }

    /// <summary>读取定长字符串。多余截取，少则补零</summary>
    /// <param name="len"></param>
    /// <returns></returns>
    public String ReadFixedString(Int32 len)
    {
        var buf = ReadBytes(len);

        // 剔除头尾非法字符
        Int32 s, e;
        for (s = 0; s < len && (buf[s] == 0x00 || buf[s] == 0xFF); s++) ;
        for (e = len - 1; e >= 0 && (buf[e] == 0x00 || buf[e] == 0xFF); e--) ;

        if (s >= len || e < 0) return null;

        var str = Encoding.GetString(buf, s, e - s + 1);
        if (TrimZero && str != null) str = str.Trim('\0');

        return str;
    }
    #endregion

    #region 跟踪日志
    ///// <summary>使用跟踪流。实际上是重新包装一次Stream，必须在设置Stream后，使用之前</summary>
    //public virtual void EnableTrace()
    //{
    //    var stream = Stream;
    //    if (stream is null or TraceStream) return;

    //    Stream = new TraceStream(stream) { Encoding = Encoding, IsLittleEndian = IsLittleEndian };
    //}
    #endregion

    #region 快捷方法
    /// <summary>快速读取</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="stream">数据流</param>
    /// <param name="encodeInt">使用7位编码整数</param>
    /// <returns></returns>
    public static T FastRead<T>(Stream stream, Boolean encodeInt = true)
    {
        var bn = new Binary() { Stream = stream, EncodeInt = encodeInt };
        return bn.Read<T>();
    }

    /// <summary>快速写入</summary>
    /// <param name="value">对象</param>
    /// <param name="encodeInt">使用7位编码整数</param>
    /// <returns></returns>
    public static Packet FastWrite(Object value, Boolean encodeInt = true)
    {
        // 头部预留8字节，方便加协议头
        var bn = new Binary { EncodeInt = encodeInt };
        bn.Stream.Seek(8, SeekOrigin.Current);
        bn.Write(value);

        var buf = bn.GetBytes();
        return new Packet(buf, 8, buf.Length - 8);
    }

    /// <summary>快速写入</summary>
    /// <param name="value">对象</param>
    /// <param name="stream">目标数据流</param>
    /// <param name="encodeInt">使用7位编码整数</param>
    /// <returns></returns>
    public static void FastWrite(Object value, Stream stream, Boolean encodeInt = true)
    {
        var bn = new Binary
        {
            Stream = stream,
            EncodeInt = encodeInt,
        };
        bn.Write(value);
    }
    #endregion
}