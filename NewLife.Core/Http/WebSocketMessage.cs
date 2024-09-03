using System.Buffers.Binary;
using System.Text;
using NewLife.Buffers;
using NewLife.Data;

namespace NewLife.Http;

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

/// <summary>WebSocket消息</summary>
public class WebSocketMessage
{
    #region 属性
    /// <summary>消息是否结束</summary>
    public Boolean Fin { get; set; }

    /// <summary>消息类型</summary>
    public WebSocketMessageType Type { get; set; }

    /// <summary>加密数据的掩码</summary>
    public Byte[]? MaskKey { get; set; }

    /// <summary>负载数据</summary>
    public IPacket? Payload { get; set; }

    /// <summary>关闭状态。仅用于Close消息</summary>
    public Int32 CloseStatus { get; set; }

    /// <summary>关闭状态描述。仅用于Close消息</summary>
    public String? StatusDescription { get; set; }
    #endregion

    #region 方法
    /// <summary>读取消息</summary>
    /// <param name="pk"></param>
    /// <returns></returns>
    public Boolean Read(IPacket pk)
    {
        if (pk.Length < 2) return false;

        var reader = new SpanReader(pk.GetSpan())
        {
            IsLittleEndian = false
        };
        //var reader = pk.GetStream();
        var b = reader.ReadByte();

        Type = (WebSocketMessageType)(b & 0x7F);

        // 仅处理一个包
        Fin = (b & 0x80) == 0x80;
        if (!Fin) return false;

        var b2 = reader.ReadByte();

        var mask = (b2 & 0x80) == 0x80;

        /*
         * 数据长度
         * len < 126    单字节表示长度
         * len = 126    后续2字节表示长度，大端
         * len = 127    后续8字节表示长度
         */
        var len = (Int64)(b2 & 0x7F);
        if (len == 126)
            len = reader.ReadUInt16();
        else if (len == 127)
            len = reader.ReadInt64();

        // 如果mask，剩下的就是数据，避免拷贝，提升性能
        if (!mask)
        {
            Payload = pk.Slice(reader.Position, (Int32)len);
        }
        else
        {
            var masks = new Byte[4];
            if (mask) reader.ReadBytes(4).CopyTo(masks);
            MaskKey = masks;

            if (mask)
            {
                // 直接在数据缓冲区修改，避免拷贝
                Payload = pk.Slice(reader.Position, (Int32)len);
                var data = Payload.GetSpan();
                for (var i = 0; i < len; i++)
                {
                    data[i] = (Byte)(data[i] ^ masks[i % 4]);
                }
            }
        }

        // 特殊处理关闭消息
        if (Type == WebSocketMessageType.Close && Payload != null)
        {
            var data = Payload.GetSpan();
            CloseStatus = BinaryPrimitives.ReadUInt16BigEndian(data[..2]);
            StatusDescription = data[2..].ToStr();
        }

        return true;
    }

    /// <summary>把消息转为封包</summary>
    /// <returns></returns>
    public virtual IPacket ToPacket()
    {
        var pk = Payload;
        var len = pk == null ? 0 : pk.Length;

        // 特殊处理关闭消息
        if (len == 0 && Type == WebSocketMessageType.Close)
        {
            len = 2;
            if (!StatusDescription.IsNullOrEmpty()) len += Encoding.UTF8.GetByteCount(StatusDescription);
        }

        var rs = new OwnerPacket(1 + 1 + 8 + 4 + len);
        var writer = new SpanWriter(rs.GetSpan())
        {
            IsLittleEndian = false
        };

        writer.WriteByte((Byte)(0x80 | (Byte)Type));

        var masks = MaskKey;

        /*
         * 数据长度
         * len < 126    单字节表示长度
         * len = 126    后续2字节表示长度，大端
         * len = 127    后续8字节表示长度
         */

        if (masks == null)
        {
            if (len < 126)
            {
                writer.WriteByte((Byte)len);
            }
            else if (len < 0xFFFF)
            {
                writer.WriteByte(126);
                writer.Write((Int16)len);
            }
            else
            {
                writer.WriteByte(127);
                writer.Write((Int64)len);
            }
        }
        else
        {
            if (len < 126)
            {
                writer.WriteByte((Byte)(len | 0x80));
            }
            else if (len < 0xFFFF)
            {
                writer.WriteByte(126 | 0x80);
                writer.Write((Int16)len);
            }
            else
            {
                writer.WriteByte(127 | 0x80);
                writer.Write((Int64)len);
            }

            writer.Write(masks);

            // 掩码混淆数据。直接在数据缓冲区修改，避免拷贝
            if (Payload != null)
            {
                var data = Payload.GetSpan();
                for (var i = 0; i < len; i++)
                {
                    data[i] = (Byte)(data[i] ^ masks[i % 4]);
                }
            }
        }

        if (pk != null && pk.Length > 0)
            writer.Write(pk.GetSpan());
        else if (Type == WebSocketMessageType.Close)
        {
            writer.Write((Int16)CloseStatus);
            if (!StatusDescription.IsNullOrEmpty()) writer.Write(StatusDescription);
        }

        return rs.Slice(0, writer.Position);
    }
    #endregion
}