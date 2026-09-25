using System.Buffers;
using NewLife.Data;
using NewLife.Http;

namespace NewLife.Messaging;

/// <summary>WebSocket 消息编解码器（RFC 6455 帧格式）。帧格式：FIN/OPCODE + 长度（1/2/8 字节大端）+ [掩码 4 字节] + 负载</summary>
/// <remarks>
/// <para>无状态、可跨连接共享；<see cref="IsServer"/> 决定掩码方向：服务端接收客户端帧（带掩码）且发送无掩码，客户端反之。</para>
/// <para><b>掩码解码</b>：解析产出的 <see cref="WsMessage.MaskKey"/> 非空时，消费方须对负载按掩码解码（每字节 XOR 密钥，<c>data[i] ^= key[i % 4]</c>，链式负载跨段连续）。整帧路径可原地解码；流式路径建议物化后解码。</para>
/// <para><b>分片</b>：FIN=0 的帧照常解析（Fin=false），分片重组由消费侧（WebSocket 服务端 / WebSocketClient）累积完成。</para>
/// </remarks>
/// <example>
/// <code>
/// // 服务端：接收带掩码的客户端帧、发送无掩码帧
/// var codec = new WebSocketCodec { IsServer = true };
///
/// if (codec.TryParse(buffer) is { } rs) { var msg = rs.Message; }
/// </code>
/// </example>
public class WebSocketCodec : IMessageCodec
{
    #region 属性
    /// <summary>是否服务端。默认 true：接收期望掩码帧、发送不加掩码；客户端置 false：发送自动加掩码</summary>
    public Boolean IsServer { get; set; } = true;
    #endregion

    #region 方法
    /// <summary>定界并构造消息。解析帧头（FIN/OPCODE/长度/掩码键），构造 <see cref="WsMessage"/></summary>
    /// <param name="buffer">帧首窗口（只读序列，可跨段）</param>
    /// <returns>解析结果；头部不足、分片帧（FIN=0）或长度非法时返回 null（不消费、不产生对象）</returns>
    /// <remarks>在只读序列上顺序读取，不拼读、不物化；掩码键挂在消息上，负载由消费方解码。</remarks>
    public ParseResult? TryParse(ReadOnlySequence<Byte> buffer)
    {
        // 帧字段由消息类自行解析（消息定义即协议），帧层只负责装配
        var message = new WsMessage();
        if (!message.TryParse(buffer, out var bodyLength, out var headerSize)) return null;

        return new ParseResult { Message = message, HeaderSize = headerSize, BodyLength = bodyLength };
    }

    /// <summary>整帧构建（帧头 + 负载）。构建成功后消息不再持有体</summary>
    /// <param name="message">消息（<see cref="WsMessage"/> 提供类型与掩码键；其他消息按二进制帧处理）</param>
    /// <returns>整帧数据包，调用方负责 Dispose</returns>
    /// <remarks>
    /// <para>服务端方向不加掩码；客户端方向（<see cref="IsServer"/> 为 false）必须掩码——<see cref="WsMessage.MaskKey"/> 未指定时自动生成随机密钥，并对负载原地 XOR 编码（破坏性操作，链式负载逐段处理）。</para>
    /// <para>拥有帧且前置空间足够时原地扩展头部（零拷贝）；否则新建头部包，负载作为后继链节点。</para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">消息体为流式模式（请使用 BuildHeader + 流式发送）</exception>
    public IPacket? Build(IMessage message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        if (message.Body is { IsStreaming: true }) throw new InvalidOperationException("流式消息体无法整帧构建，请使用 BuildHeader + 流式发送");

        var ws = message as WsMessage;
        var body = message.Payload;
        var len = body?.Total ?? 0;

        // 掩码：仅客户端方向；未指定时生成随机密钥（帧内临时，不写回消息）
        Byte[]? masks = null;
        if (!IsServer)
        {
            masks = ws?.MaskKey;
            if (masks == null || masks.Length < 4)
            {
                masks = new Byte[4];
#if NET6_0_OR_GREATER
                Random.Shared.NextBytes(masks);
#else
                new Random().NextBytes(masks);
#endif
            }
        }

        // 头部大小：FIN+OPCODE(1) + 长度(1/3/9) + 掩码(0/4)
        var size = len switch
        {
            < 126 => 1 + 1,
            <= 0xFFFF => 1 + 1 + 2,
            _ => 1 + 1 + 8,
        };
        if (masks != null) size += masks.Length;

        // 前置空间足够时原地扩展头部（零拷贝），否则新建头部包并把负载作为后继链节点
        var pk = body.ExpandHeader(size);

        // 帧头由消息类写入（FIN/OPCODE/长度/掩码键；非 WsMessage 消息按二进制帧处理）
        var header = ws ?? new WsMessage { Type = WebSocketMessageType.Binary };
        header.WriteHeader(pk.GetSpan(), len, masks);

        // 掩码混淆数据：直接在数据缓冲区修改，避免拷贝。
        // 拥有帧可能在 ExpandHeader 时被原地接管而作废，因此统一从 pk 链上取数据，跳过头部区域
        if (masks != null && len > 0)
        {
            var offset = 0;
            for (var node = pk; node != null; node = node.Next)
            {
                var data = node.GetSpan();
                var start = node == pk ? Math.Min(size, data.Length) : 0;

                ApplyMask(data[start..], masks, offset);
                offset += data.Length - start;
            }
        }

        // 所有权随构建结果转移：消息不再持有体（ExpandHeader 可能已接管源句柄，或将其作为后继链节点）
        message.SetBody((IPacket?)null);

        return pk;
    }

