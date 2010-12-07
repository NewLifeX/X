using System;
using System.IO;
using NewLife.IO;

namespace NewLife.Messaging
{
    /// <summary>
    /// 用于消息的数据流处理器。
    /// </summary>
    class MessageStreamHandler : StreamHandler
    {
        public override Stream Process(Stream stream)
        {
            // 有数据才识别
            if (stream.CanSeek && stream.Length > 0)
            {
                Int32 id = stream.ReadByte();
                // 后退一个字节
                stream.Seek(-1, SeekOrigin.Current);

                if (!MessageHandler.Support(id)) return stream;
            }

            // 没有数据的时候也处理，因为可能是空请求
            MessageHandler.Process(stream);

            return stream;
        }

        public override bool IsReusable
        {
            get { return true; }
        }
    }
}