using System.Net;
using System.Security.Cryptography;
using NewLife.Data;
using NewLife.Messaging;
using NewLife.Net;

namespace NewLife.Http;

/// <summary>WebSocket消息处理</summary>
/// <param name="socket"></param>
/// <param name="message"></param>
public delegate void WebSocketDelegate(WebSocket socket, WebSocketMessage message);

/// <summary>WebSocket会话管理</summary>
/// <remarks>HTTP 服务端升级后的 WS 会话（由 HttpSession 触发），自持数据管道与帧泵（Pipe + PacketFramer）解析帧；Net 侧通用编解码器见 <see cref="NewLife.Net.Handlers.WebSocketCodec"/>。</remarks>
public class WebSocket : IDisposable
{
    #region 属性
    /// <summary>是否还在连接</summary>
    public Boolean Connected { get; set; }

    /// <summary>消息处理器</summary>
    public WebSocketDelegate? Handler { get; set; }

    /// <summary>Http上下文</summary>
    public IHttpContext? Context { get; set; }

    /// <summary>版本</summary>
    public String? Version { get; set; }

    /// <summary>协议。如mqtt</summary>
    public String? Protocol { get; set; }

    /// <summary>活跃时间</summary>
    public DateTime ActiveTime { get; set; }

    private Pipe? _pipe;

    /// <summary>帧解析原型。TryParse 只取返回值（帧长），实例状态被丢弃；共享使用无竞争</summary>
    private static readonly WebSocketMessage _parser = new();

    /// <summary>帧泵。无状态，可跨会话共享</summary>
    private static readonly PacketFramer _framer = new() { GetFrameLength = static buffer => _parser.TryParse(buffer, out _) };
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
        // 静态 Lambda + 状态重载：接收热路径零闭包分配
        _framer.Pump(_pipe.Reader, this, static (socket, frame) =>
        {
            using var message = new WebSocketMessage();
            if (message.ReadFrame(frame)) socket.Process(message);
        });
    }

    /// <summary>处理WebSocket消息</summary>
    public void Process(WebSocketMessage message)
    {
        ActiveTime = DateTime.Now;

        // 先调用 Handler，让业务层拿到净载荷（业务层可自行复制或同步消费）
        Handler?.Invoke(this, message);

        // 如果 Ping 有负载，保留引用以便后续回显 Pong
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
                    // RFC 6455 §5.5.3：Pong 必须回传 Ping 的 Application Data
                    var msg = new WebSocketMessage
                    {
                        Type = WebSocketMessageType.Pong,
                        Payload = pingPayload,
                    };
                    Send(msg);
                }
                break;
        }

        // 负载不在此释放：所有权随消息容器（帧泵回调 using / 调用方负责）；Pong 为同步发送，容器释放前负载始终有效
    }

    private void Send(WebSocketMessage msg)
    {
        var session = Context?.Connection;
        var socket = Context?.Socket;
        if (session == null && socket == null) throw new ObjectDisposedException(nameof(Context));

        var data = msg.Build();
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
        var msg = new WebSocketMessage { Type = type, Payload = data };
        Send(msg);
    }

    /// <summary>发送消息</summary>
    /// <param name="data"></param>
    /// <param name="type"></param>
    public void Send(Byte[] data, WebSocketMessageType type)
    {
        var msg = new WebSocketMessage { Type = type, Payload = (ArrayPacket)data };
        Send(msg);
    }

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
        var msg = new WebSocketMessage { Type = type, Payload = data };
        var data2 = msg.Build();
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
        var msg = new WebSocketMessage
        {
            Type = WebSocketMessageType.Close,
            CloseStatus = closeStatus,
            StatusDescription = statusDescription
        };
        Send(msg);
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