using System.Buffers;
using NewLife.Buffers;
using NewLife.Data;

namespace NewLife.Messaging;

/// <summary>长度字段编解码器。帧格式：[头部(Offset字节)] [长度字段(Size字节)] [负载]，长度字段值等于负载字节数</summary>
/// <remarks>
/// <para>无状态、可跨连接共享。解析产出纯负载消息（<see cref="Message"/> 基类，无协议字段）；构建在负载前插入长度字段，头部 Offset 字节按零填充。</para>
/// <para>适配 MQTT、LwM2M 等以长度定界、固定头结构由上层自行解读的协议。长度字段：1/2/4 小端（正值，默认 2）、1/2/4 大端（负值）、7 位压缩变长（0）。</para>
/// </remarks>
/// <example>
/// <code>
/// // MQTT 风格：2 字节固定头 + 2 字节小端长度 + 负载
/// var codec = new LengthFieldCodec { Offset = 2 };
///
/// // 接收：定界并构造消息（体由帧层绑定）
/// if (codec.TryParse(buffer) is { } rs) { var msg = rs.Message; }
/// </code>
/// </example>
public class LengthFieldCodec : IMessageCodec, IMessageMatcher
{
    #region 属性
    /// <summary>长度字段的偏移量（字节数）。设定帧格式为 [头部(Offset字节)] [长度字段] [负载]，编解码时自动跳过该头部且不计入负载</summary>
    public Int32 Offset { get; set; }

    /// <summary>长度字段占据的字节数。1/2/4 小端（默认 2）；-1/-2/-4 大端；0 表示 7 位压缩变长</summary>
    public Int32 Size { get; set; } = 2;
    #endregion

    #region 方法
    /// <summary>定界并构造消息。读取长度字段，构造纯负载消息</summary>
    /// <param name="buffer">帧首窗口（只读序列，可跨段）</param>
    /// <returns>解析结果；头部不足或长度字段非法时返回 null（不消费、不产生对象）</returns>
    /// <remarks>在只读序列上顺序读取，不拼读、不物化；4 字节长度最高位为 1（负数）或变长编码超过 32 位视为损坏帧。</remarks>
    public ParseResult? TryParse(ReadOnlySequence<Byte> buffer)
    {
        // 跳过头部后至少要有长度字段首字节
        if (buffer.Length <= Offset) return null;

        var reader = new SequenceReader<Byte>(buffer);
        if (Offset > 0) reader.Advance(Offset);

        Int64 len;
        var fieldLen = 0;
        switch (Size)
        {
            case 0:
                // 7 位压缩变长：低位在前，最高位为继续标志；最多 5 字节（32 位表示范围）
                if (!reader.TryReadEncodedInt(out var len32)) return null;
                len = len32;
                fieldLen = (Int32)reader.Consumed - Offset;
                break;
            case 1:
            case -1:
                if (!reader.TryRead(out Byte b8)) return null;
                len = b8;
                fieldLen = 1;
                break;
            case 2:
                if (!reader.TryReadLittleEndian(out UInt16 v16)) return null;
                len = v16;
                fieldLen = 2;
                break;
            case -2:
                if (!reader.TryReadBigEndian(out UInt16 v16b)) return null;
                len = v16b;
                fieldLen = 2;
                break;
            case 4:
                if (!reader.TryReadLittleEndian(out Int32 v32)) return null;
                if (v32 < 0) return null;
                len = v32;
                fieldLen = 4;
                break;
            case -4:
                if (!reader.TryReadBigEndian(out Int32 v32b)) return null;
                if (v32b < 0) return null;
                len = v32b;
                fieldLen = 4;
                break;
            default:
                throw new NotSupportedException($"不支持的 Size 值：{Size}");
        }

        // 变长编码可以编码出负数（如 FF FF FF FF 0F → -1）：帧长度必须非负，负值按损坏帧上报，
        // 不能当成“数据不足”默默等待，也不能当成“帧已到齐”去切帧
        if (len < 0) return new ParseResult { Invalid = true };

        var message = new Message();
        return new ParseResult { Message = message, HeaderSize = Offset + fieldLen, BodyLength = len };
    }

