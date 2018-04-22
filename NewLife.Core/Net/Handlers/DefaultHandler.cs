using System;
using System.Threading;
using NewLife.Data;
using NewLife.Messaging;

namespace NewLife.Net
{
    /// <summary>标准网络封包。头部4字节定长</summary>
    public class DefaultHandler : MessageHandler<IMessage>
    {
        private Int32 _gid;

        /// <summary>写入数据</summary>
        /// <param name="context"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public override Object Write(IHandlerContext context, Object message)
        {
            if (message is Packet pk) message = new DefaultMessage { Payload = pk, Sequence = (Byte)Interlocked.Increment(ref _gid) };
            //if (message is IMessage msg)
            //{
            //    //var len = pk.Count;

            //    //// 增加4字节头部
            //    //if (pk.Offset >= 4)
            //    //    pk.Set(pk.Data, pk.Offset - 4, pk.Count + 4);
            //    //else
            //    //    pk = new Packet(new Byte[4]) { Next = pk };

            //    //// 序列号
            //    //var seq = Interlocked.Increment(ref _gid);
            //    //pk[1] = (Byte)seq;

            //    //// 长度
            //    //pk[2] = (Byte)(len >> 8);
            //    //pk[3] = (Byte)(len & 0xFF);

            //    message = msg.ToPacket();
            //}

            return base.Write(context, message);
        }

        /// <summary>解码</summary>
        /// <param name="context"></param>
        /// <param name="pk"></param>
        /// <returns></returns>
        protected override IMessage Decode(IHandlerContext context, Packet pk)
        {
            var msg = new DefaultMessage();
            if (!msg.Read(pk)) return null;

            return msg;
        }
    }
}