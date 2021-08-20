using System;
using System.IO;
using NewLife.Data;

namespace NewLife.Http
{
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
        public Byte[] MaskKey { get; set; }

        /// <summary>负载数据</summary>
        public Packet Payload { get; set; }

        /// <summary>关闭状态。仅用于Close消息</summary>
        public Int32 CloseStatus { get; set; }

        /// <summary>关闭状态描述。仅用于Close消息</summary>
        public String StatusDescription { get; set; }
        #endregion

        #region 方法
        /// <summary>读取消息</summary>
        /// <param name="pk"></param>
        /// <returns></returns>
        public Boolean Read(Packet pk)
        {
            if (pk.Count < 2) return false;

            var ms = pk.GetStream();
            var b = ms.ReadByte();

            Type = (WebSocketMessageType)(b & 0x7F);

            // 仅处理一个包
            Fin = (b & 0x80) == 0x80;
            if (!Fin) return false;

            var len = ms.ReadByte();

            var mask = (len & 0x80) == 0x80;

            /*
             * 数据长度
             * len < 126    单字节表示长度
             * len = 126    后续2字节表示长度，大端
             * len = 127    后续8字节表示长度
             */
            len &= 0x7F;
            if (len == 126)
                len = (ms.ReadByte() << 8) | ms.ReadByte();
            else if (len == 127)
            {
                var buf = new Byte[8];
                ms.Read(buf, 0, buf.Length);
                // 没有人会传输超大数据
                len = (Int32)BitConverter.ToUInt64(buf, 0);
            }

            // 如果mask，剩下的就是数据，避免拷贝，提升性能
            if (!mask)
            {
                Payload = pk.Slice((Int32)ms.Position, len);
            }
            else
            {
                var masks = new Byte[4];
                if (mask) ms.Read(masks, 0, masks.Length);
                MaskKey = masks;

                if (mask)
                {
                    // 直接在数据缓冲区修改，避免拷贝
                    var data = Payload = pk.Slice((Int32)ms.Position, len);
                    for (var i = 0; i < len; i++)
                    {
                        data[i] = (Byte)(data[i] ^ masks[i % 4]);
                    }
                }
            }

            // 特殊处理关闭消息
            if (Type == WebSocketMessageType.Close)
            {
                var data = Payload;
                CloseStatus = data.ReadBytes(0, 2).ToUInt16(0, false);
                StatusDescription = data.Slice(2).ToStr();
            }

            return true;
        }

        /// <summary>把消息转为封包</summary>
        /// <returns></returns>
        public virtual Packet ToPacket()
        {
            var pk = Payload;
            var len = pk == null ? 0 : pk.Total;

            // 特殊处理关闭消息
            if (len == 0 && Type == WebSocketMessageType.Close)
            {
                pk = new Packet(((UInt16)CloseStatus).GetBytes(false))
                {
                    Next = StatusDescription.GetBytes()
                };
                len = pk.Total;
            }

            var ms = new MemoryStream();
            ms.WriteByte((Byte)(0x80 | (Byte)Type));

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
                    ms.WriteByte((Byte)len);
                }
                else if (len < 0xFFFF)
                {
                    ms.WriteByte(126);
                    ms.Write(((Int16)len).GetBytes(false));
                }
                else
                {
                    ms.WriteByte(127);
                    ms.Write(((Int64)len).GetBytes(false));
                }
            }
            else
            {
                if (len < 126)
                {
                    ms.WriteByte((Byte)(len | 0x80));
                }
                else if (len < 0xFFFF)
                {
                    ms.WriteByte(126 | 0x80);
                    ms.Write(((Int16)len).GetBytes(false));
                }
                else
                {
                    ms.WriteByte(127 | 0x80);
                    ms.Write(((Int64)len).GetBytes(false));
                }

                ms.Write(masks);

                // 掩码混淆数据。直接在数据缓冲区修改，避免拷贝
                var data = Payload;
                for (var i = 0; i < len; i++)
                {
                    data[i] = (Byte)(data[i] ^ masks[i % 4]);
                }
            }

            return new Packet(ms.ToArray()) { Next = pk };
        }
        #endregion
    }
}