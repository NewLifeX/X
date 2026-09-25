using System.Buffers;
using NewLife.Data;

namespace NewLife.Messaging;

/// <summary>分隔符消息编解码器。以分隔符（默认 CRLF）定界：帧 = 内容 + 分隔符，内容为消息体</summary>
/// <remarks>
/// <para>典型用于行协议（文本命令、JSON 行、简化 SMTP 等）。空行（连续分隔符）为无消息帧，不产出消息。</para>
/// <para>解析：跳过前导分隔符（上一帧帧尾）后扫描内容分隔符；内容作为消息体供流式消费，分隔符不进入消息。</para>
/// <para>构建：内容尾部追加分隔符。本协议无法预声明长度，<see cref="BuildHeader"/> 不支持，请使用整帧构建。</para>
/// </remarks>
/// <example>
/// <code>
/// var pump = new MessagePump(new SplitDataCodec());
/// while (true)
/// {
///     var msg = await pump.ReadAsync(pipe.Reader, cancellationToken);
///     if (msg == null) break;
///
///     var line = await msg.Body.ReadAllAsync();   // 一行内容（不含分隔符）
///     MessagePump.DiscardAsync(msg);
///     msg.Dispose();
/// }
/// </code>
/// </example>
public class SplitDataCodec : IMessageCodec
{
    #region 属性
    /// <summary>分隔符。默认 CRLF（0x0D 0x0A）</summary>
    public Byte[] SplitData { get; set; } = [0x0D, 0x0A];
    #endregion

    #region 解析
    /// <summary>定界并构造消息。跳过前导分隔符后扫描内容分隔符，内容长度即体长度；空行为无消息帧</summary>
    /// <param name="buffer">帧首窗口（只读序列，可跨段）</param>
    /// <returns>解析结果；分隔符未出现时返回 null（不消费，等追加）。消息为 null 表示空行（无消息帧，跳过前导分隔符与空行分隔符）</returns>
    public ParseResult? TryParse(ReadOnlySequence<Byte> buffer)
    {
        var sep = SplitData;
        if (sep.Length == 0) return null;

        // 跳过前导分隔符（上一帧帧尾）
        var start = MatchAt(buffer, 0, sep) ? sep.Length : 0;

        // 从内容起点扫描分隔符；未出现则等更多数据（不消费）
        var seq = start > 0 ? buffer.Slice(start) : buffer;
        var idx = IndexOf(seq, sep);
        if (idx < 0) return null;

        // 空行：无消息帧（消费前导分隔符与空行分隔符）
        if (idx == 0) return new ParseResult { HeaderSize = start + sep.Length };

        var message = new Message();
        return new ParseResult { Message = message, HeaderSize = start, BodyLength = idx };
    }

    /// <summary>整帧构建（内容 + 分隔符）。构建不消费消息负载</summary>
    /// <param name="message">内容消息</param>
    /// <returns>整帧拥有句柄，调用方负责 Dispose</returns>
    /// <remarks>拥有句柄共享切片后挂接分隔符（零拷贝）；视图无法挂链，新分配并拷贝。两种策略都不改动消息负载。</remarks>
    /// <exception cref="InvalidOperationException">消息体为流式绑定，无法整帧构建</exception>
    public IOwnerPacket? Build(IMessage message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        if (message.Body is { IsStreaming: true }) throw new InvalidOperationException("流式消息体无法整帧构建，请使用流式发送");

        // 分隔符包（尾部追加）
        var sep = SplitData;
        var tail = new OwnerPacket(sep.Length);
        sep.CopyTo(tail.GetSpan());

        // 空消息：仅分隔符（如心跳空行）
        var body = message.Payload;
        if (body == null) return tail;

        // 内容 + 分隔符：拥有句柄共享切片后链尾挂接（零拷贝，源体保持有效）
        if (body is IOwnerPacket owner)
        {
            var head = owner.Slice(0, -1);
            head.Append(tail);

            return head;
        }

        // 视图无法挂链：新分配并拷贝
        var pk = new OwnerPacket(body.Total + sep.Length);
        body.ReadBytes(pk.GetSpan());
        sep.CopyTo(pk.GetSpan()[body.Total..]);

        return pk;
    }

    /// <summary>仅构建头部数据包。分隔符协议无法预声明长度，不支持</summary>
    /// <param name="message">消息</param>
    /// <param name="bodyLength">消息体字节数</param>
    /// <returns>头部数据包</returns>
    /// <exception cref="NotSupportedException">分隔符协议不支持头部构建（请使用整帧构建）</exception>
    public IOwnerPacket BuildHeader(IMessage message, Int64 bodyLength) => throw new NotSupportedException("分隔符协议无法预声明长度，请使用整帧构建（Build）");
    #endregion

    #region 辅助
    /// <summary>判断序列指定偏移处是否匹配分隔符（跨段安全）</summary>
    /// <param name="buffer">目标序列</param>
    /// <param name="offset">起始偏移</param>
    /// <param name="sep">分隔符</param>
    /// <returns>是否匹配</returns>
    private static Boolean MatchAt(in ReadOnlySequence<Byte> buffer, Int64 offset, ReadOnlySpan<Byte> sep)
    {
        if (buffer.Length < offset + sep.Length) return false;

        var reader = new SequenceReader<Byte>(buffer.Slice(offset));

        return MatchSequence(ref reader, sep);
    }

    /// <summary>在序列中查找分隔符（跨段安全），返回其起始偏移；未找到返回 -1</summary>
    /// <param name="seq">目标序列</param>
    /// <param name="sep">分隔符</param>
    /// <returns>分隔符起始偏移；未找到返回 -1</returns>
    private static Int64 IndexOf(in ReadOnlySequence<Byte> seq, ReadOnlySpan<Byte> sep)
    {
        var reader = new SequenceReader<Byte>(seq);
        Int64 pos = 0;
        while (!reader.End)
        {
            // 拷贝嗅探：匹配成功即当前位置为分隔符起点（TryRead 跨段安全）
            var copy = reader;
            if (MatchSequence(ref copy, sep)) return pos;

            if (!reader.TryRead(out _)) break;
            pos++;
        }

        return -1;
    }

    /// <summary>从读取器当前位置匹配分隔符（消费副本读取器，跨段安全）</summary>
    /// <param name="reader">读取器（副本）</param>
    /// <param name="sep">分隔符</param>
    /// <returns>是否匹配</returns>
    private static Boolean MatchSequence(ref SequenceReader<Byte> reader, ReadOnlySpan<Byte> sep)
    {
        foreach (var b in sep)
        {
            if (!reader.TryRead(out var x) || x != b) return false;
        }

        return true;
    }
    #endregion
}
