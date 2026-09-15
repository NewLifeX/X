using NewLife.Data;
using NewLife.Http;
using NewLife.Messaging;
using NewLife.Model;
using NewLife.Security;

namespace NewLife.Net.Handlers;

/// <summary>WebSocket消息编码器</summary>
public class WebSocketCodec : Handler
{
    /// <summary>用户数据包。写入时数据包转消息，读取时消息自动解包返回数据负载</summary>
    /// <remarks>一般用于上层还有其它编码器时，实现编码器级联</remarks>
    public Boolean UserPacket { get; set; }

    /// <summary>会话键。粘包编码器按会话隔离存放，避免处理器实例跨会话共享时串包</summary>
    /// <remarks>与消息族编码器（标准/长度字段/分割）使用的 "Codec" 键区分，允许二者级联（如 WebSocket 承载标准消息）</remarks>
    private const String CodecKey = "WebSocketCodec";

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

    /// <summary>连接关闭时，归还本会话的粘包编码器</summary>
    /// <param name="context"></param>
    /// <param name="reason"></param>
    /// <returns></returns>
    public override Boolean Close(IHandlerContext context, String reason)
    {
        // 归还本会话粘包编码器（可能持有跨轮残片缓冲）
        if (context.Owner is IExtend ss)
        {
            ss[CodecKey].TryDispose();
            ss[CodecKey] = null;
        }

        return base.Close(context, reason);
    }

    /// <summary>读取数据，通过 PacketCodec 缓冲不完整帧，支持跨 TCP 接收边界的粘包/分包场景</summary>
    /// <param name="context"></param>
    /// <param name="message">接收到的数据（接收链路为轮拥有句柄的整轮 IPacket）</param>
    /// <returns></returns>
    public override Object? Read(IHandlerContext context, Object message)
    {
        // 管道首消息为整轮 IPacket（接收层每轮拥有句柄）
        if (message is not IPacket pk)
            return base.Read(context, message);

        // 粘包拆帧后解析（接收链路帧为独立拥有切片/链，直接调用可为视图；负载经共享切片独立持有，帧句柄由本层归还）
        // 处理器实例会被多会话共享（服务器创建会话时直接引用服务器管道），因此粘包编码器必须按会话隔离存放
        if (context.Owner is not IExtend ss) return base.Read(context, message);

        if (ss[CodecKey] is not PacketCodec codec)
        {
            // 帧首头部保证：WebSocket 头部与掩码最多 14 字节（2 基础头 + 8 扩展长度 + 4 掩码）
            ss[CodecKey] = codec = new PacketCodec { GetLength = WebSocketMessage.GetFrameTotalLength, HeadSize = 14 };
        }
        var frames = codec.Parse(pk);
        foreach (var frame in frames)
        {
            var msg = new WebSocketMessage();
            if (!msg.Read(frame))
            {
                // 分片帧/数据不完整：归还帧与消息（帧可能拥有池化缓冲）
                frame.TryDispose();
                msg.Dispose();
                continue;
            }

            // 帧用毕归还：负载为共享切片（引用计数）独立持有，帧句柄自身的引用由本层释放
            frame.TryDispose();

            if (UserPacket)
            {
                var payload = msg.Payload;
                msg.Payload = null;
                msg.Dispose();
                base.Read(context, payload!);
            }
            else
            {
                base.Read(context, msg);
            }
        }
        return null;
    }

    /// <summary>发送消息时，写入数据</summary>
    /// <param name="context"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public override Object? Write(IHandlerContext context, Object message)
    {
        if (UserPacket && message is IPacket pk)
            message = new WebSocketMessage { Type = WebSocketMessageType.Binary, Payload = pk };

        // RFC 6455 §5.3：客户端发出的帧必须带掩码（服务端禁止掩码），且每帧使用新的随机掩码键；已显式设置掩码的不覆盖
        if (message is WebSocketMessage wsm && wsm.MaskKey == null && context.Owner is WebSocketClient)
            wsm.MaskKey = Rand.NextBytes(4);

        // 谁申请，谁归还
        IPacket? owner = null;
        if (message is WebSocketMessage msg)
            message = owner = msg.ToPacket();

        try
        {
            return base.Write(context, message);
        }
        finally
        {
            // 下游可能忘了释放内存，这里兜底释放
            owner.TryDispose();
        }
    }
}
