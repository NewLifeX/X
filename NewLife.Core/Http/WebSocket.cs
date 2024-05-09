using System.Net;
using System.Security.Cryptography;
using NewLife.Data;
using NewLife.Net;
using NewLife.Security;

namespace NewLife.Http;

/// <summary>WebSocket消息处理</summary>
/// <param name="socket"></param>
/// <param name="message"></param>
public delegate void WebSocketDelegate(WebSocket socket, WebSocketMessage message);

/// <summary>WebSocket会话管理</summary>
public class WebSocket
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

    /// <summary>活跃时间</summary>
    public DateTime ActiveTime { get; set; }
    #endregion

    #region 方法
    /// <summary>WebSocket 握手</summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static WebSocket? Handshake(IHttpContext context)
    {
        var request = context.Request;
        if (!request.Headers.TryGetValue("Sec-WebSocket-Key", out var key) || key.IsNullOrEmpty()) return null;

        var buf = SHA1.Create().ComputeHash((key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11").GetBytes());
        key = buf.ToBase64();

        var response = context.Response;
        response.StatusCode = HttpStatusCode.SwitchingProtocols;
        response.Headers["Upgrade"] = "websocket";
        response.Headers["Connection"] = "Upgrade";
        response.Headers["Sec-WebSocket-Accept"] = key;

        var manager = new WebSocket
        {
            Context = context,
            Connected = true,
            ActiveTime = DateTime.Now,
        };
        if (context is DefaultHttpContext dhc) dhc.WebSocket = manager;

        if (request.Headers.TryGetValue("Sec-WebSocket-Version", out var ver)) manager.Version = ver;

        return manager;
    }

    /// <summary>处理WebSocket数据包，不支持超大数据帧（默认8k）</summary>
    /// <param name="pk"></param>
    public void Process(Packet pk)
    {
        var message = new WebSocketMessage();
        if (message.Read(pk))
        {
            ActiveTime = DateTime.Now;

            Handler?.Invoke(this, message);

            var session = Context?.Connection;
            var socket = Context?.Socket;
            if (session == null && socket == null) return;

            switch (message.Type)
            {
                case WebSocketMessageType.Close:
                    {
                        Close(1000, "Finished");
                        session.TryDispose();
                        socket.TryDispose();
                        Connected = false;
                    }
                    break;
                case WebSocketMessageType.Ping:
                    {
                        var msg = new WebSocketMessage
                        {
                            Type = WebSocketMessageType.Pong,
                            Payload = $"Pong {DateTime.UtcNow.ToFullString()}",
                        };
                        Send(msg);
                    }
                    break;
            }
        }
    }

    private void Send(WebSocketMessage msg)
    {
        var session = Context?.Connection;
        var socket = Context?.Socket;
        if (session == null && socket == null) throw new ObjectDisposedException(nameof(Context));

        if (session != null)
            session.Send(msg.ToPacket());
        else
            socket?.Send(msg.ToPacket());
    }

    /// <summary>发送消息</summary>
    /// <param name="data"></param>
    /// <param name="type"></param>
    public void Send(Packet data, WebSocketMessageType type)
    {
        var msg = new WebSocketMessage { Type = type, Payload = data };
        Send(msg);
    }

    /// <summary>发送文本消息</summary>
    /// <param name="message"></param>
    public void Send(String message) => Send(message.GetBytes(), WebSocketMessageType.Text);

    /// <summary>向所有连接发送消息</summary>
    /// <param name="data"></param>
    /// <param name="type"></param>
    /// <param name="predicate"></param>
    public void SendAll(Packet data, WebSocketMessageType type, Func<INetSession, Boolean>? predicate = null)
    {
        var session = (Context?.Connection) ?? throw new ObjectDisposedException(nameof(Context));
        var msg = new WebSocketMessage { Type = type, Payload = data };
        session.Host.SendAllAsync(msg.ToPacket(), predicate);
    }

    /// <summary>想所有连接发送文本消息</summary>
    /// <param name="message"></param>
    /// <param name="predicate"></param>
    public void SendAll(String message, Func<INetSession, Boolean>? predicate = null) => SendAll(message.GetBytes(), WebSocketMessageType.Text, predicate);

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
}