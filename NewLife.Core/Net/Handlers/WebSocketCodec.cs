using NewLife.Data;
using NewLife.Http;
using NewLife.Model;

namespace NewLife.Net.Handlers;

/// <summary>WebSocket消息编码器</summary>
public class WebSocketCodec : Handler
{
    /// <summary>用户数据包。写入时数据包转消息，读取时消息自动解包返回数据负载</summary>
    /// <remarks>一般用于上层还有其它编码器时，实现编码器级联</remarks>
    public Boolean UserPacket { get; set; }

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
        if (message is Packet pk)
        {
            var msg = new WebSocketMessage();
            if (msg.Read(pk))
            {
                if (UserPacket)
                    message = msg.Payload!;
                else
                    message = msg;
            }
        }

        return base.Read(context, message);
    }

    /// <summary>发送消息时，写入数据</summary>
    /// <param name="context"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public override Object? Write(IHandlerContext context, Object message)
    {
        if (UserPacket && message is Packet pk)
            message = new WebSocketMessage { Type = WebSocketMessageType.Binary, Payload = pk };

        if (message is WebSocketMessage msg)
            message = msg.ToPacket();

        return base.Write(context, message);
    }
}
