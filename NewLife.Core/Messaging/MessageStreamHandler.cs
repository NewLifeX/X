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
            Int32 id = stream.ReadByte();
            // 后退一个字节
            stream.Seek(-1, SeekOrigin.Current);

            if (!MessageHandler.Support(id)) return stream;

            MessageHandler.Process(stream);

            return stream;
        }

        public override bool IsReusable
        {
            get { return true; }
        }
    }

    ///// <summary>
    ///// 用于消息的数据流处理器工厂
    ///// </summary>
    //class MessageStreamHandlerFactory //: StreamHandlerFactory
    //{
    //    private MessageStreamHandler Handler;

    //    ///// <summary>
    //    ///// 返回消息的数据流处理器。所有数据流都会到达这里，所以这里需要区分哪些是需要自己来处理的。
    //    ///// </summary>
    //    ///// <param name="stream"></param>
    //    ///// <returns></returns>
    //    //public override IStreamHandler GetHandler(Stream stream)
    //    //{
    //    //    Int32 id = stream.ReadByte();
    //    //    // 后退一个字节
    //    //    stream.Seek(-1, SeekOrigin.Current);

    //    //    if (!Message.Support(id)) return null;

    //    //    return Handler ?? (Handler = new MessageStreamHandler());
    //    //}
    //}
}