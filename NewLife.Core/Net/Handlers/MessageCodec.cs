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
    /// <param name="context"></param>
    /// <param name="msg"></param>
    /// <returns></returns>
    protected virtual Object? Encode(IHandlerContext context, T msg)
    {
        if (msg is IMessage msg2) return msg2.ToPacket();

        return null;
    }

    /// <summary>把请求加入队列，等待响应到来时建立请求响应匹配</summary>
    /// <param name="context"></param>
    /// <param name="msg"></param>
    /// <returns></returns>
    protected virtual void AddToQueue(IHandlerContext context, T msg)
    {
        if (msg != null && context is IExtend ext && ext["TaskSource"] is TaskCompletionSource<Object> source)
        {
            Queue ??= new DefaultMatchQueue(QueueSize);

            Queue.Add(context.Owner, msg, Timeout, source);
        }
    }

    /// <summary>连接关闭时，清空粘包编码器</summary>
    /// <param name="context"></param>
    /// <param name="reason"></param>
    /// <returns></returns>
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
    /// <param name="context"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public override Object? Read(IHandlerContext context, Object message)
    {
        if (message is not IPacket pk) return base.Read(context, message);

        // 解码得到多个消息
        var list = Decode(context, pk);
        if (list == null) return null;

        foreach (var msg in list)
        {
            if (msg == null) continue;

            Object? rs = null;
            if (UserPacket && msg is IMessage msg2)
                rs = msg2.Payload;
            else
                rs = msg;

            //var rs = message;
            if (msg is IMessage msg3)
            {
                // 匹配请求队列
                if (msg3.Reply)
                {
                    //!!! 处理结果的Packet需要拷贝一份，否则交给另一个线程使用会有冲突
                    // Match里面TrySetResult时，必然唤醒原来阻塞的Task，如果不是当前io线程执行后续代码，必然导致两个线程共用了数据区，因此需要拷贝
                    if (rs is IMessage msg4 && msg4.Payload != null && msg4.Payload == msg3.Payload)
                        msg4.Payload = msg4.Payload.Clone();

                    Queue?.Match(context.Owner, msg, rs ?? msg, IsMatch);
                }
            }
            else if (rs != null)
            {
                // 其它消息不考虑响应
                Queue?.Match(context.Owner, msg, rs, IsMatch);
            }

            // 匹配输入回调，让上层事件收到分包信息。
            // 这里很可能处于网络IO线程，阻塞了下一个Tcp包的接收
            base.Read(context, rs ?? msg);
        }

        return null;
    }

    /// <summary>解码</summary>
    /// <param name="context"></param>
    /// <param name="pk"></param>
    /// <returns></returns>
    protected virtual IList<T>? Decode(IHandlerContext context, IPacket pk) => null;

    /// <summary>是否匹配响应</summary>
    /// <param name="request"></param>
    /// <param name="response"></param>
    /// <returns></returns>
    protected virtual Boolean IsMatch(Object? request, Object? response) => true;

    #region 粘包处理
    /// <summary>从数据流中获取整帧数据长度</summary>
    /// <param name="pk">数据包</param>
    /// <param name="offset">长度的偏移量</param>
    /// <param name="size">长度大小。0变长，1/2/4小端字节，-2/-4大端字节</param>
    /// <returns>数据帧长度（包含头部长度位）</returns>
    public static Int32 GetLength(IPacket pk, Int32 offset, Int32 size)
    {
        if (offset < 0) return pk.Total;

        // 数据不够，连长度都读取不了
        if (offset >= pk.Total) return 0;

        var reader = new SpanReader(pk.GetSpan());
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
        if (len > pk.Total) return 0;

        // 数据长度加上头部长度
        len += Math.Abs(size);

        return offset + len;
    }

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

        var reader = new SpanReader(span);
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