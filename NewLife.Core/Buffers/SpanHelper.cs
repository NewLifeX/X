using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;
using NewLife.Collections;

namespace NewLife;

/// <summary>Span帮助类</summary>
/// <remarks>提供Span/Memory常用扩展，避免额外分配。</remarks>
public static class SpanHelper
{
    #region 字符串扩展
    /// <summary>转字符串</summary>
    /// <param name="span">字节数据</param>
    /// <param name="encoding">编码，默认UTF8</param>
    /// <returns>把字节序列按指定编码解码为字符串</returns>
    public static String ToStr(this ReadOnlySpan<Byte> span, Encoding? encoding = null)
    {
        if (span.Length == 0) return String.Empty;
        return (encoding ?? Encoding.UTF8).GetString(span);
    }

    /// <summary>转字符串</summary>
    /// <param name="span">字节数据</param>
    /// <param name="encoding">编码，默认UTF8</param>
    /// <returns>把字节序列按指定编码解码为字符串</returns>
    public static String ToStr(this Span<Byte> span, Encoding? encoding = null)
    {
        if (span.Length == 0) return String.Empty;
        return (encoding ?? Encoding.UTF8).GetString(span);
    }

    /// <summary>获取字符串的字节数组写入数量（指针路径，避免中间数组）。</summary>
    public static unsafe Int32 GetBytes(this Encoding encoding, ReadOnlySpan<Char> chars, Span<Byte> bytes)
    {
        fixed (Char* chars2 = &MemoryMarshal.GetReference(chars))
        fixed (Byte* bytes2 = &MemoryMarshal.GetReference(bytes))
        {
            return encoding.GetBytes(chars2, chars.Length, bytes2, bytes.Length);
        }
    }

    /// <summary>获取字节数组的字符串（指针路径，避免额外拷贝）。</summary>
    public static unsafe String GetString(this Encoding encoding, ReadOnlySpan<Byte> bytes)
    {
        if (bytes.IsEmpty) return String.Empty;

#if NET45
        return encoding.GetString(bytes.ToArray());
#else
        fixed (Byte* bytes2 = &MemoryMarshal.GetReference(bytes))
        {
            return encoding.GetString(bytes2, bytes.Length);
        }
#endif
    }

    /// <summary>把字节数组编码为十六进制字符串</summary>
    /// <param name="data">字节数组</param>
    /// <returns>大写十六进制字符串（无分隔）</returns>
    public static String ToHex(this ReadOnlySpan<Byte> data)
    {
        if (data.Length == 0) return String.Empty;

        Span<Char> cs = stackalloc Char[data.Length * 2];
        for (Int32 i = 0, j = 0; i < data.Length; i++, j += 2)
        {
            var b = data[i];
            cs[j] = GetHexValue(b >> 4);
            cs[j + 1] = GetHexValue(b & 0x0F);
        }
        return cs.ToString();
    }

    /// <summary>把字节数组编码为十六进制字符串（限制最大长度）。</summary>
    /// <param name="data">字节数组</param>
    /// <param name="maxLength">最大长度</param>
    /// <returns>大写十六进制字符串（无分隔）</returns>
    public static String ToHex(this ReadOnlySpan<Byte> data, Int32 maxLength)
    {
        if (data.Length == 0 || maxLength == 0) return String.Empty;

        if (maxLength > 0 && data.Length > maxLength) data = data[..maxLength];

        return data.ToHex();
    }

    /// <summary>把字节数组编码为十六进制字符串</summary>
    /// <param name="data">字节数组</param>
    /// <returns>大写十六进制字符串（无分隔）</returns>
    public static String ToHex(this Span<Byte> data) => ToHex((ReadOnlySpan<Byte>)data);

    // Hex 查表（大写）
    private static Char GetHexValue(Int32 i) => "0123456789ABCDEF"[i & 0xF];

