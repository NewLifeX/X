using NewLife.Data;

namespace NewLife.Messaging;

/// <summary>消息帧泵。在数据包管道上按协议（<see cref="IMessageCodec"/>）定界消息帧：头部到齐即绑定体并交付</summary>
/// <remarks>
/// <para><b>头部到齐即交付</b>：帧头一旦完整即可产出消息，不必等整帧到齐。体按到达情况绑定：</para>
/// <list type="bullet">
/// <item><description><b>帧已完整</b>：<see cref="PipeReader.TakeFrame(Int64)"/> 零拷贝切出整帧，体为内存视图（<see cref="LimitedReader.IsStreaming"/> 为 false）</description></item>
/// <item><description><b>帧未完整</b>：体为流式读取器（<see cref="PipeReader.Limit(Int64)"/>），数据随管道到达，大帧不必整载入内存</description></item>
/// </list>
/// <para>两种绑定对消费方透明（统一经 <see cref="Message.Body"/> 读取）。未读体在交付收尾时经 <see cref="DiscardAsync"/> 丢弃，保持下一帧定界对齐。</para>
/// <para><b>无状态</b>：协议实例无状态可跨连接共享；帧泵实例可复用于多连接（跟随 <see cref="PipeReader"/> 单读约束）。</para>
/// </remarks>
/// <example>
/// <code>
/// var pump = new MessagePump(new SrmpCodec());
/// while (true)
/// {
///     var msg = await pump.ReadAsync(pipe.Reader, cancellationToken);
///     if (msg == null) break;   // 流结束
///     try
///     {
///         // 头部字段立即可用；负载按需读取：await msg.Body.ReadAllAsync()
///     }
///     finally
///     {
///         await MessagePump.DiscardAsync(msg);   // 丢弃未读体对齐下一帧
///         msg.TryDispose();
///     }
/// }
/// </code>
/// </example>
public class MessagePump
{
    #region 属性
    /// <summary>消息编解码器（帧协议）</summary>
    public IMessageCodec? Codec { get; set; }

    /// <summary>最大缓存字节数（无法定界的残余上限），默认 1M。0 表示不限制</summary>
    /// <remarks>
    /// <para>正常帧头部到齐即交付，不产生累积；残余持续增长说明对端数据与协议不匹配或已损坏。</para>
    /// <para><see cref="TryRead"/> 不执行本检查（纯不等待语义），由 <see cref="ReadAsync"/> 在等待过程中执行：达到上限时抛出异常，避免连接僵死。</para>
    /// </remarks>
    public Int32 MaxCache { get; set; } = 1024 * 1024;

    /// <summary>要求整帧完整才产出（整帧模式）。默认 false：头部到齐即交付（体可为流式）</summary>
    /// <remarks>
    /// 同步泵场景（如 WebSocket 服务端的同步帧循环）无法异步消费流式体：帧未完整时不产出、不消费，留待数据到齐后重新解析。
    /// 该模式下的单帧长度上限由 <see cref="MaxFrameSize"/> 约束。
    /// </remarks>
    public Boolean RequireFullFrame { get; set; }

    /// <summary>整帧模式下的单帧长度上限，默认 16M。0 表示不限制</summary>
    /// <remarks>
    /// <para>整帧模式不消费未完整帧，仅凭对端声明一个超大帧长度即可让管道无限占用内存：<see cref="MaxCache"/> 只管“无法定界的残余”，
    /// 管不到“已定界但永远到不齐”的帧。超过上限时抛出异常，由调用方按协议错误关闭连接。</para>
    /// </remarks>
    public Int32 MaxFrameSize { get; set; } = 16 * 1024 * 1024;
    #endregion

    #region 构造
    /// <summary>实例化帧泵</summary>
    public MessagePump() { }

    /// <summary>实例化帧泵</summary>
    /// <param name="codec">消息编解码器</param>
    public MessagePump(IMessageCodec codec) => Codec = codec;
    #endregion

    #region 读取
    /// <summary>尝试读取一帧（不等待）。头部到齐即返回消息；无消息帧（空行/心跳等）自动跳过</summary>
    /// <param name="reader">数据包读取器</param>
    /// <param name="message">解析出的消息（成功时有效；头部字段就位、体已绑定）</param>
    /// <returns>是否读取到消息；头部不足或窗口内仅有无消息帧时返回 false（窗口不动，等追加）</returns>
    /// <exception cref="InvalidOperationException"><see cref="Codec"/> 未设置；或协议帧损坏（头部已完整但长度字段非法）；或整帧模式下单帧超过 <see cref="MaxFrameSize"/></exception>
    /// <remarks>协议返回无消息帧（<see cref="IMessageCodec.TryParse"/> 成功但消息为 null）时消费该帧并继续解析下一帧，直到产出消息或数据不足。</remarks>
    public Boolean TryRead(PipeReader reader, out IMessage? message) => TryReadCore(reader, out message, out _);

