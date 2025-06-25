using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;
using NewLife.Collections;

namespace NewLife;

/// <summary>Span帮助类</summary>
public static class SpanHelper
{
    #region 字符串扩展
    /// <summary>转字符串</summary>
    /// <param name="span"></param>
    /// <param name="encoding"></param>
    /// <returns></returns>
    public static String ToStr(this ReadOnlySpan<Byte> span, Encoding? encoding = null)
    {
        if (span.Length == 0) return String.Empty;
        return (encoding ?? Encoding.UTF8).GetString(span);
    }

    /// <summary>转字符串</summary>
    /// <param name="span"></param>
    /// <param name="encoding"></param>
    /// <returns></returns>
    public static String ToStr(this Span<Byte> span, Encoding? encoding = null)
    {
        if (span.Length == 0) return String.Empty;
        return (encoding ?? Encoding.UTF8).GetString(span);
    }

    /// <summary>获取字符串的字节数组</summary>
    public static unsafe Int32 GetBytes(this Encoding encoding, ReadOnlySpan<Char> chars, Span<Byte> bytes)
    {
        fixed (Char* chars2 = &MemoryMarshal.GetReference(chars))
        {
            fixed (Byte* bytes2 = &MemoryMarshal.GetReference(bytes))
            {
                return encoding.GetBytes(chars2, chars.Length, bytes2, bytes.Length);
            }
        }
    }

    /// <summary>获取字节数组的字符串</summary>
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
    /// <returns></returns>
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

    /// <summary>把字节数组编码为十六进制字符串</summary>
    /// <param name="data">字节数组</param>
    /// <param name="maxLength">最大长度</param>
    /// <returns></returns>
    public static String ToHex(this ReadOnlySpan<Byte> data, Int32 maxLength)
    {
        if (data.Length == 0) return String.Empty;

        if (maxLength > 0 && data.Length > maxLength) data = data[..maxLength];

        return data.ToHex();
    }

    /// <summary>把字节数组编码为十六进制字符串</summary>
    /// <param name="data">字节数组</param>
    /// <returns></returns>
    public static String ToHex(this Span<Byte> data) => ToHex((ReadOnlySpan<Byte>)data);

    private static Char GetHexValue(Int32 i) => i < 10 ? (Char)(i + '0') : (Char)(i - 10 + 'A');

    /// <summary>以十六进制编码表示</summary>
    /// <param name="data"></param>
    /// <param name="separate">分隔符</param>
    /// <param name="groupSize">分组大小，为0时对每个字节应用分隔符，否则对每个分组使用</param>
    /// <param name="maxLength">最大显示多少个字节。默认-1显示全部</param>
    /// <returns></returns>
    public static String ToHex(this ReadOnlySpan<Byte> data, String? separate, Int32 groupSize = 0, Int32 maxLength = -1)
    {
        if (data.Length == 0 || maxLength == 0) return String.Empty;

        if (maxLength > 0 && data.Length > maxLength) data = data[..maxLength];
        //return data.ToArray().ToHex(separate, groupSize);

        if (groupSize < 0) groupSize = 0;

        var count = data.Length;
        if (groupSize == 0)
        {
            // 没有分隔符
            if (String.IsNullOrEmpty(separate)) return data.ToHex();

            //// 特殊处理
            //if (separate == "-") return BitConverter.ToString(data, 0, count);
        }

        var len = count * 2;
        if (!separate.IsNullOrEmpty()) len += (count - 1) * separate.Length;
        if (groupSize > 0)
        {
            // 计算分组个数
            var g = (count - 1) / groupSize;
            len += g * 2;
            // 扣除间隔
            if (!separate.IsNullOrEmpty()) _ = g * separate.Length;
        }

        var sb = Pool.StringBuilder.Get();
        for (var i = 0; i < count; i++)
        {
            if (sb.Length > 0)
            {
                if (groupSize <= 0 || i % groupSize == 0)
                    sb.Append(separate);
            }

            var b = data[i];
            sb.Append(GetHexValue(b >> 4));
            sb.Append(GetHexValue(b & 0x0F));
        }

        return sb.Return(true) ?? String.Empty;
    }

    /// <summary>以十六进制编码表示</summary>
    /// <param name="span"></param>
    /// <param name="separate">分隔符</param>
    /// <param name="groupSize">分组大小，为0时对每个字节应用分隔符，否则对每个分组使用</param>
    /// <param name="maxLength">最大显示多少个字节。默认-1显示全部</param>
    /// <returns></returns>
    public static String ToHex(this Span<Byte> span, String? separate, Int32 groupSize = 0, Int32 maxLength = -1)
    {
        if (span.Length == 0 || maxLength == 0) return String.Empty;

        return ToHex((ReadOnlySpan<Byte>)span, separate, groupSize, maxLength);
    }

    /// <summary>通过指定开始与结束边界来截取数据源</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
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
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
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
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
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
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
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
    /// <param name="stream"></param>
    /// <param name="buffer"></param>
    /// <returns></returns>
    public static void Write(this Stream stream, ReadOnlyMemory<Byte> buffer)
    {
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
    /// <param name="stream"></param>
    /// <param name="buffer"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static Task WriteAsync(this Stream stream, ReadOnlyMemory<Byte> buffer, CancellationToken cancellationToken = default)
    {
        if (MemoryMarshal.TryGetArray(buffer, out var segment))
            return stream.WriteAsync(segment.Array!, segment.Offset, segment.Count, cancellationToken);

        var array = ArrayPool<Byte>.Shared.Rent(buffer.Length);
        buffer.Span.CopyTo(array);

        var writeTask = stream.WriteAsync(array, 0, buffer.Length, cancellationToken);
        return Task.Run(async () =>
        {
            try
            {
                await writeTask.ConfigureAwait(false);
            }
            finally
            {
                ArrayPool<Byte>.Shared.Return(array);
            }
        });
    }

#if NETFRAMEWORK || NETSTANDARD
    /// <summary>去掉前后字符</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="span"></param>
    /// <param name="trimElement"></param>
    /// <returns></returns>
    public static ReadOnlySpan<T> Trim<T>(this ReadOnlySpan<T> span, T trimElement) where T : IEquatable<T>
    {
        var start = ClampStart(span, trimElement);
        var length = ClampEnd(span, start, trimElement);
        return span.Slice(start, length);
    }

    /// <summary>去掉前后字符</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="span"></param>
    /// <param name="trimElement"></param>
    /// <returns></returns>
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
            ref var reference = ref trimElement;
            if (!reference.Equals(span[i]))
            {
                break;
            }
        }
        return i;
    }

    private static Int32 ClampEnd<T>(ReadOnlySpan<T> span, Int32 start, T trimElement) where T : IEquatable<T>
    {
        var num = span.Length - 1;
        while (num >= start)
        {
            ref var reference = ref trimElement;
            if (!reference.Equals(span[num]))
            {
                break;
            }
            num--;
        }
        return num - start + 1;
    }
#endif
}