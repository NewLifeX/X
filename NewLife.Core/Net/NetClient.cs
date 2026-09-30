using System.Collections.Concurrent;
using NewLife.Data;
using NewLife.Log;
using NewLife.Messaging;
using NewLife.Model;
using NewLife.Threading;

namespace NewLife.Net;

/// <summary>网络客户端。对 <see cref="ISocketClient"/> 的应用层封装，与 <see cref="NetServer"/> 配对使用</summary>
/// <remarks>
/// <para>NetClient 与 NetServer 是客户端-服务端通信的对称组件，提供一致的 API 体验。</para>
/// <para>功能特性：</para>
/// <list type="bullet">
/// <item>自动识别协议 TCP/UDP/WebSocket，通过 <see cref="Server"/> 或 <see cref="Remote"/> 属性指定地址</item>
/// <item>断线自动重连，内部透明替换 <see cref="ISocketClient"/> 对象，上层无感知</item>
/// <item>支持协议编解码器，通过 <see cref="Protocol"/> 属性配置</item>
/// <item>事件驱动接收，订阅 <see cref="Received"/> 事件</item>
/// <item>同步 / 异步数据收发，兼容低版本 .NET</item>
/// </list>
/// <para>典型用法：</para>
/// <code>
/// var client = new NetClient("tcp://127.0.0.1:8080");
/// client.Protocol = new SrmpCodec();
/// client.Received += (s, e) =&gt; XTrace.WriteLine("收到：{0}", e.Packet?.ToStr());
/// client.Open();
/// client.SendMessage(payload);
/// </code>
/// </remarks>
public class NetClient : DisposeBase, ILogFeature, ITracerFeature
{
    #region 属性
    /// <summary>名称，用于日志输出</summary>
    public String Name { get; set; }

    /// <summary>服务端地址字符串</summary>
    /// <remarks>支持 tcp://host:port / udp://host:port / ws://host:port 等格式，设置后自动解析为 <see cref="Remote"/></remarks>
    public String? Server
    {
        get => Remote?.ToString();
        set => Remote = value.IsNullOrEmpty() ? null : new NetUri(value!);
    }

    /// <summary>远程服务端地址</summary>
    /// <remarks>指定要连接的服务端网络地址，包含协议、主机和端口信息</remarks>
    public NetUri? Remote { get; set; }

    private volatile ISocketClient? _client;

    /// <summary>当前内部 Socket 客户端</summary>
    /// <remarks>断线重连后会替换为新实例，不建议外部持有引用</remarks>
    public ISocketClient? Client => _client;

    /// <summary>是否已连接</summary>
    public Boolean Active => _client?.Active ?? false;

    /// <summary>为连接设置的本地绑定地址</summary>
    /// <remarks>通常不需要指定，由系统自动分配</remarks>
    public NetUri Local { get; set; } = new NetUri();

    /// <summary>本地监听或绑定端口</summary>
    public Int32 Port { get => Local.Port; set => Local.Port = value; }

    /// <summary>超时时间（毫秒）。默认 3000</summary>
    public Int32 Timeout { get; set; } = 3_000;

    /// <summary>自动接收。为 true 时打开后自动启动接收环进入事件模式（拉取不可用）；为 false 时只允许拉取。默认 true</summary>
    /// <remarks>透传给内部Socket客户端，请在打开之前设置</remarks>
    public Boolean AutoReceive { get; set; } = true;

    /// <summary>是否自动重连。默认 true</summary>
    /// <remarks>连接意外断开后自动发起重连，主动调用 <see cref="Close"/> 不触发重连</remarks>
    public Boolean AutoReconnect { get; set; } = true;

    /// <summary>重连间隔（毫秒）。默认 5000</summary>
    public Int32 ReconnectDelay { get; set; } = 5_000;

    /// <summary>最大重连次数。默认 0 表示无限重连</summary>
    public Int32 MaxReconnect { get; set; }

    /// <summary>协议编解码器。非空时启用协议模式（透传给内部 Socket 客户端），请在打开之前设置</summary>
    /// <remarks>协议模式下收发消息经 <see cref="SendMessage(object)"/> 与 <see cref="Received"/> 事件。</remarks>
    public IMessageCodec? Protocol { get; set; }

    /// <summary>请求-响应匹配等待超时（毫秒）。默认30_000，透传给内部 Socket 客户端</summary>
    public Int32 MatchTimeout { get; set; } = 30_000;

