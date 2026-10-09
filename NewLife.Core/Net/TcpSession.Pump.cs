using NewLife.Data;
using NewLife.Messaging;

namespace NewLife.Net;

/// <summary>增强TCP客户端（协议模式的消息泵与帧定界交付）</summary>
/// <remarks>
/// <para>协议模式下数据经数据管道定界，由消息泵逐帧交付。流式专有配置（最大残留、单帧上限、整帧模式）与并发派发信号量随本类，
/// 不再放到 <see cref="SessionBase"/>：数据报会话没有粘包与定界问题，这些成员在它那里只会是"未使用"。</para>
/// </remarks>
public partial class TcpSession
{
    #region 属性

    /// <summary>消息泵最大缓存字节数（协议模式下无法定界的残余上限），默认 1M。0 表示不限制</summary>
    /// <remarks>残余达到上限说明对端数据与协议不匹配或已损坏，消息泵随即报错并关闭会话，避免连接僵死</remarks>
    public Int32 MaxCache { get; set; } = 1024 * 1024;

    /// <summary>整帧模式下的单帧长度上限，默认 16M。0 表示不限制</summary>
    /// <remarks>仅 <see cref="RequireFullFrame"/> 为 true 时生效：整帧解析要求整个帧驻留内存，本上限是单连接的内存安全阀</remarks>
    public Int32 MaxFrameSize { get; set; } = 16 * 1024 * 1024;

    /// <summary>消息泵是否要求整帧完整才产出。默认 false（头部到齐即交付，体可为流式）</summary>
    /// <remarks>在消息泵任务上同步消费流式体的实现应重写为 true：否则大帧会在事件内同步等待后续数据，占用线程池线程</remarks>
    protected virtual Boolean RequireFullFrame => false;

    #endregion

    #region 接收流程

    /// <summary>启动接收处理。协议模式先启动消息泵（先于接收环，首个数据到达前就绪）</summary>
    /// <remarks>仅流式会话有消息泵：数据经数据管道定界后逐帧交付，接收环收到的原始字节不再进事件（见 <see cref="RawReceiveTaken"/>）</remarks>
    protected override void StartReceiveFlow()
    {
        if (Protocol == null) return;

        if (AutoReceive) StartMessagePump();
        else WriteLog("协议模式需要自动接收（AutoReceive），拉取模式下消息泵未启动，收到的是原始字节");
    }

    /// <summary>停止接收处理。关闭流程开头先停消息泵（取消挂起读取），随后关闭处理器链与会话</summary>
    protected override void StopReceiveFlow() => StopMessagePump();

    /// <summary>本轮原始数据是否已被消息泵接管</summary>
    /// <remarks>协议模式下数据经数据管道定界交付，接收环不再把原始字节抛给事件；拉取模式（无泵）时仍走事件</remarks>
    protected override Boolean RawReceiveTaken => _pumpTask != null;

