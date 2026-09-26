using System.Buffers;
using System.IO.Compression;
using System.Runtime.InteropServices;
using NewLife.Data;

namespace NewLife.Messaging;

/// <summary>压缩消息编解码器。装饰内层协议，只对消息负载做 Deflate 压缩/解压（帧头与帧定界仍由内层协议明文封装）</summary>
/// <remarks>
/// <para>组合示例：<c>server.Protocol = new CompressedCodec(new SrmpCodec());</c>——同一 <see cref="IMessageCodec"/> 接口可任意嵌套组合（压缩/加密等变换层）。</para>
/// <para><b>连接级约定</b>：收发两端须同时配置本编码器，线格式不含压缩标志。</para>
/// <para><b>接收</b>：要求完整帧到齐后整载解压（压缩体无法流式解压），解压后经 <see cref="Message.SetBody"/> 预绑定，帧泵检测到已绑定体时直接消费整帧；坏数据抛出异常交由帧泵关闭会话。</para>
/// <para><b>发送</b>：整帧构建时压缩内存体再交内层协议；空体不压缩。流式发送不支持（<see cref="BuildHeader"/> 抛异常）。</para>
/// </remarks>
public class CompressedCodec(IMessageCodec inner) : IMessageCodec
{
    /// <summary>内层协议</summary>
    public IMessageCodec Inner { get; } = inner;

    /// <summary>定界并构造消息。内层定界后，完整帧解压消息体并预绑定；帧未完整返回 null 等更多数据</summary>
    /// <param name="buffer">帧首窗口（只读序列，可跨段）</param>
    /// <returns>解析结果；头部不足或帧未完整返回 null</returns>
    public ParseResult? TryParse(ReadOnlySequence<Byte> buffer)
    {
        var rs = Inner.TryParse(buffer);
        if (rs == null) return null;

        var r = rs.Value;
        var msg = r.Message;
        if (msg == null) return rs;   // 无消息帧直通

        // 压缩体必须整载解压：帧未完整时丢弃暂建消息，等更多数据（不消费）
        if (r.HeaderSize + r.BodyLength > buffer.Length)
        {
            msg.Dispose();
            return null;
        }

        if (r.BodyLength > 0)
        {
            // 压缩体整载取出：单段直接引用帧窗口（零拷贝），跨段才拼一份；解压结果窃取内存流缓冲
            var body = buffer.Slice(r.HeaderSize, r.BodyLength);
            using var input = body.IsSingleSegment && MemoryMarshal.TryGetArray(body.First, out var segment)
                ? new MemoryStream(segment.Array!, segment.Offset, segment.Count, false)
                : new MemoryStream(body.ToArray());

            var ms = new MemoryStream();
            using (var inflate = new DeflateStream(input, CompressionMode.Decompress, true)) inflate.CopyTo(ms);

            ms.Position = 0;
            msg.SetBody(new ArrayPacket(ms));
        }

        return r;
    }

    /// <summary>整帧构建。压缩消息体后交内层协议构建，构建后还原消息负载</summary>
    /// <param name="message">消息</param>
    /// <returns>整帧拥有句柄，调用方负责 Dispose</returns>
    public IOwnerPacket? Build(IMessage message)
    {
        var body = message.Payload;
        if (body == null || body.Total == 0) return Inner.Build(message);

        // 逐段写入压缩流，链式负载不必先聚合；压缩结果窃取内存流缓冲
        var ms = new MemoryStream();
        using (var deflate = new DeflateStream(ms, CompressionLevel.Optimal, true)) body.CopyTo(deflate);

        ms.Position = 0;

        // 内层协议从 Payload 取体：先摘除原负载（放弃持有、不归还）再顶上压缩体，构建后原样换回
        message.SetBody(null);
        message.SetBody(new ArrayPacket(ms));

        try
        {
            return Inner.Build(message);
        }
        finally
        {
            message.SetBody(body);
        }
    }

    /// <summary>仅构建头部。压缩体长度无法预声明，不支持流式发送</summary>
    /// <param name="message">消息</param>
    /// <param name="bodyLength">消息体字节数</param>
    /// <returns>头部数据包</returns>
    /// <exception cref="NotSupportedException">压缩协议无法预声明压缩后长度</exception>
    public IOwnerPacket BuildHeader(IMessage message, Int64 bodyLength) => throw new NotSupportedException("压缩协议无法预声明压缩后长度，请使用整帧构建（Build）");
}
