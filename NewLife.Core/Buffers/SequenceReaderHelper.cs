using System;
using System.Buffers;
using System.Buffers.Binary;

namespace NewLife.Buffers;

/// <summary>序列读取器端序扩展。BCL 的 SequenceReaderExtensions 只提供有符号重载（Int16/Int32/Int64），本类补充无符号端序的读取与窥视，全部目标框架可用</summary>
/// <remarks>
/// 与 BCL 扩展的配合关系：有符号端序读取用 BCL（或 Stub 垫片）的 SequenceReaderExtensions；
/// 无符号端序在各框架的 BCL 中始终缺失，统一由本类提供，因此生态代码可全 TFM 统一写法。
/// </remarks>
public static class SequenceReaderHelper
{
    #region 无符号读取
    /// <summary>尝试以小端序读取一个 UInt16。数据不足时返回 false 且不前进读取位置</summary>
    /// <param name="reader">序列读取器</param>
    /// <param name="value">读取到的值</param>
    /// <returns>是否读取成功</returns>
    public static Boolean TryReadLittleEndian(ref this SequenceReader<Byte> reader, out UInt16 value)
    {
        Span<Byte> buffer = stackalloc Byte[2];
        if (!reader.TryCopyTo(buffer))
        {
            value = default;
            return false;
        }

        value = BinaryPrimitives.ReadUInt16LittleEndian(buffer);
        reader.Advance(2);
        return true;
    }

    /// <summary>尝试以大端序读取一个 UInt16。数据不足时返回 false 且不前进读取位置</summary>
    /// <param name="reader">序列读取器</param>
    /// <param name="value">读取到的值</param>
    /// <returns>是否读取成功</returns>
    public static Boolean TryReadBigEndian(ref this SequenceReader<Byte> reader, out UInt16 value)
    {
        Span<Byte> buffer = stackalloc Byte[2];
        if (!reader.TryCopyTo(buffer))
        {
            value = default;
            return false;
        }

        value = BinaryPrimitives.ReadUInt16BigEndian(buffer);
        reader.Advance(2);
        return true;
    }

    /// <summary>尝试以小端序读取一个 UInt32。数据不足时返回 false 且不前进读取位置</summary>
    /// <param name="reader">序列读取器</param>
    /// <param name="value">读取到的值</param>
    /// <returns>是否读取成功</returns>
    public static Boolean TryReadLittleEndian(ref this SequenceReader<Byte> reader, out UInt32 value)
    {
        Span<Byte> buffer = stackalloc Byte[4];
        if (!reader.TryCopyTo(buffer))
        {
            value = default;
            return false;
        }

        value = BinaryPrimitives.ReadUInt32LittleEndian(buffer);
        reader.Advance(4);
        return true;
    }

    /// <summary>尝试以大端序读取一个 UInt32。数据不足时返回 false 且不前进读取位置</summary>
    /// <param name="reader">序列读取器</param>
    /// <param name="value">读取到的值</param>
    /// <returns>是否读取成功</returns>
    public static Boolean TryReadBigEndian(ref this SequenceReader<Byte> reader, out UInt32 value)
    {
        Span<Byte> buffer = stackalloc Byte[4];
        if (!reader.TryCopyTo(buffer))
        {
            value = default;
            return false;
        }

        value = BinaryPrimitives.ReadUInt32BigEndian(buffer);
        reader.Advance(4);
        return true;
    }

    /// <summary>尝试以小端序读取一个 UInt64。数据不足时返回 false 且不前进读取位置</summary>
    /// <param name="reader">序列读取器</param>
    /// <param name="value">读取到的值</param>
    /// <returns>是否读取成功</returns>
    public static Boolean TryReadLittleEndian(ref this SequenceReader<Byte> reader, out UInt64 value)
    {
        Span<Byte> buffer = stackalloc Byte[8];
        if (!reader.TryCopyTo(buffer))
        {
            value = default;
            return false;
        }

        value = BinaryPrimitives.ReadUInt64LittleEndian(buffer);
        reader.Advance(8);
        return true;
    }