    /// <summary>最大并发处理数。透传给内部 Socket 客户端：1=串行（默认），大于1=并行派发（兼作并发上限）</summary>
    /// <remarks>并行下同一连接的多个消息处理顺序不定（SRMP 按序列号配对）；并行要求业务处理器线程安全。</remarks>
    public Int32 MaxConcurrency { get; set; } = 1;

    /// <summary>APM 性能追踪器</summary>
    public ITracer? Tracer { get; set; }

    #endregion

    #region 构造

    /// <summary>实例化网络客户端</summary>
    public NetClient() => Name = GetType().Name;

    /// <summary>通过服务端地址字符串实例化</summary>
    /// <param name="server">服务端地址，如 tcp://127.0.0.1:8080</param>
    public NetClient(String server) : this() => Server = server;

    /// <summary>通过网络地址实例化</summary>
    /// <param name="remote">远程地址</param>
    public NetClient(NetUri remote) : this() => Remote = remote;

    /// <summary>销毁资源</summary>
    /// <param name="disposing">是否释放托管资源</param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        StopReconnect();

        var client = Interlocked.Exchange(ref _client, null);
        if (client != null)
        {
            Detach(client);
            client.TryDispose();
        }
    }

    /// <summary>返回远程地址字符串</summary>
    public override String ToString() => Remote?.ToString() ?? Name;

    #endregion

    #region 连接管理

    private volatile Boolean _userClosed;

    /// <summary>打开连接</summary>
    /// <returns>是否成功连接</returns>
    public Boolean Open()
    {
        if (Disposed) return false;
        if (_client != null && _client.Active) return true;

        _userClosed = false;
        ISocketClient? client = null;
        try
        {
            client = CreateClient();
            if (!client.Open())
            {
                client.TryDispose();
                return false;
            }

            SwitchClient(client);
            return true;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            // 连接失败时释放新建客户端，避免资源泄漏
            client?.TryDispose();
            return false;
        }
    }

    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功连接</returns>
    public async Task<Boolean> OpenAsync(CancellationToken cancellationToken = default)
    {
        if (Disposed) return false;
        if (_client != null && _client.Active) return true;

        _userClosed = false;
        ISocketClient? client = null;
        try
        {
            client = CreateClient();
            if (!await client.OpenAsync(cancellationToken).ConfigureAwait(false))
            {
                client.TryDispose();
                return false;
            }

            SwitchClient(client);
            return true;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            // 连接失败时释放新建客户端，避免资源泄漏
            client?.TryDispose();
            return false;
        }
    }

    /// <summary>关闭连接</summary>
    /// <param name="reason">关闭原因</param>
    /// <returns>是否成功</returns>
    public Boolean Close(String reason)
    {
        // 置位用户关闭标记与摘除内部客户端必须在同一把重连锁内完成，
        // 否则重连流程可能恰好穿过这两步之间的空隙，把迟到的活连接重新写回 _client
        ISocketClient? client;
        lock (_reconnectLock)
        {
            _userClosed = true;
            client = Interlocked.Exchange(ref _client, null);
        }

        StopReconnect();
        if (client == null) return true;

        Detach(client);
        var result = client.Close(reason);
        client.TryDispose();

        // Detach 之后内部客户端的 Closed 不再转发，此处手动触发
        Closed?.Invoke(this, EventArgs.Empty);
        return result;
    }

    /// <summary>异步关闭连接</summary>
    /// <param name="reason">关闭原因</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    public async Task<Boolean> CloseAsync(String reason, CancellationToken cancellationToken = default)
    {
        // 同 Close：置位与摘除必须原子，避免重连接管迟到的活连接
        ISocketClient? client;
        lock (_reconnectLock)
        {
            _userClosed = true;
            client = Interlocked.Exchange(ref _client, null);
        }

        StopReconnect();
        if (client == null) return true;

        Detach(client);
        var result = await client.CloseAsync(reason, cancellationToken).ConfigureAwait(false);
        client.TryDispose();

        // Detach 之后内部客户端的 Closed 不再转发，此处手动触发
        Closed?.Invoke(this, EventArgs.Empty);
        return result;
    }

    /// <summary>创建并初始化内部 Socket 客户端</summary>
    /// <remarks>子类可重载此方法以定制客户端行为，如配置 SSL、KeepAlive 等</remarks>
    /// <returns>初始化完成的 Socket 客户端</returns>
    protected virtual ISocketClient CreateClient()
    {
        var remote = Remote ?? throw new InvalidOperationException("未设置远程地址，请先设置 Server 或 Remote 属性");

        var client = remote.CreateRemote();
        client.Name = Name;
        client.Timeout = Timeout;
        client.Log = Log;

        if (client is SessionBase session)
        {
            session.AutoReceive = AutoReceive;
            if (Protocol != null) session.Protocol = Protocol;
            if (MatchTimeout > 0) session.MatchTimeout = MatchTimeout;
            if (MaxConcurrency > 1) session.MaxConcurrency = MaxConcurrency;
        }

        if (Tracer != null) client.Tracer = Tracer;
        if (Local.Port > 0) client.Local = Local;

        Attach(client);
        return client;
    }

    private void Attach(ISocketClient client)
    {
        client.Received += OnClientReceived;
        client.Opened += OnClientOpened;
        client.Closed += OnClientClosed;
        client.Error += OnClientError;
    }

    private void Detach(ISocketClient client)
    {
        client.Received -= OnClientReceived;
        client.Opened -= OnClientOpened;
        client.Closed -= OnClientClosed;
        client.Error -= OnClientError;
    }

    /// <summary>替换内部Socket客户端。摘除旧实例的事件订阅并释放，避免旧连接继续转发事件或泄漏资源</summary>
    /// <param name="client">新的客户端实例</param>
    private void SwitchClient(ISocketClient client) => RetireClient(SwapClient(client), client);

    /// <summary>原子替换内部Socket客户端，返回被替换下来的旧实例</summary>
    /// <param name="client">新的客户端实例</param>
    private ISocketClient? SwapClient(ISocketClient client) => Interlocked.Exchange(ref _client, client);

    /// <summary>摘除旧客户端的事件订阅并释放</summary>
    /// <remarks>应在锁外调用，避免持锁期间触发旧连接的关闭回调</remarks>
    /// <param name="old">被替换下来的旧实例</param>
    /// <param name="current">新实例，与旧实例相同则不做处理</param>
    private void RetireClient(ISocketClient? old, ISocketClient? current)
    {
        if (old == null || ReferenceEquals(old, current)) return;

        Detach(old);
        old.TryDispose();
    }

    #endregion

    #region 断线重连

    private readonly Object _reconnectLock = new();
    private TimerX? _reconnectTimer;
    private volatile Int32 _reconnectCount;

    private void StopReconnect()
    {
        TimerX? t;
        lock (_reconnectLock)
        {
            t = _reconnectTimer;
            _reconnectTimer = null;
        }
        t?.TryDispose();
    }

    private void ScheduleReconnect()
    {
        if (!AutoReconnect || Disposed || _userClosed) return;

        // 并发 Closed/Error 事件可能同时到达，用锁保证“检查+创建”原子，避免排出多个一次性定时器
        lock (_reconnectLock)
        {
            // 已有定时器挂起中，不重复创建
            if (_reconnectTimer != null) return;

            // 超过最大重连次数后停止。
            // 注意：此处不清零计数，确保后续 Error/Closed 事件再次触发 ScheduleReconnect 时仍被拦住
            if (MaxReconnect > 0 && _reconnectCount >= MaxReconnect)
            {
                WriteLog("已达最大重连次数 {0}，停止重连", MaxReconnect);
                return;
            }

            var delay = ReconnectDelay > 0 ? ReconnectDelay : 5_000;
            WriteLog("连接断开，{0}ms 后发起第 {1} 次重连 {2}", delay, _reconnectCount + 1, Remote);

            // Period = 0 表示一次性定时器，触发后不再重复
            _reconnectTimer = new TimerX(DoReconnect, null, delay, 0) { Async = true };
        }
    }

    private async void DoReconnect(Object? state)
    {
        // 清除定时器引用，使 ScheduleReconnect 可在失败时创建新的一次性定时器
        StopReconnect();
        if (Disposed || _userClosed || (_client != null && _client.Active)) return;

        _reconnectCount++;
        WriteLog("正在重连 [{0}] {1}", _reconnectCount, Remote);

        try
        {
            var client = CreateClient();
            if (await client.OpenAsync().ConfigureAwait(false))
            {
                // 等待连接期间可能已被主动关闭/销毁：此时不得收纳这个迟到连接，
                // 否则 _client 指向一个无人关闭的活连接（Active 为真、计数已清零），Socket 与对端资源双双泄露。
                // 复查与接管必须在同一把重连锁内完成：Close/CloseAsync 也在该锁内置位 _userClosed 并摘除 _client，
                // 二者互斥后才能排除“复查通过后、接管之前被 Close 插入”的窗口
                var adopted = false;
                ISocketClient? old = null;
                lock (_reconnectLock)
                {
                    if (!Disposed && !_userClosed)
                    {
                        old = SwapClient(client);
                        // 重连成功：清零计数，下次断线后可重新累计
                        _reconnectCount = 0;
                        adopted = true;
                    }
                }

                if (!adopted)
                {
                    client.TryDispose();
                    return;
                }

                // 锁外释放被替换的旧客户端，避免在持锁期间触发其关闭回调
                RetireClient(old, client);
                WriteLog("重连成功 {0}", Remote);
            }
            else
            {
                client.TryDispose();
                // 重连失败：计数保留，由 ScheduleReconnect 决定是否继续
                ScheduleReconnect();
            }
        }
        catch (Exception ex)
        {
            WriteLog("重连失败：{0}", ex.Message);
            // 重连失败：计数保留，由 ScheduleReconnect 决定是否继续
            ScheduleReconnect();
        }
    }

    #endregion

    #region 发送数据

    /// <summary>发送数据包</summary>
    /// <param name="data">数据包</param>
    /// <returns>实际发送字节数，失败返回负数</returns>
    public Int32 Send(IPacket data) => EnsureClient().Send(data);

    /// <summary>发送字节数组</summary>
    /// <param name="data">字节数组</param>
    /// <param name="offset">起始偏移</param>
    /// <param name="count">发送字节数，-1 表示全部</param>
    /// <returns>实际发送字节数，失败返回负数</returns>
    public Int32 Send(Byte[] data, Int32 offset = 0, Int32 count = -1) => EnsureClient().Send(data, offset, count);

    /// <summary>发送数组段</summary>
    /// <param name="data">数组段</param>
    /// <returns>实际发送字节数，失败返回负数</returns>
    public Int32 Send(ArraySegment<Byte> data) => EnsureClient().Send(data);

    /// <summary>发送只读内存段（高性能零拷贝）</summary>
    /// <param name="data">只读内存段</param>
    /// <returns>实际发送字节数，失败返回负数</returns>
    public Int32 Send(ReadOnlySpan<Byte> data) => EnsureClient().Send(data);

    /// <summary>发送字符串</summary>
    /// <param name="data">字符串</param>
    /// <returns>实际发送字节数，失败返回负数</returns>
    public Int32 Send(String data) => EnsureClient().Send(data.GetBytes());

    /// <summary>发送消息，经过管道编码后发送，不等待响应</summary>
    /// <param name="message">消息对象，可为 Byte[]、IPacket、String 或其他业务对象。</param>
    /// <returns>实际发送字节数，失败返回负数</returns>
    public Int32 SendMessage(Object message) => EnsureClient().SendMessage(message);

    /// <summary>异步发送消息并等待响应（需要管道支持请求-响应匹配）</summary>
    /// <param name="message">消息对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>响应消息对象</returns>
    public ValueTask<Object> SendMessageAsync(Object message, CancellationToken cancellationToken = default)
        => EnsureClient().SendMessageAsync(message, cancellationToken);

    /// <summary>发送消息并等待匹配的响应（协议模式）</summary>
    /// <param name="message">请求消息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>响应消息；调用方负责消费负载并释放</returns>
    /// <exception cref="NotSupportedException">内部客户端不支持协议模式请求响应</exception>
    public ValueTask<IMessage> SendMessageAsync(IMessage message, CancellationToken cancellationToken = default)
    {
        if (EnsureClient() is SessionBase session) return session.SendMessageAsync(message, cancellationToken);

        throw new NotSupportedException($"内部客户端不支持协议模式请求响应 [{_client?.GetType().Name}]");
    }

    /// <summary>发送流式消息（协议模式）：先发协议头部（声明体长），再把数据流内容经发送管道分块送出</summary>
    /// <param name="message">消息（头部字段就位）</param>
    /// <param name="body">消息体数据流</param>
    /// <param name="bodyLength">消息体字节数；负数时从可定位流推导</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>已写入发送管道的内容字节数</returns>
    /// <exception cref="NotSupportedException">内部客户端不支持协议模式流式发送</exception>
    public ValueTask<Int64> SendMessageAsync(Message message, Stream body, Int64 bodyLength = -1, CancellationToken cancellationToken = default)
    {
        if (EnsureClient() is SessionBase session) return session.SendMessageAsync(message, body, bodyLength, cancellationToken);

        throw new NotSupportedException($"内部客户端不支持协议模式流式发送 [{_client?.GetType().Name}]");
    }

    private ISocketClient EnsureClient()
    {
        var client = _client;
        if (client != null && client.Active) return client;

        // AutoReconnect=false 时不自动尝试，直接抛出上层错误
        if (!AutoReconnect)
            throw new InvalidOperationException($"网络客户端 [{Name}] 未连接，请先调用 Open()");

        // 尝试建立连接
        Open();
        return _client ?? throw new InvalidOperationException($"网络客户端 [{Name}] 未连接，且连接尝试失败");
    }

    #endregion

    #region 接收数据

    /// <summary>同步接收数据包（阻塞）</summary>
    /// <returns>数据包，无数据时返回 null</returns>
    public IOwnerPacket? Receive() => EnsureClient().Receive();

    /// <summary>异步接收数据包</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>数据包，无数据时返回 null</returns>
    public Task<IOwnerPacket?> ReceiveAsync(CancellationToken cancellationToken = default)
        => EnsureClient().ReceiveAsync(cancellationToken);

    #endregion

    #region 事件转发

    /// <summary>连接打开后触发</summary>
    public event EventHandler? Opened;

    /// <summary>连接关闭后触发</summary>
    public event EventHandler? Closed;

    /// <summary>数据接收事件，适用于事件驱动模式</summary>
    /// <remarks>当有新数据到达或管道解码出消息时触发</remarks>
    public event EventHandler<ReceivedEventArgs>? Received;

    /// <summary>错误或连接断开事件</summary>
    public event EventHandler<ExceptionEventArgs>? Error;

    private void OnClientOpened(Object? sender, EventArgs e)
    {
        StopReconnect();
        Opened?.Invoke(this, e);
    }

    private void OnClientClosed(Object? sender, EventArgs e)
    {
        Closed?.Invoke(this, e);

        // 非主动关闭时触发重连
        if (!_userClosed) ScheduleReconnect();
    }

    private void OnClientReceived(Object? sender, ReceivedEventArgs e) => Received?.Invoke(this, e);

    private void OnClientError(Object? sender, ExceptionEventArgs e)
    {
        Error?.Invoke(this, e);

        // 连接断开时触发重连（非主动关闭）
        if (!_userClosed
            && (e.Action == "Disconnect" || e.Action == "Close" || e.Action == "Receive"))
            ScheduleReconnect();
    }

    #endregion

    #region 扩展数据（IExtend）

    private ConcurrentDictionary<String, Object?>? _items;

    /// <summary>扩展数据字典</summary>
    /// <remarks>并发首用时以 CAS 保证只保留一份实例，避免各自新建导致败者刚写入的数据被丢弃</remarks>
    public IDictionary<String, Object?> Items
    {
        get
        {
            var items = _items;
            if (items != null) return items;

            var created = new ConcurrentDictionary<String, Object?>();

            return Interlocked.CompareExchange(ref _items, created, null) ?? created;
        }
    }

    /// <summary>获取或设置扩展数据项</summary>
    /// <param name="key">键名</param>
    /// <returns>对应值，不存在时返回 null</returns>
    public Object? this[String key]
    {
        get => _items != null && _items.TryGetValue(key, out var obj) ? obj : null;
        set => Items[key] = value;
    }

    #endregion

    #region 日志

    /// <summary>日志对象</summary>
    public ILog Log { get; set; } = Logger.Null;

    private String? _logPrefix;

    /// <summary>日志前缀</summary>
    public virtual String LogPrefix
    {
        get
        {
            _logPrefix ??= $"{Name} ";
            return _logPrefix;
        }
        set => _logPrefix = value;
    }

    /// <summary>输出日志</summary>
    /// <param name="format">格式化字符串</param>
    /// <param name="args">参数</param>
    public virtual void WriteLog(String format, params Object?[] args) => Log.Info(LogPrefix + format, args);

    #endregion
}
