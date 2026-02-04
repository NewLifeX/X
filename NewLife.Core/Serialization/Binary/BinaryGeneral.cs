using System.Buffers.Binary;
using System.Text;
using NewLife.Collections;
using NewLife.Reflection;

namespace NewLife.Serialization;

/// <summary>二进制基础类型处理器</summary>
public class BinaryGeneral : BinaryHandlerBase
{
    private static readonly DateTime _dt1970 = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>实例化</summary>
    public BinaryGeneral() => Priority = 10;

    /// <summary>写入一个对象</summary>
    /// <param name="value">目标对象</param>
    /// <param name="type">类型</param>
    /// <returns>是否处理成功</returns>
    public override Boolean Write(Object? value, Type type)
    {
        //if (value == null && type != typeof(String)) return false;

        // 可空类型，先写入一个字节表示是否为空
        if (type.IsNullable())
        {
            if (value == null)
            {
                Host.Write((Byte)0);
                return true;
            }
            else
                Host.Write((Byte)1);
        }

        switch (type.GetTypeCode())
        {
            case TypeCode.Boolean:
                Host.Write((Byte)(value != null && (Boolean)value ? 1 : 0));
                return true;
            case TypeCode.Byte:
            case TypeCode.SByte:
                Host.Write(Convert.ToByte(value));
                return true;
            case TypeCode.Char:
                Write((Char)(value ?? 0));
                return true;
            case TypeCode.DBNull:
            case TypeCode.Empty:
                Host.Write(0);
                return true;
            case TypeCode.DateTime:
                if (Host is Binary bn && bn.FullTime)
                {
                    if (value is DateTime dt)
                        Write(dt.ToBinary());
                    else
                        Write((Int64)0);
                }
                else
                {
                    if (value is DateTime dt && dt > DateTime.MinValue)
                    {
                        var seconds = (dt - _dt1970).TotalSeconds;
                        if (seconds >= UInt32.MaxValue) throw new InvalidDataException("Cannot serialize time less than 1970, please use FullTime");

                        Write((UInt32)seconds);
                    }
                    else
                        Write((UInt32)0);
                }
                return true;
            case TypeCode.Decimal:
                Write((Decimal)(value ?? 0));
                return true;
            case TypeCode.Double:
                Write((Double)(value ?? 0));
                return true;
            case TypeCode.Int16:
                Write((Int16)(value ?? 0));
                return true;
            case TypeCode.Int32:
                Write((Int32)(value ?? 0));
                return true;
            case TypeCode.Int64:
                Write((Int64)(value ?? 0));
                return true;
            case TypeCode.Object:
                break;
            case TypeCode.Single:
                Write((Single)(value ?? 0));
                return true;
            case TypeCode.String:
                Write((String)(value ?? String.Empty));
                return true;
            case TypeCode.UInt16:
                Write((UInt16)(value ?? 0));
                return true;
            case TypeCode.UInt32:
                Write((UInt32)(value ?? 0));
                return true;
            case TypeCode.UInt64:
                Write((UInt64)(value ?? 0));
                return true;
            default:
                break;
        }

        return false;
    }

