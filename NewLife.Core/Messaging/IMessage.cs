using System;
using System.Collections.Generic;
using System.IO;
using NewLife.Data;
using NewLife.Reflection;

namespace NewLife.Messaging
{
    /// <summary>消息命令</summary>
    public interface IMessage //: IAccessor
    {
        /// <summary>是否响应</summary>
        Boolean Reply { get; }

        /// <summary>负载数据</summary>
        Packet Payload { get; set; }

        /// <summary>根据请求创建配对的响应消息</summary>
        /// <returns></returns>
        IMessage CreateReply();

        /// <summary>从数据包中读取消息</summary>
        /// <param name="pk"></param>
        /// <returns>是否成功</returns>
        Boolean Read(Packet pk);

        /// <summary>把消息写入到数据流中</summary>
        /// <param name="stream"></param>
        void Write(Stream stream);
    }

    /// <summary>消息命令基类</summary>
    public class Message : IMessage
    {
        /// <summary>是否响应</summary>
        public Boolean Reply { get; set; }

        /// <summary>负载数据</summary>
        public Packet Payload { get; set; }

        /// <summary>根据请求创建配对的响应消息</summary>
        /// <returns></returns>
        public virtual IMessage CreateReply()
        {
            if (Reply) throw new Exception("不能根据响应消息创建响应消息");

            var msg = GetType().CreateInstance() as Message;
            msg.Reply = true;

            return msg;
        }

        /// <summary>从数据包中读取消息</summary>
        /// <param name="pk"></param>
        /// <returns>是否成功</returns>
        public virtual Boolean Read(Packet pk)
        {
            Payload = pk;

            return true;
        }

        /// <summary>把消息写入到数据流中</summary>
        /// <param name="stream"></param>
        public virtual void Write(Stream stream) { Payload?.WriteTo(stream); }
    }

    /// <summary>消息助手</summary>
    public static class MessageHelper
    {
        /// <summary>获取消息的数据流表示。指针位置为0</summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static Stream GetStream(this IMessage msg)
        {
            var ms = new MemoryStream();
            msg.Write(ms);
            ms.Position = 0;
            return ms;
        }

        /// <summary>获取消息的字节数组表示。</summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static Byte[] ToArray(this IMessage msg)
        {
            return msg.GetStream().ToArray();
        }
    }
}