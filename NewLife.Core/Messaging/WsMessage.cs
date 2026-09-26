using System.Buffers;
using NewLife.Buffers;
using NewLife.Data;

namespace NewLife.Messaging;

/// <summary>WebSocket消息类型</summary>
public enum WebSocketMessageType
{
    /// <summary>附加数据</summary>
    Data = 0,

    /// <summary>文本数据</summary>
    Text = 1,

    /// <summary>二进制数据</summary>
    Binary = 2,

    /// <summary>连接关闭</summary>
    Close = 8,

    /// <summary>心跳</summary>
    Ping = 9,

    /// <summary>心跳响应</summary>
    Pong = 10,
}

/// <summary>WebSocket 消息</summary>
/// <remarks>
/// <para>承载 WebSocket 帧协议字段（FIN/opcode/掩码键），体的读写经 <see cref="Message"/> 协作面（整帧为内存视图、头先行为流式）。</para>
/// <para><b>掩码</b>：服务端→客户端方向不加掩码；客户端→服务端方向必须掩码——发送时未指定 <see cref="MaskKey"/> 则自动生成随机密钥（帧内临时，不写回消息）；接收侧保存客户端帧的掩码键，由消费方解码（参考 <see cref="WebSocketCodec"/> 的说明）。</para>
/// </remarks>
public class WsMessage : Message
{
    #region 属性
    /// <summary>消息是否结束（FIN）。接收侧反映帧的 FIN 位；发送路径当前恒置 1（不支持发送分片）</summary>
    public Boolean Fin { get; set; }

    /// <summary>消息类型（opcode）</summary>
    public WebSocketMessageType Type { get; set; } = WebSocketMessageType.Binary;

    /// <summary>掩码键。接收侧为客户端帧的掩码键（解码用）；发送侧服务端忽略、客户端未指定时自动生成</summary>
    public Byte[]? MaskKey { get; set; }

    /// <summary>关闭状态。仅 Close 帧经 <see cref="TryReadCloseStatus"/> 解析后有效</summary>
    public Int32 CloseStatus { get; set; }

    /// <summary>关闭状态描述。仅 Close 帧经 <see cref="TryReadCloseStatus"/> 解析后有效</summary>
    public String? StatusDescription { get; set; }
    #endregion

    #region 方法
    /// <summary>解析 WebSocket 帧头并填充当前实例。数据不足返回 false，失败路径不产生副作用</summary>
    /// <remarks>帧字段的读写属于消息类；掩码解码由消费方（<see cref="Demask"/>）执行。</remarks>
    /// <param name="buffer">帧首窗口（只读序列，可跨段）</param>
    /// <param name="bodyLength">解析到的负载长度</param>
    /// <param name="headerSize">帧头字节数（含掩码键）</param>
    /// <returns>是否解析成功</returns>
    public Boolean TryParse(ReadOnlySequence<Byte> buffer, out Int64 bodyLength, out Int32 headerSize) => TryParse(buffer, out bodyLength, out headerSize, out _);

    /// <summary>解析 WebSocket 帧头并填充当前实例</summary>
    /// <remarks>
    /// <para>帧字段的读写属于消息类；掩码解码由消费方（<see cref="Demask"/>）执行。</para>
    /// <para>返回 false 时用 <paramref name="invalid"/> 区分“数据不足”与“帧损坏”：前者等待更多数据，后者必须立即报错，
    /// 否则连接会一直等待一个永远不会完整的帧。</para>
    /// </remarks>
    /// <param name="buffer">帧首窗口（只读序列，可跨段）</param>
    /// <param name="bodyLength">解析到的负载长度</param>
    /// <param name="headerSize">帧头字节数（含掩码键）</param>
    /// <param name="invalid">是否为损坏帧（长度非法）</param>
    /// <returns>是否解析成功</returns>
    public Boolean TryParse(ReadOnlySequence<Byte> buffer, out Int64 bodyLength, out Int32 headerSize, out Boolean invalid)
    {
        invalid = false;
        bodyLength = 0;
        headerSize = 0;
        if (buffer.Length < 2) return false;

        var reader = new SequenceReader<Byte>(buffer);
        if (!reader.TryRead(out var b0) || !reader.TryRead(out var b1)) return false;

        // 第1字节：FIN(1) RSV1-3(3) OPCODE(4)；第2字节：MASK(1) + 长度(7)
        // RFC 6455 §5.2：未协商扩展时 RSV1-3 必须为 0；保留 opcode（3~7 / 11~15）必须导致连接失败
        if ((b0 & 0x70) != 0)
        {
            invalid = true;
            return false;
        }

        var fin = (b0 & 0x80) != 0;
        var opcode = (Byte)(b0 & 0x0F);
        if (opcode is not (0 or 1 or 2 or 8 or 9 or 10))
        {
            invalid = true;
            return false;
        }
        var masked = (b1 & 0x80) != 0;
        var len = (Int64)(b1 & 0x7F);

        // 扩展长度：126 → 2 字节；127 → 8 字节（均网络序）
        var fieldLen = 2;
        if (len == 126)
        {
            if (reader.Remaining < 2) return false;
            if (!reader.TryReadBigEndian(out UInt16 len16)) return false;
            len = len16;
            fieldLen += 2;
        }
        else if (len == 127)
        {
            if (reader.Remaining < 8) return false;
            if (!reader.TryReadBigEndian(out Int64 len64)) return false;
            len = len64;
            fieldLen += 8;
        }

        // 长度最高位为 1（负数）：损坏帧，必须与“数据不足”区分开
        if (len < 0)
        {
            invalid = true;
            return false;
        }

        // 掩码键（4 字节），随头部一并消费
        Byte[]? masks = null;
        if (masked)
        {
            if (reader.Remaining < 4) return false;

            masks = new Byte[4];
            for (var i = 0; i < 4; i++)
            {
                if (!reader.TryRead(out masks[i])) return false;
            }
            fieldLen += 4;
        }

        // 全部校验通过后才写入实例字段，失败路径不产生副作用
        Fin = fin;
        Type = (WebSocketMessageType)opcode;
        MaskKey = masks;
        bodyLength = len;
        headerSize = fieldLen;
        return true;
    }