    /// <summary>尝试读取指定类型对象</summary>
    /// <param name="type"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public override Boolean TryRead(Type type, ref Object? value)
    {
        if (type == null)
        {
            if (value == null) return false;
            type = value.GetType();
        }

        // 可空类型，先写入一个字节表示是否为空
        Byte b = 0;
        if (type.IsNullable())
        {
            if (!Host.TryReadByte(out b)) return false;
            //var v = Host.ReadByte();
            if (b == 0)
            {
                value = null;
                return true;
            }
        }

        var code = type.GetTypeCode();
        switch (code)
        {
            case TypeCode.Boolean:
                if (!Host.TryReadByte(out b)) return false;
                value = b > 0;
                return true;
            case TypeCode.Byte:
            case TypeCode.SByte:
                if (!Host.TryReadByte(out b)) return false;
                value = b;
                return true;
            case TypeCode.Char:
                if (!Host.TryReadByte(out b)) return false;
                value = Convert.ToChar(b);
                //value = ReadChar();
                return true;
            case TypeCode.DBNull:
                value = DBNull.Value;
                return true;
            case TypeCode.DateTime:
                if (Host is Binary bn && bn.FullTime)
                {
                    if (!TryReadInt64(out var n)) return false;
                    value = DateTime.FromBinary(n);
                }
                else
                {
                    if (!TryReadInt32(out var n)) return false;
                    if (n == 0)
                        value = DateTime.MinValue;
                    else
                        value = _dt1970.AddSeconds((UInt32)n);
                }
                return true;
            case TypeCode.Decimal:
                //value = ReadDecimal();
                var data = new Int32[4];
                for (var i = 0; i < data.Length; i++)
                {
                    if (!TryReadInt32(out data[i])) return false;
                }
                value = new Decimal(data);
                return true;
            case TypeCode.Double:
                {
                    if (!TryReadDouble(out var num)) return false;
                    value = num;
                }
                return true;
            case TypeCode.Empty:
                value = null;
                return true;
            case TypeCode.Int16:
                {
                    if (!TryReadInt16(out var num)) return false;
                    value = num;
                }
                return true;
            case TypeCode.Int32:
                {
                    if (!TryReadInt32(out var num)) return false;
                    value = num;
                }
                return true;
            case TypeCode.Int64:
                {
                    if (!TryReadInt64(out var num)) return false;
                    value = num;
                }
                return true;
            case TypeCode.Object:
                break;
            case TypeCode.Single:
                {
                    if (!TryReadSingle(out var num)) return false;
                    value = num;
                }
                return true;
            case TypeCode.String:
                {
                    if (!TryReadString(out var str)) return false;
                    value = str;
                }
                return true;
            case TypeCode.UInt16:
                {
                    if (!TryReadInt16(out var num)) return false;
                    value = (UInt16)num;
                }
                return true;
            case TypeCode.UInt32:
                {
                    if (!TryReadInt32(out var num)) return false;
                    value = (UInt32)num;
                }
                return true;
            case TypeCode.UInt64:
                {
                    if (!TryReadInt64(out var num)) return false;
                    value = (UInt64)num;
                }
                return true;
            default:
                break;
        }

        return false;
    }

    #region 基元类型写入
    #region 字节
    /// <summary>将一个无符号字节写入</summary>
    /// <param name="value">要写入的无符号字节。</param>
    public virtual void Write(Byte value) => Host.Write(value);

    /// <summary>将字节数组写入，如果设置了UseSize，则先写入数组长度。</summary>
    /// <param name="buffer">包含要写入的数据的字节数组。</param>
    public virtual void Write(Byte[] buffer)
    {
        // 可能因为FieldSize设定需要补充0字节
        if (buffer == null || buffer.Length == 0)
        {
            var size = Host.WriteSize(0);
            if (size > 0) Host.Write(new Byte[size], 0, -1);
        }
        else
        {
            var size = Host.WriteSize(buffer.Length);
            if (size > 0)
            {
                // 写入数据，超长截断，不足补0
                if (buffer.Length >= size)
                    Host.Write(buffer, 0, size);
                else
                {
                    Host.Write(buffer, 0, buffer.Length);
                    Host.Write(new Byte[size - buffer.Length], 0, -1);
                }
            }
            else
            {
                // 非FieldSize写入
                Host.Write(buffer, 0, buffer.Length);
            }
        }
    }

    /// <summary>将字节数组部分写入当前流，不写入数组长度。</summary>
    /// <param name="buffer">包含要写入的数据的字节数组。</param>
    /// <param name="offset">buffer 中开始写入的起始点。</param>
    /// <param name="count">要写入的字节数。</param>
    public virtual void Write(Byte[] buffer, Int32 offset, Int32 count)
    {
        if (buffer == null || buffer.Length <= 0 || count <= 0 || offset >= buffer.Length) return;

        Host.Write(buffer, offset, count);
    }

    /// <summary>写入字节数组，自动计算长度</summary>
    /// <param name="buffer">缓冲区</param>
    /// <param name="count">数量</param>
    private void Write(Byte[] buffer, Int32 count)
    {
        if (buffer == null) return;

        if (count < 0 || count > buffer.Length) count = buffer.Length;

        Write(buffer, 0, count);
    }
    #endregion

    #region 有符号整数
    /// <summary>将 2 字节有符号整数写入当前流，并将流的位置提升 2 个字节。</summary>
    /// <param name="value">要写入的 2 字节有符号整数。</param>
    public virtual void Write(Int16 value)
    {
        if (Host.EncodeInt && Host is Binary bn)
            bn.WriteEncoded(value);
        else
            WriteIntBytes(BitConverter.GetBytes(value));
    }

