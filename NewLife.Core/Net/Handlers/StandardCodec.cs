using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                message = new DefaultMessage { Payload = pk, Sequence = (Byte)Interlocked.Increment(ref _gid) };
            else if (message is DefaultMessage msg && !msg.Reply && msg.Sequence == 0)
                msg.Sequence = (Byte)Interlocked.Increment(ref _gid);

#if DEBUG
            //Log.XTrace.WriteLine("Write {0}", message);
#endif

            return base.Write(context, message);
        }

        /// <summary>加入队列</summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        protected override void AddToQueue(IHandlerContext context, IMessage msg)
        {
            if (!msg.Reply) base.AddToQueue(context, msg);
        }

        /// <summary>解码</summary>
        /// <param name="context"></param>
        /// <param name="pk"></param>
        /// <returns></returns>
        protected override IList<IMessage> Decode(IHandlerContext context, Packet pk)
        {
            if (_ms == null) _ms = new MemoryStream();

            var pks = Parse(pk, _ms, ref _last, 2, 2);
            return pks.Select(e =>
            {
                var msg = new DefaultMessage();
                if (!msg.Read(e)) return null;

#if DEBUG
                //Log.XTrace.WriteLine("Decode {0} Seq={1}", msg.Payload, msg.Sequence);
#endif

                return msg as IMessage;
            }).ToList();
        }

        /// <summary>内部缓存</summary>
        private MemoryStream _ms;
        private DateTime _last;

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