using NewLife.Buffers;
using NewLife.Data;
using NewLife.Messaging;
using NewLife.Model;

namespace NewLife.Net.Handlers;

/// <summary>消息封包编码器</summary>
/// <remarks>
/// 该编码器向基于请求响应模型的协议提供了匹配队列，能够根据响应序列号去匹配请求。
/// 
/// 消息封包编码器实现网络处理器，具体用法是添加网络客户端或服务端主机。主机收发消息时，会自动调用编码器对消息进行编码解码。
/// 发送消息SendMessage时调用编码器Write/Encode方法；
/// 接收消息时调用编码器Read/Decode方法，消息存放在ReceivedEventArgs.Message。
/// 
/// 网络编码器支持多层添加，每个编码器处理后交给下一个编码器处理，直到最后一个编码器，然后发送出去。
/// </remarks>
public class MessageCodec<T> : Handler
{
    /// <summary>消息队列。用于匹配请求响应包</summary>
    public IMatchQueue? Queue { get; set; }

    /// <summary>匹配队列大小</summary>
    public Int32 QueueSize { get; set; } = 256;

    /// <summary>请求消息匹配队列中等待响应的超时时间。默认30_000ms</summary>
    /// <remarks>
    /// 某些RPC场景需要更长时间等待响应时，可以加大该值。
    /// 该值不宜过大，否则会导致请求队列过大，影响并行请求数。
    /// </remarks>
    public Int32 Timeout { get; set; } = 30_000;

    /// <summary>最大缓存待处理数据。默认10M</summary>
    public Int32 MaxCache { get; set; } = 10 * 1024 * 1024;

    /// <summary>用户数据包。写入时数据包转消息，读取时消息自动解包返回数据负载，要求T实现IMessage。默认true</summary>
    /// <remarks>一般用于上层还有其它编码器时，实现编码器级联</remarks>
    public Boolean UserPacket { get; set; } = true;

    /// <summary>打开链接</summary>
    /// <param name="context">处理器上下文</param>
    /// <returns>是否成功打开</returns>
    public override Boolean Open(IHandlerContext context)
    {
        if (context.Owner is ISocketClient client) Timeout = client.Timeout;

        return base.Open(context);
    }

    /// <summary>发送消息时，写入数据，编码并加入队列</summary>
    /// <remarks>
    /// 遇到消息T时，调用Encode编码并加入队列。
    /// Encode返回空时，跳出调用链。
    /// </remarks>
    /// <param name="context">处理器上下文</param>
    /// <param name="message">消息</param>
    /// <returns>处理后的消息</returns>
    public override Object? Write(IHandlerContext context, Object message)
    {
        // 谁申请，谁归还
        IPacket? owner = null;
        if (message is T msg)
        {
            var rs = Encode(context, msg);
            if (rs == null) return null;

            message = rs;
            owner = rs as IPacket;

            // 加入队列，忽略请求消息
            if (message is IMessage msg2)
            {
                if (!msg2.Reply) AddToQueue(context, msg);
            }
            else
                AddToQueue(context, msg);
        }

        try
        {
            return base.Write(context, message);
        }
        finally
        {
            // 下游可能忘了释放内存，这里兜底释放
            owner.TryDispose();
        }
    }

    /// <summary>编码消息，一般是编码为Packet后传给下一个处理器</summary>
    /// <param name="context">处理器上下文</param>
    /// <param name="msg">消息</param>
    /// <returns>编码后的数据包</returns>
    protected virtual Object? Encode(IHandlerContext context, T msg)
    {
        if (msg is IMessage msg2) return msg2.ToPacket();

        return null;
    }

    /// <summary>把请求加入队列，等待响应到来时建立请求响应匹配</summary>
    /// <param name="context">处理器上下文</param>
    /// <param name="msg">消息</param>
    protected virtual void AddToQueue(IHandlerContext context, T msg)
    {
        if (msg != null && context is IExtend ext)
        {
            var source = ext["TaskSource"];
            if (source != null)
            {
                Queue ??= new DefaultMatchQueue(QueueSize);

                Queue.Add(context.Owner, msg, Timeout, source);
            }
        }
    }

    /// <summary>连接关闭时，清空粘包编码器</summary>
    /// <param name="context">处理器上下文</param>
    /// <param name="reason">关闭原因</param>
    /// <returns>是否成功关闭</returns>
    public override Boolean Close(IHandlerContext context, String reason)
    {
        Queue?.Clear();

        return base.Close(context, reason);
    }

