using NewLife.Http;
using NewLife.Log;

namespace Zero.WebSocketServer;

/// <summary>WebSocket处理器</summary>
public class MyWebSocketHandler : IHttpHandler
{
    /// <summary>处理请求</summary>
    /// <param name="context"></param>
    public virtual void ProcessRequest(IHttpContext context)
    {
        var ws = context.WebSocket;
        ws.Handler = ProcessMessage;

        WriteLog("WebSocket连接 {0}", context.Connection.Remote);
    }

    /// <summary>处理消息</summary>
    /// <param name="socket"></param>
    /// <param name="message"></param>
    public virtual void ProcessMessage(WebSocket socket, WebSocketMessage message)
    {
        var remote = socket.Context.Connection.Remote;
        var msg = message.Payload?.ToStr();
        switch (message.Type)
        {
            case WebSocketMessageType.Text:
                WriteLog("WebSocket收到[{0}] {1}", message.Type, msg);
                // 群发所有客户端
                socket.SendAll($"[{remote}]说，{msg}");
                break;
            case WebSocketMessageType.Close:
                WriteLog("WebSocket关闭[{0}] [{1}] {2}", remote, message.CloseStatus, message.StatusDescription);
                break;
            case WebSocketMessageType.Ping:
            case WebSocketMessageType.Pong:
                WriteLog("WebSocket心跳[{0}] {1}", message.Type, msg);
                break;
            default:
                WriteLog("WebSocket收到[{0}] {1}", message.Type, msg);
                break;
        }
    }

    private void WriteLog(String format, params Object[] args) => XTrace.WriteLine(format, args);
}
