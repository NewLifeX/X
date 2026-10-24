using NewLife.Data;

namespace NewLife.Http;

/// <summary>分块传输解码器（RFC 9112 §7.1）</summary>
/// <remarks>
/// 服务端 <see cref="HttpSession"/> 接收分块请求体、客户端 <see cref="TinyHttpClient"/> 解码分块响应体共用，
/// 避免两套等价实现各自漂移。基于连续缓冲的增量式解码：数据不足返回 <see cref="ChunkedStatus.Incomplete"/>，
/// 调用方补读后重试即可。
/// </remarks>
internal static class ChunkedDecoder
{
    /// <summary>分块传输解码结果</summary>
    internal enum ChunkedStatus
    {
        /// <summary>数据不足，需继续接收</summary>
        Incomplete,

        /// <summary>已收齐终止块</summary>
        Complete,

        /// <summary>分块格式非法（坏块长度行）</summary>
        Invalid,
    }

    /// <summary>解码分块传输体（RFC 9112 §7.1）。块长度十六进制、忽略“;”扩展，终止块之后的 trailer 段一并消费</summary>
    /// <param name="buffer">原始字节</param>
    /// <param name="start">待解码区域的起始下标</param>
    /// <param name="end">待解码区域的结束下标（不含）</param>
    /// <param name="body">解码后的主体；未收齐或非法时为 null</param>
    /// <param name="consumed">相对 <paramref name="start"/> 消耗的原始字节数（收齐时有效）</param>
    /// <returns>解码结果</returns>
    internal static ChunkedStatus TryDecode(Byte[] buffer, Int32 start, Int32 end, out IPacket body, out Int32 consumed)
    {
        body = null!;
        consumed = 0;

        var decoded = new MemoryStream();
        var pos = start;
        while (true)
        {
            var eol = IndexOfCrlf(buffer, pos, end);
            if (eol < 0) return ChunkedStatus.Incomplete;

            // 块长度行：十六进制长度，可带 ";" 扩展
            var line = buffer.AsSpan(pos, eol - pos);
            var sc = line.IndexOf((Byte)';');
            if (sc >= 0) line = line[..sc];
            line = line.Trim((Byte)' ');

            if (line.Length == 0 || line.Length > 8) return ChunkedStatus.Invalid;

            var size = 0L;
            foreach (var b in line)
            {
                var d = HexValue(b);
                if (d < 0) return ChunkedStatus.Invalid;

                size = size * 16 + d;
            }

            pos = eol + 2;

            // 终止块：其后可能有 trailer 段，以空行结束
            if (size == 0)
            {
                if (pos + 1 < end && buffer[pos] == (Byte)'\r' && buffer[pos + 1] == (Byte)'\n')
                    consumed = pos + 2 - start;
                else
                {
                    var trailer = IndexOfCrlf2(buffer, pos, end);
                    if (trailer < 0) return ChunkedStatus.Incomplete;

                    consumed = trailer + 4 - start;
                }

                body = new ArrayPacket(decoded.GetBuffer(), 0, (Int32)decoded.Length);

                return ChunkedStatus.Complete;
            }

            // 数据不足：块体与末尾 CRLF 必须都在缓冲区内
            if (size > end - pos - 2) return ChunkedStatus.Incomplete;

            decoded.Write(buffer, pos, (Int32)size);
            pos += (Int32)size;

            // 块数据后的 CRLF
            if (pos + 2 > end || buffer[pos] != (Byte)'\r' || buffer[pos + 1] != (Byte)'\n') return ChunkedStatus.Incomplete;

            pos += 2;
        }
    }

    /// <summary>在指定范围内查找 CRLF，返回起始下标；未找到返回 -1</summary>
    private static Int32 IndexOfCrlf(Byte[] buffer, Int32 start, Int32 length)
    {
        for (var i = start; i + 1 < length; i++)
        {
            if (buffer[i] == (Byte)'\r' && buffer[i + 1] == (Byte)'\n') return i;
        }

        return -1;
    }

    /// <summary>在指定范围内查找空行（CRLFCRLF），返回其起始下标；未找到返回 -1</summary>
    private static Int32 IndexOfCrlf2(Byte[] buffer, Int32 start, Int32 length)
    {
        for (var i = start; i + 3 < length; i++)
        {
            if (buffer[i] == (Byte)'\r' && buffer[i + 1] == (Byte)'\n' && buffer[i + 2] == (Byte)'\r' && buffer[i + 3] == (Byte)'\n') return i;
        }

        return -1;
    }

    /// <summary>十六进制字符转数值；非法字符返回 -1</summary>
    private static Int32 HexValue(Byte b) => b switch
    {
        >= (Byte)'0' and <= (Byte)'9' => b - (Byte)'0',
        >= (Byte)'a' and <= (Byte)'f' => b - (Byte)'a' + 10,
        >= (Byte)'A' and <= (Byte)'F' => b - (Byte)'A' + 10,
        _ => -1,
    };
}
