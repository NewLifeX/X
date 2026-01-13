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
    private Int32 _gid;

    /// <summary>写入数据</summary>
    /// <param name="context">处理器上下文</param>
    /// <param name="message">消息</param>
    /// <returns>处理后的消息</returns>
    public override Object? Write(IHandlerContext context, Object message)
    {
        DataKinds? kind = null;

        // 基础类型优先编码
        if (message.GetType().IsBaseType())
        {
            kind = DataKinds.String;
            message = (ArrayPacket)(message + "").GetBytes();
        }
        else if (message is Byte[] buf)
        {
            message = new ArrayPacket(buf);
        }
        else if (message is IAccessor accessor)
        {
            message = accessor.ToPacket();
        }

        if (message is IPacket pk)
        {
            // 优先复用请求消息创建响应
            var request = GetRequest(context);
            var response = (request != null && !request.Reply)
                ? request.CreateReply() as DefaultMessage ?? new DefaultMessage()
                : new DefaultMessage();

            response.Flag = (Byte)(kind ?? DataKinds.Packet);
            response.Payload = pk;
            message = response;

            // 从上下文中获取标记位
            if (context is IExtend ext && ext["Flag"] is DataKinds dk)
                response.Flag = (Byte)dk;
        }

        // 为请求消息分配序列号
        if (message is DefaultMessage msg && !msg.Reply && msg.Sequence == 0)
            msg.Sequence = (Byte)Interlocked.Increment(ref _gid);

        return base.Write(context, message);
    }

    /// <summary>加入队列</summary>
    /// <param name="context">处理器上下文</param>
    /// <param name="msg">消息</param>
    protected override void AddToQueue(IHandlerContext context, IMessage msg)
    {
        // 只有请求消息才加入队列等待响应
        if (!msg.Reply) base.AddToQueue(context, msg);
    }

    /// <summary>解码</summary>
    /// <param name="context">处理器上下文</param>
    /// <param name="pk">数据包</param>
    /// <returns>解码后的消息列表</returns>
    protected override IList<IMessage>? Decode(IHandlerContext context, IPacket pk)
    {
        if (context.Owner is not IExtend ss) return null;

        if (ss["Codec"] is not PacketCodec pc)
        {
#pragma warning disable CS0618 // 类型或成员已过时
            ss["Codec"] = pc = new PacketCodec
            {
                GetLength = DefaultMessage.GetLength,
                GetLength2 = DefaultMessage.GetLength,
                MaxCache = MaxCache,
                Tracer = (context.Owner as ISocket)?.Tracer
            };
#pragma warning restore CS0618 // 类型或成员已过时
        }

        var pks = pc.Parse(pk);
        var list = new List<IMessage>(pks.Count);
        foreach (var item in pks)
        {
            var msg = new DefaultMessage();
            if (msg.Read(item)) list.Add(msg);
        }

        return list;
    }

    /// <summary>是否匹配响应</summary>
    /// <param name="request">请求消息</param>
    /// <param name="response">响应消息</param>
    /// <returns>是否匹配</returns>
    protected override Boolean IsMatch(Object? request, Object? response) =>
        request is DefaultMessage req &&
        response is DefaultMessage res &&
        req.Sequence == res.Sequence;

    /// <summary>连接关闭时，清空粘包编码器</summary>
    /// <param name="context">处理器上下文</param>
    /// <param name="reason">关闭原因</param>
    /// <returns>是否成功关闭</returns>
    public override Boolean Close(IHandlerContext context, String reason)
    {
        if (context.Owner is IExtend ss) ss["Codec"] = null;

        return base.Close(context, reason);
    }
}