using NewLife.Buffers;
using NewLife.Data;
using NewLife.Reflection;

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
    #endregion

    #region 构造
    /// <summary>释放资源</summary>
    /// <param name="disposing">是否释放托管资源</param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        if (disposing) _raw = null;
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

    /// <summary>从数据包中读取消息</summary>
    /// <param name="pk">原始数据包</param>
    /// <returns>是否成功解析</returns>
    public override Boolean Read(IPacket pk)
    {
        _raw = pk;

        var count = pk.Total;
        if (count < 4) throw new ArgumentOutOfRangeException(nameof(pk), "The length of the packet header is less than 4 bytes");

        // 取头部4个字节
        var size = 4;
        var header = pk.GetSpan()[..size];

        // 清理状态位
        Reply = false;
        Error = false;
        OneWay = false;

        // 前2位作为标识位
        Flag = (Byte)(header[0] & 0b0011_1111);
        var mode = header[0] >> 6;
        switch (mode)
        {
            case 0: Reply = false; break;
            case 1: OneWay = true; break;
            case 2: Reply = true; break;
            case 3: Reply = true; Error = true; break;
            default:
                break;
        }

        // 1个字节的序列号
        Sequence = header[1];

        // 负载长度。2字节小端
        var len = (header[3] << 8) | header[2];
        if (size + len > count) throw new ArgumentOutOfRangeException(nameof(pk), $"The packet length {count} is less than {size + len} bytes");

        // 支持超过64k的超大包
        if (len == 0xFFFF)
        {
            size += 4;
            if (count < size) throw new ArgumentOutOfRangeException(nameof(pk), "The length of the packet header is less than 8 bytes");

            // 4字节小端
            len = pk.GetSpan().Slice(size - 4, 4).ToArray().ToInt();
            if (size + len > count) throw new ArgumentOutOfRangeException(nameof(pk), $"The packet length {count} is less than {size + len} bytes");
        }

        // 负载数据
        Payload = pk.Slice(size, len, true);

        return true;
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
    /// <returns>序列化后的数据包</returns>
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

    /// <summary>重置消息状态，用于对象池复用</summary>
    public override void Reset()
    {
        base.Reset();

        Flag = (Byte)DataKinds.Packet;
        Sequence = 0;
        _raw = null;
    }
    #endregion

    #region 辅助
    /// <summary>获取数据包长度</summary>
    /// <param name="pk">数据包</param>
    /// <returns>完整消息长度（包含头部），返回0表示数据不足</returns>
    public static Int32 GetLength(IPacket pk) => GetLength(pk.GetSpan());

    /// <summary>获取数据包长度</summary>
    /// <param name="span">数据片段</param>
    /// <returns>完整消息长度（包含头部），返回0表示数据不足</returns>
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

    /// <summary>获取解析数据时的原始报文</summary>
    /// <returns>原始数据包</returns>
    public IPacket? GetRaw() => _raw;

    /// <summary>消息摘要</summary>
    /// <returns>消息的字符串表示</returns>
    public override String ToString() => $"{Flag:X2} Seq={Sequence:X2} {Payload}";
    #endregion
}