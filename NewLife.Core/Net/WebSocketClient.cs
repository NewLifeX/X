using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using NewLife.Data;
using NewLife.Http;
using NewLife.Log;
using NewLife.Messaging;
using NewLife.Security;
using NewLife.Threading;

namespace NewLife.Net;

/// <summary>WebSocket客户端</summary>
public class WebSocketClient : TcpSession
{
    #region 属性
    /// <summary>资源地址</summary>
    public Uri Uri { get; set; } = null!;

    /// <summary>WebSocket心跳间隔。默认120秒</summary>
    public TimeSpan KeepAlive { get; set; } = TimeSpan.FromSeconds(120);

    /// <summary>请求头。ws握手时可以传递Token</summary>
    public IDictionary<String, String?>? RequestHeaders { get; set; }

    /// <summary>客户端掩码密钥。RFC 6455 要求客户端发送的所有帧必须带掩码，服务端发送的帧不能带掩码</summary>
    /// <remarks>一般无需设置：出站帧默认由协议（WebSocketCodec）为每帧生成新的随机掩码（RFC 6455 §5.3 要求每帧使用不可预测的新 key）。显式设置后优先使用该值。</remarks>
    public Byte[]? MaskKey { get; set; }

    /// <summary>最近收到 Pong 响应的时间。用于心跳超时检测</summary>
    public DateTime LastPongTime { get; private set; }

    /// <summary>Pong 超时时间。超过此时间未收到 Pong 响应将触发 <see cref="OnPongTimeout"/>。默认 0 表示不检测</summary>
    public TimeSpan PongTimeout { get; set; }
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    public WebSocketClient()
    {
        // 协议模式：客户端角色（发送自动加掩码，接收服务端无掩码帧）
        Protocol = new WebSocketCodec { IsServer = false };

        // 拉取 API：接收消息入队
        Received += OnReceivedMessage;
    }

    /// <summary>实例化</summary>
    /// <param name="uri"></param>
    public WebSocketClient(Uri uri) : this()
    {
        Uri = uri;

        Remote = new NetUri(uri.ToString());

        // 安全语义来自地址方案：wss 启用TLS。此前需调用方手工设置 SslProtocol，否则静默明文连接
        if (Remote.IsSecure) SslProtocol = NetHelper.DefaultSslProtocols;
    }

    /// <summary>实例化</summary>
    /// <param name="url"></param>
    public WebSocketClient(String url) : this(new Uri(url)) { }
    #endregion

    /// <summary>打开连接，建立WebSocket请求</summary>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns></returns>
    protected override async Task<Boolean> OnOpenAsync(CancellationToken cancellationToken)
    {
        var remote = Remote;
        if (remote == null || remote.Address.IsAny() || remote.Port == 0)
        {
            remote = Remote = new NetUri(Uri.ToString());
        }

        // 安全语义优先于端口号：wss 启用TLS；调用方显式设置的 SslProtocol 不被覆盖
        if (remote.IsSecure && SslProtocol == SslProtocols.None) SslProtocol = NetHelper.DefaultSslProtocols;

        var rs = await base.OnOpenAsync(cancellationToken).ConfigureAwait(false);
        if (!rs) return false;

        // 历史同步握手实现：早期在此同步收发，后移至 WebSocketCodec.Open（因其时 Active 已置位），
        // 现由下方异步握手取代（直读原语，不经 Open 守卫）；静态 Handshake 保留供手工调用
        //// 连接必须是ws/wss协议
        //if (remote.Type != NetType.WebSocket) return false;

        //// 设置为激活
        //Active = true;

        //var rs = Handshake(this, Uri);

        //Active = false;

        // 异步握手。失败即整条打开失败，释放底层避免半开连接
        if (!await HandshakeAsync(cancellationToken).ConfigureAwait(false))
        {
            // 必须与其它失败路径一样置空：已释放的 Socket 其 IsBound 恒为 true，留着会让下一次重试
            // 跳过建套接字、直接拿已释放实例去建连，以 ObjectDisposedException 收场而不是返回 false
            Client.TryDispose();
            Client = null;

            return false;
        }

        // 打开成功，复位关闭标记
        SetOpened();

        // 订阅 Received 事件以跟踪 Pong 响应（仅事件模式有效）。
        // 先退订再订阅：重复执行打开流程时，重复订阅会让每个 Pong 触发多次回调
        Received -= OnReceivedPong;
        Received += OnReceivedPong;

        var p = (Int32)KeepAlive.TotalMilliseconds;
        if (p > 0)
            _timer = new TimerX(DoPing, null, 5_000, p) { Async = true };

        return true;
    }