    /// <summary>销毁资源</summary>
    /// <param name="disposing">是否释放托管资源</param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        // 只释放、不置空：置空后并发任务的 ??= 会新建“满额”信号量，其 Release 直接抛 SemaphoreFullException。
        // 保留已释放实例，迟到的 Release 得到 ObjectDisposedException，由收尾逻辑按正常时序忽略
        _concurrency?.Dispose();
    }

    #endregion

    #region 消息泵

    private CancellationTokenSource? _pumpCts;

    /// <summary>泵任务的观察续体。存的是 ContinueWith 续体而非泵本体：仅用于观察失败，以及标记“协议模式已启动”（停止时清空）</summary>
    private Task? _pumpTask;

    /// <summary>启动消息泵。协议模式（<see cref="SessionBase.Protocol"/> 非空）下由打开流程与服务端会话启动流程调用</summary>
    /// <remarks>首次访问数据管道确保其在接收环启动前就绪。调用方须保证时序（每次会话生命周期至多一次）</remarks>
    protected void StartMessagePump()
    {
        var codec = Protocol;
        if (codec == null) return;

        // 触建数据管道（接收环启动前就绪，首个数据到达即可投递）
        var reader = Pipe.Reader;

        var cts = new CancellationTokenSource();
        _pumpCts = cts;

        WriteLog("启动消息泵：{0}", codec);

        var task = PumpAsync(new MessagePump(codec) { MaxCache = MaxCache, RequireFullFrame = RequireFullFrame, MaxFrameSize = MaxFrameSize }, reader, cts.Token);

        // 观察泵任务：泵内异常若无人观察会被静默吞掉，表现为“连接还在但再也收不到消息”，极难定位
        _pumpTask = task.ContinueWith(
            t =>
            {
                try
                {
                    if (t.IsFaulted) OnError("MessagePump", t.Exception!.GetBaseException());
                }
                catch { }
            },
            CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    /// <summary>停止消息泵。取消挂起读取，泵任务随后自行退出（数据管道完成同样唤醒读取）</summary>
    private void StopMessagePump()
    {
        var cts = Interlocked.Exchange(ref _pumpCts, null);
        if (cts != null)
        {
            cts.Cancel();
            cts.Dispose();
        }

        // 清空“泵已启动”标记：RawReceiveTaken 以本生命周期是否已起泵为准。
        // 不清会让标志在泵退出后恒真——后续接收判断仍按“数据已交给泵”静默丢弃原始字节
        _pumpTask = null;
    }

    /// <summary>消息泵循环。定界消息帧并逐帧交付；同连接消息串行处理</summary>
    /// <param name="pump">消息帧泵</param>
    /// <param name="reader">数据管道读取器</param>
    /// <param name="cancellationToken">取消通知（会话关闭）</param>
    private async Task PumpAsync(MessagePump pump, PipeReader reader, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            IMessage? message;
            try
            {
                message = await pump.ReadAsync(reader, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                OnError("MessagePump", ex);

                // 数据流不可恢复（协议错误/IO 异常）：关闭会话，避免半开连接僵死
                Close("MessagePumpError");
                break;
            }

            // 数据管道完成（连接关闭）：退出
            if (message == null) break;

            // 有匹配队列时，入站消息都可能被配对交付给等待方：等待方在事件链之后还要异步消费同一份体，
            // 而流式体不能被二次读（泵会继续读同一 PipeReader，导致单读者冲突或把下一帧字节当成体），因此先物化为内存体。
            // 不能依赖 message.Reply——无方向位协议（如 LengthFieldCodec 的恒真 matcher）恒为 false，会让等待方拿到流式体而串包
            if (MatchQueue != null && message.Body is { IsStreaming: true })
            {
                try
                {
                    var all = await message.Body.ReadAllAsync(cancellationToken).ConfigureAwait(false);
                    message.SetBody(all);
                }
                catch (OperationCanceledException)
                {
                    message.TryDispose();
                    continue;
                }
                catch (Exception ex)
                {
                    // 流式体读满失败（对端半途断开/管道故障）：数据流已不可恢复，与帧层读失败同口径关闭会话。
                    // 异常若向上逃出泵任务，续体只记日志不关会话，表现为“连接还在、消息不再到达、也不触发关闭”的半死态
                    message.TryDispose();
                    OnError("MessagePump", ex);
                    Close("MessagePumpError");
                    break;
                }
            }

            // 并行模式：先物化流式体（一次拷贝换并行安全），信号量约束并发后派发；处理顺序不定（SRMP 按序列号配对）
            if (MaxConcurrency > 1)
            {
                if (message.Body is { IsStreaming: true })
                {
                    try
                    {
                        var all = await message.Body.ReadAllAsync(cancellationToken).ConfigureAwait(false);
                        message.SetBody(all);
                    }
                    catch (OperationCanceledException)
                    {
                        message.TryDispose();
                        continue;
                    }
                    catch (Exception ex)
                    {
                        // 同匹配队列分支：流式体读满失败即数据流不可恢复，丢弃消息并关闭会话
                        message.TryDispose();
                        OnError("MessagePump", ex);
                        Close("MessagePumpError");
                        break;
                    }
                }

                try
                {
                    await Concurrency.WaitAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    message.TryDispose();
                    break;
                }
                catch (ObjectDisposedException)
                {
                    // 会话销毁：并发信号量已释放，消息无人处理
                    message.TryDispose();
                    break;
                }

                // Task.Run 派发：async 方法首段同步执行，须真正切换线程池，否则同步处理器会阻塞泵循环
                _ = Task.Run(() => ProcessMessageAsync(message, cancellationToken, true));
                continue;
            }

            // 串行模式：同连接依次处理（前一条收尾后才读下一帧）
            await ProcessMessageAsync(message, cancellationToken, false).ConfigureAwait(false);
        }
    }

    /// <summary>处理单个消息：统一进入事件链（可观测）→ 响应尝试匹配交付 → 收尾</summary>
    /// <param name="message">消息</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <param name="releaseSlot">完成后是否释放并发信号量（并行派发为 true）</param>
    /// <remarks>
    /// <para>交付收尾：未交付等待方的消息丢弃未读负载对齐帧尾，随后释放；命中交付的消息由等待方释放。</para>
    /// <para>并行模式（<see cref="SessionBase.MaxConcurrency"/> 大于1）下由独立任务调用，异常统一经 <see cref="SessionBase.OnError"/> 上报，不向任务外部抛出。</para>
    /// </remarks>
    private async Task ProcessMessageAsync(IMessage message, CancellationToken cancellationToken, Boolean releaseSlot)
    {
        try
        {
            var delivered = false;
            try
            {
                await OnMessageAsync(message).ConfigureAwait(false);

                // 事件链（可观测）可能已在处理器内读空消息体；命中配对的等待方还要消费同一份体，
                // 交付前把内存模式体复位到起点（流式体不参与配对交付，不可重放）
                if (message.Body is { IsStreaming: false } body) body.Reset();

                delivered = TryMatchResponse(message);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                OnError("OnMessage", ex);
            }
            finally
            {
                // 交付收尾：未交付等待方的消息丢弃未读负载对齐帧尾，随后释放
                if (!delivered)
                {
                    try
                    {
                        await MessagePump.DiscardAsync(message, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) { }
                    finally
                    {
                        message.TryDispose();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // 兕底：防止 fire-and-forget 任务异常未观察
            OnError("ProcessMessage", ex);
            message.TryDispose();
        }
        finally
        {
            // 会话销毁时信号量可能已释放：任务收尾晚于 Dispose 属正常时序，忽略该异常
            if (releaseSlot)
            {
                try { Concurrency.Release(); }
                catch (ObjectDisposedException) { }
            }
        }
    }

    private SemaphoreSlim? _concurrency;

    /// <summary>并发信号量。并行模式（<see cref="SessionBase.MaxConcurrency"/> 大于1）下约束同连接并发处理数，等待时形成背压</summary>
    /// <remarks>惰性创建后<b>只释放、不置空</b>：置空后迟到的归还会经 <c>??=</c> 新建出“满额”信号量，
    /// 其 <c>Release</c> 抛 <see cref="SemaphoreFullException"/>；保留已释放实例则只抛
    /// <see cref="ObjectDisposedException"/>，由收尾逻辑按正常时序忽略。</remarks>
    private SemaphoreSlim Concurrency => _concurrency ??= new SemaphoreSlim(MaxConcurrency, MaxConcurrency);

    #endregion
}