    /// <summary>以十六进制编码表示，支持分隔符与分组。</summary>
    /// <param name="data">数据</param>
    /// <param name="separate">分隔符</param>
    /// <param name="groupSize">分组大小，为0表示每个字节前都可插入分隔符</param>
    /// <param name="maxLength">最大显示字节数，-1表示全部</param>
    /// <returns>大写十六进制字符串（可选分隔符/分组）</returns>
    public static String ToHex(this ReadOnlySpan<Byte> data, String? separate, Int32 groupSize = 0, Int32 maxLength = -1)
    {
        if (data.Length == 0 || maxLength == 0) return String.Empty;

        if (maxLength > 0 && data.Length > maxLength) data = data[..maxLength];
        //return data.ToArray().ToHex(separate, groupSize);

        if (String.IsNullOrEmpty(separate)) return data.ToHex();

        if (groupSize < 0) groupSize = 0;

        var count = data.Length;
        var sb = Pool.StringBuilder.Get();
        for (var i = 0; i < count; i++)
        {
            if (i > 0)
            {
                if (groupSize <= 0 || i % groupSize == 0)
                {
                    if (!separate.IsNullOrEmpty()) sb.Append(separate);
                }
            }
            var b = data[i];
            sb.Append(GetHexValue(b >> 4));
            sb.Append(GetHexValue(b & 0x0F));
        }

        return sb.Return(true) ?? String.Empty;
    }

    /// <summary>以十六进制编码表示，支持分隔符与分组。</summary>
    /// <param name="span">数据</param>
    /// <param name="separate">分隔符</param>
    /// <param name="groupSize">分组大小，为0时对每个字节应用分隔符，否则对每个分组使用</param>
    /// <param name="maxLength">最大显示多少个字节。默认-1显示全部</param>
    /// <returns>大写十六进制字符串（可选分隔符/分组）</returns>
    public static String ToHex(this Span<Byte> span, String? separate, Int32 groupSize = 0, Int32 maxLength = -1)
    {
        if (span.Length == 0 || maxLength == 0) return String.Empty;

        return ToHex((ReadOnlySpan<Byte>)span, separate, groupSize, maxLength);
    }

    /// <summary>通过指定开始与结束边界来截取数据源</summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="source">源数据</param>
    /// <param name="start">起始边界</param>
    /// <param name="end">结束边界</param>
    /// <returns>位于边界之间的切片；未命中返回空切片</returns>
    public static ReadOnlySpan<T> Substring<T>(this ReadOnlySpan<T> source, ReadOnlySpan<T> start, ReadOnlySpan<T> end) where T : IEquatable<T>
    {
        var startIndex = source.IndexOf(start);
        if (startIndex == -1) return [];

        startIndex += start.Length;

        var endIndex = source[startIndex..].IndexOf(end);
        if (endIndex == -1) return [];

        return source.Slice(startIndex, endIndex);
    }

    /// <summary>通过指定开始与结束边界来截取数据源</summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="source">源数据</param>
    /// <param name="start">起始边界</param>
    /// <param name="end">结束边界</param>
    /// <returns>位于边界之间的切片；未命中返回空切片</returns>
    public static Span<T> Substring<T>(this Span<T> source, ReadOnlySpan<T> start, ReadOnlySpan<T> end) where T : IEquatable<T>
    {
        var startIndex = source.IndexOf(start);
        if (startIndex == -1) return [];

        startIndex += start.Length;

        var endIndex = source[startIndex..].IndexOf(end);
        if (endIndex == -1) return [];

        return source.Slice(startIndex, endIndex);
    }

    /// <summary>在数据源中查找开始与结束边界</summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="source">源数据</param>
    /// <param name="start">起始边界</param>
    /// <param name="end">结束边界</param>
    /// <returns>返回 (offset, count)。未命中返回 (-1, -1)，只命中起始返回 (startOffset, -1)</returns>
    public static (Int32 offset, Int32 count) IndexOf<T>(this ReadOnlySpan<T> source, ReadOnlySpan<T> start, ReadOnlySpan<T> end) where T : IEquatable<T>
    {
        var startIndex = source.IndexOf(start);
        if (startIndex == -1) return (-1, -1);

        startIndex += start.Length;

        var endIndex = source[startIndex..].IndexOf(end);
        if (endIndex == -1) return (startIndex, -1);

        return (startIndex, endIndex);
    }

