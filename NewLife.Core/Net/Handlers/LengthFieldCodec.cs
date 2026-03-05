using NewLife.Buffers;
using NewLife.Data;
using NewLife.Messaging;
using NewLife.Model;

namespace NewLife.Net.Handlers;

/// <summary>长度字段编解码器。</summary>
/// <remarks>
/// 数据包格式：[头部(Offset字节)] [长度字段(Size字节)] [负载]
/// 编码时：在负载前插入长度字段；解码时：根据长度读取完整包，然后去掉头部和长度字段。
/// 支持灵活的头部偏移和长度字段位置，适配 MQTT、LwM2M 等多种协议。
/// </remarks>
public class LengthFieldCodec : MessageCodec<IPacket>
{
    #region 属性
    /// <summary>长度字段的偏移量（字节数）。</summary>
    /// <remarks>
    /// 设定数据包格式为：[头部(Offset字节)] [长度字段] [负载]。
    /// 例如：Offset=0 表示长度字段在最开始；Offset=2 表示头部有 2 字节，然后是长度字段（MQTT 风格）。
    /// 编解码时自动跳过此长度的头部数据，不包含在返回的负载中。
    /// </remarks>
    public Int32 Offset { get; set; }

    /// <summary>长度字段占据的字节数。</summary>
    /// <remarks>
    /// 取值说明：
    /// - 1：1 字节长度（小端，最大 255）
    /// - 2：2 字节长度（小端，最大 65535），默认值
    /// - 4：4 字节长度（小端，最大 4GB）
    /// - 0：变长压缩编码（根据 IOHelper.GetEncodedInt）
    /// - -1：1 字节长度（大端）
    /// - -2：2 字节长度（大端）
    /// - -4：4 字节长度（大端）
    /// 负值表示大端序（Big Endian），绝对值表示字节数。
    /// </remarks>
    public Int32 Size { get; set; } = 2;

    /// <summary>缓存过期时间（毫秒），不完整的数据包超过此时间后按废弃数据处理，默认 5000ms</summary>
    public Int32 Expire { get; set; } = 5_000;
    #endregion

    /// <summary>编码：在负载前插入长度字段。</summary>
    /// <param name="context">处理器上下文</param>
    /// <param name="msg">待编码的负载包</param>
    /// <returns>带长度头的完整包</returns>
    protected override Object Encode(IHandlerContext context, IPacket msg)
    {
        var dlen = msg.Total;

        // 计算长度字段大小
        var len = Math.Abs(Size);
        Byte[]? encodedLen = null;
        if (Size == 0)
        {
            encodedLen = IOHelper.GetEncodedInt(dlen);
            len = encodedLen.Length;
        }

        // 尝试在原包前面插入头部，零拷贝优化
        var pk = msg.ExpandHeader(Offset + len);
        var writer = new SpanWriter(pk.GetSpan()) { IsLittleEndian = Size > 0 };

        // 预留协议头（Offset）
        if (Offset > 0) writer.Fill(0, Offset);

        // 根据 Size 写入长度字段
        switch (Size)
        {
            case 0:
                if (encodedLen == null) encodedLen = IOHelper.GetEncodedInt(dlen);
                writer.Write(encodedLen);
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
                throw new NotSupportedException($"不支持的 Size 值：{Size}");
        }

        return pk;
    }

    /// <summary>解码：根据长度字段拆分完整的包，返回去掉头部和长度字段的纯负载。</summary>
    /// <param name="context">处理器上下文</param>
    /// <param name="pk">接收到的原始数据</param>
    /// <returns>拆分后的完整包（已去掉头部和长度字段）</returns>
    protected override IEnumerable<IPacket>? Decode(IHandlerContext context, IPacket pk)
    {
        if (context.Owner is not IExtend ss) yield break;

        // 初始化或获取粘包处理器
        if (ss["Codec"] is not PacketCodec pc)
        {
#pragma warning disable CS0618 // 类型或成员已过时
            ss["Codec"] = pc = new PacketCodec
            {
                Expire = Expire,
                GetLength = p => GetLength(p, Offset, Size),
                GetLength2 = p => GetLength(p, Offset, Size),
                //Offset = Offset,
                MaxCache = MaxCache,
                Tracer = (context.Owner as ISocket)?.Tracer
            };
#pragma warning restore CS0618 // 类型或成员已过时
        }

        // 粘包拆分，返回完整的包
        var pks = pc.Parse(pk);

        // 跳过头部(Offset)和长度字段(Size)，返回纯负载
        for (var i = 0; i < pks.Count; i++)
        {
            var headerLen = Offset + Math.Abs(Size);
            if (Size == 0)
            {
                var span = pks[i].GetSpan();
                var reader = new SpanReader(span) { IsLittleEndian = true };
                reader.Advance(Offset);
                var p = reader.Position;
                _ = reader.ReadEncodedInt();
                headerLen = Offset + reader.Position - p;
            }

            yield return pks[i].Slice(headerLen, -1, true);
        }
    }

    /// <summary>连接关闭时清空粘包编码器的缓存，防止内存泄漏</summary>
    /// <param name="context">处理器上下文</param>
    /// <param name="reason">连接关闭原因</param>
    /// <returns>继续传播关闭事件</returns>
    public override Boolean Close(IHandlerContext context, String reason)
    {
        // 清理缓存的 PacketCodec，释放 MemoryStream
        if (context.Owner is IExtend ss) ss["Codec"] = null;

        return base.Close(context, reason);
    }
}