    /// <summary>仅构建头部数据包，声明负载长度（头 + 流式体发送）</summary>
    /// <param name="message">消息（<see cref="WsMessage"/> 提供类型；其他消息按二进制帧处理）</param>
    /// <param name="bodyLength">负载字节数（0 ~ 2147483647）</param>
    /// <returns>头部数据包，调用方负责 Dispose</returns>
    /// <exception cref="ArgumentOutOfRangeException">长度为负或超过 32 位协议上限</exception>
    /// <exception cref="NotSupportedException">客户端方向（带掩码的帧不支持流式发送，请使用整帧构建）</exception>
    public IPacket BuildHeader(IMessage message, Int64 bodyLength)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        if (bodyLength < 0) throw new ArgumentOutOfRangeException(nameof(bodyLength), "Payload length must be non-negative.");
        if (bodyLength > Int32.MaxValue) throw new ArgumentOutOfRangeException(nameof(bodyLength), "Payload length exceeds the 32-bit protocol limit.");
        if (!IsServer) throw new NotSupportedException("带掩码的帧不支持流式发送，请使用整帧构建");

        var size = bodyLength switch
        {
            < 126 => 1 + 1,
            <= 0xFFFF => 1 + 1 + 2,
            _ => 1 + 1 + 8,
        };
        var pk = new OwnerPacket(size);

        // 帧头由消息类写入（服务端方向无掩码；非 WsMessage 消息按二进制帧处理）
        var header = message as WsMessage ?? new WsMessage { Type = WebSocketMessageType.Binary };
        header.WriteHeader(pk.GetSpan(), bodyLength, null);

        return pk;
    }
    #endregion

    #region 辅助
    /// <summary>构造关闭帧正文：2 字节网络序状态码加 UTF-8 原因（RFC 6455 §5.5.1）</summary>
    /// <param name="closeStatus">关闭状态码</param>
    /// <param name="statusDescription">原因描述，可为空</param>
    /// <returns>关闭帧正文数据包，调用方负责 Dispose</returns>
    internal static IPacket BuildClosePayload(Int32 closeStatus, String? statusDescription)
    {
        var desc = (statusDescription ?? String.Empty).GetBytes();
        var buf = new Byte[2 + desc.Length];
        buf[0] = (Byte)(closeStatus >> 8);
        buf[1] = (Byte)closeStatus;
        desc.CopyTo(buf, 2);

        return new ArrayPacket(buf);
    }

    /// <summary>按掩码键对数据原地 XOR 解码/编码（跨段连续：偏移按负载起点累计）</summary>
    /// <param name="data">目标数据</param>
    /// <param name="masks">掩码键（至少 4 字节）</param>
    /// <param name="offset">当前数据在负载中的偏移</param>
    private static void ApplyMask(Span<Byte> data, Byte[] masks, Int64 offset)
    {
        for (var i = 0; i < data.Length; i++)
        {
            data[i] = (Byte)(data[i] ^ masks[(Int32)((offset + i) & 3)]);
        }
    }
    #endregion
}