    /// <summary>在数据源中查找开始与结束边界</summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="source">源数据</param>
    /// <param name="start">起始边界</param>
    /// <param name="end">结束边界</param>
    /// <returns>返回 (offset, count)。未命中返回 (-1, -1)，只命中起始返回 (startOffset, -1)</returns>
    public static (Int32 offset, Int32 count) IndexOf<T>(this Span<T> source, ReadOnlySpan<T> start, ReadOnlySpan<T> end) where T : IEquatable<T>
    {
        var startIndex = source.IndexOf(start);
        if (startIndex == -1) return (-1, -1);

        startIndex += start.Length;

        var endIndex = source[startIndex..].IndexOf(end);
        if (endIndex == -1) return (startIndex, -1);

        return (startIndex, endIndex);
    }
    #endregion

    /// <summary>写入Memory到数据流。从内存池借出缓冲区拷贝，仅作为兜底使用</summary>
    /// <param name="stream">目标流</param>
    /// <param name="buffer">源内存</param>
    public static void Write(this Stream stream, ReadOnlyMemory<Byte> buffer)
    {
        if (buffer.Length == 0) return;

        if (MemoryMarshal.TryGetArray(buffer, out var segment))
        {
            stream.Write(segment.Array!, segment.Offset, segment.Count);

            return;
        }

        var array = ArrayPool<Byte>.Shared.Rent(buffer.Length);

        try
        {
            buffer.Span.CopyTo(array);

            stream.Write(array, 0, buffer.Length);
        }
        finally
        {
            ArrayPool<Byte>.Shared.Return(array);
        }
    }

    /// <summary>写入Memory到数据流。从内存池借出缓冲区拷贝，仅作为兜底使用</summary>
    /// <param name="stream">目标流</param>
    /// <param name="buffer">源内存</param>
    /// <param name="cancellationToken">取消标记</param>
    public static async Task WriteAsync(this Stream stream, ReadOnlyMemory<Byte> buffer, CancellationToken cancellationToken = default)
    {
        if (buffer.Length == 0) return;

        if (MemoryMarshal.TryGetArray(buffer, out var segment))
        {
            await stream.WriteAsync(segment.Array!, segment.Offset, segment.Count, cancellationToken).ConfigureAwait(false);
            return;
        }

        var array = ArrayPool<Byte>.Shared.Rent(buffer.Length);
        try
        {
            buffer.Span.CopyTo(array);
            await stream.WriteAsync(array, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<Byte>.Shared.Return(array);
        }
    }

    /// <summary>去掉前后指定元素。</summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="span">目标切片</param>
    /// <param name="trimElement">要修剪的元素</param>
    /// <returns>去除前后连续 <paramref name="trimElement"/> 后的切片</returns>
    public static ReadOnlySpan<T> Trim<T>(this ReadOnlySpan<T> span, T trimElement) where T : IEquatable<T>
    {
        var start = ClampStart(span, trimElement);
        var length = ClampEnd(span, start, trimElement);
        return span.Slice(start, length);
    }

    /// <summary>去掉前后指定元素。</summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="span">目标切片</param>
    /// <param name="trimElement">要修剪的元素</param>
    /// <returns>去除前后连续 <paramref name="trimElement"/> 后的切片</returns>
    public static Span<T> Trim<T>(this Span<T> span, T trimElement) where T : IEquatable<T>
    {
        var start = ClampStart(span, trimElement);
        var length = ClampEnd(span, start, trimElement);
        return span.Slice(start, length);
    }

    private static Int32 ClampStart<T>(ReadOnlySpan<T> span, T trimElement) where T : IEquatable<T>
    {
        var i = 0;
        for (; i < span.Length; i++)
        {
            if (!trimElement.Equals(span[i])) break;
        }
        return i;
    }

    private static Int32 ClampEnd<T>(ReadOnlySpan<T> span, Int32 start, T trimElement) where T : IEquatable<T>
    {
        var num = span.Length - 1;
        while (num >= start)
        {
            if (!trimElement.Equals(span[num])) break;
            num--;
        }
        return num - start + 1;
    }
}