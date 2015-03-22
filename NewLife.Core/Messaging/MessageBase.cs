using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NewLife.Serialization;

namespace NewLife.Messaging
{
    /// <summary>消息基类</summary>
    public abstract class MessageBase : IMessage
    {
        /// <summary>从数据流中读取消息</summary>
        /// <param name="stream"></param>
        /// <returns>是否成功</returns>
        public virtual Boolean Read(Stream stream)
        {
            var fm = GetFormatter(true);
            Object obj = this;
            return fm.TryRead(this.GetType(), ref obj);
        }

        /// <summary>把消息写入到数据流中</summary>
        /// <param name="stream"></param>
        public virtual void Write(Stream stream)
        {
            var fm = GetFormatter(false);
            fm.Write(this);
        }

        /// <summary>获取序列化器</summary>
        /// <param name="isRead"></param>
        /// <returns></returns>
        protected virtual IFormatterX GetFormatter(Boolean isRead)
        {
            var binary = new Binary();

            return binary;
        }
    }

    ///// <summary>消息泛型基类</summary>
    ///// <typeparam name="TMessage"></typeparam>
    //public abstract class Message<TMessage> : MessageBase where TMessage : Message<TMessage>, new()
    //{
    //    public static TMessage Read(Stream stream)
    //    {
    //        var msg = new TMessage();
    //        if (msg.Read(stream)) return msg;

    //        return default(TMessage);
    //    }
    //}
}
