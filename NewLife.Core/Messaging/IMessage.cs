using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NewLife.Messaging
{
    /// <summary>消息接口</summary>
    public interface IMessage
    {
        /// <summary>从数据流中读取消息</summary>
        /// <param name="stream"></param>
        /// <returns>是否成功</returns>
        Boolean Read(Stream stream);

        /// <summary>把消息写入到数据流中</summary>
        /// <param name="stream"></param>
        void Write(Stream stream);
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