using NewLife.Buffers;
using NewLife.Data;
using NewLife.Messaging;
using NewLife.Model;

namespace NewLife.Net.Handlers;

/// <summary>长度字段作为头部</summary>
public class LengthFieldCodec : MessageCodec<IPacket>
{
    #region 属性
    /// <summary>长度所在位置</summary>
    public Int32 Offset { get; set; }

    /// <summary>长度占据字节数，1/2/4个字节，0表示压缩编码整数，默认2</summary>
    public Int32 Size { get; set; } = 2;

    /// <summary>过期时间，超过该时间后按废弃数据处理，默认500ms</summary>
    public Int32 Expire { get; set; } = 500;
    #endregion

    /// <summary>编码</summary>
    /// <param name="context"></param>
    /// <param name="msg"></param>
    /// <returns></returns>
    protected override Object Encode(IHandlerContext context, IPacket msg)
    {
        var dlen = msg.Total;

        // 修正压缩编码
        var len = Math.Abs(Size);
        if (Size == 0) len = IOHelper.GetEncodedInt(dlen).Length;

        // 尝试退格，直接利用缓冲区
        if (msg is ArrayPacket ap && ap.Offset >= len)
        {
            // 向下传递时，不要转移所有权。向上传递到较高层级才需要转移所有权。
            msg = new ArrayPacket(ap.Buffer, ap.Offset - len, ap.Length + len) { Next = ap.Next/*, HasOwner = ap.HasOwner*/ };
        }
        else
        {
            msg = new ArrayPacket(len) { Next = msg };
        }

        var writer = new SpanWriter(msg.GetSpan()) { IsLittleEndian = Size > 0 };

        switch (Size)
        {
            case 0:
                writer.WriteEncodedInt(dlen);
                break;
            case 1:
                writer.WriteByte((Byte)dlen);
                break;
            case 2:
                writer.Write((UInt16)dlen);
                break;
            case 4:
                writer.Write((UInt32)dlen);
                break;
            case -2:
                writer.Write((UInt16)dlen);
                break;
            case -4:
                writer.Write((UInt32)dlen);
                break;
            default:
                throw new NotSupportedException();
        }

        return msg;
    }

    /// <summary>解码</summary>
    /// <param name="context"></param>
    /// <param name="pk"></param>
    /// <returns></returns>
    protected override IList<IPacket>? Decode(IHandlerContext context, IPacket pk)
    {
        if (context.Owner is not IExtend ss) return null;

        if (ss["Codec"] is not PacketCodec pc)
        {
            ss["Codec"] = pc = new PacketCodec
            {
                Expire = Expire,
                GetLength = p => GetLength(p, Offset, Size),
                Offset = Offset,
                MaxCache = MaxCache,
                Tracer = (context.Owner as ISocket)?.Tracer
            };
        }

        var pks = pc.Parse(pk);

        // 跳过头部长度
        var len = Offset + Math.Abs(Size);
        for (var i = 0; i < pks.Count; i++)
        {
            pks[i] = pks[i].Slice(len);
        }

        return pks;
    }

    /// <summary>连接关闭时，清空粘包编码器</summary>
    /// <param name="context"></param>
    /// <param name="reason"></param>
    /// <returns></returns>
    public override Boolean Close(IHandlerContext context, String reason)
    {
        if (context.Owner is IExtend ss) ss["Codec"] = null;

        return base.Close(context, reason);
    }
}