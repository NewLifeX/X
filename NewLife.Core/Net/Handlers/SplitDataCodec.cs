using NewLife.Data;
using NewLife.Messaging;
using NewLife.Model;

namespace NewLife.Net.Handlers;

/// <summary>按指定分割字节来处理粘包的处理器</summary>
/// <remarks>
/// 默认以"0x0D 0x0A"即换行来分割，分割的包包含分割字节本身，使用时请注意。
/// 使用方式：
/// <code>
/// // 默认分割方式（\r\n）
/// ISocket.Add&lt;SplitDataCodec&gt;();
///
/// // 自定义分割字节
/// ISocket.Add(new SplitDataCodec { SplitData = [0x01, 0x02] });
///
/// // 自定义最大缓存大小
/// ISocket.Add(new SplitDataCodec { MaxCacheDataLength = 2048 });
/// </code>
/// </remarks>
public class SplitDataCodec : Handler
{
    #region 属性
    /// <summary>粘包分割字节数据（默认0x0D,0x0A）</summary>
    public Byte[] SplitData { get; set; } = [0x0D, 0x0A];

    /// <summary>最大缓存待处理数据，默认1024字节</summary>
    public Int32 MaxCacheDataLength { get; set; } = 1024;
    #endregion

    /// <summary>写入数据，发送时在末尾追加分割字节</summary>
    /// <param name="context">处理器上下文</param>
    /// <param name="message">待发送的消息或数据包</param>
    /// <returns>追加分割字节后的数据包</returns>
    public override Object? Write(IHandlerContext context, Object message)
    {
        if (message is IPacket pk)
            message = pk.Append(SplitData);

        return base.Write(context, message);
    }

    /// <summary>读取数据，按分割字节拆包后逐个发送给后续处理器</summary>
    /// <param name="context">处理器上下文</param>
    /// <param name="message">接收到的数据（接收链路为轮拥有句柄的整轮 IPacket）</param>
    /// <returns>拆包后的消息已逐个分发，返回null</returns>
    public override Object? Read(IHandlerContext context, Object message)
    {
        // 管道首消息为整轮 IPacket（接收层每轮拥有句柄）
        if (message is not IPacket pk) return base.Read(context, message);

        // 解码得到多个消息
        var list = Decode(context, pk);
        if (list == null) return null;

        try
        {
            foreach (var msg in list)
            {
                // 把拆分帧转发给后续处理器（帧为独立拥有切片/链，零拷贝；同步链路内直接消费，跨轮带出请在事件内 Slice）
                base.Read(context, msg);
            }
        }
        finally
        {
            // 帧用毕归还：同步链（含事件）已消费完毕，帧句柄自身的引用由本层释放；
            // 负载如需跨轮携带，请在事件内经 Slice 切出共享切片（引用计数，用后 Dispose）；异常路径同样归还，避免池化缓冲无人释放
            foreach (var msg in list)
            {
                msg.TryDispose();
            }
        }

        return null;
    }

    /// <summary>连接关闭时，归还本会话的粘包编码器</summary>
    /// <param name="context">处理器上下文</param>
    /// <param name="reason">关闭原因</param>
    /// <returns>是否继续向下传递关闭通知</returns>
    public override Boolean Close(IHandlerContext context, String reason)
    {
        // 归还本会话粘包编码器（可能持有跨轮残片缓冲）
        if (context.Owner is IExtend ss)
        {
            ss["Codec"].TryDispose();
            ss["Codec"] = null;
        }

        return base.Close(context, reason);
    }

    #region 粘包处理
    /// <summary>解码，从数据流中拆出多个完整消息</summary>
    /// <param name="context">处理器上下文</param>
    /// <param name="pk">本轮接收数据（接收链路为轮拥有句柄的整轮 IPacket）</param>
    /// <returns>拆分后的帧列表（拥有切片/链，由调用方用毕归还）；无法解码时返回null</returns>
    protected IList<IPacket>? Decode(IHandlerContext context, IPacket pk)
    {
        if (context.Owner is not IExtend ss) return null;

        if (ss["Codec"] is not PacketCodec pc)
        {
            ss["Codec"] = pc = new PacketCodec
            {
                MaxCache = MaxCacheDataLength,
                GetLength = GetLineLength,

                // 分隔符链内扫描定界，不需要连续头部；设为 0 禁用并段拷贝，保持全链路零拷贝
                HeadSize = 0,
                Tracer = (context.Owner as ISocket)?.Tracer
            };
        }

        // 粘包拆帧：链内扫描定界，分隔符可落在任意节点或横跨节点边界（无需合并拷贝）
        var frames = pc.Parse(pk);
        if (frames.Count == 0) return null;

        // 直接交付帧句柄（拥有切片/链，零拷贝）：PacketCodec 已把帧与缓存解耦，交付后由 Read 在同步消费完成时统一归还；
        // 需要跨轮携带的数据请在事件内经 Slice 切出共享切片（引用计数，用后 Dispose）
        return frames;
    }

    /// <summary>获取包含分割字节在内的数据长度（匹配 GetLength 委托）</summary>
    /// <param name="pk">缓存链头（帧首节点链），分隔符可落在任意后续节点或横跨节点边界</param>
    /// <returns>包含分割字节在内的数据长度，未找到分割字节时返回0</returns>
    /// <remarks>用 IPacket 扩展方法 IndexOf 做跨段查找，零拷贝；返回“帧首到分隔符尾”的字节数</remarks>
    protected Int32 GetLineLength(IPacket pk)
    {
        var idx = pk.IndexOf(SplitData);
        return idx < 0 ? 0 : idx + SplitData.Length;
    }

    /// <summary>获取包含分割字节在内的数据长度（兼容重载，匹配旧版 span 委托）</summary>
    /// <param name="span">数据片段</param>
    /// <returns>包含分割字节在内的数据长度，未找到分割字节时返回0</returns>
    [Obsolete("请改用 GetLineLength(IPacket)，支持链内跨段扫描。")]
    protected Int32 GetLineLength(ReadOnlySpan<Byte> span)
    {
        var idx = span.IndexOf(SplitData);
        if (idx < 0) return 0;

        return idx + SplitData.Length;
    }
    #endregion
}