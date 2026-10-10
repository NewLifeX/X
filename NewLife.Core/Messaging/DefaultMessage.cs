using System.Buffers;
using NewLife.Buffers;
﻿namespace NewLife.Messaging;

/// <summary>数据类型。可用于标准消息的Flag</summary>
public enum DataKinds : Byte
{
    /// <summary>字符串</summary>
    String = 0,

    /// <summary>二进制数据包</summary>
    Packet = 1,

    /// <summary>二进制对象</summary>
    Binary = 2,

    /// <summary>Json对象</summary>
    Json = 3,
}

/// <summary>标准消息SRMP</summary>
/// <remarks>
/// 标准协议最大优势是短小，头部定长，没有序列化成本，适用于专业级RPC以及嵌入式通信。
/// 缺点是可读性差，不能适用于字符串通信场景。
///
/// <para><b>协议格式</b>：1 Flag + 1 Sequence + 2 Length + N Payload</para>
/// <list type="bullet">
/// <item><description>1字节标识位：高2位为消息模式（00请求/01单向/10响应/11响应+错误），低6位为数据类型Flag</description></item>
/// <item><description>1字节序列号：用于请求响应包配对</description></item>
/// <item><description>2字节数据长度N：小端字节序，指示后续负载数据长度（不包含头部4字节）</description></item>
/// <item><description>N字节负载数据：数据内容完全由业务决定，最大长度65535=64k</description></item>
/// </list>
///
/// <para><b>示例</b>：Open => OK</para>
/// <code>01-01-04-00-"Open" => 81-01-02-00-"OK"</code>
///
/// <para><b>超大包支持</b>：Length为0xFFFF时，后续4字节为正式长度，以支持超过64k的扩展包</para>
/// <para><b>帧格式</b>：定界/解析/构建由 <see cref="SrmpCodec"/> 承担；本类型为纯数据载体，不承载帧读写。</para>
/// </remarks>
public class DefaultMessage : Message
{
    #region 属性
    /// <summary>标记位。可用于标识消息数据类型DataKinds（非强制），内置0标识字符串，默认1标识二进制</summary>
    public Byte Flag { get; set; } = (Byte)DataKinds.Packet;

    /// <summary>序列号。匹配请求和响应，仅低8位有效</summary>
    public Int32 Sequence { get; set; }
    #endregion

    #region 方法
    /// <summary>解析 SRMP 头部并填充当前实例。数据不足或非法返回 false，失败路径不产生副作用</summary>
    /// <remarks>
    /// 协议字段的读写属于消息类（协议即消息定义）；帧层装配由 <see cref="SrmpCodec"/> 承担。
    /// 在只读序列上顺序读取，不拼读、不物化；扩展长度读出负数（协议上限 Int32.MaxValue）视为损坏帧返回 false。
    /// </remarks>
    /// <param name="buffer">帧首窗口（只读序列，可跨段）</param>
    /// <param name="bodyLength">解析到的负载长度</param>
    /// <param name="headerSize">头部字节数（4 或 8）</param>
    /// <returns>是否解析成功</returns>
    public Boolean TryParse(ReadOnlySequence<Byte> buffer, out Int64 bodyLength, out Int32 headerSize)
        => TryParse(buffer, out bodyLength, out headerSize, out _);

    /// <summary>解析 SRMP 头部并填充当前实例，同时区分“数据不足”与“帧已损坏”</summary>
    /// <param name="buffer">帧首窗口（只读序列，可跨段）</param>
    /// <param name="bodyLength">解析到的负载长度</param>
    /// <param name="headerSize">头部字节数（4 或 8）</param>
    /// <param name="invalid">是否为损坏帧（头部已完整但内容非法）。返回 false 且本值为 false 时表示数据不足</param>
    /// <returns>是否解析成功</returns>
    /// <remarks>帧层对“不足”是等待更多数据，对“损坏”必须立即报错，两者的处置完全不同，不能合为一个 false</remarks>
    public Boolean TryParse(ReadOnlySequence<Byte> buffer, out Int64 bodyLength, out Int32 headerSize, out Boolean invalid)
    {
        if (!TryReadHeader(buffer, out var flag, out var sequence, out var kind, out bodyLength, out headerSize, out invalid)) return false;

        // 全部校验通过后才写入实例字段，失败路径不产生副作用
        Flag = flag;
        Sequence = sequence;
        Kind = kind;
        return true;
    }

