using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
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
            Client.TryDispose();

            return false;
        }

        // 订阅 Received 事件以跟踪 Pong 响应（仅事件模式有效）
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

        // 唤醒接收等待者（连接关闭，ReceiveMessageAsync 返回 null）
        _receivedSignal?.TrySetResult(false);

        return base.OnCloseAsync(reason, cancellationToken);
    }

    /// <summary>销毁。清空未取走的接收消息，避免负载滞留</summary>
    /// <param name="disposing"></param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        while (_received.TryDequeue(out var msg)) msg.TryDispose();
        _receivedSignal?.TrySetResult(false);
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

            // 等待新消息信号或取消；信号触发后重试出队
#if NET45
            // net45 没有 RunContinuationsAsynchronously，接受同步续体
            _receivedSignal ??= new TaskCompletionSource<Boolean>();
#else
            _receivedSignal ??= new TaskCompletionSource<Boolean>(TaskCreationOptions.RunContinuationsAsynchronously);
#endif
            var tcs = _receivedSignal;
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
            return;
        }
        if (msg.Type == WebSocketMessageType.Data)
        {
            if (_fragment.Append(msg.Fin, msg.Payload) is not { } whole) return;
            msg = whole;
        }

        _received.Enqueue(msg);

        // 唤醒等待者（无等待者时静默）
        _receivedSignal?.TrySetResult(true);
    }

    /// <summary>发送文本</summary>
    /// <param name="data"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task SendTextAsync(IPacket data, CancellationToken cancellationToken = default)
    {
        var ws = new WsMessage
        {
            Type = WebSocketMessageType.Text,
            MaskKey = MaskKey,
        };
        ws.SetBody(data);

        SendMessage(ws);

        return TaskEx.CompletedTask;
    }

    /// <summary>发送文本</summary>
    /// <param name="data"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task SendTextAsync(Byte[] data, CancellationToken cancellationToken = default)
    {
        var ws = new WsMessage
        {
            Type = WebSocketMessageType.Text,
            MaskKey = MaskKey,
        };
        ws.SetBody((ArrayPacket)data);

        SendMessage(ws);

        return TaskEx.CompletedTask;
    }

    /// <summary>发送文本</summary>
    /// <param name="text"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task SendTextAsync(String text, CancellationToken cancellationToken = default) => SendTextAsync(text.GetBytes(), cancellationToken);

    /// <summary>发送二进制数据</summary>
    /// <param name="data"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task SendBinaryAsync(IPacket data, CancellationToken cancellationToken = default)
    {
        var ws = new WsMessage
        {
            Type = WebSocketMessageType.Binary,
            MaskKey = MaskKey,
        };
        ws.SetBody(data);

        SendMessage(ws);

        return TaskEx.CompletedTask;
    }

    /// <summary>发送关闭</summary>
    /// <param name="closeStatus"></param>
    /// <param name="statusDescription"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task CloseAsync(Int32 closeStatus, String? statusDescription = null, CancellationToken cancellationToken = default)
    {
        var ws = new WsMessage { Type = WebSocketMessageType.Close };
        ws.SetBody(WebSocketCodec.BuildClosePayload(closeStatus, statusDescription));

        SendMessage(ws);

        return TaskEx.CompletedTask;
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

        var ws = new WsMessage
        {
            Type = WebSocketMessageType.Ping,
            MaskKey = MaskKey,
        };
        ws.SetBody((ArrayPacket)$"Ping {now.ToFullString()}");

        SendMessage(ws);

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
    /// <returns>是否有效</returns>
    private static Boolean ValidateHandshake(IPacket response, String key)
    {
        // 解析响应
        using var res = new HttpResponse();
        if (!res.Parse(response)) return false;

        //if (res.StatusCode != HttpStatusCode.OK) throw new Exception($"{(Int32)res.StatusCode} {res.StatusDescription}");
        if (res.StatusCode != HttpStatusCode.SwitchingProtocols) throw new Exception("WebSocket握手失败！" + res.StatusDescription);

        // 检查响应头
        if (!res.Headers.TryGetValue("Sec-WebSocket-Accept", out var accept) ||
            accept != SHA1.Create().ComputeHash((key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11").GetBytes()).ToBase64())
            throw new Exception("WebSocket握手失败！");

        return true;
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

            return ValidateHandshake(rs, key);
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

            return ValidateHandshake(rs, key);
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