    /// <summary>尝试以大端序读取一个 UInt64。数据不足时返回 false 且不前进读取位置</summary>
    /// <param name="reader">序列读取器</param>
    /// <param name="value">读取到的值</param>
    /// <returns>是否读取成功</returns>
    public static Boolean TryReadBigEndian(ref this SequenceReader<Byte> reader, out UInt64 value)
    {
        Span<Byte> buffer = stackalloc Byte[8];
        if (!reader.TryCopyTo(buffer))
        {
            value = default;
            return false;
        }

        value = BinaryPrimitives.ReadUInt64BigEndian(buffer);
        reader.Advance(8);
        return true;
    }
    #endregion

    #region 无符号窥视
    /// <summary>尝试以小端序窥视一个 UInt16（不前进读取位置）</summary>
    /// <param name="reader">序列读取器</param>
    /// <param name="value">读取到的值</param>
    /// <returns>是否读取成功</returns>
    public static Boolean TryPeekLittleEndian(ref this SequenceReader<Byte> reader, out UInt16 value)
    {
        Span<Byte> buffer = stackalloc Byte[2];
        if (!reader.TryCopyTo(buffer))
        {
            value = default;
            return false;
        }

        value = BinaryPrimitives.ReadUInt16LittleEndian(buffer);
        return true;
    }

    /// <summary>尝试以大端序窥视一个 UInt16（不前进读取位置）</summary>
    /// <param name="reader">序列读取器</param>
    /// <param name="value">读取到的值</param>
    /// <returns>是否读取成功</returns>
    public static Boolean TryPeekBigEndian(ref this SequenceReader<Byte> reader, out UInt16 value)
    {
        Span<Byte> buffer = stackalloc Byte[2];
        if (!reader.TryCopyTo(buffer))
        {
            value = default;
            return false;
        }

        value = BinaryPrimitives.ReadUInt16BigEndian(buffer);
        return true;
    }

    /// <summary>尝试以小端序窥视一个 UInt32（不前进读取位置）</summary>
    /// <param name="reader">序列读取器</param>
    /// <param name="value">读取到的值</param>
    /// <returns>是否读取成功</returns>
    public static Boolean TryPeekLittleEndian(ref this SequenceReader<Byte> reader, out UInt32 value)
    {
        Span<Byte> buffer = stackalloc Byte[4];
        if (!reader.TryCopyTo(buffer))
        {
            value = default;
            return false;
        }

        value = BinaryPrimitives.ReadUInt32LittleEndian(buffer);
        return true;
    }

    /// <summary>尝试以大端序窥视一个 UInt32（不前进读取位置）</summary>
    /// <param name="reader">序列读取器</param>
    /// <param name="value">读取到的值</param>
    /// <returns>是否读取成功</returns>
    public static Boolean TryPeekBigEndian(ref this SequenceReader<Byte> reader, out UInt32 value)
    {
        Span<Byte> buffer = stackalloc Byte[4];
        if (!reader.TryCopyTo(buffer))
        {
            value = default;
            return false;
        }

        value = BinaryPrimitives.ReadUInt32BigEndian(buffer);
        return true;
    }

    /// <summary>尝试以小端序窥视一个 UInt64（不前进读取位置）</summary>
    /// <param name="reader">序列读取器</param>
    /// <param name="value">读取到的值</param>
    /// <returns>是否读取成功</returns>
    public static Boolean TryPeekLittleEndian(ref this SequenceReader<Byte> reader, out UInt64 value)
    {
        Span<Byte> buffer = stackalloc Byte[8];
        if (!reader.TryCopyTo(buffer))
        {
            value = default;
            return false;
        }

        value = BinaryPrimitives.ReadUInt64LittleEndian(buffer);
        return true;
    }

    /// <summary>尝试以大端序窥视一个 UInt64（不前进读取位置）</summary>
    /// <param name="reader">序列读取器</param>
    /// <param name="value">读取到的值</param>
    /// <returns>是否读取成功</returns>
    public static Boolean TryPeekBigEndian(ref this SequenceReader<Byte> reader, out UInt64 value)
    {
        Span<Byte> buffer = stackalloc Byte[8];
        if (!reader.TryCopyTo(buffer))
        {
            value = default;
            return false;
        }

        value = BinaryPrimitives.ReadUInt64BigEndian(buffer);
        return true;
    }
    #endregion
}
