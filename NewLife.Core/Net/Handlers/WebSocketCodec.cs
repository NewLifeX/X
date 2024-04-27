using NewLife.Data;
using NewLife.Http;
using NewLife.Model;

namespace NewLife.Net.Handlers;

/// <summary>WebSocket消息编码器</summary>
public class WebSocketCodec : Handler
{
    /// <summary>打开连接</summary>
    /// <param name="context">上下文</param>
    public override Boolean Open(IHandlerContext context)
    {
        if (context.Owner is ISocketClient client)
        {
            // 连接必须是ws/wss协议
            if (client.Remote.Type == NetType.WebSocket && client is WebSocketClient ws)
            {
                WebSocketClient.Handshake(client, ws.Uri);
            }
        }

        return base.Open(context);
    }

    /// <summary>连接关闭时，清空粘包编码器</summary>
    /// <param name="context"></param>
    /// <param name="reason"></param>
    /// <returns></returns>
    public override Boolean Close(IHandlerContext context, String reason)
    {
        if (context.Owner is IExtend ss) ss["Codec"] = null;

        return base.Close(context, reason);
    }

    /// <summary>读取数据</summary>
    /// <param name="context"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public override Object? Read(IHandlerContext context, Object message)
    {
        if (message is not Packet pk) return base.Read(context, message);

        var msg = new WebSocketMessage();
        if (msg.Read(pk)) message = msg;

        return base.Read(context, message);
    }

    /// <summary>发送消息时，写入数据</summary>
    /// <param name="context"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public override Object? Write(IHandlerContext context, Object message)
    {
        if (message is WebSocketMessage msg)
            message = msg.ToPacket();

        return base.Write(context, message);
    }
}