    /// <summary>将 4 字节有符号整数写入当前流，并将流的位置提升 4 个字节。</summary>
    /// <param name="value">要写入的 4 字节有符号整数。</param>
    public virtual void Write(Int32 value)
    {
        if (Host.EncodeInt && Host is Binary bn)
            bn.WriteEncoded(value);
        else
            WriteIntBytes(BitConverter.GetBytes(value));
    }

    /// <summary>将 8 字节有符号整数写入当前流，并将流的位置提升 8 个字节。</summary>
    /// <param name="value">要写入的 8 字节有符号整数。</param>
    public virtual void Write(Int64 value)
    {
        if (Host.EncodeInt && Host is Binary bn)
            bn.WriteEncoded(value);
        else
            WriteIntBytes(BitConverter.GetBytes(value));
    }

    /// <summary>判断字节顺序</summary>
    /// <param name="buffer">缓冲区</param>
    private void WriteIntBytes(Byte[] buffer)
    {
        if (buffer == null || buffer.Length <= 0) return;

        // 如果不是小端字节顺序，则倒序
        if (!Host.IsLittleEndian) Array.Reverse(buffer);

        Write(buffer, 0, buffer.Length);
    }
    #endregion

    #region 无符号整数
    /// <summary>将 2 字节无符号整数写入当前流，并将流的位置提升 2 个字节。</summary>
    /// <param name="value">要写入的 2 字节无符号整数。</param>
    //[CLSCompliant(false)]
    public virtual void Write(UInt16 value) => Write((Int16)value);

    /// <summary>将 4 字节无符号整数写入当前流，并将流的位置提升 4 个字节。</summary>
    /// <param name="value">要写入的 4 字节无符号整数。</param>
    //[CLSCompliant(false)]
    public virtual void Write(UInt32 value) => Write((Int32)value);

    /// <summary>将 8 字节无符号整数写入当前流，并将流的位置提升 8 个字节。</summary>
    /// <param name="value">要写入的 8 字节无符号整数。</param>
    //[CLSCompliant(false)]
    public virtual void Write(UInt64 value) => Write((Int64)value);
    #endregion

    #region 浮点数
    /// <summary>将 4 字节浮点值写入当前流，并将流的位置提升 4 个字节。</summary>
    /// <param name="value">要写入的 4 字节浮点值。</param>
    public virtual void Write(Single value)
    {
#if NET5_0_OR_GREATER
        Span<Byte> buffer = stackalloc Byte[4];
        if (Host.IsLittleEndian)
            BinaryPrimitives.WriteSingleLittleEndian(buffer, value);
        else
            BinaryPrimitives.WriteSingleBigEndian(buffer, value);
        Host.Write(buffer);
#else
        var buffer = BitConverter.GetBytes(value);
        if (!Host.IsLittleEndian) Array.Reverse(buffer);
        Host.Write(buffer, 0, buffer.Length);
#endif
    }

    /// <summary>将 8 字节浮点值写入当前流，并将流的位置提升 8 个字节。</summary>
    /// <param name="value">要写入的 8 字节浮点值。</param>
    public virtual void Write(Double value)
    {
#if NET5_0_OR_GREATER
        Span<Byte> buffer = stackalloc Byte[8];
        if (Host.IsLittleEndian)
            BinaryPrimitives.WriteDoubleLittleEndian(buffer, value);
        else
            BinaryPrimitives.WriteDoubleBigEndian(buffer, value);
        Host.Write(buffer);
#else
        var buffer = BitConverter.GetBytes(value);
        if (!Host.IsLittleEndian) Array.Reverse(buffer);
        Host.Write(buffer, 0, buffer.Length);
#endif
    }

    /// <summary>将一个十进制值写入当前流，并将流位置提升十六个字节。</summary>
    /// <param name="value">要写入的十进制值。</param>
    protected virtual void Write(Decimal value)
    {
        var data = Decimal.GetBits(value);
        for (var i = 0; i < data.Length; i++)
        {
            Write(data[i]);
        }
    }
    #endregion

    #region 字符串
    /// <summary>将 Unicode 字符写入当前流，并根据所使用的 Encoding 和向流中写入的特定字符，提升流的当前位置。</summary>
    /// <param name="ch">要写入的非代理项 Unicode 字符。</param>
    public virtual void Write(Char ch) => Write(Convert.ToByte(ch));

