#if NETFRAMEWORK || NETSTANDARD2_0
using System.Runtime.CompilerServices;

namespace System;

/// <summary>
/// 表示一个索引，可从序列开头或结尾计算。等同于 .NET Core 的 System.Index。
/// </summary>
public readonly struct Index : IEquatable<Index>
{
    /// <summary>内部存储的索引值。非负表示从开头，负值按位取反后表示从末尾。</summary>
    private readonly Int32 _value;

    /// <summary>表示序列起始位置（0）。</summary>
    public static Index Start => new(0);

    /// <summary>表示序列末尾位置（^0）。</summary>
    public static Index End => new(-1);

    /// <summary>获取绝对值。若 <see cref="IsFromEnd"/> 为 true，则返回从末尾的偏移量。</summary>
    public Int32 Value => _value < 0 ? ~_value : _value;

    /// <summary>指示该索引是否从序列末尾计算。</summary>
    public Boolean IsFromEnd => _value < 0;

    /// <summary>
    /// 使用给定值与是否从末尾的标志创建索引。
    /// </summary>
    /// <param name="value">索引值，必须是非负数。</param>
    /// <param name="fromEnd">是否从末尾计算。</param>
    /// <exception cref="ArgumentOutOfRangeException">当 value 小于 0 时抛出。</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Index(Int32 value, Boolean fromEnd = false)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException("value", "value must be non-negative");
        }
        _value = fromEnd ? ~value : value;
    }

    private Index(Int32 value) => _value = value;

    /// <summary>从序列头部创建索引。</summary>
    /// <param name="value">非负索引值。</param>
    /// <returns>从头部计算的索引。</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 value 小于 0 时抛出。</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Index FromStart(Int32 value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException("value", "value must be non-negative");
        }
        return new Index(value);
    }

    /// <summary>从序列尾部创建索引。</summary>
    /// <param name="value">非负索引值。</param>
    /// <returns>从尾部计算的索引。</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 value 小于 0 时抛出。</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Index FromEnd(Int32 value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException("value", "value must be non-negative");
        }
        return new Index(~value);
    }

    /// <summary>
    /// 根据给定序列长度获取该索引对应的偏移量。
    /// </summary>
    /// <param name="length">序列长度。</param>
    /// <returns>偏移量。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Int32 GetOffset(Int32 length)
    {
        var offset = _value;
        if (IsFromEnd)
        {
            offset += length + 1;
        }
        return offset;
    }

    /// <inheritdoc />
    public override Boolean Equals(Object value) => value is Index index && _value == index._value;

    /// <inheritdoc />
    public Boolean Equals(Index other) => _value == other._value;

    /// <inheritdoc />
    public override Int32 GetHashCode() => _value;

    /// <summary>从 <see cref="Int32"/> 隐式转换为从起始位置计算的 <see cref="Index"/>。</summary>
    public static implicit operator Index(Int32 value) => FromStart(value);

    /// <inheritdoc />
    public override String ToString()
    {
        if (IsFromEnd)
        {
            return "^" + (UInt32)Value;
        }
        return ((UInt32)Value).ToString();
    }
}
#endif