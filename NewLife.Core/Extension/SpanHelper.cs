using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;

namespace NewLife;

/// <summary>Span帮助类</summary>
public static class SpanHelper
{
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

    /// <summary>写入Memory到数据流</summary>
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
}