    /// <summary>将字符数组部分写入当前流，并根据所使用的 Encoding（可能还根据向流中写入的特定字符），提升流的当前位置。</summary>
    /// <param name="chars">包含要写入的数据的字符数组。</param>
    /// <param name="index">chars 中开始写入的起始点。</param>
    /// <param name="count">要写入的字符数。</param>
    public virtual void Write(Char[] chars, Int32 index, Int32 count)
    {
        if (chars == null)
        {
            //Host.WriteSize(0);
            // 可能因为FieldSize设定需要补充0字节
            Write([]);
            return;
        }

        if (chars.Length <= 0 || count <= 0 || index >= chars.Length)
        {
            //Host.WriteSize(0);
            // 可能因为FieldSize设定需要补充0字节
            Write([]);
            return;
        }

        // 先用写入字节长度
        var buffer = Host.Encoding.GetBytes(chars, index, count);
        Write(buffer);
    }

    /// <summary>写入字符串</summary>
    /// <param name="value">要写入的值。</param>
    public virtual void Write(String value)
    {
        if (value == null || value.Length == 0)
        {
            //Host.WriteSize(0);
            Write([]);
            return;
        }

        // 先用写入字节长度
        var buffer = Host.Encoding.GetBytes(value);
        Write(buffer);
    }
    #endregion
    #endregion

    #region 基元类型读取
    #region 有符号整数
    /// <summary>从当前流中读取 2 字节有符号整数，并使流的当前位置提升 2 个字节。</summary>
    /// <returns></returns>
    public virtual Boolean TryReadInt16(out Int16 value)
    {
        value = 0;

        if (Host.EncodeInt && Host is Binary bn)
            return bn.TryReadEncodedInt16(out value);

        const Int32 SIZE = 2;
#if NETCOREAPP || NETSTANDARD2_1
        Span<Byte> buffer = stackalloc Byte[SIZE];
        if (Host.ReadBytes(buffer) == 0) return false;

        if (Host.IsLittleEndian)
            value = BinaryPrimitives.ReadInt16LittleEndian(buffer);
        else
            value = BinaryPrimitives.ReadInt16BigEndian(buffer);
#else
        var buffer = Pool.Shared.Rent(SIZE);
        if (Host.ReadBytes(buffer, 0, SIZE) == 0) return false;

        if (!Host.IsLittleEndian) Array.Reverse(buffer, 0, SIZE);

        value = BitConverter.ToInt16(buffer, 0);
        Pool.Shared.Return(buffer);
#endif

        return true;
    }

    /// <summary>从当前流中读取 4 字节有符号整数，并使流的当前位置提升 4 个字节。</summary>
    /// <returns></returns>
    public virtual Boolean TryReadInt32(out Int32 value)
    {
        value = 0;

        if (Host.EncodeInt && Host is Binary bn)
            return bn.TryReadEncodedInt32(out value);

        const Int32 SIZE = 4;
#if NETCOREAPP || NETSTANDARD2_1
        Span<Byte> buffer = stackalloc Byte[SIZE];
        if (Host.ReadBytes(buffer) == 0) return false;

        if (Host.IsLittleEndian)
            value = BinaryPrimitives.ReadInt32LittleEndian(buffer);
        else
            value = BinaryPrimitives.ReadInt32BigEndian(buffer);
#else
        var buffer = Pool.Shared.Rent(SIZE);
        if (Host.ReadBytes(buffer, 0, SIZE) == 0) return false;

        if (!Host.IsLittleEndian) Array.Reverse(buffer, 0, SIZE);

        value = BitConverter.ToInt32(buffer, 0);
        Pool.Shared.Return(buffer);
#endif

        return true;
    }

    /// <summary>从当前流中读取 8 字节有符号整数，并使流的当前位置向前移动 8 个字节。</summary>
    /// <returns></returns>
    public virtual Boolean TryReadInt64(out Int64 value)
    {
        value = 0;

        if (Host.EncodeInt && Host is Binary bn)
            return bn.TryReadEncodedInt64(out value);

        const Int32 SIZE = 8;
#if NETCOREAPP || NETSTANDARD2_1
        Span<Byte> buffer = stackalloc Byte[SIZE];
        if (Host.ReadBytes(buffer) == 0) return false;

        if (Host.IsLittleEndian)
            value = BinaryPrimitives.ReadInt64LittleEndian(buffer);
        else
            value = BinaryPrimitives.ReadInt64BigEndian(buffer);
#else
        var buffer = Pool.Shared.Rent(SIZE);
        if (Host.ReadBytes(buffer, 0, SIZE) == 0) return false;

        if (!Host.IsLittleEndian) Array.Reverse(buffer, 0, SIZE);

        value = BitConverter.ToInt64(buffer, 0);
        Pool.Shared.Return(buffer);
#endif

        return true;
    }
    #endregion

