using System.Buffers;
using NewLife.Data;
using NewLife.Http;

namespace NewLife.Messaging;

/// <summary>WebSocket 消息编解码器（RFC 6455 帧格式）。帧格式：FIN/OPCODE + 长度（1/2/8 字节大端）+ [掩码 4 字节] + 负载</summary>
/// <remarks>
/// <para>无状态、可跨连接共享；<see cref="IsServer"/> 只决定<b>发送</b>方向的掩码（服务端不加掩码、客户端自动加随机掩码），解析侧对带掩码与不带掩码的帧都接受，由消费方依据 <see cref="WsMessage.MaskKey"/> 决定是否解码。</para>
/// <para><b>掩码解码</b>：解析产出的 <see cref="WsMessage.MaskKey"/> 非空时，消费方须对负载按掩码解码（每字节 XOR 密钥，<c>data[i] ^= key[i % 4]</c>，链式负载跨段连续）。整帧路径可原地解码；流式路径建议物化后解码。<b>此条只约束直接消费本 codec 的调用方，且解码只可执行一次</b>（<see cref="WsMessage.Demask"/> 非幂等）；经 <c>Http/WebSocket</c>（服务端）与 <c>WebSocketClient</c>（客户端）交付的消息，框架已在交付前完成解码，此时 <see cref="WsMessage.MaskKey"/> 仅作诊断观察，业务侧不得再次解码。</para>
/// <para><b>不要直接用作 <c>SessionBase.Protocol</c></b>：本 codec 不做掩码解码，会话层也没有解码时机（负载可能还是流式的），
/// 直接装配会把未解码的掩码负载静默交给业务。WebSocket 通道请走 <c>Http/WebSocket</c>（服务端）与 <c>WebSocketClient</c>（客户端），
/// 两者在交付前完成解码与分片重组。</para>
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
    /// <returns>头部不足返回 null（不消费、不产生对象）；长度非法返回 <see cref="ParseResult.Invalid"/>（帧损坏须立即报错）。FIN=0 的分片帧照常产出，Fin 字段标识</returns>
    /// <remarks>在只读序列上顺序读取，不拼读、不物化；掩码键挂在消息上，负载由消费方解码。</remarks>
    public ParseResult? TryParse(ReadOnlySequence<Byte> buffer)
    {
        // 帧字段由消息类自行解析（消息定义即协议），帧层只负责装配。
        // 先做无副作用的头部探测：帧头未到齐时直接返回，不构造随即被丢弃的消息对象
        if (!WsMessage.TryReadHeader(buffer, out var fin, out var type, out var maskKey, out var bodyLength, out var headerSize, out var invalid))
            return invalid ? new ParseResult { Invalid = true } : null;

        var message = new WsMessage { Fin = fin, Type = type, MaskKey = maskKey };

        return new ParseResult { Message = message, HeaderSize = headerSize, BodyLength = bodyLength };
    }

    /// <summary>整帧构建（帧头 + 负载）。构建不改动消息数据</summary>
    /// <param name="message">消息（<see cref="WsMessage"/> 提供类型与掩码键；其他消息按二进制帧处理）</param>
    /// <returns>整帧拥有句柄，调用方负责 Dispose</returns>
    /// <remarks>
    /// <para><b>服务端方向</b>：已预留的负载零拷贝借位共享（帧头落在预留区）；其余以新头节点挂接负载链。两种策略都不改动消息负载。</para>
    /// <para><b>客户端方向</b>：掩码是原地 XOR（破坏性），必须独占——新分配头部+负载，拷贝的同时掩码，源负载不受影响。</para>
    /// <para><b>时效</b>：服务端结果可能引用消息负载缓冲，帧发送完成前不得复用或改写。</para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">消息体为流式模式（请使用 BuildHeader + 流式发送）</exception>
    public IOwnerPacket? Build(IMessage message)
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
        // 线上掩码键固定 4 字节：按数组实际长度算头长，会与 WriteHeader 写出的字节数不一致，导致帧错位
        if (masks != null) size += 4;

        IOwnerPacket pk;
        if (masks == null)
        {
            // 无掩码：已预留的拥有句柄零拷贝借位共享，其余新头节点挂接负载链
            pk = body.PrepareHeader(size);
        }
        else
        {
            // 掩码是原地 XOR（破坏性），必须独占：新分配头部+负载，拷贝的同时掩码，源负载不受影响
            pk = new OwnerPacket(size + len);
            var span = pk.GetSpan();
            if (len > 0)
            {
                body!.ReadBytes(span[size..]);
                ApplyMask(span[size..], masks, 0);
            }
        }

        // 帧头由消息类写入（FIN/OPCODE/长度/掩码键；非 WsMessage 消息按二进制帧处理）
        var header = ws ?? new WsMessage { Type = WebSocketMessageType.Binary };
        header.WriteHeader(pk.GetSpan(), len, masks);

        return pk;
    }

    /// <summary>仅构建头部数据包，声明负载长度（头 + 流式体发送）</summary>
    /// <param name="message">消息（<see cref="WsMessage"/> 提供类型；其他消息按二进制帧处理）</param>
    /// <param name="bodyLength">负载字节数（0 ~ 2147483647）</param>
    /// <returns>头部数据包，调用方负责 Dispose</returns>
    /// <exception cref="ArgumentOutOfRangeException">长度为负或超过 32 位协议上限</exception>
    /// <exception cref="NotSupportedException">客户端方向（带掩码的帧不支持流式发送，请使用整帧构建）</exception>
    public IOwnerPacket BuildHeader(IMessage message, Int64 bodyLength)
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
    /// <summary>判断关闭状态码是否允许出现在线路上（RFC 6455 §7.4.1）</summary>
    /// <param name="closeStatus">关闭状态码</param>
    /// <returns>可发送返回 true</returns>
    /// <remarks>1000~1003、1007~1014 为协议与 IANA 已分配值，3000~4999 供应用与注册使用；
    /// 1004/1005/1006/1015 属保留值，0~999 与 1016~2999 未分配，端点均禁止发送——
    /// 发送这类码会被对端判定为协议错误，把优雅关闭变成异常关闭</remarks>
    internal static Boolean IsSendableCloseStatus(Int32 closeStatus) => closeStatus switch
    {
        >= 1000 and <= 1003 => true,
        >= 1007 and <= 1014 => true,
        >= 3000 and <= 4999 => true,
        _ => false,
    };

    /// <summary>构造关闭帧正文：2 字节网络序状态码加 UTF-8 原因（RFC 6455 §5.5.1）</summary>
    /// <param name="closeStatus">关闭状态码</param>
    /// <param name="statusDescription">原因描述，可为空</param>
    /// <returns>关闭帧正文数据包，调用方负责 Dispose</returns>
    /// <remarks>不可发送的状态码退化为无负载（即“无状态码关闭帧”语义）；原因按控制帧上限截断到 123 字节且不切断多字节字符</remarks>
    internal static IPacket BuildClosePayload(Int32 closeStatus, String? statusDescription)
    {
        if (!IsSendableCloseStatus(closeStatus))
        {
            NewLife.Log.XTrace.WriteLine("WebSocket 关闭状态码 {0} 不允许出现在线路上，改为发送无状态码关闭帧", closeStatus);

            return new ArrayPacket(new Byte[0]);
        }

        var desc = (statusDescription ?? String.Empty).GetBytes();

        // RFC 6455 §5.5：控制帧负载不得超过 125 字节——状态码 2 字节 + 原因最多 123 字节。
        // 不截断会发出非法控制帧，对端（含本库服务端）会按协议错误 1002 关闭，优雅关闭变成异常关闭
        if (desc.Length > 123) desc = TruncateUtf8(desc, 123);

        var buf = new Byte[2 + desc.Length];
        buf[0] = (Byte)(closeStatus >> 8);
        buf[1] = (Byte)closeStatus;
        desc.CopyTo(buf, 2);

        return new ArrayPacket(buf);
    }

    /// <summary>按 UTF-8 字符边界截断字节，不切断多字节字符</summary>
    /// <param name="bytes">原始字节</param>
    /// <param name="max">最大字节数</param>
    /// <returns>截断结果</returns>
    private static Byte[] TruncateUtf8(Byte[] bytes, Int32 max)
    {
        var len = max;

        // 续字节（10xxxxxx）不能作为起点：回退到该字符首字节，否则截出半个字符（对端读到非法 UTF-8）
        while (len > 0 && (bytes[len] & 0xC0) == 0x80) len--;

        var buf = new Byte[len];
        Array.Copy(bytes, buf, len);

        return buf;
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
