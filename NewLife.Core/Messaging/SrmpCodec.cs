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
    /// <returns>解析结果；头部不足返回 null（不消费、不产生对象）；损坏帧返回 <see cref="ParseResult.Invalid"/> 结果</returns>
    /// <remarks>在只读序列上顺序读取，不拼读、不物化；扩展长度读出负数（协议上限 Int32.MaxValue）视为损坏帧。</remarks>
    public ParseResult? TryParse(ReadOnlySequence<Byte> buffer)
    {
        // 协议字段由消息类自行解析（消息定义即协议），帧层只负责装配
        var message = new DefaultMessage();
        if (!message.TryParse(buffer, out var bodyLength, out var headerSize, out var invalid))
        {
            // 头部已完整但长度非法：损坏帧交由帧层按协议错误处置（流式关闭连接、数据报丢包）；
            // 只有数据不足才返回 null 进入等待
            return invalid ? new ParseResult { Invalid = true } : null;
        }

        return new ParseResult { Message = message, HeaderSize = headerSize, BodyLength = bodyLength };
    }

    /// <summary>整帧构建（头 + 内存体）。构建不消费消息负载</summary>
    /// <param name="message">标准消息</param>
    /// <returns>整帧拥有句柄，调用方负责 Dispose</returns>
    /// <remarks>
    /// <para>已预留的负载零拷贝借位共享（帧头落在预留区）；其余以新头节点挂接负载链。两种策略都不改动消息负载。</para>
    /// <para><b>时效</b>：结果可能引用消息负载缓冲，帧发送完成前不得复用或改写。</para>
    /// </remarks>
    /// <exception cref="ArgumentException">消息类型不是 <see cref="DefaultMessage"/></exception>
    /// <exception cref="InvalidOperationException">消息体为流式模式（请使用 BuildHeader + 流式发送）</exception>
    public IOwnerPacket? Build(IMessage message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        if (message is not DefaultMessage msg) throw new ArgumentException($"消息类型 [{message.GetType().FullName}] 不支持标准消息帧格式", nameof(message));

        if (msg.Body is { IsStreaming: true }) throw new InvalidOperationException("流式消息体无法整帧构建，请使用 BuildHeader + 流式发送");

        var body = msg.Payload;
        var len = 0;
        if (body != null) len = body.Total;

        // 增加协议头：已预留的拥有句柄零拷贝借位共享，其余新头节点挂接负载链
        var size = len < 0xFFFF ? 4 : 8;
        var pk = body.PrepareHeader(size);

        msg.WriteHeader(pk.GetSpan(), len);

        return pk;
    }

    /// <summary>仅构建头部数据包，声明消息体长度（头 + 流式体发送）</summary>
    /// <param name="message">标准消息</param>
    /// <param name="bodyLength">消息体字节数（0 ~ 2147483647）</param>
    /// <returns>头部数据包，调用方负责 Dispose</returns>
    /// <remarks>负载长度 &lt; 0xFFFF 用 4 字节头，否则用 8 字节扩展头，与整帧构建长度分界一致。</remarks>
    /// <exception cref="ArgumentException">消息类型不是 <see cref="DefaultMessage"/></exception>
    /// <exception cref="ArgumentOutOfRangeException">长度为负或超过 32 位协议上限</exception>
    public IOwnerPacket BuildHeader(IMessage message, Int64 bodyLength)
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

    /// <summary>判断响应是否匹配请求。标准消息按序列号较低8位配对</summary>
    /// <param name="request">挂起的请求消息</param>
    /// <param name="response">收到的响应消息</param>
    /// <returns>是否配对</returns>
    /// <remarks>
    /// <para>线格式的序列号只有 1 字节（见 <see cref="DefaultMessage.WriteHeader"/>），对端回显的也只是低 8 位；
    /// 请求侧若用自增 Int32 计数器，比较完整 32 位会恒不相等，只能等配对超时。</para>
    /// <para>同一连接上并发数超过 256 时，低 8 位会重复，此时从最近入队的请求开始配对，与队列的搜索方向一致。</para>
    /// </remarks>
    public Boolean Match(IMessage request, IMessage response)
    {
        if (request is not DefaultMessage rq || response is not DefaultMessage rs) return false;

        // 仅应答类消息参与配对（响应识别下放协议 matcher，无方向协议可用恒真 matcher）
        if (!rs.Reply) return false;

        return (rq.Sequence & 0xFF) == (rs.Sequence & 0xFF);
    }
    #endregion
}