    #region 浮点数
    /// <summary>从当前流中读取 4 字节浮点值，并使流的当前位置提升 4 个字节。</summary>
    /// <returns></returns>
    public virtual Boolean TryReadSingle(out Single value)
    {
        value = 0;

        const Int32 SIZE = 4;
#if NET5_0_OR_GREATER
        Span<Byte> buffer = stackalloc Byte[SIZE];
        if (Host.ReadBytes(buffer) == 0) return false;

        if (Host.IsLittleEndian)
            value = BinaryPrimitives.ReadSingleLittleEndian(buffer);
        else
            value = BinaryPrimitives.ReadSingleBigEndian(buffer);
#else
        var buffer = Pool.Shared.Rent(SIZE);
        if (Host.ReadBytes(buffer, 0, SIZE) == 0) return false;

        if (!Host.IsLittleEndian) Array.Reverse(buffer, 0, SIZE);

        value = BitConverter.ToSingle(buffer, 0);
        Pool.Shared.Return(buffer);
#endif

        return true;
    }

    /// <summary>从当前流中读取 8 字节浮点值，并使流的当前位置提升 8 个字节。</summary>
    /// <returns></returns>
    public virtual Boolean TryReadDouble(out Double value)
    {
        value = 0;

        const Int32 SIZE = 8;
#if NET5_0_OR_GREATER
        Span<Byte> buffer = stackalloc Byte[SIZE];
        if (Host.ReadBytes(buffer) == 0) return false;

        if (Host.IsLittleEndian)
            value = BinaryPrimitives.ReadDoubleLittleEndian(buffer);
        else
            value = BinaryPrimitives.ReadDoubleBigEndian(buffer);
#else
        var buffer = Pool.Shared.Rent(SIZE);
        if (Host.ReadBytes(buffer, 0, SIZE) == 0) return false;

        if (!Host.IsLittleEndian) Array.Reverse(buffer, 0, SIZE);

        value = BitConverter.ToDouble(buffer, 0);
        Pool.Shared.Return(buffer);
#endif

        return true;
    }
    #endregion

    #region 字符串
    /// <summary>从当前流中读取一个字符串。字符串有长度前缀，7位压缩编码整数。</summary>
    /// <returns></returns>
    public virtual Boolean TryReadString(out String value)
    {
        value = String.Empty;

        // 先读长度
        if (!Host.TryReadSize(out var n)) return false;
        if (n <= 0) return true;

#if NETCOREAPP || NETSTANDARD2_1
        // 栈分配阈值：避免大字符串导致栈溢出
        const Int32 STACK_ALLOC_THRESHOLD = 512;

        Byte[]? rentedBuffer = null;
        var buffer = n <= STACK_ALLOC_THRESHOLD
            ? stackalloc Byte[n]
            : (rentedBuffer = Pool.Shared.Rent(n)).AsSpan(0, n);

        try
        {
            if (Host.ReadBytes(buffer) == 0) return false;

            var enc = Host.Encoding ?? Encoding.UTF8;

            var str = enc.GetString(buffer);
            if (Host is Binary bn && bn.TrimZero && str != null) str = str.Trim('\0');

            value = str ?? String.Empty;

            return true;
        }
        finally
        {
            if (rentedBuffer != null)
                Pool.Shared.Return(rentedBuffer);
        }
#else
        var buffer = Pool.Shared.Rent(n);
        if (Host.ReadBytes(buffer, 0, n) == 0) return false;

        var enc = Host.Encoding ?? Encoding.UTF8;

        var str = enc.GetString(buffer);
        if (Host is Binary bn && bn.TrimZero && str != null) str = str.Trim('\0');

        value = str ?? String.Empty;

        Pool.Shared.Return(buffer);

        return true;
#endif
    }
    #endregion
    #endregion
}