    /// <summary>关闭连接</summary>
    /// <param name="reason">关闭原因。便于日志分析</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns></returns>
    protected override Task<Boolean> OnCloseAsync(String reason, CancellationToken cancellationToken)
    {
        _timer?.Dispose();
        _timer = null;

        // 断开即退订，避免持有与重复累积
        Received -= OnReceivedPong;

        // 唤醒接收等待者（连接关闭，ReceiveMessageAsync 返回 null）
        SetClosed();

        return base.OnCloseAsync(reason, cancellationToken);
    }

    /// <summary>销毁。释放心跳定时器并唤醒等待者</summary>
    /// <param name="disposing"></param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        // 心跳定时器一并释放：CloseAsync 在未激活（无连接）时直接返回、不走 OnCloseAsync，
        // "打开过程中被销毁"这类路径会留下定时器——TimerX 持有会话对象，且每次到期都在已释放会话上发送 Ping
        _timer?.Dispose();
        _timer = null;

        // 不清空接收队列：会话可能因对端关闭被内部收尾销毁，已到达消息（如 Close 消息）仍须可被拉取方观察。
        // ReceiveMessageAsync 的契约为“先出队、后判断关闭”，此处清队列会让拉取方随机丢失最后一条消息；
        // 未取走的消息持有物化拷贝负载，随对象一并由 GC 回收
        SetClosed();
    }

    /// <summary>等待信号与关闭标记的互斥锁。等待登记与关闭置位同锁互斥，保证关闭瞬间不丢唤醒</summary>
    private readonly Object _signalLock = new();

    /// <summary>是否已关闭。关闭后不再登记新的等待，已在等待的立即唤醒</summary>
    private Boolean _closed;

    /// <summary>标记关闭并唤醒等待者（连接关闭、销毁时调用）</summary>
    /// <remarks>
    /// <para>置位与 <see cref="ReceiveMessageAsync"/> 的等待登记同锁互斥：等待者要么在登记时看到 <see cref="_closed"/> 直接返回，
    /// 要么其信号必然被本次置位看到并唤醒，不会出现“登记晚了一步却没人再唤醒”的永久挂起。</para>
    /// <para>唤醒在锁外执行，避免 net45 的同步续体（无 RunContinuationsAsynchronously）在锁内跑用户代码。</para>
    /// </remarks>
    private void SetClosed()
    {
        TaskCompletionSource<Boolean>? tcs;
        lock (_signalLock)
        {
            _closed = true;
            tcs = _receivedSignal;
        }

        tcs?.TrySetResult(false);
    }

    /// <summary>标记打开，复位关闭标记</summary>
    private void SetOpened()
    {
        lock (_signalLock) _closed = false;
    }

    /// <summary>设置请求头。ws握手时可以传递Token</summary>
    /// <param name="headerName"></param>
    /// <param name="headerValue"></param>
    public void SetRequestHeader(String headerName, String? headerValue)
    {
        RequestHeaders ??= new Dictionary<String, String?>();

        RequestHeaders[headerName] = headerValue;
    }

    #region 消息收发
    /// <summary>接收单条 WebSocket 消息（异步等待）</summary>
    /// <remarks>
    /// <para>接收经消息泵事件驱动：已到达消息进入内部队列，本方法出队返回；无消息时异步等待，连接关闭或取消时返回 null。</para>
    /// <para>返回消息的负载为物化拷贝（与事件流解耦，调用方完全拥有）；支持流水线收取，不丢失粘包中的后续帧。</para>
    /// <para>等待登记与关闭置位同锁互斥：连接恰在登记等待瞬间关闭时，等待也会立即返回 null，不会因丢唤醒永久挂起（无取消令牌同样成立）。</para>
    /// </remarks>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>消息；连接关闭或取消时返回 null</returns>
    public virtual async Task<WsMessage?> ReceiveMessageAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            // 已到达消息直接出队
            if (_received.TryDequeue(out var msg)) return msg;

            // 连接已关闭：不再等待
            if (Disposed || !Active) return null;

            TaskCompletionSource<Boolean> tcs;
            lock (_signalLock)
            {
                // 关闭可能刚发生在上面两道检查之间：此处双检，避免登记出一个不会再被唤醒的等待
                if (_closed) return null;

                // 等待新消息信号或取消；信号触发后重试出队
#if NET45
                // net45 没有 RunContinuationsAsynchronously，接受同步续体
                tcs = _receivedSignal ??= new TaskCompletionSource<Boolean>();
#else
                tcs = _receivedSignal ??= new TaskCompletionSource<Boolean>(TaskCreationOptions.RunContinuationsAsynchronously);
#endif
            }

            var signal = tcs.Task;

            // 赋值间隙到达的消息可能读不到信号（入队方见 null 不唤醒），重查一次兜底
            if (_received.TryDequeue(out msg)) return msg;

            var done = await Task.WhenAny(signal, Task.Delay(-1, cancellationToken)).ConfigureAwait(false);
            if (done != signal) return null;    // 取消

            Interlocked.CompareExchange<TaskCompletionSource<Boolean>>(ref _receivedSignal, null!, tcs);
        }
    }

    /// <summary>分片重组器（RFC 6455 §5.4）。数据帧 FIN=0 累积，末片合并成完整消息后入队</summary>
    private readonly WebSocketFragment _fragment = new();

    /// <summary>消息泵要求整帧完整才产出。客户端接收事件在消息泵任务上同步读体（<see cref="OnReceivedMessage"/>），
    /// 必须整帧交付，否则大帧会在事件内同步等待后续数据，占用线程池线程</summary>
    protected override Boolean RequireFullFrame => true;

    /// <summary>收到消息。重写以先做接收侧协议校验，违规帧不进入事件链与拉取队列，按协议错误失败连接</summary>
    /// <param name="message">消息（头部字段就位、体已绑定）</param>
    /// <remarks>
    /// <para>校验与服务端帧循环（<c>Http/WebSocket.ReadFrames</c>）逐条对齐，缺一不可：</para>
    /// <list type="bullet">
    /// <item>RFC 6455 §5.1：服务端发往客户端的帧不得带掩码，检出即必须失败连接——不校验会把未解码的掩码负载当业务数据交付，
    /// 对端违规时业务侧拿到乱码却没有任何信号</item>
    /// <item>RFC 6455 §5.5：控制帧（Close/Ping/Pong）必须 FIN=1 且负载不超过 125 字节——不校验会让超大 Ping 触发等量 Pong 回显（反射放大），
    /// 分片 Ping 还会被按不完整负载提前应答</item>
    /// </list>
    /// <para>违规帧不进入事件链与拉取队列，由帧泵按交付收尾统一丢弃负载并释放；失败连接后不再处理后续帧。</para>
    /// </remarks>
    protected override void OnMessage(IMessage message)
    {
        if (message is WsMessage ws)
        {
            // RFC 6455 §5.1：客户端检出服务端带掩码的帧必须失败连接
            if (ws.MaskKey != null)
            {
                CloseWithError(1002, "masked frame from server");
                return;
            }

            // RFC 6455 §5.5：控制帧必须 FIN=1 且负载不超过 125 字节
            if (ws.Type is WebSocketMessageType.Close or WebSocketMessageType.Ping or WebSocketMessageType.Pong
                && (!ws.Fin || ws.Payload?.Total > 125))
            {
                CloseWithError(1002, "invalid control frame");
                return;
            }
        }

        base.OnMessage(message);
    }

    /// <summary>待收消息队列（拉取 API）</summary>
    private readonly ConcurrentQueue<WsMessage> _received = new();

    /// <summary>新消息信号</summary>
    private TaskCompletionSource<Boolean>? _receivedSignal;

    /// <summary>接收消息入队并唤醒等待者（协议模式接收事件）</summary>
    private void OnReceivedMessage(Object? sender, ReceivedEventArgs e)
    {
        if (e.Message is not WsMessage ws) return;

        // Close 帧：从负载解析状态码与描述（在负载物化前）
        if (ws.Type == WebSocketMessageType.Close) ws.TryReadCloseStatus();

        // RFC 6455 §5.5.2/§5.5.3：服务端 Ping 必须回应 Pong，且回传同一 Application Data。
        // 消息照常入队，拉取 API 行为不变；会话关闭中不回应，避免 Send 的自动打开把连接重新拉起
        if (ws.Type == WebSocketMessageType.Ping) ReplyPong(ws.Payload);

        // 物化拷贝：与事件流解耦，拉取方完全拥有（含负载）
        var msg = new WsMessage
        {
            Fin = ws.Fin,
            Type = ws.Type,
            MaskKey = ws.MaskKey,
            CloseStatus = ws.CloseStatus,
            StatusDescription = ws.StatusDescription,
        };

        // 交付契约：事件内读满（大帧为流式体，Payload 为空）。读满后回填消息体，
        // 事件链后续订阅者（用户回调）仍可通过 Body 读取；回填体随消息归还
        if (ws.Payload != null)
            msg.SetBody((ArrayPacket)ws.Payload.ToArray());
        else if (ws.Body != null)
        {
            var all = ws.Body.ReadAllAsync().AsTask().GetAwaiter().GetResult();
            ws.SetBody(all);
            msg.SetBody((ArrayPacket)all.ToArray());
        }

        // 分片重组：数据帧 FIN=0 累积，续片追加，末片合并成完整消息后入队；控制帧直通
        if (msg.Type is WebSocketMessageType.Text or WebSocketMessageType.Binary && !msg.Fin)
        {
            _fragment.Begin(msg.Type, msg.Payload);

            // RFC 6455 §7.4.1：分片超限应失败连接并回 1009（Message Too Big），而非静默丢弃让服务端以为已送达
            if (_fragment.TooBig) CloseWithError(1009, "message too big");

            return;
        }
        if (msg.Type == WebSocketMessageType.Data)
        {
            var whole = _fragment.Append(msg.Fin, msg.Payload);

            // 无首片的孤立续片（分片序列未开始或已结束）：RFC 6455 §5.4 属协议错误，静默忽略会让服务端以为已送达
            if (whole == null && !_fragment.Active)
            {
                CloseWithError(_fragment.TooBig ? 1009 : 1002, _fragment.TooBig ? "message too big" : "unexpected continuation frame");

                return;
            }

            // 分片序列进行中：等后续片
            if (whole == null)
            {
                if (_fragment.TooBig) CloseWithError(1009, "message too big");

                return;
            }

            msg = whole;
        }

        // RFC 6455 §8.1：文本消息的负载必须是合法 UTF-8（分片消息在重组后校验），畸变数据按 1007 失败连接
        if (msg.Type == WebSocketMessageType.Text && !WebSocketCodec.IsValidUtf8(msg.Payload))
        {
            msg.TryDispose();

            CloseWithError(1007, "invalid utf-8");

            return;
        }

        _received.Enqueue(msg);

        // 唤醒等待者（无等待者时静默）
        _receivedSignal?.TrySetResult(true);

        // RFC 6455 §5.5.1：收到 Close 须回写关闭帧（本端未发送过时）并关闭连接。
        // 放在入队与唤醒之后：拉取方仍能先观察到这条 Close 消息，随后 ReceiveMessageAsync 返回 null
        if (msg.Type == WebSocketMessageType.Close) ReplyClose(ws);
    }

    /// <summary>关闭帧发送标记。关闭应答（自动回写）与应用主动关闭可能并发，同一会话只允许发出一帧关闭帧</summary>
    private Int32 _closeSent;

    /// <summary>回应服务端 Close 帧（RFC 6455 §5.5.1：未发送过关闭帧时必须回写）并关闭连接</summary>
    /// <param name="message">收到的 Close 消息（状态码与描述已解析）</param>
    /// <remarks>
    /// <para>策略与服务端帧循环（<c>Http/WebSocket.Process</c>）一致：可发送状态码回显同码同描述；保留/未分配码按协议错误回 1002；无状态码回空负载关闭帧。</para>
    /// <para>会话已关闭/销毁时不回写，避免 Send 的自动打开（OpenAsync 重连并重新握手）把已关闭的连接重新拉起；
    /// 本端已发送过关闭帧时不重复发送，由关闭帧发送处的原子去重保证。</para>
    /// <para>本方法运行在同步接收事件链内，关闭动作点火后由续体观察异常（同 <see cref="CloseWithError"/>）。</para>
    /// </remarks>
    private void ReplyClose(WsMessage message)
    {
        if (Disposed || !Active) return;

        var status = message.CloseStatus;
        var desc = message.StatusDescription;

        // RFC 6455 §7.4.1：1005/1006/1015 等保留值与未分配码禁止出现在线路上，收到即按协议错误处理
        if (status > 0 && !WebSocketCodec.IsSendableCloseStatus(status))
        {
            status = 1002;
            desc = "invalid close code";
        }

        // status 为 0 表示对端未带状态码：回空负载关闭帧（禁止把 1005 之类的保留值发到线上）
        _ = CloseCoreAsync(status > 0 ? status : null, status > 0 ? desc ?? "Finished" : null, default).ContinueWith(
            t => { if (t.IsFaulted) NewLife.Log.XTrace.WriteException(t.Exception!.GetBaseException()); },
            CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    /// <summary>回应服务端 Ping。RFC 6455 §5.5.3：Pong 必须回传 Ping 的 Application Data</summary>
    /// <param name="payload">Ping 的负载（与消息同生命周期，本方法同步消费）</param>
    private void ReplyPong(IPacket? payload)
    {
        // 会话正在关闭时不回写：Send 在未打开时会自动重连，心跳回写不得把已关闭的连接拉起
        if (Disposed || !Active) return;

        try
        {
            // 拥有句柄切出共享引用交给容器（发送链路随即掩码拷贝，随后容器释放该引用，原消息负载不受影响）；
            // 视图无所有权、在本同步链路内有效，可直接借用；空负载 Ping 回空负载 Pong
            SendFrame(payload is IOwnerPacket owner ? owner.Slice(0, -1) : payload ?? new ArrayPacket([]), WebSocketMessageType.Pong);
        }
        catch (Exception ex)
        {
            // 回写失败不打断接收链：连接故障由发送链路按既有口径上报并关闭会话
            WriteLog("发送 Pong 失败：" + ex.Message);
        }
    }

    /// <summary>协议错误时关闭连接（同步事件路径无法 await，关闭异常只记日志）</summary>
    /// <param name="closeStatus">关闭状态码，如 1002 协议错误、1007 负载非法、1009 消息过大</param>
    /// <param name="reason">关闭描述</param>
    private void CloseWithError(Int32 closeStatus, String reason)
    {
        // 会话正在关闭/已释放时不回写：发送关闭帧会走 Send 的自动打开（OpenAsync 重连并重新握手），
        // 把已关闭的连接重新拉起。连接已在收尾，对端无需额外的关闭帧
        if (Disposed || !Active) return;

        _ = CloseAsync(closeStatus, reason).ContinueWith(
            t => { if (t.IsFaulted) NewLife.Log.XTrace.WriteException(t.Exception!.GetBaseException()); },
            CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    /// <summary>发送文本帧。拥有句柄所有权转移：发送返回即已消费/释放，不得再使用；视图（ArrayPacket 等）无所有权、不被释放</summary>
    /// <remarks>
    /// <para>与 <see cref="IMessage"/> 的 SetBody 既定语义一致：容器持有传入的负载句柄，发送帧构建（客户端方向会拷入掩码帧）
    /// 与写出完成后随容器一并释放。旧实现不释放容器，按转移语义传入的拥有句柄无人归还——池化负载引用永久截留
    /// （DEBUG 每次发送告警、发布版丢池缓冲）。需要发送后继续持有原句柄时，请先 <c>Slice</c> 切出独立句柄再传入。</para>
    /// </remarks>
    /// <param name="data">负载数据包</param>
    /// <param name="cancellationToken">取消通知（同步发送形态，保留参数兼容）</param>
    /// <returns></returns>
    public Task SendTextAsync(IPacket data, CancellationToken cancellationToken = default)
    {
        SendFrame(data, WebSocketMessageType.Text);

        return TaskEx.CompletedTask;
    }

    /// <summary>发送文本帧</summary>
    /// <param name="data">负载（视图，无所有权）</param>
    /// <param name="cancellationToken">取消通知（同步发送形态，保留参数兼容）</param>
    /// <returns></returns>
    public Task SendTextAsync(Byte[] data, CancellationToken cancellationToken = default)
    {
        SendFrame((ArrayPacket)data, WebSocketMessageType.Text);

        return TaskEx.CompletedTask;
    }

    /// <summary>发送文本</summary>
    /// <param name="text"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task SendTextAsync(String text, CancellationToken cancellationToken = default) => SendTextAsync(text.GetBytes(), cancellationToken);

    /// <summary>发送二进制帧。拥有句柄所有权转移：发送返回即已消费/释放，不得再使用；视图（ArrayPacket 等）无所有权、不被释放</summary>
    /// <param name="data">负载数据包</param>
    /// <param name="cancellationToken">取消通知（同步发送形态，保留参数兼容）</param>
    /// <returns></returns>
    public Task SendBinaryAsync(IPacket data, CancellationToken cancellationToken = default)
    {
        SendFrame(data, WebSocketMessageType.Binary);

        return TaskEx.CompletedTask;
    }

    /// <summary>发送帧。负载所有权转移（随帧容器释放）；客户端方向构建时已把负载拷入掩码帧，释放不会影响已发出的数据</summary>
    /// <param name="data">负载。拥有句柄发送后即释放；视图无所有权，释放为空操作</param>
    /// <param name="type">帧类型</param>
    private void SendFrame(IPacket data, WebSocketMessageType type)
    {
        var ws = new WsMessage
        {
            Type = type,
            MaskKey = MaskKey,
        };
        try
        {
            // 容器接管负载句柄（SetBody 的既定语义），发送完成后随容器释放，调用方不再持有
            ws.SetBody(data);

            SendMessage(ws);
        }
        finally
        {
            ws.TryDispose();
        }
    }

    /// <summary>发送关闭帧并关闭连接</summary>
    /// <param name="closeStatus">关闭状态码</param>
    /// <param name="statusDescription">关闭描述</param>
    /// <param name="cancellationToken">取消通知</param>
    /// <remarks>与基类的 CloseAsync(String, CancellationToken) 语义一致：关闭帧发出后即关闭会话；
    /// 本端此前已发出过关闭帧（含自动回写、重复调用）时不重复发送，只保证会话关闭</remarks>
    public Task CloseAsync(Int32 closeStatus, String? statusDescription = null, CancellationToken cancellationToken = default)
        => CloseCoreAsync(closeStatus, statusDescription, cancellationToken);

    /// <summary>关闭核心。关闭帧原子去重后发送（本端先发为准），随后关闭会话</summary>
    /// <param name="closeStatus">关闭状态码；null 表示发送无状态码的空负载关闭帧</param>
    /// <param name="statusDescription">关闭描述</param>
    /// <param name="cancellationToken">取消通知</param>
    private async Task CloseCoreAsync(Int32? closeStatus, String? statusDescription, CancellationToken cancellationToken)
    {
        // 关闭帧去重：关闭应答（自动回写）与应用主动关闭可能并发，同一会话只允许发出一帧关闭帧。
        // 发送失败不重试：会话已在收尾，对端收不到关闭帧时连接关闭本身即为终态
        if (Interlocked.Exchange(ref _closeSent, 1) == 0)
        {
            var ws = new WsMessage { Type = WebSocketMessageType.Close };
            try
            {
                if (closeStatus is { } cs) ws.SetBody(WebSocketCodec.BuildClosePayload(cs, statusDescription));

                SendMessage(ws);
            }
            catch (Exception ex)
            {
                WriteLog("发送 WebSocket 关闭帧失败：" + ex.Message);
            }
            finally
            {
                ws.TryDispose();
            }
        }

        // 关闭帧发出后关闭会话：旧实现只发帧就返回，会话保持 Active、心跳定时器继续运行，
        // 调用方 await 读起来是“关闭连接”，实际只是发了一帧（与基类同名重载语义冲突）
        await CloseAsync("WebSocketClose", cancellationToken).ConfigureAwait(false);
    }
    #endregion

    #region 心跳
    private TimerX? _timer;
    private DateTime _lastPingTime;

    private void DoPing(Object? state)
    {
        var now = DateTime.UtcNow;
        // Pong 超时检测（仅在事件模式下有效）
        if (PongTimeout > TimeSpan.Zero && _lastPingTime != DateTime.MinValue)
        {
            if (LastPongTime < _lastPingTime && now - _lastPingTime > PongTimeout)
            {
                OnPongTimeout();
            }
        }

        SendFrame((ArrayPacket)$"Ping {now.ToFullString()}", WebSocketMessageType.Ping);

        _lastPingTime = now;

        var p = (Int32)KeepAlive.TotalMilliseconds;
        _timer?.Period = p;
    }

    /// <summary>Pong 超时时触发。默认输出警告日志，可重写实现自动重连等策略</summary>
    protected virtual void OnPongTimeout()
    {
        WriteLog("WebSocket心跳超时，{0:HH:mm:ss} 发送 Ping 后未收到 Pong", _lastPingTime);
    }

    private void OnReceivedPong(Object? sender, ReceivedEventArgs e)
    {
        if (e.Message is WsMessage msg && msg.Type == WebSocketMessageType.Pong)
        {
            LastPongTime = DateTime.UtcNow;
        }
    }
    #endregion

    #region 辅助
    /// <summary>构建握手请求。返回请求与客户端密钥（响应校验用）</summary>
    /// <param name="client">客户端</param>
    /// <param name="uri">地址</param>
    /// <returns>请求与密钥</returns>
    private static (HttpRequest Request, String Key) BuildHandshake(ISocketClient client, Uri uri)
    {
        // 建立WebSocket请求
        var request = new HttpRequest
        {
            Method = "GET",
            RequestUri = uri
        };

        if (client is WebSocketClient ws && ws.RequestHeaders != null)
        {
            foreach (var item in ws.RequestHeaders)
            {
                request.Headers[item.Key] = item.Value!;
            }
        }

        request.Headers["Connection"] = "Upgrade";
        request.Headers["Upgrade"] = "websocket";
        request.Headers["Sec-WebSocket-Version"] = "13";

        var key = Rand.NextBytes(16).ToBase64();
        request.Headers["Sec-WebSocket-Key"] = key;

        // 注入链路跟踪标记
        DefaultSpan.Current?.Attach(request.Headers);

        return (request, key);
    }

    /// <summary>校验握手响应。解析失败返回 false；非 101 或校验头不匹配抛出异常</summary>
    /// <param name="response">响应数据包</param>
    /// <param name="key">客户端密钥</param>
    /// <param name="remainder">响应头之后的剩余字节（服务器可能在同一 TCP 段内紧跟首帧）；所有权转移给调用方，无剩余时为 null</param>
    /// <returns>是否有效</returns>
    /// <remarks>RFC 6455 §4.1：除状态码 101 与 Sec-WebSocket-Accept 外，响应还必须声明 Upgrade: websocket 与 Connection: Upgrade，
    /// 否则对端可能根本不是 WebSocket 服务端，连接会被错误地当成 WebSocket 使用</remarks>
    internal static Boolean ValidateHandshake(IPacket response, String key, out IPacket? remainder)
    {
        remainder = null;

        // 解析响应
        var res = new HttpResponse();
        try
        {
            if (!res.Parse(response)) return false;

            //if (res.StatusCode != HttpStatusCode.OK) throw new Exception($"{(Int32)res.StatusCode} {res.StatusDescription}");
            if (res.StatusCode != HttpStatusCode.SwitchingProtocols) throw new Exception("WebSocket握手失败！" + res.StatusDescription);

            // 检查响应头。Upgrade/Connection 允许携多个令牌（如 keep-alive, Upgrade），按逗号拆分逐项比较
            if (!res.Headers.TryGetValue("Upgrade", out var upgrade) || !HasToken(upgrade, "websocket"))
                throw new Exception("WebSocket握手失败！响应缺少 Upgrade: websocket");

            if (!res.Headers.TryGetValue("Connection", out var connection) || !HasToken(connection, "Upgrade"))
                throw new Exception("WebSocket握手失败！响应缺少 Connection: Upgrade");

            // RFC 6455 §4.1：Accept = base64(SHA1(客户端密钥 + 固定魔法串))，不匹配则拒绝
            if (!res.Headers.TryGetValue("Sec-WebSocket-Accept", out var accept) ||
                accept != SHA1.Create().ComputeHash((key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11").GetBytes()).ToBase64())
                throw new Exception("WebSocket握手失败！");

            // 响应头之后的字节可能是服务器握手后立即推送的首帧：转移所有权给调用方，
            // 否则会随响应对象一起释放（首帧静默丢失，表现为连上了但第一条消息没到）
            remainder = res.Body;
            res.Body = null;

            return true;
        }
        finally
        {
            res.Dispose();
        }
    }

    /// <summary>判断响应头值是否包含指定令牌（逗号分隔，大小写不敏感，忽略首尾空白）</summary>
    /// <param name="value">响应头值，可为空</param>
    /// <param name="token">令牌</param>
    /// <returns>是否包含</returns>
    private static Boolean HasToken(String? value, String token)
    {
        if (value == null) return false;

        foreach (var item in value.Split(','))
        {
            if (item.Trim().Equals(token, StringComparison.OrdinalIgnoreCase)) return true;
        }

        return false;
    }

    /// <summary>打开链路内的异步握手。经直读原语收发，不经过 Open 守卫与接收环；失败返回 false</summary>
    /// <param name="cancellationToken">取消通知</param>
    /// <returns>是否成功</returns>
    private async Task<Boolean> HandshakeAsync(CancellationToken cancellationToken)
    {
        var uri = Uri;
        var (request, key) = BuildHandshake(this, uri);

        using var span = Tracer?.NewSpan($"net:{Name}:WebSocket", uri + "");
        IOwnerPacket? rs = null;
        try
        {
            // 发送请求。用完后释放数据包，还给缓冲池
            {
                using var req = request.Build();
                OnSend(req);
            }

            // 接收响应。打开链路尚未启动接收环，直接原语直读（SSL 会话由 TcpSession 重写适配）
#if NETFRAMEWORK || NETSTANDARD2_0
            // 旧目标无带取消令牌的异步直读重载，临时收紧套接字接收超时后同步直读（沿用旧行为）
            if (Client != null) Client.ReceiveTimeout = Timeout > 0 ? Timeout : 3_000;
            rs = OnDirectReceive();
#else
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(Timeout > 0 ? Timeout : 3_000);
            rs = await OnDirectReceiveAsync(cts.Token).ConfigureAwait(false);
#endif
            if (rs == null || rs.Length == 0) return false;

            if (!ValidateHandshake(rs, key, out var remainder)) return false;

            // 首帧残片投递到入站管道：接收环随后从管道消费，消息泵按帧定界交付。
            // 不能丢弃——服务器常把 101 响应与首个推送帧放在同一个 TCP 段
            if (remainder != null)
            {
                try
                {
                    Pipe.Writer.Append(remainder);
                }
                catch (Exception ex)
                {
                    remainder.TryDispose();
                    span?.SetError(ex, null);
                    WriteLog("WebSocket 握手残片投递失败！" + ex.Message);

                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            WriteLog("WebSocket握手失败！" + ex.Message);

            return false;
        }
        finally
        {
            rs.TryDispose();
        }
    }

    /// <summary>握手（同步阻塞版，供手工调用）</summary>
    /// <param name="client"></param>
    /// <param name="uri"></param>
    /// <returns></returns>
    public static Boolean Handshake(ISocketClient client, Uri uri)
    {
        var (request, key) = BuildHandshake(client, uri);

        using var span = client.Tracer?.NewSpan($"net:{client.Name}:WebSocket", uri + "");
        try
        {
            // 发送请求。用完后释放数据包，还给缓冲池
            {
                using var req = request.Build();
                client.Send(req);
            }

            // 接收响应
            using var rs = client.Receive();
            if (rs == null || rs.Length == 0) return false;

            // 同步版无管道上下文，不做残片投递（剩余字节随响应释放）；
            // 需保留服务器首帧的场景请用实例的异步握手（OnOpenAsync）
            return ValidateHandshake(rs, key, out _);
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            client.WriteLog("WebSocket握手失败！" + ex.Message);

            client.Close("WebSocket");
            client.Dispose();

            return false;
        }
    }
    #endregion
}
