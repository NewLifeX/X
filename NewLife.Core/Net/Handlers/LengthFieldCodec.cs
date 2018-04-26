using System;
using System.Collections.Generic;
using System.IO;
using NewLife.Data;

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
            //var rs = new List<Packet>();

            //var total = pk.Total;
            //while (total > 0)
            //{
            //    var dlen = 0;
            //    var len = Math.Abs(Size);
            //    if (len == 0)
            //    {
            //        var ms = pk.GetStream();
            //        dlen = ms.ReadEncodedInt();
            //        len = (Int32)ms.Position;
            //    }

            //    var buf = pk.ReadBytes(Offset, len);
            //    switch (Size)
            //    {
            //        case 0: break;
            //        case 1:
            //            dlen = buf[0];
            //            break;
            //        case 2:
            //            dlen = buf.ToUInt16();
            //            break;
            //        case 4:
            //            dlen = (Int32)buf.ToUInt32();
            //            break;
            //        case -2:
            //            dlen = buf.ToUInt16(0, false);
            //            break;
            //        case -4:
            //            dlen = (Int32)buf.ToUInt32(0, false);
            //            break;
            //        default:
            //            throw new NotSupportedException();
            //    }

            //    //!!! 还需要考虑粘包问题
            //    if (len + dlen > total) throw new Exception("数据不足！");

            //    //rs.Add(new Packet(pk.Data, pk.Offset + len, dlen));
            //    var pk2 = pk.Sub(0, Offset + len + dlen);
            //    rs.Add(pk2);

            //    pk.Set(pk.Data, pk.Offset + pk2.Count, pk.Count - pk2.Count);
            //    total -= pk2.Count;
            //}

            if (_ms == null) _ms = new MemoryStream();

            return Parse(pk, _ms, ref _last, Offset, Size, Expire);
        }

        /// <summary>内部缓存</summary>
        private MemoryStream _ms;
        private DateTime _last;
    }
}