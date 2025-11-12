using NewLife.Data;
using NewLife.Log;

namespace NewLife.Http;

/// <summary>WebSocket处理器。默认实现：
/// 1. 文本消息广播给所有连接。
/// 2. 关闭/心跳帧打印日志。
/// 3. 可继承重写 <see cref="ProcessMessage"/> 实现业务协议。
/// </summary>
public class WebSocketHandler : IHttpHandler
{
    /// <summary>处理请求。若为 WebSocket 升级请求则绑定消息处理委托。</summary>
    /// <param name="context">Http 上下文</param>
    public virtual void ProcessRequest(IHttpContext context)
    {
        var ws = context.WebSocket;
        ws?.Handler = ProcessMessage;

        WriteLog("WebSocket连接 {0}", context.Connection?.Remote);
    }

    /// <summary>处理消息。可在子类中重写。</summary>
    /// <param name="socket">WebSocket 会话</param>
    /// <param name="message">消息</param>
    public virtual void ProcessMessage(WebSocket socket, WebSocketMessage message)
    {
        var remote = (socket.Context?.Connection?.Remote) ?? throw new ObjectDisposedException(nameof(socket.Context));

        var payload = message.Payload;

        switch (message.Type)
        {
            case WebSocketMessageType.Text:
                var msg = payload?.ToStr();
                WriteLog("WebSocket收到[{0}] {1}", remote, msg);
                // 群发所有客户端
                socket.SendAll($"[{remote}]说，{msg}");
                break;
            case WebSocketMessageType.Binary:
                // 示例：只记录长度。实际可根据业务解码。
                WriteLog("WebSocket收到[{0}] {1} bytes", remote, payload?.Total ?? 0);
                break;
            case WebSocketMessageType.Close:
                WriteLog("WebSocket关闭[{0}] [{1}] {2}", remote, message.CloseStatus, message.StatusDescription);
                break;
            case WebSocketMessageType.Ping:
            case WebSocketMessageType.Pong:
                WriteLog("WebSocket心跳[{0}] {1}", message.Type, payload?.ToStr());
                break;
            default:
                WriteLog("WebSocket收到[{0}] {1} bytes", message.Type, payload?.Total ?? 0);
                break;
        }
    }

    private void WriteLog(String format, params Object?[] args) => XTrace.WriteLine(format, args);
}