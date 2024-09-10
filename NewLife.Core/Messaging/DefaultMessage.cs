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
/// 标准网络封包协议：1 Flag + 1 Sequence + 2 Length + N Payload
/// 1个字节标识位，标识请求、响应、错误等；
/// 1个字节序列号，用于请求响应包配对；
/// 2个字节数据长度N，小端，指示后续负载数据长度（不包含头部4个字节），解决粘包问题；
/// N个字节负载数据，数据内容完全由业务决定，最大长度65535=64k。
/// 如：
/// Open => OK
/// 01-01-04-00-"Open" => 81-01-02-00-"OK"
/// 
/// Length为0xFFFF时，后续4字节为正式长度，以支持超过64k的扩展包
/// </remarks>
public class DefaultMessage : Message
{
    #region 属性
    /// <summary>标记位。可用于标识消息数据类型DataKinds（非强制），内置0标识字符串，默认1标识二进制</summary>
    public Byte Flag { get; set; } = (Byte)DataKinds.Packet;

    /// <summary>序列号，匹配请求和响应</summary>
    public Int32 Sequence { get; set; }

    /// <summary>解析数据时的原始报文</summary>
    private IPacket? _raw;
    #endregion

    #region 方法
    /// <summary>根据请求创建配对的响应消息</summary>
    /// <returns></returns>
    public override IMessage CreateReply()
    {
        if (Reply) throw new Exception("Cannot create response message based on response message");

        var type = GetType();
        var msg = type == typeof(DefaultMessage) ? new DefaultMessage() : type.CreateInstance() as DefaultMessage;
        if (msg == null) throw new InvalidDataException($"Cannot create an instance of type [{type.FullName}]");

        msg.Flag = Flag;
        msg.Reply = true;
        msg.Sequence = Sequence;

        return msg;
    }

    /// <summary>从数据包中读取消息</summary>
    /// <param name="pk"></param>
    /// <returns>是否成功</returns>
    public override Boolean Read(IPacket pk)
    {
        _raw = pk;

        var count = pk.GetTotal();
        if (count < 4) throw new ArgumentOutOfRangeException(nameof(pk), "The length of the packet header is less than 4 bytes");

        // 取头部4个字节
        var size = 4;
        var header = pk.GetSpan()[..size];

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

        // 负载长度
        var len = (header[3] << 8) | header[2];
        if (size + len > count) throw new ArgumentOutOfRangeException(nameof(pk), $"The packet length {count} is less than {size + len} bytes");

        // 支持超过64k的超大包
        if (len == 0xFFFF)
        {
            size += 4;
            if (count < size) throw new ArgumentOutOfRangeException(nameof(pk), "The length of the packet header is less than 8 bytes");

            len = pk.GetSpan().Slice(size - 4, 4).ToArray().ToInt();
            if (size + len > count) throw new ArgumentOutOfRangeException(nameof(pk), $"The packet length {count} is less than {size + len} bytes");
        }

        // 负载数据
        Payload = pk.Slice(size, len);

        return true;
    }

    /// <summary>把消息转为封包</summary>
    /// <returns></returns>
    public override IPacket ToPacket()
    {
        var body = Payload;
        var len = 0;
        if (body != null) len = body.Length;

        // 增加4字节头部，如果负载数据之前有足够空间则直接使用，否则新建数据包形成链式结构
        var size = len < 0xFFFF ? 4 : 8;
        IPacket pk;
        if (body is ArrayPacket ap && ap.Offset >= size)
            pk = new ArrayPacket(ap.Buffer, ap.Offset - size, ap.Length + size) { Next = ap.Next };
        else
            pk = new ArrayPacket(new Byte[size]) { Next = body };

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
            writer.Write(len);
        }

        return pk;
    }
    #endregion

    #region 辅助
    /// <summary>获取数据包长度</summary>
    /// <param name="pk"></param>
    /// <returns></returns>
    public static Int32 GetLength(IPacket pk)
    {
        if (pk.Length < 4) return 0;

        var reader = new SpanReader(pk.GetSpan()) { IsLittleEndian = true };
        reader.Advance(2);

        // 小于64k，直接返回
        //var len = pk.Data.ToUInt16(pk.Offset + 2);
        var len = reader.ReadUInt16();
        if (len < 0xFFFF) return 4 + len;

        // 超过64k的超大数据包，再来4个字节
        if (pk.Length < 8) return 0;

        //return 8 + (Int32)pk.Data.ToUInt32(pk.Offset + 2 + 2);
        return 8 + reader.ReadInt32();
    }

    /// <summary>获取解析数据时的原始报文</summary>
    /// <returns></returns>
    public IPacket? GetRaw() => _raw;

    /// <summary>消息摘要</summary>
    /// <returns></returns>
    public override String ToString() => $"{Flag:X2} Seq={Sequence:X2} {Payload}";
    #endregion
}