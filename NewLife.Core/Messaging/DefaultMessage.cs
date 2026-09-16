using System.Buffers;
using NewLife.Buffers;
using NewLife.Data;

namespace NewLife.Messaging;

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
/// </remarks>
public class DefaultMessage : Message
{
    #region 属性
    /// <summary>标记位。可用于标识消息数据类型DataKinds（非强制），内置0标识字符串，默认1标识二进制</summary>
    public Byte Flag { get; set; } = (Byte)DataKinds.Packet;

    /// <summary>序列号。匹配请求和响应，仅低8位有效</summary>
    public Int32 Sequence { get; set; }

    /// <summary>解析数据时的原始报文</summary>
    private IPacket? _raw;

    /// <summary>帧头副本缓冲。拥有帧被切片作废后，与负载组成展示链供事件读取（实例内复用，不持有缓冲引用）</summary>
    private readonly Byte[] _head = new Byte[8];
    #endregion

    #region 构造
    /// <summary>从池中借出消息实例（兼容旧版）。消息已不再池化，直接新建</summary>
    /// <returns>消息实例</returns>
    /// <remarks>仅供基于旧版编译的库使用；新代码请直接 <c>new DefaultMessage()</c>。</remarks>
    [Obsolete("消息已不再池化，请直接 new DefaultMessage()。")]
    public static DefaultMessage Rent() => new();

    /// <summary>归还消息实例（兼容旧版）。消息已不再池化，直接释放</summary>
    /// <param name="msg">消息实例</param>
    /// <remarks>仅供基于旧版编译的库使用；新代码请直接 <c>Dispose()</c>。</remarks>
    [Obsolete("消息已不再池化，请直接 Dispose()。")]
    public static void Return(DefaultMessage? msg) => msg?.Dispose();

