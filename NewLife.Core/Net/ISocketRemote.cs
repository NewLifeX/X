using System.Text;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Model;

namespace NewLife.Net;

/// <summary>远程通信Socket，仅具有收发功能</summary>
/// <remarks>
/// <para>提供基于网络连接的双向通信能力，专注于数据收发操作。</para>
/// <para>设计理念：轻量化接口，统一TCP/UDP处理模式，支持异步和事件驱动编程。</para>
/// <para>继承自 <see cref="ISocket"/> 和 <see cref="IExtend"/>，具备完整的Socket基础功能和扩展能力。</para>
/// </remarks>
public interface ISocketRemote : ISocket, IExtend
{
    #region 属性
    /// <summary>会话标识符</summary>
    /// <remarks>用于在多会话环境中唯一标识当前连接，便于日志追踪和会话管理</remarks>
    Int32 ID { get; }

    /// <summary>远程终结点地址</summary>
    /// <remarks>
    /// <para>发送操作的目标地址，同时也是接收数据的来源标识。</para>
    /// <para>对于TCP连接，该地址在建立连接后固定；对于UDP，可动态设置不同的目标。</para>
    /// </remarks>
    NetUri Remote { get; set; }

    /// <summary>最后一次通信时间</summary>
    /// <remarks>
    /// <para>记录最近一次成功收发数据的时间戳，用于会话活跃度检测。</para>
    /// <para>包括发送和接收操作，可用于实现超时断开、心跳检测等机制。</para>
    /// </remarks>
    DateTime LastTime { get; }
    #endregion

    #region 数据发送
    /// <summary>发送数据包</summary>
    /// <param name="data">要发送的数据包</param>
    /// <returns>实际发送的字节数，失败时返回负数</returns>
    /// <remarks>目标地址由 <see cref="Remote"/> 属性决定</remarks>
    Int32 Send(IPacket data);

    /// <summary>发送字节数组</summary>
    /// <param name="data">字节数组</param>
    /// <param name="offset">数据起始偏移量</param>
    /// <param name="count">发送字节数，-1表示发送从偏移量开始的所有数据</param>
    /// <returns>实际发送的字节数，失败时返回负数</returns>
    /// <remarks>目标地址由 <see cref="Remote"/> 属性决定</remarks>
    Int32 Send(Byte[] data, Int32 offset = 0, Int32 count = -1);

    /// <summary>发送数组段</summary>
    /// <param name="data">数组段</param>
    /// <returns>实际发送的字节数，失败时返回负数</returns>
    /// <remarks>目标地址由 <see cref="Remote"/> 属性决定</remarks>
    Int32 Send(ArraySegment<Byte> data);

    /// <summary>发送只读内存段</summary>
    /// <param name="data">只读内存段</param>
    /// <returns>实际发送的字节数，失败时返回负数</returns>
    /// <remarks>
    /// <para>目标地址由 <see cref="Remote"/> 属性决定。</para>
    /// <para>高性能API，避免不必要的内存拷贝，适用于.NET Core/.NET 5+环境。</para>
    /// </remarks>
    Int32 Send(ReadOnlySpan<Byte> data);
    #endregion

    #region 数据接收
    /// <summary>同步接收数据包</summary>
    /// <returns>接收到的数据包，无数据时返回null</returns>
    /// <remarks>
    /// <para>该方法会阻塞当前线程直到有数据到达或连接关闭。</para>
    /// <para>返回的数据包需要在使用完毕后正确释放，避免内存泄漏。</para>
    /// </remarks>
    IOwnerPacket? Receive();

    /// <summary>异步接收数据包</summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>接收到的数据包，无数据时返回null</returns>
    /// <remarks>
    /// <para>推荐的异步接收方式，不会阻塞调用线程。</para>
    /// <para>返回的数据包需要在使用完毕后正确释放，避免内存泄漏。</para>
    /// </remarks>
    Task<IOwnerPacket?> ReceiveAsync(CancellationToken cancellationToken = default);

    /// <summary>数据接收事件</summary>
    /// <remarks>
    /// <para>当有新数据到达时触发，适用于事件驱动编程模式。</para>
    /// <para>事件参数包含原始数据包和经过管道处理后的消息对象。</para>
    /// </remarks>
    event EventHandler<ReceivedEventArgs> Received;
    #endregion

    #region 消息处理
    /// <summary>异步发送消息并等待响应</summary>
    /// <param name="message">要发送的消息对象</param>
    /// <returns>响应消息对象</returns>
    /// <remarks>
    /// <para>高级消息通信API，支持请求-响应模式。</para>
    /// <para>消息将经过 <see cref="ISocket.Pipeline"/> 编码后发送，响应经解码后返回。</para>
    /// </remarks>
    Task<Object> SendMessageAsync(Object message);

    /// <summary>异步发送消息并等待响应</summary>
    /// <param name="message">要发送的消息对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>响应消息对象</returns>
    /// <remarks>
    /// <para>高级消息通信API，支持请求-响应模式和超时控制。</para>
    /// <para>消息将经过 <see cref="ISocket.Pipeline"/> 编码后发送，响应经解码后返回。</para>
    /// </remarks>
    Task<Object> SendMessageAsync(Object message, CancellationToken cancellationToken);

    /// <summary>发送消息，不等待响应</summary>
    /// <param name="message">要发送的消息对象</param>
    /// <returns>实际发送的字节数，失败时返回负数</returns>
    /// <remarks>
    /// <para>单向消息发送，适用于通知、推送等场景。</para>
    /// <para>消息将经过 <see cref="ISocket.Pipeline"/> 编码后发送。</para>
    /// </remarks>
    Int32 SendMessage(Object message);

