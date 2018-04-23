using System;
using System.Threading;
using NewLife.Data;
using NewLife.Messaging;

namespace NewLife.Net.Handlers
{
    /// <summary>标准网络封包。头部4字节定长</summary>
    public class StandardCodec : MessageCodec<IMessage>
    {
        private Int32 _gid;

        /// <summary>写入数据</summary>
        /// <param name="context"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public override Object Write(IHandlerContext context, Object message)
        {
            if (UserPacket && message is Packet pk)
            {
                message = new DefaultMessage { Payload = pk, Sequence = (Byte)Interlocked.Increment(ref _gid) };
            }

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

        /// <summary>是否匹配响应</summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        protected override Boolean IsMatch(Object request, Object response)
        {
            return request is DefaultMessage req &&
                response is DefaultMessage res &&
                req.Sequence == res.Sequence;
        }
    }
}