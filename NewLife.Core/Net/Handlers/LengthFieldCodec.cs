using System;
using System.Collections.Generic;
using NewLife.Data;
using NewLife.Model;

namespace NewLife.Net.Handlers
{
    /// <summary>长度字段作为头部</summary>
    public class LengthFieldCodec : MessageCodec<Packet>
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
        protected override Object Encode(IHandlerContext context, Packet msg)
        {
            var len = Math.Abs(Size);
            var buf = msg.Data;
            var idx = 0;
            var dlen = msg.Total;

            // 修正压缩编码
            if (len == 0) len = IOHelper.GetEncodedInt(dlen).Length;

            // 尝试退格，直接利用缓冲区
            if (msg.Offset >= len)
            {
                idx = msg.Offset - len;
                msg.Set(msg.Data, msg.Offset - len, msg.Count + len);
            }
            // 新建数据包，形成链式结构
            else
            {
                buf = new Byte[len];
                msg = new Packet(buf) { Next = msg };
            }

            switch (Size)
            {
                case 0:
                    var buf2 = IOHelper.GetEncodedInt(dlen);
                    buf.Write(idx, buf2);
                    break;
                case 1:
                    buf[idx] = (Byte)dlen;
                    break;
                case 2:
                    buf.Write((UInt16)dlen, idx);
                    break;
                case 4:
                    buf.Write((UInt32)dlen, idx);
                    break;
                case -2:
                    buf.Write((UInt16)dlen, idx, false);
                    break;
                case -4:
                    buf.Write((UInt32)dlen, idx, false);
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
        protected override IList<Packet> Decode(IHandlerContext context, Packet pk)
        {
            var ss = context.Owner as IExtend;
            var mcp = ss["CodecItem"] as CodecItem;
            if (mcp == null) ss["CodecItem"] = mcp = new CodecItem();

            var pks = Parse(pk, mcp, ms => GetLength(ms, Offset, Size), Expire);

            // 跳过头部长度
            var len = Offset + Math.Abs(Size);
            foreach (var item in pks)
            {
                item.Set(item.Data, item.Offset + len, item.Count - len);
                //item.SetSub(len, item.Count - len);
            }

            return pks;
        }
    }
}