using System.Net;
using System.Security.Cryptography;
using NewLife.Data;
using NewLife.Messaging;
using NewLife.Net;

namespace NewLife.Http;

/// <summary>WebSocket消息处理（新协议栈消息）</summary>
/// <param name="socket"></param>
/// <param name="message">消息（回调内负载完整可用，返回后收尾）</param>
public delegate void WsMessageDelegate(WebSocket socket, WsMessage message);

/// <summary>WebSocket会话管理</summary>
/// <remarks>HTTP 服务端升级后的 WS 会话（由 HttpSession 触发），自持消息泵（MessagePump + WebSocketCodec）解析帧。</remarks>
public class WebSocket : IDisposable
{
    #region 属性
    /// <summary>是否还在连接</summary>
    public Boolean Connected { get; set; }

    /// <summary>消息处理器</summary>
    /// <remarks>回调内消息负载完整可用（已解码），返回后消息收尾</remarks>
    public WsMessageDelegate? MessageHandler { get; set; }

    /// <summary>Http上下文</summary>
    public IHttpContext? Context { get; set; }

    /// <summary>版本</summary>
    public String? Version { get; set; }

    /// <summary>协议。如mqtt</summary>
    public String? Protocol { get; set; }

    /// <summary>活跃时间</summary>
    public DateTime ActiveTime { get; set; }

    private Pipe? _pipe;

    /// <summary>消息编解码器（服务端角色：接收带掩码帧、发送无掩码）。无状态，可跨会话共享</summary>
    private static readonly WebSocketCodec _codec = new() { IsServer = true };

    /// <summary>帧泵。整帧模式：同步泵只能消费整帧，帧未完整留待下一轮</summary>
    private static readonly MessagePump _pump = new(_codec) { RequireFullFrame = true };

    /// <summary>分片重组器（RFC 6455 §5.4）。数据帧 FIN=0 累积，末片合并成完整消息后交付</summary>
    private readonly WebSocketFragment _fragment = new();
    #endregion

    #region 方法
    /// <summary>WebSocket 握手</summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static WebSocket? Handshake(IHttpContext context)
    {
        var request = context.Request;
        if (!request.Headers.TryGetValue("Sec-WebSocket-Key", out var key) || key.IsNullOrEmpty()) return null;

        var manager = new WebSocket();
        manager.ProcessRequest(context);

        return manager;
    }

