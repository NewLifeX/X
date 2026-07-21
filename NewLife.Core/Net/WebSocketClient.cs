using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using NewLife.Data;
using NewLife.Http;
using NewLife.Log;
using NewLife.Net.Handlers;
using NewLife.Security;
using NewLife.Threading;

namespace NewLife.Net;

/// <summary>WebSocket客户端</summary>
public class WebSocketClient : TcpSession
{
    #region 属性
    /// <summary>资源地址</summary>
    public Uri Uri { get; set; } = null!;

    /// <summary>WebSocket心跳间隔。默认60秒</summary>
    public TimeSpan KeepAlive { get; set; } = TimeSpan.FromSeconds(120);

    /// <summary>请求头。ws握手时可以传递Token</summary>
    public IDictionary<String, String?>? RequestHeaders { get; set; }

    /// <summary>客户端掩码密钥。RFC 6455 要求客户端发送的所有帧必须带掩码，服务端发送的帧不能带掩码</summary>
    /// <remarks>在握手成功后自动生成 4 字节随机掩码。所有出站消息自动设置此掩码，无需手动干预。</remarks>
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
        // 加入WebSocket编码器，实现报文编解码
        this.Add<WebSocketCodec>();
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

        //// 连接必须是ws/wss协议
        //if (remote.Type != NetType.WebSocket) return false;

        //// 设置为激活
        //Active = true;

        //var rs = Handshake(this, Uri);

        //Active = false;

        // 生成随机掩码密钥（RFC 6455 §5.1：客户端帧必须掩码）
        MaskKey = Rand.NextBytes(4);

        // 订阅 Received 事件以跟踪 Pong 响应（仅 MaxAsync=1 后台接收模式有效）
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

        return base.OnCloseAsync(reason, cancellationToken);
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
    /// <summary>接收单条 WebSocket 消息。</summary>
    /// <remarks>
    /// 底层调用 <see cref="SessionBase.ReceiveAsync(CancellationToken)"/> 读取一次原始 TCP 数据，
    /// 再用 <see cref="WebSocketMessage.Read"/> 解析其中第一个 WS 帧，剩余字节随即丢弃。
    /// 因此本方法有以下约束：
    /// <list type="bullet">
    /// <item>必须将 <see cref="SessionBase.MaxAsync"/> 设为 0 以禁用后台接收循环，
    ///   否则与后台循环竞争同一 TCP 流，导致帧乱序或丢失。</item>
    /// <item>仅适用于<b>严格顺序请求-响应</b>场景（发一条→等回复→再发下一条）。
    ///   若客户端流水线发送多条消息，多个 WS 帧可能合并到同一 TCP 段，
    ///   本方法只处理首帧，其余帧永久丢失，接收循环将永久阻塞。</item>
    /// <item>流水线/批量发送场景请改用 <see cref="SessionBase.MaxAsync"/> = 1（默认值）
    ///   并通过 <see cref="SessionBase.Received"/> 事件接收；
    ///   管道内的 WebSocketCodec 配合 PacketCodec 会正确完成粘包/拆包。</item>
    /// </list>
    /// </remarks>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>解析到的消息；连接已关闭或数据不完整时返回 null</returns>
    public virtual async Task<WebSocketMessage?> ReceiveMessageAsync(CancellationToken cancellationToken = default)
    {
        using var rs = await base.ReceiveAsync(cancellationToken).ConfigureAwait(false);
        if (rs == null) return null;

        var msg = new WebSocketMessage();
        if (!msg.Read(rs)) return null;

        return msg;
    }

    /// <summary>发送消息</summary>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task SendMessageAsync(WebSocketMessage message, CancellationToken cancellationToken = default)
    {
        //var pk = message.ToPacket();
        //Send(pk);

        // RFC 6455 §5.1：客户端帧必须带掩码
        message.MaskKey ??= MaskKey;

        SendMessage(message);

        return TaskEx.CompletedTask;
    }

    /// <summary>发送文本</summary>
    /// <param name="data"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task SendTextAsync(IPacket data, CancellationToken cancellationToken = default)
    {
        var msg = new WebSocketMessage
        {
            Type = WebSocketMessageType.Text,
            Payload = data,
        };

        return SendMessageAsync(msg, cancellationToken);
    }

    /// <summary>发送文本</summary>
    /// <param name="data"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task SendTextAsync(Byte[] data, CancellationToken cancellationToken = default)
    {
        var msg = new WebSocketMessage
        {
            Type = WebSocketMessageType.Text,
            Payload = (ArrayPacket)data,
        };

        return SendMessageAsync(msg, cancellationToken);
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
        var msg = new WebSocketMessage
        {
            Type = WebSocketMessageType.Binary,
            Payload = data,
        };

        return SendMessageAsync(msg, cancellationToken);
    }

    /// <summary>发送关闭</summary>
    /// <param name="closeStatus"></param>
    /// <param name="statusDescription"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task CloseAsync(Int32 closeStatus, String? statusDescription = null, CancellationToken cancellationToken = default)
    {
        var msg = new WebSocketMessage
        {
            Type = WebSocketMessageType.Close,
            CloseStatus = closeStatus,
            StatusDescription = statusDescription,
        };

        return SendMessageAsync(msg, cancellationToken);
    }
    #endregion

    #region 心跳
    private TimerX? _timer;
    private DateTime _lastPingTime;

    private void DoPing(Object? state)
    {
        var now = DateTime.UtcNow;
        // Pong 超时检测（仅在 MaxAsync=1 后台接收模式下有效）
        if (PongTimeout > TimeSpan.Zero && _lastPingTime != DateTime.MinValue)
        {
            if (LastPongTime < _lastPingTime && now - _lastPingTime > PongTimeout)
            {
                OnPongTimeout();
            }
        }

        var msg = new WebSocketMessage
        {
            Type = WebSocketMessageType.Ping,
            Payload = (ArrayPacket)$"Ping {now.ToFullString()}",
        };

        // RFC 6455 §5.1：客户端帧必须带掩码
        msg.MaskKey ??= MaskKey;

        SendMessage(msg);

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
        if (e.Message is WebSocketMessage msg && msg.Type == WebSocketMessageType.Pong)
        {
            LastPongTime = DateTime.UtcNow;
        }
    }
    #endregion

    #region 辅助
    /// <summary>握手</summary>
    /// <param name="client"></param>
    /// <param name="uri"></param>
    /// <returns></returns>
    public static Boolean Handshake(ISocketClient client, Uri uri)
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

            // 解析响应
            using var res = new HttpResponse();
            if (!res.Parse(rs)) return false;

            //if (res.StatusCode != HttpStatusCode.OK) throw new Exception($"{(Int32)res.StatusCode} {res.StatusDescription}");
            if (res.StatusCode != HttpStatusCode.SwitchingProtocols) throw new Exception("WebSocket握手失败！" + res.StatusDescription);

            // 检查响应头
            if (!res.Headers.TryGetValue("Sec-WebSocket-Accept", out var accept) ||
                accept != SHA1.Create().ComputeHash((key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11").GetBytes()).ToBase64())
                throw new Exception("WebSocket握手失败！");
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            client.WriteLog("WebSocket握手失败！" + ex.Message);

            client.Close("WebSocket");
            client.Dispose();

            return false;
        }

        return true;
    }
    #endregion
}