    /// <summary>尝试读取一帧（核心）。frameLength 返回“已定界但整帧未到齐”的帧长，0 表示无进展（头部不足）</summary>
    private Boolean TryReadCore(PipeReader reader, out IMessage? message, out Int64 frameLength)
    {
        if (reader == null) throw new ArgumentNullException(nameof(reader));

        message = null;
        frameLength = 0;

        var codec = Codec ?? throw new InvalidOperationException("MessagePump.Codec not set.");

        while (true)
        {
            var buffer = reader.Buffer;
            if (buffer.IsEmpty) return false;

            // 定界：头部不足返回 null，不消费、不产生对象
            var rs = codec.TryParse(buffer);
            if (rs == null) return false;

            // 损坏帧：立即报错（由会话层关闭连接），不进入等待——否则会僵死到残余上限才断开
            if (rs.Value.Invalid) throw new InvalidOperationException($"协议帧损坏：头部已完整但帧长度字段非法（协议 {codec.GetType().Name}）");

            var headerSize = rs.Value.HeaderSize;
            var bodyLength = rs.Value.BodyLength;

            // 已定界但需整帧（装饰协议声明，或本泵整帧模式）：不产出、不消费，等数据到齐后重新解析。
            // 必须与“头部不足”区分——否则上层会把正在正常累积的大帧当成无法定界的残余而误断连接
            if ((rs.Value.NeedFullFrame || (RequireFullFrame && rs.Value.Message != null)) && headerSize + bodyLength > buffer.Length)
            {
                rs.Value.Message?.Dispose();
                frameLength = headerSize + bodyLength;

                // 单帧长度超上限立即报错：否则对端只需声明一个超大帧，就能让本连接的内存无限增长
                if (MaxFrameSize > 0 && frameLength > MaxFrameSize)
                    throw new InvalidOperationException($"帧长度 {frameLength} 超过上限 {MaxFrameSize}，拒绝为超大帧无限缓冲");

                return false;
            }

            // 无消息帧（空行/心跳等）：消费该帧后继续解析下一帧
            if (rs.Value.Message == null)
            {
                var skip = headerSize;

                // 协议错误防护：既不产出消息又不消费字节会死循环，也不得消费超出窗口
                if (skip <= 0 || skip > buffer.Length) return false;

                reader.AdvanceTo(skip, skip);
                continue;
            }

            var msg = rs.Value.Message;

            // 协议已预绑定体（如压缩协议解压后重绑）：直接消费整帧，不再二次绑定。
            // 预绑定体的协议自己保证整帧到齐，但这里仍要校验窗口：自定义协议若提前绑定体，
            // 帧未到齐时推进窗口会越界抛异常，打断整条接收链
            if (msg.Payload != null)
            {
                if (headerSize + bodyLength > buffer.Length)
                {
                    msg.Dispose();
                    return false;
                }

                reader.AdvanceTo(headerSize + bodyLength, headerSize + bodyLength);

                message = msg;
                return true;
            }

            // 消费头部；体起点即当前读取位置
            reader.AdvanceTo(headerSize, headerSize);

            // 帧已完整：零拷贝切出整帧，体为内存视图；否则绑定流式体，数据随管道到达
            if (headerSize + bodyLength <= buffer.Length)
            {
                var frame = reader.TakeFrame(bodyLength);
                msg.SetBody(frame);
            }
            else
            {
                msg.BindBody(reader.Limit(bodyLength));
            }

            message = msg;
            return true;
        }
    }

    /// <summary>读取一帧；数据不足时等待追加</summary>
    /// <param name="reader">数据包读取器</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns>消息（头部字段就位、体已绑定）；流结束或有残余无法成帧时返回 null</returns>
    /// <remarks>数据不足时按 examined 语义标记已检查并继续等待；流结束时窗口内无法成帧的残余数据被丢弃。</remarks>
    public async ValueTask<IMessage?> ReadAsync(PipeReader reader, CancellationToken cancellationToken = default)
    {
        if (reader == null) throw new ArgumentNullException(nameof(reader));

        while (true)
        {
            var rr = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            if (rr.IsCanceled) return null;

            // 头部到齐：直接产出消息（帧完整/未完整在核心内部分流）
            if (TryReadCore(reader, out var message, out var frameLength)) return message;

            // 无法定界：流已结束则丢弃残余，否则标记已检查到窗口末尾等待追加。
            // 带异常结束时不伪装成优雅关闭（BCL 的 IsCompletedOrThrow 同义）：把故障抛给调用方按错误处理
            if (rr.IsCompleted)
            {
                if (reader.Error is { } error) throw error;

                return null;
            }

            var buffer = reader.Buffer;

            // 协议错误防护：残余无法定界且持续增长，达到上限即快速失败，避免连接僵死。
            // 已定界但整帧未到齐（frameLength > 0）不属于“无法定界的残余”，其上限由 MaxFrameSize 把关
            if (frameLength <= 0 && MaxCache > 0 && buffer.Length >= MaxCache)
                throw new InvalidOperationException($"无法定界的残余数据 {buffer.Length} 字节达到上限 {MaxCache}，对端数据与协议不匹配或已损坏");

            reader.AdvanceTo(0, buffer.Length);
        }
    }
    #endregion

    #region 交付收尾
    /// <summary>丢弃消息未读体，使主读取器对齐帧尾（交付收尾；随后应释放消息）</summary>
    /// <param name="message">消息（可为 null，安全）</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns>未读余量清零；流结束时可能保留余量（无法对齐）</returns>
    /// <remarks>处理方不读消息体时（仅头部语义即可完成处理），由交付路径调用本方法跳过余量，保持下一帧定界对齐；已读满或已释放的消息调用无操作。</remarks>
    public static async ValueTask DiscardAsync(IMessage? message, CancellationToken cancellationToken = default)
    {
        if (message?.Body is { Remaining: > 0 } body) await body.DrainAsync(cancellationToken).ConfigureAwait(false);
    }
    #endregion
}
