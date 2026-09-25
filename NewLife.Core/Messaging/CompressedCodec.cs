using System.Buffers;
using NewLife.Data;
using NewLife.IO;

namespace NewLife.Messaging;

/// <summary>压缩消息编解码器。装饰内层协议，对消息体做 Deflate 压缩/解压</summary>
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
            var compressed = buffer.Slice(r.HeaderSize, r.BodyLength).ToArray();
            msg.SetBody(new ArrayPacket(compressed.Decompress()));
        }

        return r;
    }

    /// <summary>整帧构建。压缩消息体后交内层协议构建（消息体所有权随构建转移）</summary>
    /// <param name="message">消息</param>
    /// <returns>整帧数据包，调用方负责 Dispose</returns>
    public IPacket? Build(IMessage message)
    {
        var body = message.Payload;
        if (body == null || body.Total == 0) return Inner.Build(message);

        // 压缩体替换后交由内层整帧构建（SetBody 归还旧体）
        var raw = body.ToArray();
        message.SetBody(new ArrayPacket(raw.Compress()));

        return Inner.Build(message);
    }

    /// <summary>仅构建头部。压缩体长度无法预声明，不支持流式发送</summary>
    /// <param name="message">消息</param>
    /// <param name="bodyLength">消息体字节数</param>
    /// <returns>头部数据包</returns>
    /// <exception cref="NotSupportedException">压缩协议无法预声明压缩后长度</exception>
    public IPacket BuildHeader(IMessage message, Int64 bodyLength) => throw new NotSupportedException("压缩协议无法预声明压缩后长度，请使用整帧构建（Build）");
}
