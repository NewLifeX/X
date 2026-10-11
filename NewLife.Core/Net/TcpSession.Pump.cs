using NewLife.Data;
using NewLife.Messaging;

namespace NewLife.Net;

/// <summary>增强TCP客户端（协议模式的消息泵与帧定界交付）</summary>
/// <remarks>
/// <para>协议模式下数据经数据管道定界，由消息泵逐帧交付。流式专有配置（最大残留、单帧上限、整帧模式）、并发派发信号量
/// 与消息交付入口（<see cref="OnMessageAsync"/> 等）随本类，不再放到 <see cref="SessionBase"/>：
/// 数据报会话没有粘包与定界问题，这些成员在它那里只会是"未使用"。</para>
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

    /// <summary>关闭时等待在途帧交付完成的上限（毫秒），默认 1000。0 表示不等待</summary>
    /// <remarks>
    /// <para>数据由接收环写入管道、消息泵异步交付，而关闭（含对端 FIN 的被动关闭）在接收环上同步推进：
    /// 不等在途交付直接关闭，会把“已读入管道但尚未交给上层”的帧连同关闭一起丢掉。
    /// 对端“发完即关”时最后一条消息最先受损（如 MQTT 的 DISCONNECT 丢失，上层把正常断开误判为异常断开并误发遗嘱）。</para>
    /// <para>关闭流程先取消挂起读取，再等待泵退出（泵退出前会把已读出的帧交付完），最多等待本上限；
    /// 处理器长时间不返回时不再等待，按剩余流程关闭。</para>
    /// </remarks>
    public Int32 PumpDrainTimeout { get; set; } = 1000;

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

    /// <summary>停止接收处理（异步）。取消挂起读取并等待在途帧交付完成，保证在途帧先于关闭交付给上层</summary>
    protected override Task StopReceiveFlowAsync() => StopMessagePumpAsync();

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

    /// <summary>泵本体任务。关闭时等待在途帧交付完成用（观察续体与泵本体完成时序不同，不可用于等待）</summary>
    private Task? _pumpRunTask;

    /// <summary>当前异步流是否处于消息泵内。用于识别泵内关闭（处理器内主动关闭、泵内报错关闭），避免等待泵自身退出</summary>
#if NET45
    // net45 无 AsyncLocal，退回线程本地（与 TokenHttpFilter 同一降级口径）；异步续体切线程后标记可能丢失，此时退化为等到上限
    private readonly ThreadLocal<Boolean> _inPump = new();
#else
    private readonly AsyncLocal<Boolean> _inPump = new();
