using System.Buffers;
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
/// 接收消息时调用编码器Read/Decode方法，消息存放在接收事件参数的 Message 属性。
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

                // UserPacket 模式：负载以 IPacket 上抛（拥有切片或借阅视图），维持级联编码器契约；
                // 事件内可直接消费或交给应答/发送链路；需跨轮持有时在事件内 Slice 切出共享句柄（用后 Dispose）
                rs = userPacket ? imsg.Payload! : msg;
            }
            else
            {
                rs = msg;
            }

            // 响应交付句柄：事件之前先切出独立共享句柄（引用计数加一）。
            // 订阅者可在事件内随意处置原负载（读取/切片/Slice 带出/交给发送链消费），等待方拿到的仍是有效数据；
            // 命中后所有权归等待方，未命中随匹配归还。负载不是拥有句柄时走匹配内兜底克隆。
            IPacket? claim = null;
            if (userPacket && queue != null && msg is IMessage m0 && m0.Reply && m0.Payload is IOwnerPacket)
                claim = m0.Payload.Slice(0, -1);

            try
            {
                // 先向上层触发 Received 事件（全量监视，同步消费），再做请求-响应匹配。
                // 等待方 continuation 异步执行，与同步事件链无竞态；交付句柄已独立，订阅者处置不影响匹配。
                base.Read(context, rs);
            }
            catch
            {
                // 事件链异常：放弃交付并归还预切句柄，避免引用泄漏
                claim?.TryDispose();
                throw;
            }

            if (msg is IMessage imsg2)
            {
                // 响应匹配（后）：命中时把独立交付句柄的所有权转移给 await 等待方
                if (queue != null && imsg2.Reply)
                    MatchResponse(queue, context.Owner, imsg2, userPacket, claim);
                else
                    claim?.TryDispose();
            }
            else
            {
                // 非协议消息直接匹配；同步链消费完毕，未命中（或无匹配队列）时归还交付句柄，
                // 避免拥有切片无人释放（视图无操作）；需要跨轮携带请在事件内经 Slice 切出共享句柄
                var matched = queue != null && queue.Match(context.Owner, msg, rs, IsMatch);
                if (!matched && msg is IPacket mp) mp.TryDispose();
            }

            // 释放消息容器（归还负载引用）
            // userPacket==true: msg 仅作解码容器，负载所有权已转移给上层或随容器释放，msg 可安全释放
            // userPacket==false: msg 本身就是上层消费的数据，可能被异步使用（如Remoting），不能释放
            if (msg is DefaultMessage dm && userPacket) dm.Dispose();
        }

        return null;
    }

    /// <summary>匹配响应消息到请求队列，把负载所有权安全交付给 await 等待方</summary>
    /// <remarks>
    /// 按负载归属分类处理：
    /// <list type="bullet">
    /// <item><b>拥有负载</b>（<see cref="IOwnerPacket"/>，接收链路共享切片）：userPacket → 交付事件前预切的独立句柄（<paramref name="claim"/>），容器照常归还原负载引用；!userPacket → 消息整体交付（等待方 Dispose 消息即归还）。</item>
    /// <item><b>兜底</b>：负载为借阅视图（非拥有输入）或非 Message 实现时，克隆为独立拥有副本（<see cref="PacketHelper.Clone"/>）后交付。</item>
    /// <item><b>未命中</b>：无人等待（无挂起请求 / 无匹配）时立即归还拥有缓冲（消息容器释放、交付句柄归还），避免缓冲无人释放。</item>
    /// </list>
    /// </remarks>
    /// <param name="queue">匹配队列</param>
    /// <param name="owner">拥有者</param>
    /// <param name="msg">响应消息</param>
    /// <param name="userPacket">是否负载模式</param>
    /// <param name="claim">事件前预切的独立交付句柄。可为空</param>
    /// <returns>是否已匹配到挂起请求</returns>
    private Boolean MatchResponse(IMatchQueue queue, Object? owner, IMessage msg, Boolean userPacket, IPacket? claim)
    {
        var payload = msg.Payload;

        // 交付：命中等待方则所有权转移给等待方；未命中立即归还，避免池化缓冲无人释放。
        // received 供 IsMatch 与挂起请求配对；result 为交付给等待方的值
        Boolean Deliver(Object received, Object result, Object? orphan)
        {
            var ok = queue.Match(owner, received, result, IsMatch);
            if (!ok) orphan.TryDispose();

            return ok;
        }

        // 拥有负载：交付独立句柄或负载本身，无需二次拷贝
        if (payload is IOwnerPacket)
        {
            if (userPacket)
            {
                // 独立句柄交付：订阅者事件内处置原负载不影响等待方；原负载引用仍由容器归还
                if (claim != null) return Deliver(msg, claim, claim);

                // 兜底：无预切句柄时直接交付负载；容器回池前摘除，避免二次归还
                msg.Payload = null;

                return Deliver(msg, payload, payload);
            }

            // 消息整体交付：消息持有拥有负载，等待方 Dispose 消息即归还
            return Deliver(msg, msg, msg);
        }

        // 兜底：借阅视图（非拥有输入）无法跨链路交付，克隆为独立拥有副本
        var copy = payload?.Clone();

        if (!userPacket && msg is Message m2)
        {
            m2.Payload = copy;

            return Deliver(msg, msg, msg);
        }
        msg.Payload = null;

        return Deliver(msg, copy ?? new ArrayPacket([]), copy);
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
    /// <param name="pk">本轮接收数据（接收链路为轮拥有句柄，直接调用可为借阅视图）</param>
    /// <returns>解码后的消息列表</returns>
    protected virtual IEnumerable<T>? Decode(IHandlerContext context, IPacket pk) => null;

    /// <summary>是否匹配响应</summary>
    /// <param name="request">请求消息</param>
    /// <param name="response">响应消息</param>
    /// <returns>是否匹配</returns>
    protected virtual Boolean IsMatch(Object? request, Object? response) => true;

    #region 粘包处理
    /// <summary>从数据流中获取整帧数据长度（链感知：帧头不足时跨节点拼接）</summary>
    /// <param name="pk">数据包（可为链，帧头可能跨节点）</param>
    /// <param name="offset">长度的偏移量。负数表示无长度字段，整包即一帧</param>
    /// <param name="size">长度大小。0变长，1/2/4小端字节，-2/-4大端字节</param>
    /// <returns>完整帧长度（可能大于现有数据）；返回0表示头部不足无法定界</returns>
    public static Int32 GetLength(IPacket pk, Int32 offset, Int32 size)
    {
        if (offset < 0) return pk.Total;

        var need = offset + (size == 0 ? 5 : Math.Abs(size));

        // 帧首节点足够或无后续链：直接解析
        var span = pk.GetSpan();
        if (span.Length >= need || pk.Next == null) return GetLength(span, offset, size);

        // 帧头跨节点：前缀拼读；超大头部退回数组拷贝
        if (need > 256)
        {
            var data = pk.ReadBytes(0, need);
            return GetLength(data, offset, size);
        }

        Span<Byte> buf = stackalloc Byte[need];

        return GetLength(pk.GetPrefix(buf, need), offset, size);
    }

    /// <summary>从数据流中获取整帧数据长度</summary>
    /// <param name="span">数据包</param>
    /// <param name="offset">长度的偏移量</param>
    /// <param name="size">长度大小。0变长，1/2/4小端字节，-2/-4大端字节</param>
    /// <returns>完整帧长度（可能大于现有数据，调用方需自行判断数据是否足够）；返回0表示头部不足无法定界</returns>
    public static Int32 GetLength(ReadOnlySpan<Byte> span, Int32 offset, Int32 size)
    {
        if (offset < 0) return span.Length;

        // 数据不够，连长度都读取不了
        if (offset >= span.Length) return 0;

        // 长度字段本身不完整，视为数据不足（避免 SpanReader 越界抛出）
        var lenBytes = size == 0 ? 1 : Math.Abs(size);
        if (span.Length - offset < lenBytes) return 0;

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
            case -1:
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

        // 数据长度加上头部长度，得到完整帧长。可能大于现有数据（段保留依赖此声明长度跨轮累积），
        // 调用方需自行判断数据是否足够（ParseFrames/DrainPending 均有边界检查）
        len += Math.Abs(size);

        return offset + len;
    }

    /// <summary>从数据流中获取整帧数据长度（只读序列版本，供流式帧层跨段定界）</summary>
    /// <param name="buffer">帧首窗口（只读序列，可跨段）</param>
    /// <param name="offset">长度的偏移量</param>
    /// <param name="size">长度大小。0变长，1/2/4小端字节，-2/-4大端字节</param>
    /// <returns>完整帧长度（可能大于现有数据，调用方需自行判断数据是否足够）；返回0表示头部不足无法定界</returns>
    /// <remarks>与链式版本同模式：按需把头部前缀拼入栈缓冲后复用跨度解析，不要求头部连续。</remarks>
    public static Int32 GetLength(ReadOnlySequence<Byte> buffer, Int32 offset, Int32 size)
    {
        if (offset < 0) return (Int32)buffer.Length;

        // 头部前缀至多 offset + 长度字段（变长编码最多5字节）
        var need = offset + (size == 0 ? 5 : Math.Abs(size));
        var buf = need <= 256 ? stackalloc Byte[need] : new Byte[need];
        var n = PacketHelper.CopyPrefix(buffer, buf);

        return GetLength(buf[..n], offset, size);
    }
    #endregion
}