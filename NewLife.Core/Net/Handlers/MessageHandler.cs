using System;
using System.Threading.Tasks;
using NewLife.Data;
using NewLife.Messaging;

namespace NewLife.Net
{
    /// <summary>消息封包</summary>
    public class MessageHandler<T> : Handler
    {
        public IPacketQueue Queue { get; set; } = new DefaultPacketQueue();

        /// <summary>写入数据</summary>
        /// <param name="context"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public override Object Write(IHandlerContext context, Object message)
        {
            if (message is T msg)
            {
                context["Message"] = msg;
                message = Encode(context, msg);

                // 加入队列
                if (context["TaskSource"] is TaskCompletionSource<Object> source)
                    Queue.Add(context.Session, msg, 15000, source);
            }

            return message;
        }

        /// <summary>编码</summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        protected virtual Object Encode(IHandlerContext context, T msg)
        {
            if (msg is IMessage msg2) return msg2.ToPacket();

            return null;
        }

        /// <summary>读取数据</summary>
        /// <param name="context"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public override Object Read(IHandlerContext context, Object message)
        {
            if (message is Packet pk)
            {
                var msg = Decode(context, pk);
                context["Message"] = msg;

                if (msg is IMessage msg2)
                    message = msg2.Payload;
                else
                    message = msg;
            }

            return message;
        }

        /// <summary>解码</summary>
        /// <param name="context"></param>
        /// <param name="pk"></param>
        /// <returns></returns>
        protected virtual T Decode(IHandlerContext context, Packet pk) => default(T);


        /// <summary>读取数据完成</summary>
        /// <param name="context">上下文</param>
        /// <param name="message">最终消息</param>
        public override void ReadComplete(IHandlerContext context, Object message)
        {
            if (context["Message"] is IMessage msg)
            {
                // 匹配
                if (msg.Reply) Queue.Match(context.Session, msg, message, IsMatch);
            }
        }

        /// <summary>是否匹配响应</summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        protected virtual Boolean IsMatch(Object request, Object response) => true;
    }
}