#endif

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
        _pumpRunTask = task;

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
        _pumpRunTask = null;
    }

    /// <summary>停止消息泵（异步）。取消挂起读取，并等待在途帧交付完成，避免关闭前已读出的帧被丢弃</summary>
    /// <remarks>
    /// <para>取消只作用于挂起读取：读取已完成的帧仍会被泵交付，因此等待泵退出即可保证这些帧先于关闭交付给上层。</para>
    /// <para>泵内主动关闭（处理器内、泵内报错）不得等待自身退出，否则要等到上限，直接返回。</para>
    /// </remarks>
    /// <returns>泵收尾任务</returns>
    protected virtual async Task StopMessagePumpAsync()
    {
        var run = Interlocked.Exchange(ref _pumpRunTask, null);

        StopMessagePump();

        var timeout = PumpDrainTimeout;
        if (run == null || run.IsCompleted || timeout <= 0 || _inPump.Value) return;

        // 在途帧交付通常微秒级完成；本等待是处理器长时间不返回时的兜底，超时后不再等待
        var done = await Task.WhenAny(run, Task.Delay(timeout)).ConfigureAwait(false);
        if (done != run) return;

        // 观察泵任务的异常，避免无人消费的故障任务堆积
        try { await run.ConfigureAwait(false); } catch { }
    }

    /// <summary>消息泵循环。定界消息帧并逐帧交付；同连接消息串行处理</summary>
    /// <param name="pump">消息帧泵</param>
    /// <param name="reader">数据管道读取器</param>
    /// <param name="cancellationToken">取消通知（会话关闭）</param>
    private async Task PumpAsync(MessagePump pump, PipeReader reader, CancellationToken cancellationToken)
    {
        // 先让出一次再置标记：异步流标记若在同步前缀赋值，会随执行上下文泄漏到启动会话的调用方，
        // 之后在同一线程上发生的关闭会被误判为“泵内关闭”而跳过等待，丢帧照旧
        await Task.Yield();

        // 标记当前异步流处于泵内：泵内（含处理器）主动关闭会话时不得等待泵自身退出
        _inPump.Value = true;

        // 不按取消令牌提前退出：取消只应终止“等待新数据”，已到达并写入管道的帧仍要交付完再退出。
        // 循环靠读取结果终止——无数据可读时的取消以 OperationCanceledException 逃出，由下方捕获后退出。
        // 反之（读到即退）会把“对端发完即关”时已到达的最后一条消息连同关闭一起丢掉。
        while (true)
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
                // 关闭已在进行（令牌已取消）：读取失效随关闭一并收尾，不再当作数据流故障报错与关闭，
                // 否则关闭流程释放管道后泵的收尾读取会刷出无意义的 MessagePumpError
                if (cancellationToken.IsCancellationRequested) break;

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

    #region 消息交付

    /// <summary>尝试把响应消息匹配给等待中的请求（协议模式）。命中则交付等待方，跳过收尾</summary>
    /// <param name="message">收到的消息</param>
    /// <returns>是否已匹配交付</returns>
    /// <remarks>
    /// <para>无匹配队列（未发过等待请求）或无配对协议时快速返回，不产生额外开销；
    /// 消息是否可配对由协议 matcher 判定（如 SRMP 要求应答消息+序列号相等；无方向协议可用恒真 matcher）。</para>
    /// <para>流式负载在事件交付前物化为内存模式：事件链可观察读取，等待方在任意时机异步消费（一次拷贝换正确性）。
    /// 未命中时消息按普通流程收尾，负载随消息归还。</para>
    /// </remarks>
    private Boolean TryMatchResponse(IMessage message) => Matcher.TryMatch(this, Protocol, message);

    /// <summary>收到消息（异步）。协议模式（<see cref="SessionBase.Protocol"/> 非空）下由消息泵逐帧调用</summary>
    /// <param name="message">消息（头部字段就位、体已绑定）</param>
    /// <remarks>
    /// <para>默认触发同步 <see cref="SessionBase.Received"/> 事件链。处理器返回后消息进入收尾：未读体被丢弃对齐帧尾、消息释放。</para>
    /// <para>需要异步读取流式主体的场景，继承会话重写本方法，在 await 期间消息与数据窗口保持有效；同步事件处理器内需要流式数据时请先物化（<see cref="LimitedReader.ReadAllAsync"/>，数据未到齐会等待——串行语义下正确），或物化后交给后台异步链处理。</para>
    /// </remarks>
    protected virtual ValueTask OnMessageAsync(IMessage message)
    {
        OnMessage(message);

        return default;
    }

    /// <summary>收到消息。协议模式（<see cref="SessionBase.Protocol"/> 非空）下由消息泵逐帧调用</summary>
    /// <param name="message">消息（头部字段就位、体已绑定）</param>
    /// <remarks>
    /// <para>构造接收事件参数并进入 <see cref="SessionBase.OnReceive"/> 事件链；消息体未读部分由消息泵在交付后丢弃对齐下一帧。</para>
    /// <para>事件参数的 <see cref="ReceivedEventArgs.Packet"/> 为消息负载视图（整帧快路径），流式模式下为 null；与消息同生命周期（处理器返回后失效），需要留存请先物化或切出共享句柄。业务请统一经 <see cref="ReceivedEventArgs.Message"/> 读取头部与流式体。</para>
    /// </remarks>
    protected virtual void OnMessage(IMessage message)
    {
        var e = ReceivedEventArgs.Rent();
        try
        {
            e.Local = Local.Address;
            e.Remote = Remote.EndPoint;
            e.Packet = message.Payload;
            e.Message = message;

            OnReceive(e);
        }
        finally
        {
            ReceivedEventArgs.Return(e);
        }
    }

    #endregion
}
