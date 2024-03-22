using NewLife.Data;
using NewLife.Messaging;
using NewLife.Model;
using NewLife.Reflection;
using NewLife.Serialization;

namespace NewLife.Net.Handlers;

/// <summary>Json编码器。用于把用户对象编码为Json字符串</summary>
public class JsonCodec : Handler
{
    /// <summary>发送消息时，写入数据，编码并加入队列</summary>
    /// <remarks>
    /// 遇到消息T时，调用Encode编码并加入队列。
    /// Encode返回空时，跳出调用链。
    /// </remarks>
    /// <param name="context"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public override Object? Write(IHandlerContext context, Object message)
    {
        var ext = context as IExtend;
        if (message.GetType().GetTypeCode() != TypeCode.Object)
        {
            var str = message is DateTime dt ? dt.ToFullString() : message + "";
            message = str.GetBytes();

            // 通知标准网络封包使用的Flag
            if (ext != null) ext["Flag"] = DataKinds.String;
        }
        else if (message is not Packet and not IMessage)
        {
            message = message.ToJson().GetBytes();

            // 通知标准网络封包使用的Flag
            if (ext != null) ext["Flag"] = DataKinds.Json;
        }

        if (message is Byte[] buf)
            message = new Packet(buf);

        return base.Write(context, message);
    }

    /// <summary>接收数据后，读取数据包，Decode解码得到消息</summary>
    /// <remarks>
    /// Decode可以返回多个消息，每个消息调用一次下一级处理器。
    /// Decode返回空时，跳出调用链。
    /// </remarks>
    /// <param name="context"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public override Object? Read(IHandlerContext context, Object message)
    {
        if (message is Packet pk)
        {
            //var str = pk.ToStr();
            //if (!str.IsNullOrEmpty())
            //    message = JsonParser.Decode(str);
        }

        return base.Read(context, message);
    }
}