    /// <summary>接收数据后，读取数据包，Decode解码得到消息</summary>
    /// <remarks>
    /// Decode可以返回多个消息，每个消息调用一次下一级处理器。
    /// Decode返回空时，跳出调用链。
    /// </remarks>
    /// <param name="context">处理器上下文</param>
    /// <param name="message">消息</param>
    /// <returns>处理后的消息</returns>
    public override Object? Read(IHandlerContext context, Object message)
    {
        if (message is not IPacket pk) return base.Read(context, message);

        // 解码得到多个消息
        var list = Decode(context, pk);
        if (list == null) return null;

        var queue = Queue;
        var userPacket = UserPacket;

        foreach (var msg in list)
        {
            if (msg == null) continue;

            // 区分 IMessage 协议消息与普通消息，分别处理负载提取和响应匹配
            Object? rs;
            if (msg is IMessage imsg)
            {
                // 保存原始消息到上下文，供上层 Write 构造响应时使用
                if (context is IExtend ext) ext["_raw_message"] = imsg;

                // 提取负载或保留整个消息
                rs = userPacket ? imsg.Payload! : msg;

                // 响应消息匹配请求队列（客户端收到服务端回复）
                if (queue != null && imsg.Reply)
                    MatchResponse(queue, context.Owner, imsg, userPacket);
            }
            else
            {
                rs = msg;

                // 非协议消息直接匹配
                queue?.Match(context.Owner, msg, rs, IsMatch);
            }

            // 向上层传递分包消息，注意这里可能处于网络IO线程
            base.Read(context, rs);

            // 归还池化的 DefaultMessage
            // userPacket==true: msg 仅作解码容器，上层拿到的是独立的 Payload，msg 可安全归还到池
            // userPacket==false: msg 本身就是上层消费的数据，可能被异步使用（如Remoting），不能归还
            if (msg is DefaultMessage dm && userPacket) DefaultMessage.Return(dm);
        }

        return null;
    }

    /// <summary>匹配响应消息到请求队列，克隆共享缓冲区数据后传给等待线程</summary>
    private void MatchResponse(IMatchQueue queue, Object? owner, IMessage msg, Boolean userPacket)
    {
        // 网络缓冲区数据必须克隆后才能安全传给等待线程
        // userPacket: 克隆 Payload 作为结果，msg 仍可归还到池
        // 非 userPacket: 克隆 Payload 使 msg 脱离共享缓冲区，msg 整体作为结果传给等待线程
        Object result;
        if (userPacket)
        {
            result = msg.Payload!.Clone();
        }
        else
        {
            if (msg.Payload != null) msg.Payload = msg.Payload.Clone();
            result = msg;
        }

        queue.Match(owner, msg, result, IsMatch);
    }

    /// <summary>从上下文中获取原始请求</summary>
    /// <param name="context">处理器上下文</param>
    /// <returns>原始消息</returns>
    protected IMessage? GetRequest(IHandlerContext context)
    {
        if (context is IExtend ext) return ext["_raw_message"] as IMessage;

        return null;
    }

    /// <summary>解码</summary>
    /// <param name="context">处理器上下文</param>
    /// <param name="pk">数据包</param>
    /// <returns>解码后的消息列表</returns>
    protected virtual IEnumerable<T>? Decode(IHandlerContext context, IPacket pk) => null;

    /// <summary>是否匹配响应</summary>
    /// <param name="request">请求消息</param>
    /// <param name="response">响应消息</param>
    /// <returns>是否匹配</returns>
    protected virtual Boolean IsMatch(Object? request, Object? response) => true;

    #region 粘包处理
    /// <summary>从数据流中获取整帧数据长度</summary>
    /// <param name="pk">数据包</param>
    /// <param name="offset">长度的偏移量</param>
    /// <param name="size">长度大小。0变长，1/2/4小端字节，-2/-4大端字节</param>
    /// <returns>数据帧长度（包含头部长度位）</returns>
    public static Int32 GetLength(IPacket pk, Int32 offset, Int32 size) => GetLength(pk.GetSpan(), offset, size);

    /// <summary>从数据流中获取整帧数据长度</summary>
    /// <param name="span">数据包</param>
    /// <param name="offset">长度的偏移量</param>
    /// <param name="size">长度大小。0变长，1/2/4小端字节，-2/-4大端字节</param>
    /// <returns>数据帧长度（包含头部长度位）</returns>
    public static Int32 GetLength(ReadOnlySpan<Byte> span, Int32 offset, Int32 size)
    {
        if (offset < 0) return span.Length;

        // 数据不够，连长度都读取不了
        if (offset >= span.Length) return 0;

        var reader = new SpanReader(span) { IsLittleEndian = true };
        reader.Advance(offset);

        // 读取大小
        var len = 0;
        switch (size)
        {
            case 0:
                // 计算变长的头部长度
                var p = reader.Position;
                len = reader.ReadEncodedInt() + reader.Position - p;
                break;
            case 1:
                len = reader.ReadByte();
                break;
            case 2:
                len = reader.ReadUInt16();
                break;
            case 4:
                len = reader.ReadInt32();
                break;
            case -2:
                reader.IsLittleEndian = false;
                len = reader.ReadUInt16();
                break;
            case -4:
                reader.IsLittleEndian = false;
                len = reader.ReadInt32();
                break;
            default:
                throw new NotSupportedException();
        }

        // 判断后续数据是否足够
        if (len > span.Length) return 0;

        // 数据长度加上头部长度
        len += Math.Abs(size);

        return offset + len;
    }
    #endregion
}