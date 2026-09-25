using System.Buffers;
using NewLife.Data;

namespace NewLife.Messaging;

/// <summary>标准消息 SRMP 编解码器。帧格式：1 Flag + 1 Sequence + 2 Length + N Payload；Length=0xFFFF 时 8 字节扩展头</summary>
/// <remarks>
/// <para>无状态、可跨连接共享。长度分界与 <see cref="DefaultMessage"/> 帧格式一致：负载长度 &lt; 0xFFFF 用 4 字节头，否则 8 字节扩展头（0xFFFF 标记 + 4 字节小端正式长度）。</para>
/// <para>解析产出 <see cref="DefaultMessage"/> 消息实例；构建接受 <see cref="DefaultMessage"/>。
/// 协议字段（Flag/Sequence/Kind 与扩展头判定）的读写由 <see cref="DefaultMessage"/> 自身承担，本类负责帧层装配与序列号配对。</para>
/// </remarks>
/// <example>
/// <code>
/// var codec = new SrmpCodec();
///
/// // 发送：整帧构建
/// var msg = new DefaultMessage { Sequence = 1 };
/// msg.SetBody(data);
/// var frame = codec.Build(msg);
///
/// // 接收：定界并构造消息（体由帧层绑定）
/// if (codec.TryParse(buffer) is { } rs) { var m = rs.Message; }
/// </code>
/// </example>
public class SrmpCodec : IMessageCodec, IMessageMatcher
{
    #region 方法
    /// <summary>定界并构造消息。解析头部（4 或 8 字节），构造头部字段就位的 <see cref="DefaultMessage"/></summary>
    /// <param name="buffer">帧首窗口（只读序列，可跨段）</param>
    /// <returns>解析结果；头部不足返回 null（不消费、不产生对象）</returns>
    /// <remarks>在只读序列上顺序读取，不拼读、不物化；扩展长度读出负数（协议上限 Int32.MaxValue）视为损坏帧返回 null。</remarks>
    public ParseResult? TryParse(ReadOnlySequence<Byte> buffer)
    {
        // 协议字段由消息类自行解析（消息定义即协议），帧层只负责装配
        var message = new DefaultMessage();
        if (!message.TryParse(buffer, out var bodyLength, out var headerSize)) return null;

        return new ParseResult { Message = message, HeaderSize = headerSize, BodyLength = bodyLength };
    }

    /// <summary>整帧构建（头 + 内存体链式）。构建成功后消息不再持有体</summary>
    /// <param name="message">标准消息</param>
    /// <returns>整帧数据包，调用方负责 Dispose</returns>
    /// <remarks>拥有帧且前置空间足够时原地扩展头部（零拷贝）；否则新建头部包，负载作为后继链节点。</remarks>
    /// <exception cref="ArgumentException">消息类型不是 <see cref="DefaultMessage"/></exception>
    /// <exception cref="InvalidOperationException">消息体为流式模式（请使用 BuildHeader + 流式发送）</exception>
    public IPacket? Build(IMessage message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        if (message is not DefaultMessage msg) throw new ArgumentException($"消息类型 [{message.GetType().FullName}] 不支持标准消息帧格式", nameof(message));

        if (msg.Body is { IsStreaming: true }) throw new InvalidOperationException("流式消息体无法整帧构建，请使用 BuildHeader + 流式发送");

        var body = msg.Payload;
        var len = 0;
        if (body != null) len = body.Total;

        // 增加4字节头部，如果负载数据之前有足够空间则直接使用，否则新建数据包形成链式结构
        var size = len < 0xFFFF ? 4 : 8;
        var pk = body.ExpandHeader(size);

        msg.WriteHeader(pk.GetSpan(), len);

        // 所有权随构建结果转移：消息不再持有体（ExpandHeader 可能已接管源句柄，或将其作为后继链节点）
        msg.SetBody((IPacket?)null);

        return pk;
    }

    /// <summary>仅构建头部数据包，声明消息体长度（头 + 流式体发送）</summary>
    /// <param name="message">标准消息</param>
    /// <param name="bodyLength">消息体字节数（0 ~ 2147483647）</param>
    /// <returns>头部数据包，调用方负责 Dispose</returns>
    /// <remarks>负载长度 &lt; 0xFFFF 用 4 字节头，否则用 8 字节扩展头，与整帧构建长度分界一致。</remarks>
    /// <exception cref="ArgumentException">消息类型不是 <see cref="DefaultMessage"/></exception>
    /// <exception cref="ArgumentOutOfRangeException">长度为负或超过 32 位协议上限</exception>
    public IPacket BuildHeader(IMessage message, Int64 bodyLength)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        if (message is not DefaultMessage msg) throw new ArgumentException($"消息类型 [{message.GetType().FullName}] 不支持标准消息帧格式", nameof(message));

        if (bodyLength < 0) throw new ArgumentOutOfRangeException(nameof(bodyLength), "Body length must be non-negative.");
        if (bodyLength > Int32.MaxValue) throw new ArgumentOutOfRangeException(nameof(bodyLength), "Body length exceeds the 32-bit protocol limit.");

        var size = bodyLength < 0xFFFF ? 4 : 8;
        var pk = new OwnerPacket(size);

        msg.WriteHeader(pk.GetSpan(), bodyLength);

        return pk;
    }

    /// <summary>判断响应是否匹配请求。标准消息按序列号配对（低8位）</summary>
    /// <param name="request">挂起的请求消息</param>
    /// <param name="response">收到的响应消息</param>
    /// <returns>是否配对</returns>
    public Boolean Match(IMessage request, IMessage response)
    {
        if (request is not DefaultMessage rq || response is not DefaultMessage rs) return false;

        // 仅应答类消息参与配对（响应识别下放协议 matcher，无方向协议可用恒真 matcher）
        if (!rs.Reply) return false;

        return rq.Sequence == rs.Sequence;
    }
    #endregion
}