    /// <summary>释放资源</summary>
    /// <param name="disposing">是否释放托管资源</param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _raw = null;
        }
    }
    #endregion

    #region 方法
    /// <summary>根据请求创建配对的响应消息</summary>
    /// <returns>响应消息实例</returns>
    public override IMessage CreateReply()
    {
        if (Reply) throw new InvalidOperationException("Cannot create response message based on response message");

        var msg = CreateInstance() as DefaultMessage ?? new DefaultMessage();
        msg.Flag = Flag;
        msg.Reply = true;
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

    /// <summary>从数据包中读取消息（主入口）</summary>
    /// <param name="pk">完整帧数据（帧首节点需含完整协议头；PacketCodec 输出的帧首已保证，直调可为链式）</param>
    /// <returns>是否成功解析</returns>
    /// <remarks>
    /// 头部最多 8 字节：帧首节点足够时直接引用（<see cref="IPacket.GetSpan"/>）；不足且为链式时拼入栈缓冲兼容（不物化整帧）。
    /// 负载沿帧切片（<see cref="IPacket.Slice(Int32, Int32)"/>），共享切片使 Payload 获得独立引用（消息 Dispose/Reset 时唯一归还）；单段与链式帧均支持。
    /// 本方法不释放入参，帧句柄由调用方释放；借阅视图只取视图。
    /// 事件期间展示的完整帧（<see cref="GetRaw"/>）改用“帧头副本 + 负载”展示链，帧头为实例内复用缓冲，不持有缓冲引用。
    /// </remarks>
    public override Boolean Read(IPacket pk)
    {
        if (pk == null || pk.Total < 4)
            throw new ArgumentOutOfRangeException(nameof(pk), "The length of the packet header is less than 4 bytes");

        var total = pk.Total;

        // 头部最多 8 字节：帧首节点足够时直接引用；不足且为链式时拼入栈缓冲（兼容直调链式帧；PacketCodec 输出帧首已保证）
        Span<Byte> buf = stackalloc Byte[8];
        var span = pk.GetPrefix(buf, 8);

        var size = ParseHeader(span, out var len);
        if (size + len > total)
            throw new ArgumentOutOfRangeException(nameof(pk), $"The frame length {total} is less than {size + len} bytes");

        if (pk is OwnerPacket)
        {
            // 负载：共享切片获得独立引用（零拷贝），消息统一持有；入参帧句柄由调用方释放
            Payload = pk.Slice(size, len);

            // 帧头副本 + 负载组成展示链，供事件期间读取原始报文；头副本为实例内复用缓冲，不持有帧引用
            span[..size].CopyTo(_head);
            _raw = new ArrayPacket(_head, 0, size) { Next = Payload };
        }
        else
        {
            // 借阅视图：不持有引用，仅在本轮同步链路内有效
            Payload = pk.Slice(size, len);
            _raw = pk;
        }

        return true;
    }

    /// <summary>解析头部，返回头部总长度与负载长度，并填充状态位/序列号/类型</summary>
    /// <param name="header">头部字节（普通4字节/超大包8字节）</param>
    /// <param name="len">负载长度</param>
    /// <returns>头部总长度（4 或 8）</returns>
    private Int32 ParseHeader(ReadOnlySpan<Byte> header, out Int32 len)
    {
        // 状态位：高2位 00请求 / 01单向 / 10响应 / 11响应+错误
        var mode = header[0] >> 6;
        Reply = mode >= 2;
        Error = mode == 3;
        OneWay = mode == 1;

        // 低6位数据类型 + 1字节序列号
        Flag = (Byte)(header[0] & 0b0011_1111);
        Sequence = header[1];

        // 负载长度（2字节小端；0xFFFF 扩展需 8 字节头）
        var need = header[2] == 0xFF && header[3] == 0xFF ? 8 : 4;
        if (header.Length < need) throw new ArgumentOutOfRangeException(nameof(header), "The length of the packet header is less than 8 bytes");

        return ReadLength(header, out len);
    }

    /// <summary>读取负载长度（2字节小端；Length=0xFFFF 时后续4字节为正式长度）。返回头部大小 4/8</summary>
    /// <param name="header">头部字节（须满足所需长度）</param>
    /// <param name="len">负载长度</param>
    /// <returns>头部总长度（4 或 8）</returns>
    private static Int32 ReadLength(ReadOnlySpan<Byte> header, out Int32 len)
    {
        len = header[2] | (header[3] << 8);
        if (len != 0xFFFF) return 4;

        len = (header[7] << 24) | (header[6] << 16) | (header[5] << 8) | header[4];
        return 8;
    }

    /// <summary>尝试从数据包中读取消息（安全版本）</summary>
    /// <param name="pk">原始数据包</param>
    /// <param name="message">成功时返回解析后的消息</param>
    /// <returns>是否成功解析</returns>
    public static Boolean TryRead(IPacket pk, out DefaultMessage? message)
    {
        message = null;
        if (pk == null || pk.Total < 4) return false;

        try
        {
            message = new DefaultMessage();
            return message.Read(pk);
        }
        catch
        {
            message = null;
            return false;
        }
    }

    /// <summary>把消息转为封包</summary>
    /// <returns>序列化后的数据包，调用方负责 Dispose</returns>
    /// <remarks>拥有负载且前置空间足够时原地扩展头部（零拷贝）；否则新建头部包，负载作为后继链节点。</remarks>
    public override IPacket ToPacket()
    {
        var body = Payload;
        var len = 0;
        if (body != null) len = body.Total;

        // 增加4字节头部，如果负载数据之前有足够空间则直接使用，否则新建数据包形成链式结构
        var size = len < 0xFFFF ? 4 : 8;
        var pk = body.ExpandHeader(size);

        // 标记位
        var header = pk.GetSpan();
        var b = Flag & 0b0011_1111;
        if (Reply) b |= 0x80;
        if (Error || OneWay) b |= 0x40;
        header[0] = (Byte)b;

        // 序列号
        header[1] = (Byte)(Sequence & 0xFF);

        if (len < 0xFFFF)
        {
            // 2字节长度，小端字节序
            header[2] = (Byte)(len & 0xFF);
            header[3] = (Byte)(len >> 8);
        }
        // 支持64k以上超大包
        else
        {
            header[2] = 0xFF;
            header[3] = 0xFF;

            // 再来4字节写长度
            //pk.Data.Write((UInt32)len, pk.Offset + 4, true);
            //BinaryPrimitives.WriteInt32LittleEndian(header[4..], len);
            var writer = new SpanWriter(header) { IsLittleEndian = true };
            writer.Advance(4);
            writer.Write(len);
        }

        return pk;
    }

    /// <summary>重置消息状态</summary>
    public override void Reset()
    {
        base.Reset();

        Flag = (Byte)DataKinds.Packet;
        Sequence = 0;
        _raw = null;
    }
    #endregion

    #region 辅助
    /// <summary>获取数据包长度（帧首节点含完整头部时直读；不足且链式时拼入栈缓冲）</summary>
    /// <param name="pk">数据包（PacketCodec 缓存的帧首；直调可为链式）</param>
    /// <returns>完整消息长度（可能大于现有数据）；返回0表示头部不足无法定界</returns>
    public static Int32 GetLength(IPacket pk)
    {
        // 帧头可能跨节点：前缀拼读（大包路径需要 8 字节）
        Span<Byte> buf = stackalloc Byte[8];

        return GetLength(pk.GetPrefix(buf, 8));
    }

    /// <summary>获取数据包长度</summary>
    /// <param name="span">数据片段</param>
    /// <returns>完整消息长度（可能大于现有数据）；返回0表示头部不足无法定界</returns>
    public static Int32 GetLength(ReadOnlySpan<Byte> span)
    {
        if (span.Length < 4) return 0;

        var reader = new SpanReader(span) { IsLittleEndian = true };
        reader.Advance(2);

        // 小于64k，直接返回
        var len = reader.ReadUInt16();
        if (len < 0xFFFF) return 4 + len;

        // 超过64k的超大数据包，再来4个字节
        if (span.Length < 8) return 0;

        return 8 + reader.ReadInt32();
    }

    /// <summary>获取数据包长度（只读序列版本，供流式帧层跨段定界）</summary>
    /// <param name="buffer">帧首窗口（只读序列，可跨段）</param>
    /// <returns>完整消息长度（可能大于现有数据）；返回0表示头部不足无法定界</returns>
    /// <remarks>与链式版本同模式：在只读序列上顺序读取长度字段，不要求头部连续。</remarks>
    public static Int32 GetLength(ReadOnlySequence<Byte> buffer)
    {
        if (buffer.Length < 4) return 0;

        // 在只读序列上顺序读取：跳过状态位/序列号，读取 2 字节小端长度；扩展长度跨段直读
        var reader = new SequenceReader<Byte>(buffer);
        reader.Advance(2);

        if (!reader.TryReadLittleEndian(out UInt16 len)) return 0;

        // 小于64k，直接返回
        if (len < 0xFFFF) return 4 + len;

        // 超过64k的超大数据包，再来4个字节
        if (reader.Remaining < 4) return 0;

        return reader.TryReadLittleEndian(out Int32 len32) ? 8 + len32 : 0;
    }

    /// <summary>获取解析数据时的原始报文视图（事件期间展示当前帧）</summary>
    /// <returns>原始数据包视图；拥有帧为“帧头副本+负载”展示链，仅在本轮同步链路内有效</returns>
    public IPacket? GetRaw() => _raw;

    /// <summary>消息摘要</summary>
    /// <returns>消息的字符串表示</returns>
    public override String ToString() => $"{Flag:X2} Seq={Sequence:X2} {Payload}";
    #endregion
}