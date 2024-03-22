using NewLife.Data;
using NewLife.Messaging;
using NewLife.Model;
using NewLife.Reflection;
using NewLife.Serialization;

namespace NewLife.Net.Handlers;

/// <summary>标准网络封包。头部4字节定长</summary>
/// <remarks>
/// 文档 https://newlifex.com/core/srmp
/// </remarks>
public class StandardCodec : MessageCodec<IMessage>
{
    ///// <summary>编码器。用于编码非消息对象</summary>
    //public IPacketEncoder? Encoder { get; set; }

    private Int32 _gid;

    /// <summary>写入数据</summary>
    /// <param name="context"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public override Object? Write(IHandlerContext context, Object message)
    {
        // 基础类型优先编码
        if (message.GetType().GetTypeCode() != TypeCode.Object)
        {
            message = new DefaultMessage { Flag = (Byte)DataKinds.String, Payload = (message + "").GetBytes() };
        }
        else if (message is Byte[] buf)
        {
            message = new Packet(buf);
        }
        else if (message is IAccessor accessor)
        {
            message = accessor.ToPacket();
        }

        if (message is Packet pk)
        {
            var dm = new DefaultMessage { Flag = (Byte)DataKinds.Packet, Payload = pk };
            message = dm;

            // 从上下文中获取标记位
            if (context is IExtend ext && ext["Flag"] is DataKinds dk)
                dm.Flag = (Byte)dk;
        }

        if (message is DefaultMessage msg && !msg.Reply && msg.Sequence == 0)
            msg.Sequence = (Byte)Interlocked.Increment(ref _gid);

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
    protected override IList<IMessage>? Decode(IHandlerContext context, Packet pk)
    {
        if (context.Owner is not IExtend ss) return null;

        if (ss["Codec"] is not PacketCodec pc)
        {
            ss["Codec"] = pc = new PacketCodec
            {
                GetLength = DefaultMessage.GetLength,
                MaxCache = MaxCache,
                Tracer = (context.Owner as ISocket)?.Tracer
            };
        }

        var pks = pc.Parse(pk);
        var list = new List<IMessage>();
        foreach (var item in pks)
        {
            var msg = new DefaultMessage();
            if (msg.Read(item)) list.Add(msg);
        }

        return list;
    }

    /// <summary>是否匹配响应</summary>
    /// <param name="request"></param>
    /// <param name="response"></param>
    /// <returns></returns>
    protected override Boolean IsMatch(Object? request, Object? response)
    {
        return request is DefaultMessage req &&
            response is DefaultMessage res &&
            req.Sequence == res.Sequence;
    }

    /// <summary>连接关闭时，清空粘包编码器</summary>
    /// <param name="context"></param>
    /// <param name="reason"></param>
    /// <returns></returns>
    public override Boolean Close(IHandlerContext context, String reason)
    {
        if (context.Owner is IExtend ss) ss["Codec"] = null;

        return base.Close(context, reason);
    }
}