    /// <summary>写入 WebSocket 帧头（FIN/OPCODE/长度/[掩码键]，均为大端）</summary>
    /// <param name="header">头部目标跨度（至少所需字节）</param>
    /// <param name="bodyLength">负载长度</param>
    /// <param name="maskKey">掩码键（4 字节时写入并置掩码位；null 不掩码）</param>
    /// <returns>帧头字节数</returns>
    public Int32 WriteHeader(Span<Byte> header, Int64 bodyLength, Byte[]? maskKey)
    {
        var masked = maskKey != null && maskKey.Length >= 4;
        var size = bodyLength switch
        {
            < 126 => 1 + 1,
            <= 0xFFFF => 1 + 1 + 2,
            _ => 1 + 1 + 8,
        };
        if (masked) size += 4;

        var writer = new SpanWriter(header) { IsLittleEndian = false };

        // FIN + OPCODE（当前实现仅支持单帧消息，恒 FIN=1）
        writer.WriteByte((Byte)(0x80 | (Byte)Type));

        // 长度：< 126 单字节；≤ 0xFFFF 用 2 字节；否则用 8 字节（均网络序）
        WriteLength(ref writer, masked ? (Byte)0x80 : (Byte)0, bodyLength);

        if (masked) writer.Write(maskKey!);

        return size;
    }

    /// <summary>写入负载长度（网络序大端）。<paramref name="maskBit"/> 为 0x80 时置掩码位</summary>
    /// <param name="writer">目标写入器（须为大端；<see cref="SpanWriter"/> 为可变 ref struct，必须按引用传递，否则位置推进丢失）</param>
    /// <param name="maskBit">掩码位（0 或 0x80）</param>
    /// <param name="length">负载长度</param>
    private static void WriteLength(ref SpanWriter writer, Byte maskBit, Int64 length)
    {
        if (length < 126)
        {
            writer.WriteByte((Byte)((Byte)length | maskBit));
        }
        else if (length <= 0xFFFF)
        {
            writer.WriteByte((Byte)(126 | maskBit));
            writer.Write((Int16)length);
        }
        else
        {
            writer.WriteByte((Byte)(127 | maskBit));
            writer.Write(length);
        }
    }

    /// <summary>对消息负载按掩码键解码（原地 XOR，链式负载跨段连续）</summary>
    /// <remarks>
    /// <para><b>整帧（内存体）路径</b>：对负载原地解码（帧内字节独享，属破坏性操作）；未持掩码键或负载为空时无操作。</para>
    /// <para><b>流式体</b>：本方法不处理（数据未到齐时逐窗解码无法保持偏移连续），请先读满物化（<c>ReadAllAsync</c> 后 <c>SetBody</c>）再解码。</para>
    /// <para>服务端方向接收客户端帧时调用；服务端自身发送与客户端接收均无掩码，无需调用。</para>
    /// </remarks>
    /// <returns>是否执行了解码</returns>
    public Boolean Demask()
    {
        var masks = MaskKey;
        var body = Payload;
        if (masks == null || masks.Length < 4 || body == null || body.Total == 0) return false;

        var offset = 0;
        for (var node = body; node != null; node = node.Next)
        {
            var data = node.GetSpan();
            for (var i = 0; i < data.Length; i++)
            {
                data[i] = (Byte)(data[i] ^ masks[(Int32)((offset + i) & 3)]);
            }
            offset += data.Length;
        }

        return true;
    }

    /// <summary>从消息体读取关闭状态码与描述（RFC 6455：2 字节网络序状态码 + UTF8 原因，可为空）</summary>
    /// <returns>是否解析成功（体为内存模式且至少 2 字节）</returns>
    public Boolean TryReadCloseStatus()
    {
        var body = Payload;
        if (body == null || body.Total < 2) return false;

        CloseStatus = (body[0] << 8) | body[1];
        StatusDescription = body.Total > 2 ? body.ToStr(null, 2) : null;

        return true;
    }
    #endregion
}
