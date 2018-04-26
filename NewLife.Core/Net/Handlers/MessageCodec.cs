using System;
using System.Threading.Tasks;
using NewLife.Data;
using NewLife.Messaging;

namespace NewLife.Net.Handlers
{
    /// <summary>消息封包</summary>
    public class MessageCodec<T> : Handler
    {
        /// <summary>消息队列。用于匹配请求响应包</summary>
        public IMatchQueue Queue { get; set; } = new DefaultMatchQueue();

        /// <summary>调用超时时间。默认15000ms</summary>
        public Int32 Timeout { get; set; } = 15000;

        /// <summary>使用数据包，写入时数据包转消息，读取时消息自动解包返回数据负载。默认true</summary>
        public Boolean UserPacket { get; set; } = true;

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
                if (message != null && context["TaskSource"] is TaskCompletionSource<Object> source)
                {
                    var timeout = Timeout;
                    //if (context.Session is ISocketClient client) timeout = client.Timeout;
                    Queue.Add(context.Session, msg, timeout, source);
                }
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

                if (UserPacket && msg is IMessage msg2)
                    message = msg2.Payload;
                else
                    message = msg;

                // 后续处理器
                message = Next?.Read(context, message);

                if (msg is IMessage msg3)
                {
                    // 匹配
                    if (msg3.Reply) Queue.Match(context.Session, msg3, message, IsMatch);
                }
                else if (msg != null)
                {
                    // 其它消息不考虑响应
                    Queue.Match(context.Session, msg, message, IsMatch);
                }
            }

            return message;
        }

        /// <summary>解码</summary>
        /// <param name="context"></param>
        /// <param name="pk"></param>
        /// <returns></returns>
        protected virtual T Decode(IHandlerContext context, Packet pk) => default(T);

        ///// <summary>读取数据完成</summary>
        ///// <param name="context">上下文</param>
        ///// <param name="message">最终消息</param>
        //public override void ReadComplete(IHandlerContext context, Object message)
        //{
        //    var msg = context["Message"];
        //    if (msg is IMessage msg2)
        //    {
        //        // 匹配
        //        if (msg2.Reply) Queue.Match(context.Session, msg2, message, IsMatch);
        //    }
        //    else if (msg != null)
        //    {
        //        // 其它消息不考虑响应
        //        Queue.Match(context.Session, msg, message, IsMatch);
        //    }
        //}

        /// <summary>是否匹配响应</summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        protected virtual Boolean IsMatch(Object request, Object response) => true;
    }
}