    /// <summary>读取 SRMP 头部字段（不写入实例、不产生对象）。供帧层在构造消息之前探测头部是否已就绪</summary>
    /// <param name="buffer">帧首窗口（只读序列，可跨段）</param>
    /// <param name="flag">标记位（高 2 位已被种类占用，本值只含低 6 位数据类型）</param>
    /// <param name="sequence">序列号</param>
    /// <param name="kind">消息种类（头部高 2 位）</param>
    /// <param name="bodyLength">解析到的负载长度</param>
    /// <param name="headerSize">头部字节数（4 或 8）</param>
    /// <param name="invalid">是否为损坏帧（头部已完整但内容非法）。返回 false 且本值为 false 时表示数据不足</param>
    /// <returns>是否解析成功</returns>
    /// <remarks>
    /// 与实例版 <see cref="TryParse(ReadOnlySequence{Byte}, out Int64, out Int32, out Boolean)"/> 共用同一套校验，只是不落到任何实例上。
    /// 分开的原因：帧层在半包（头部未到齐）时也会调用解析，每次都会构造一个随即被丢弃的消息对象；
    /// 逐字节到达的长连接上这笔分配纯属浪费，故先探测、确认头部就绪后再构造消息。
    /// </remarks>
    internal static Boolean TryReadHeader(ReadOnlySequence<Byte> buffer, out Byte flag, out Int32 sequence, out MessageKinds kind, out Int64 bodyLength, out Int32 headerSize, out Boolean invalid)
    {
        flag = 0;
        sequence = 0;
        kind = MessageKinds.Request;
        bodyLength = 0;
        headerSize = 0;
        invalid = false;
        if (buffer.Length < 4) return false;

        var reader = new SequenceReader<Byte>(buffer);
        if (!reader.TryRead(out var b0) || !reader.TryRead(out var b1) || !reader.TryReadLittleEndian(out UInt16 len)) return false;

        // 扩展长度需要 8 字节头，不足时等更多数据
        var size = 4;
        Int64 payloadLen = len;
        if (len == 0xFFFF)
        {
            if (reader.Remaining < 4) return false;

            // 头部已完整而长度仍非法（负数，超出协议上限）：损坏帧，不是数据不足
            if (!reader.TryReadLittleEndian(out Int32 len32) || len32 < 0)
            {
                invalid = true;
                return false;
            }

            size = 8;
            payloadLen = len32;
        }

        flag = (Byte)(b0 & 0b0011_1111);
        sequence = b1;
        kind = (MessageKinds)(b0 >> 6);
        bodyLength = payloadLen;
        headerSize = size;
        return true;
    }

    /// <summary>写入 SRMP 头部（4 或 8 字节，不含负载）</summary>
    /// <param name="header">头部目标跨度（至少 4/8 字节）</param>
    /// <param name="bodyLength">负载长度（0 ~ 2147483647）</param>
    /// <returns>头部字节数（4 或 8）</returns>
    /// <exception cref="ArgumentOutOfRangeException">长度为负或超过 32 位协议上限</exception>
    public Int32 WriteHeader(Span<Byte> header, Int64 bodyLength)
    {
        // 长度字段只有 32 位：负数会写成 4 字节头里的 FF FF（被解析侧当成需要 8 字节扩展头），
        // 超过 32 位上限会在 (Int32) 转换时静默截断，两者都写出与实际体长不符的帧，必须在源头拦住
        if (bodyLength < 0) throw new ArgumentOutOfRangeException(nameof(bodyLength), "Body length must be non-negative.");
        if (bodyLength > Int32.MaxValue) throw new ArgumentOutOfRangeException(nameof(bodyLength), "Body length exceeds the 32-bit protocol limit.");

        var size = bodyLength < 0xFFFF ? 4 : 8;

        // 标记位：高2位消息种类，低6位数据类型
        header[0] = (Byte)((Flag & 0b0011_1111) | ((Int32)Kind << 6));

        // 序列号
        header[1] = (Byte)(Sequence & 0xFF);

        if (size == 4)
        {
            // 2字节长度，小端字节序
            header[2] = (Byte)(bodyLength & 0xFF);
            header[3] = (Byte)(bodyLength >> 8);
        }
        else
        {
            // 支持64k以上超大包：0xFFFF 标记 + 4 字节正式长度
            header[2] = 0xFF;
            header[3] = 0xFF;

            var writer = new SpanWriter(header) { IsLittleEndian = true };
            writer.Advance(4);
            writer.Write((Int32)bodyLength);
        }

        return size;
    }

    /// <summary>根据请求创建配对的响应消息（继承 Flag 与序列号）</summary>
    /// <returns>响应消息实例；当前消息已是应答时返回 null</returns>
    public override IMessage? CreateReply()
    {
        if (Reply) return null;

        var msg = CreateInstance() as DefaultMessage ?? new DefaultMessage();
        msg.Flag = Flag;
        msg.Kind = MessageKinds.Response;
        msg.Sequence = Sequence;

        return msg;
    }

    /// <summary>创建当前类型的新实例</summary>
    /// <returns>新的消息实例</returns>
    protected override Message CreateInstance()
    {
        var type = GetType();
        if (type == typeof(DefaultMessage)) return new DefaultMessage();

        return base.CreateInstance();
    }

    /// <summary>消息摘要</summary>
    /// <returns>消息的字符串表示</returns>
    public override String ToString() => $"{Flag:X2} Seq={Sequence:X2} {Payload}";
    #endregion
}