    /// <summary>整帧构建（长度字段 + 负载）。构建不消费消息负载</summary>
    /// <param name="message">消息</param>
    /// <returns>整帧拥有句柄，调用方负责 Dispose</returns>
    /// <remarks>
    /// <para>已预留的负载零拷贝借位共享（帧头落在预留区）；其余以新头节点挂接负载链。两种策略都不改动消息负载。</para>
    /// <para><b>时效</b>：结果可能引用消息负载缓冲，帧发送完成前不得复用或改写。</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">消息为 null</exception>
    /// <exception cref="InvalidOperationException">消息体为流式模式（请使用 BuildHeader + 流式发送）</exception>
    /// <exception cref="ArgumentOutOfRangeException">负载长度超出长度字段表示范围</exception>
    public IOwnerPacket? Build(IMessage message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        if (message.Body is { IsStreaming: true }) throw new InvalidOperationException("流式消息体无法整帧构建，请使用 BuildHeader + 流式发送");

        var body = message.Payload;
        var len = body?.Total ?? 0;
        var fieldLen = Size == 0 ? GetVarintLength(len) : Math.Abs(Size);

        // 增加协议头：已预留的拥有句柄零拷贝借位共享，其余新头节点挂接负载链
        var pk = body.PrepareHeader(Offset + fieldLen);
        try
        {
            var writer = new SpanWriter(pk.GetSpan()) { IsLittleEndian = Size > 0 };
            if (Offset > 0) writer.Fill(0, Offset);
            WriteLength(ref writer, len);

            return pk;
        }
        catch
        {
            // 长度超出字段表示范围等异常：构建产物已持有借位/新头句柄，异常路径必须归还，否则池化缓冲泄漏
            pk.TryDispose();
            throw;
        }
    }

    /// <summary>仅构建头部数据包，声明消息体长度（头 + 流式体发送）</summary>
    /// <param name="message">消息</param>
    /// <param name="bodyLength">消息体字节数</param>
    /// <returns>头部数据包，调用方负责 Dispose</returns>
    /// <exception cref="ArgumentNullException">消息为 null</exception>
    /// <exception cref="ArgumentOutOfRangeException">长度为负或超出长度字段表示范围</exception>
    public IOwnerPacket BuildHeader(IMessage message, Int64 bodyLength)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        if (bodyLength < 0) throw new ArgumentOutOfRangeException(nameof(bodyLength), "Body length must be non-negative.");

        var fieldLen = Size == 0 ? GetVarintLength(bodyLength) : Math.Abs(Size);
        var pk = new OwnerPacket(Offset + fieldLen);
        try
        {
            var writer = new SpanWriter(pk.GetSpan()) { IsLittleEndian = Size > 0 };
            if (Offset > 0) writer.Fill(0, Offset);
            WriteLength(ref writer, bodyLength);

            return pk;
        }
        catch
        {
            // 长度超出字段表示范围等异常：新建句柄必须归还，否则池化缓冲泄漏
            pk.TryDispose();
            throw;
        }
    }
    #endregion

    #region 辅助
    /// <summary>把负载长度写入目标（按 Size 校验字段容量）</summary>
    /// <param name="writer">目标写入器（<see cref="SpanWriter"/> 为可变 ref struct，必须按引用传递，否则位置推进丢失）</param>
    /// <param name="length">负载长度</param>
    /// <exception cref="ArgumentOutOfRangeException">长度超出长度字段表示范围</exception>
    private void WriteLength(ref SpanWriter writer, Int64 length)
    {
        switch (Size)
        {
            case 0:
                if (length > Int32.MaxValue) throw new ArgumentOutOfRangeException(nameof(length), $"负载长度 {length} 超出变长长度字段的 32 位表示范围");
                writer.WriteEncodedInt((Int32)length);
                break;
            case 1:
            case -1:
                if (length > Byte.MaxValue) throw new ArgumentOutOfRangeException(nameof(length), $"负载长度 {length} 超出 1 字节长度字段上限 255");
                writer.WriteByte((Byte)length);
                break;
            case 2:
            case -2:
                if (length > UInt16.MaxValue) throw new ArgumentOutOfRangeException(nameof(length), $"负载长度 {length} 超出 2 字节长度字段上限 65535");
                writer.Write((UInt16)length);
                break;
            case 4:
            case -4:
                if (length > Int32.MaxValue) throw new ArgumentOutOfRangeException(nameof(length), $"负载长度 {length} 超出 4 字节长度字段上限 2147483647");
                writer.Write((UInt32)length);
                break;
            default:
                throw new NotSupportedException($"不支持的 Size 值：{Size}");
        }
    }

    /// <summary>计算 7 位压缩编码所需字节数</summary>
    /// <param name="value">非负长度值</param>
    /// <returns>编码字节数</returns>
    private static Int32 GetVarintLength(Int64 value)
    {
        var num = (UInt64)value;
        var count = 1;
        while (num >= 0x80)
        {
            count++;
            num >>= 7;
        }

        return count;
    }
    #endregion

    #region 配对
    /// <summary>判断响应是否匹配请求。长度字段帧无配对键，任意响应均视为匹配，适合串行请求-响应</summary>
    /// <param name="request">挂起的请求消息</param>
    /// <param name="response">收到的响应消息</param>
    /// <returns>恒为 true</returns>
    public Boolean Match(IMessage request, IMessage response) => true;
    #endregion
}