    /// <summary>处理接收到的消息数据帧</summary>
    /// <param name="data">消息数据帧</param>
    /// <remarks>
    /// <para>供内部管道处理器调用，用于处理解码后的消息。</para>
    /// <para>通常由框架内部调用，应用代码一般无需直接使用。</para>
    /// </remarks>
    void Process(IData data);
    #endregion
}

/// <summary>Socket远程通信扩展方法</summary>
/// <remarks>
/// <para>提供便捷的数据发送、接收和消息处理功能。</para>
/// <para>包含字符串发送、流传输、文件传输等高级功能。</para>
/// </remarks>
public static class SocketRemoteHelper
{
    #region 扩展发送方法
    /// <summary>发送数据流</summary>
    /// <param name="session">Socket会话</param>
    /// <param name="stream">数据流</param>
    /// <param name="bufferSize"></param>
    /// <returns>实际发送的字节数</returns>
    /// <remarks>
    /// <para>以8KB缓冲区分块读取并发送流数据，适用于大文件传输。</para>
    /// <para>发送过程中如果出现错误会立即停止并返回已发送的字节数。</para>
    /// </remarks>
    public static Int32 Send(this ISocketRemote session, Stream stream, Int32 bufferSize = 8192)
    {
        var totalSent = 0;
        var buffer = Pool.Shared.Rent(bufferSize);

        try
        {
            while (true)
            {
                var bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead <= 0) break;

                var sent = session.Send(buffer, 0, bytesRead);
                if (sent < 0) break;

                totalSent += sent;
                if (bytesRead < buffer.Length) break;
            }
        }
        finally
        {
            Pool.Shared.Return(buffer);
        }

        return totalSent;
    }

    /// <summary>发送字符串</summary>
    /// <param name="session">Socket会话</param>
    /// <param name="message">要发送的字符串</param>
    /// <param name="encoding">文本编码，null表示UTF-8</param>
    /// <returns>实际发送的字节数</returns>
    public static Int32 Send(this ISocketRemote session, String message, Encoding? encoding = null)
    {
        if (String.IsNullOrEmpty(message))
            return session.Send(Pool.Empty);

        encoding ??= Encoding.UTF8;
        return session.Send(encoding.GetBytes(message));
    }
    #endregion

    #region 扩展接收方法
    /// <summary>接收字符串数据</summary>
    /// <param name="session">Socket会话</param>
    /// <param name="encoding">文本编码，null表示UTF-8</param>
    /// <returns>接收到的字符串，无数据时返回空字符串</returns>
    /// <remarks>该方法会阻塞当前线程直到有数据到达</remarks>
    public static String ReceiveString(this ISocketRemote session, Encoding? encoding = null)
    {
        using var packet = session.Receive();
        if (packet == null || packet.Length == 0) return String.Empty;

        return packet.ToStr(encoding ?? Encoding.UTF8);
    }
    #endregion

    #region 管道处理器扩展
    /// <summary>添加类型化处理器</summary>
    /// <typeparam name="THandler">处理器类型</typeparam>
    /// <param name="session">Socket会话</param>
    /// <remarks>处理器必须有无参构造函数</remarks>
    public static void Add<THandler>(this ISocket session) where THandler : IHandler, new() => GetPipeline(session).Add(new THandler());

    /// <summary>添加处理器实例</summary>
    /// <param name="session">Socket会话</param>
    /// <param name="handler">处理器实例</param>
    public static void Add(this ISocket session, IHandler handler) => GetPipeline(session).Add(handler);

    /// <summary>获取或创建消息管道</summary>
    private static IPipeline GetPipeline(ISocket session) => session.Pipeline ??= new Pipeline();
    #endregion

    #region 高级消息传输
    /// <summary>分块发送数据流为多个消息包</summary>
    /// <param name="session">Socket会话</param>
    /// <param name="stream">数据流</param>
    /// <returns>发送的消息包数量</returns>
    /// <remarks>
    /// <para>将大数据流切分为多个消息包发送，接收方可按顺序重组。</para>
    /// <para>每个消息包会添加标准4字节消息头，由StandardCodec处理器负责编解码。</para>
    /// </remarks>
    public static Int32 SendMessages(this ISocketRemote session, Stream stream)
    {
        var messageCount = 0;
        var bufferSize = SocketSetting.Current.BufferSize;
        var buffer = Pool.Shared.Rent(bufferSize);

        try
        {
            while (true)
            {
                // 预留4字节消息头空间
                var bytesRead = stream.Read(buffer, 4, bufferSize - 4);
                if (bytesRead <= 0) break;

                // StandardCodec将在头部添加4字节长度信息
                var packet = new ArrayPacket(buffer, 4, bytesRead);
                session.SendMessage(packet);
                messageCount++;
            }
        }
        finally
        {
            Pool.Shared.Return(buffer);
        }

        return messageCount;
    }

    /// <summary>以消息包形式发送文件</summary>
    /// <param name="session">Socket会话</param>
    /// <param name="filePath">文件路径</param>
    /// <param name="compressed">是否启用压缩</param>
    /// <returns>发送的消息包数量</returns>
    /// <remarks>
    /// <para>自动处理文件读取和分块传输，支持可选的压缩功能。</para>
    /// <para>接收方需要按相同顺序重组消息包以还原完整文件。</para>
    /// </remarks>
    public static Int32 SendFile(this ISocketRemote session, String filePath, Boolean compressed = false)
    {
        var messageCount = 0;
        filePath.AsFile().OpenRead(compressed, stream =>
        {
            messageCount = session.SendMessages(stream);
        });
        return messageCount;
    }
    #endregion
}
