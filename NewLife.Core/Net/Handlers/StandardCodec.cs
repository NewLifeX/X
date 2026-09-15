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
        var origin = message;

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
        else if (message is ISpanSerializable span)
        {
            message = span.ToPacket();
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

            // 负载承载；拥有包（OwnerPacket）所有权随消息持有，消息 Dispose（发送后兜底）时唯一归还
            response.Payload = pk;

            message = response;

            // 从上下文中获取标记位
            if (context is IExtend ext && ext["Flag"] is DataKinds dk)
                response.Flag = (Byte)dk;
        }

        // 为请求消息分配序列号
        if (message is DefaultMessage msg && !msg.Reply && msg.Sequence == 0)
            msg.Sequence = (Byte)Interlocked.Increment(ref _gid);

        try
        {
            return base.Write(context, message);
        }
        finally
        {
            // message 已被替换为新对象且支持释放时，兜底释放。
            // 不做整对象复用：无上游请求时该响应以 Reply=false 进入请求匹配队列，
            // Reset 会清空 Sequence 导致匹配失败；这里只释放负载引用，匹配用的序列号必须保留
            if (!ReferenceEquals(message, origin))
                message.TryDispose();
        }
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
    /// <param name="pk">本轮接收数据（接收链路为轮拥有句柄，直接调用可为借阅视图）</param>
    /// <returns>解码后的消息列表</returns>
    protected override IEnumerable<IMessage>? Decode(IHandlerContext context, IPacket pk)
    {
        if (context.Owner is not IExtend ss) yield break;

        if (ss["Codec"] is not PacketCodec pc)
        {
            ss["Codec"] = pc = new PacketCodec
            {
                GetLength = DefaultMessage.GetLength,
                // 帧首头部保证：DefaultMessage 头部最多 8 字节（4 固定 + 4 扩展长度）
                HeadSize = 8,
                MaxCache = MaxCache,
                Tracer = (context.Owner as ISocket)?.Tracer
            };
        }

        // 粘包拆帧：拥有切片零拷贝；纯残片轮进入段缓存，跨轮边界帧为独立拥有切片/链
        var frames = pc.Parse(pk);
        foreach (var frame in frames)
        {
            var msg = new DefaultMessage();
            Boolean ok;
            try
            {
                ok = msg.Read(frame);
            }
            catch
            {
                // 损坏帧解析异常：释放消息（归还可能已挂载的负载引用）后传播
                msg.Dispose();
                throw;
            }
            finally
            {
                // 帧用毕归还：负载为共享切片（引用计数）独立持有，帧句柄自身的引用由本层释放；
                // 异常路径（损坏帧）同样必须归还，避免池化缓冲无人释放
                frame.TryDispose();
            }

            if (ok)
                yield return msg;
            else
                msg.Dispose();
        }
    }

    /// <summary>是否匹配响应</summary>
    /// <param name="request">请求消息</param>
    /// <param name="response">响应消息</param>
    /// <returns>是否匹配</returns>
    protected override Boolean IsMatch(Object? request, Object? response) =>
        request is DefaultMessage req &&
        response is DefaultMessage res &&
        req.Sequence == res.Sequence;

    /// <summary>连接关闭时，归还本会话的粘包编码器</summary>
    /// <param name="context">处理器上下文</param>
    /// <param name="reason">关闭原因</param>
    /// <returns>是否成功关闭</returns>
    public override Boolean Close(IHandlerContext context, String reason)
    {
        // 归还本会话粘包编码器（可能持有跨轮残片缓冲）
        if (context.Owner is IExtend ss)
        {
            ss["Codec"].TryDispose();
            ss["Codec"] = null;
        }

        return base.Close(context, reason);
    }
}