    /// <summary>处理 WebSocket 握手</summary>
    /// <param name="context"></param>
    public Boolean ProcessRequest(IHttpContext context)
    {
        var request = context.Request;
        if (!request.Headers.TryGetValue("Sec-WebSocket-Key", out var key) || key.IsNullOrEmpty()) return false;

        var buf = SHA1.Create().ComputeHash((key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11").GetBytes());
        key = buf.ToBase64();

        var response = context.Response;
        response.StatusCode = HttpStatusCode.SwitchingProtocols;
        response.Headers["Upgrade"] = "websocket";
        response.Headers["Connection"] = "Upgrade";
        response.Headers["Sec-WebSocket-Accept"] = key;

        if (context is DefaultHttpContext dhc) dhc.WebSocket = this;

        if (!Protocol.IsNullOrEmpty())
            response.Headers["Sec-WebSocket-Protocol"] = Protocol;
        if (!Version.IsNullOrEmpty())
            response.Headers["Sec-WebSocket-Version"] = Version;
        //if (request.Headers.TryGetValue("Sec-WebSocket-Version", out var ver)) Version = ver;

        Context = context;
        Connected = true;
        ActiveTime = DateTime.Now;

        return true;
    }

    /// <summary>处理WebSocket数据包。数据进入数据管道，逐帧同步泵出完整帧交给消息处理，支持跨接收边界的粘包/分包</summary>
    /// <param name="pk">已到达的原始数据包，可能包含零个或多个完整 WebSocket 帧</param>
    /// <remarks>帧未完整时残片保留在管道内等下一轮；入参为借阅视图时自动转为自有拷贝（跨轮安全）</remarks>
    public void Process(IPacket pk)
    {
        // 数据进入管道：共享切片保持调用方句柄不受影响；借阅视图不能跨轮保留，转为自有拷贝
        var node = pk.Slice(0, -1);
        if (node is not OwnerPacket op || op.RefCount == 0) node = node.Clone();

        _pipe ??= new Pipe();
        _pipe.Writer.Append(node);

        // 同步泵：当前缓冲内可成的整帧全部处理；头部不足或帧未完整则留给下一轮
        while (_pump.TryRead(_pipe.Reader, out var message))
        {
            try
            {
                if (message is not WsMessage ws) continue;

                // 客户端帧带掩码：整帧路径对负载原地解码（帧内字节独享）
                ws.Demask();

                // 分片重组：数据帧 FIN=0 累积，续片追加，末片合并成完整消息后交付；控制帧直通
                if (ws.Type is WebSocketMessageType.Text or WebSocketMessageType.Binary && !ws.Fin)
                {
                    _fragment.Begin(ws.Type, ws.Payload);
                    continue;
                }
                if (ws.Type == WebSocketMessageType.Data)
                {
                    if (_fragment.Append(ws.Fin, ws.Payload) is { } whole) Process(whole);
                    continue;
                }

                // 消息化入口：业务回调直达（负载已解码），协议帧处理内联
                Process(ws);
            }
            finally
            {
                message.TryDispose();
            }
        }
    }

    /// <summary>处理WebSocket消息</summary>
    /// <param name="message">消息（负载已解码；回调内完整可用，返回后收尾）</param>
    public void Process(WsMessage message)
    {
        ActiveTime = DateTime.Now;

        // Close 帧：从负载解析状态码与描述（在业务回调前，供回调与回显使用）
        if (message.Type == WebSocketMessageType.Close) message.TryReadCloseStatus();

        // 业务回调：负载完整可用
        MessageHandler?.Invoke(this, message);

        // 协议帧处理：Close 回显关闭 / Ping 回显 Pong
        var pingPayload = message.Type == WebSocketMessageType.Ping ? message.Payload : null;

        var session = Context?.Connection;
        var socket = Context?.Socket;
        if (session == null && socket == null) return;

        switch (message.Type)
        {
            case WebSocketMessageType.Close:
                {
                    // RFC 6455 §5.5.1：关闭帧应回显收到的状态码，若无状态码则用 1005（无状态码）
                    var status = message.CloseStatus > 0 ? message.CloseStatus : 1005;
                    Close(status, message.StatusDescription ?? "Finished");
                    session?.Dispose();
                    socket?.Dispose();
                    Connected = false;
                }
                break;
            case WebSocketMessageType.Ping:
                {
                    // RFC 6455 §5.5.3：Pong 必须回传 Ping 的 Application Data。
                    // 负载所有权从 Ping 消息转移到 Pong 帧（消息先清体，避免收尾时重复归还）
                    var pong = new WsMessage { Type = WebSocketMessageType.Pong };
                    pong.SetBody(pingPayload);
                    message.SetBody((IPacket?)null);
                    Send(pong);
                }
                break;
        }

        // 负载不在此释放：所有权随消息容器（帧泵回调 using / 调用方负责）；Pong 为同步发送，容器释放前负载始终有效
    }

    private void Send(WsMessage msg)
    {
        var session = Context?.Connection;
        var socket = Context?.Socket;
        if (session == null && socket == null) throw new ObjectDisposedException(nameof(Context));

        var data = _codec.Build(msg)!;
        if (session != null)
            session.Send(data);
        else
            socket?.Send(data);
        data.TryDispose();
    }

    /// <summary>发送消息</summary>
    /// <param name="data"></param>
    /// <param name="type"></param>
    public void Send(IPacket data, WebSocketMessageType type)
    {
        var ws = new WsMessage { Type = type };
        ws.SetBody(data);
        Send(ws);
    }

    /// <summary>发送消息</summary>
    /// <param name="data"></param>
    /// <param name="type"></param>
    public void Send(Byte[] data, WebSocketMessageType type) => Send((ArrayPacket)data, type);

    /// <summary>发送文本消息</summary>
    /// <param name="message"></param>
    public void Send(String message) => Send(message.GetBytes(), WebSocketMessageType.Text);

    /// <summary>向所有连接发送消息</summary>
    /// <param name="data"></param>
    /// <param name="type"></param>
    /// <param name="predicate"></param>
    /// <returns>已群发客户端总数</returns>
    public async Task<Int32> SendAllAsync(IPacket data, WebSocketMessageType type, Func<INetSession, Boolean>? predicate = null)
    {
        var session = (Context?.Connection) ?? throw new ObjectDisposedException(nameof(Context));
        var ws = new WsMessage { Type = type };
        ws.SetBody(data);

        var data2 = _codec.Build(ws)!;
        try
        {
            // 经服务端对各会话并行送出，等待完成后再归还封包（封包持有负载引用，释放封包即归还整链）
            return await session.Host.SendAllAsync(data2, predicate).ConfigureAwait(false);
        }
        finally
        {
            data2.TryDispose();
        }
    }

    /// <summary>向所有连接发送文本消息</summary>
    /// <param name="message"></param>
    /// <param name="predicate"></param>
    /// <returns>已群发客户端总数</returns>
    public Task<Int32> SendAllAsync(String message, Func<INetSession, Boolean>? predicate = null) => SendAllAsync((ArrayPacket)message.GetBytes(), WebSocketMessageType.Text, predicate);

    /// <summary>发送关闭连接</summary>
    /// <param name="closeStatus"></param>
    /// <param name="statusDescription"></param>
    public void Close(Int32 closeStatus, String statusDescription)
    {
        var ws = new WsMessage { Type = WebSocketMessageType.Close };
        ws.SetBody(WebSocketCodec.BuildClosePayload(closeStatus, statusDescription));
        Send(ws);
    }
    #endregion

    #region 销毁
    /// <summary>销毁。归还数据管道的段链缓冲（连接结束时调用）</summary>
    public void Dispose()
    {
        _pipe?.Dispose();
        _pipe = null;
    }
    #endregion
}